using Microsoft.AspNetCore.Mvc;
using WedBanSach.Data;
using WedBanSach.Helpers;
using WedBanSach.ViewModels;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Models;

namespace WedBanSach.Controllers;

public class CartController : Controller
{
    private readonly BookStoreDbContext _context;
    private readonly WedBanSach.Services.EmailService _emailService;
    private const string CART_KEY = "Cart";

    public CartController(BookStoreDbContext context, WedBanSach.Services.EmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    [Route("gio-hang")]
    public async Task<IActionResult> Index()
    {
        // Cleanup Abandoned Orders (Pending Payment but user navigated away)
        var pendingOrderIdStr = HttpContext.Session.GetString("PendingOrderId");
        if (!string.IsNullOrEmpty(pendingOrderIdStr) && int.TryParse(pendingOrderIdStr, out int pendingId))
        {
            var pendingOrder = await _context.Orders.FindAsync(pendingId);
            // Only delete if PaymentStatus is NOT Paid (safety check)
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderID == pendingId);
            
            if (pendingOrder != null && (payment == null || payment.PaymentStatus != "Paid"))
            {
                // Delete Related Data first (Fix FK Constraints)
                var relatedDetails = _context.OrderDetails.Where(od => od.OrderID == pendingId);
                _context.OrderDetails.RemoveRange(relatedDetails);
                
                var relatedShippings = _context.Shippings.Where(s => s.OrderID == pendingId);
                _context.Shippings.RemoveRange(relatedShippings);

                var relatedPayments = _context.Payments.Where(p => p.OrderID == pendingId);
                _context.Payments.RemoveRange(relatedPayments);

                var relatedNotifications = _context.Notifications.Where(n => n.Message.Contains($"#{pendingId}"));
                _context.Notifications.RemoveRange(relatedNotifications);

                // Finally Delete Order
                _context.Orders.Remove(pendingOrder);
                await _context.SaveChangesAsync();
            }
            HttpContext.Session.Remove("PendingOrderId");
        }

        var cart = GetCartItems();
        
        // Fetch available promotions (AdminPromotions)
        var promotions = await _context.Promotions
            .Where(p => (p.StartDate == null || p.StartDate <= DateTime.Now) && 
                        (p.EndDate == null || p.EndDate >= DateTime.Now))
            .ToListAsync();
            
        ViewBag.Promotions = promotions;

        // Fetch standard shipping fee
        var standardShipping = await _context.ShippingMethods
            .FirstOrDefaultAsync(s => s.Name.Contains("tiêu chuẩn") || s.Name.Contains("Standard"));
        ViewBag.StandardShippingFee = standardShipping?.Price ?? 0;

        return View(cart);
    }

    [HttpPost]
    public async Task<IActionResult> SetShippingAddress(string provinceCode, string provinceName, string districtCode, string districtName, string wardCode, string wardName, string? houseNumber)
    {
        HttpContext.Session.SetString("Shipping_ProvinceCode", provinceCode ?? "");
        HttpContext.Session.SetString("Shipping_ProvinceName", provinceName ?? "");
        HttpContext.Session.SetString("Shipping_DistrictCode", districtCode ?? "");
        HttpContext.Session.SetString("Shipping_DistrictName", districtName ?? "");
        HttpContext.Session.SetString("Shipping_WardCode", wardCode ?? "");
        HttpContext.Session.SetString("Shipping_WardName", wardName ?? "");
        HttpContext.Session.SetString("Shipping_HouseNumber", houseNumber ?? "");

        // Persist to database if logged in
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (int.TryParse(userIdStr, out int userId))
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.ProvinceCode = provinceCode;
                user.ProvinceName = provinceName;
                user.DistrictCode = districtCode;
                user.DistrictName = districtName;
                user.WardCode = wardCode;
                user.WardName = wardName;
                user.HouseNumber = houseNumber;
                await _context.SaveChangesAsync();
            }
        }
        
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> ApplyCoupon(string code)
    {
        var cart = GetCartItems();
        
        // Find in Promotions (AdminPromotions) - matching Name as Code
        var promotion = await _context.Promotions
            .FirstOrDefaultAsync(p => p.PromotionName == code && 
                                     (p.StartDate == null || p.StartDate <= DateTime.Now) && 
                                     (p.EndDate == null || p.EndDate >= DateTime.Now));

        if (promotion != null)
        {
            cart.CouponCode = promotion.PromotionName;
            
            // Handle Discount logic
            // Assuming DiscountValue is the amount. If DiscountType is "Percent", calculate it.
            if (promotion.DiscountType == "Percent" && promotion.DiscountValue.HasValue)
            {
                 cart.DiscountAmount = cart.TotalAmount * (promotion.DiscountValue.Value / 100);
            }
            else
            {
                cart.DiscountAmount = promotion.DiscountValue ?? 0;
            }

            TempData["SuccessMessage"] = "Áp dụng mã khuyến mãi thành công!";
        }
        else
        {
            TempData["ErrorMessage"] = "Mã khuyến mãi không hợp lệ hoặc đã hết hạn!";
        }

        SaveCartSession(cart);
        
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Query["ajax"] == "true")
        {
             return Json(new { success = true, discount = cart.DiscountAmount, finalTotal = cart.FinalTotal });
        }
        
        return RedirectToAction("Index");
    }

    public IActionResult RemoveCoupon()
    {
        var cart = GetCartItems();
        cart.CouponCode = null;
        cart.DiscountAmount = 0;
        SaveCartSession(cart);
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> AddToCart(int id, int quantity = 1)
    {
        var cart = GetCartItems();
        var cartItem = cart.Items.FirstOrDefault(c => c.BookID == id);

        if (cartItem != null)
        {
            cartItem.Quantity += quantity;
        }
        else
        {
            var book = await _context.Books.Include(b => b.BookImages).FirstOrDefaultAsync(b => b.BookID == id);
            if (book == null) return NotFound();

            cart.Items.Add(new CartItemViewModel
            {
                BookID = book.BookID,
                Title = book.Title,
                Price = book.Price,
                DiscountPrice = book.DiscountPrice,
                ImageUrl = book.BookImages?.FirstOrDefault(i => i.IsMain)?.ImageUrl ?? "/images/default-book.png",
                Quantity = quantity
            });
        }

        SaveCartSession(cart);
        
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Query["ajax"] == "true")
        {
            return Json(new { cartCount = cart.TotalQuantity, message = "Đã thêm vào giỏ hàng" });
        }

        // Check if "Buy Now" triggered this or standard Add to Cart
        if (Request.Form.ContainsKey("buyNow") && Request.Form["buyNow"] == "true")
        {
            return RedirectToAction("Index");
        }

        return Redirect(Request.Headers["Referer"].ToString());
    }

    [HttpGet]
    [Route("mua-ngay/{id}")]
    public async Task<IActionResult> BuyNow(int id)
    {
        // Add item to cart
        var cart = GetCartItems();
        var cartItem = cart.Items.FirstOrDefault(c => c.BookID == id);

        if (cartItem != null)
        {
            cartItem.Quantity += 1;
        }
        else
        {
            var book = await _context.Books.Include(b => b.BookImages).FirstOrDefaultAsync(b => b.BookID == id);
            if (book == null) return NotFound();

            cart.Items.Add(new CartItemViewModel
            {
                BookID = book.BookID,
                Title = book.Title,
                Price = book.Price,
                DiscountPrice = book.DiscountPrice,
                ImageUrl = book.BookImages?.FirstOrDefault(i => i.IsMain)?.ImageUrl ?? "/images/default-book.png",
                Quantity = 1
            });
        }

        SaveCartSession(cart);
        
        // Redirect to cart page
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult UpdateQuantity(int id, int quantity)
    {
        var cart = GetCartItems();
        var item = cart.Items.FirstOrDefault(i => i.BookID == id);
        if (item != null)
        {
            item.Quantity = quantity;
            if (item.Quantity <= 0) cart.Items.Remove(item);
        }
        SaveCartSession(cart);
        return RedirectToAction("Index");
    }

    public IActionResult Remove(int id)
    {
        var cart = GetCartItems();
        var item = cart.Items.FirstOrDefault(i => i.BookID == id);
        if (item != null)
        {
            cart.Items.Remove(item);
        }
        SaveCartSession(cart);
        return RedirectToAction("Index");
    }

    [Route("thanh-toan")]
    public async Task<IActionResult> Checkout()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr))
        {
            return RedirectToAction("Login", "Account", new { returnUrl = "/Cart" });
        }

        var cart = GetCartItems();
        if (cart.Items.Count == 0)
        {
            return RedirectToAction("Index");
        }

        // Fetch user info for pre-filling
        int userId = int.Parse(userIdStr);
        var user = await _context.Users.FindAsync(userId);
        ViewBag.User = user;

        // Fetch user addresses
        var addresses = await _context.UserAddresses
            .Where(a => a.UserID == userId)
            .OrderByDescending(a => a.IsDefault)
            .ToListAsync();
        ViewBag.UserAddresses = addresses;

        // Fetch address info from session, fallback to Default Address or User DB
        var defaultAddress = addresses.FirstOrDefault(a => a.IsDefault) ?? addresses.FirstOrDefault();
        
        string pCode, pName, dCode, dName, wCode, wName, hNum;

        if (defaultAddress != null)
        {
            pCode = defaultAddress.ProvinceCode;
            pName = defaultAddress.ProvinceName;
            dCode = defaultAddress.DistrictCode;
            dName = defaultAddress.DistrictName;
            wCode = defaultAddress.WardCode;
            wName = defaultAddress.WardName;
            hNum = defaultAddress.AddressDetail;
        }
        else
        {
             pCode = HttpContext.Session.GetString("Shipping_ProvinceCode") ?? user?.ProvinceCode;
             pName = HttpContext.Session.GetString("Shipping_ProvinceName") ?? user?.ProvinceName;
             dCode = HttpContext.Session.GetString("Shipping_DistrictCode") ?? user?.DistrictCode;
             dName = HttpContext.Session.GetString("Shipping_DistrictName") ?? user?.DistrictName;
             wCode = HttpContext.Session.GetString("Shipping_WardCode") ?? user?.WardCode;
             wName = HttpContext.Session.GetString("Shipping_WardName") ?? user?.WardName;
             hNum = HttpContext.Session.GetString("Shipping_HouseNumber") ?? user?.HouseNumber;
        }

        ViewBag.ProvinceCode = pCode;
        ViewBag.ProvinceName = pName;
        ViewBag.DistrictCode = dCode;
        ViewBag.DistrictName = dName;
        ViewBag.WardCode = wCode;
        ViewBag.WardName = wName;
        ViewBag.HouseNumber = hNum;

        // Fetch promotions for display if needed
        var promotions = await _context.Promotions
            .Where(p => (p.StartDate == null || p.StartDate <= DateTime.Now) && 
                        (p.EndDate == null || p.EndDate >= DateTime.Now))
            .ToListAsync();
        ViewBag.Promotions = promotions;

        // Fetch shipping methods
        ViewBag.ShippingMethods = await _context.ShippingMethods.ToListAsync();

        // Fetch active payment settings
        ViewBag.PaymentSettings = await _context.PaymentSettings
            .Where(ps => ps.IsEnabled)
            .ToListAsync();

        return View(cart);
    }


    [HttpPost]
    public async Task<IActionResult> PlaceOrder(string shippingAddress, string paymentMethod, int shippingMethodId, int? addressId)
    {
        var userIdString = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
        {
            return RedirectToAction("Login", "Account", new { returnUrl = "/Cart" });
        }

        var cart = GetCartItems();
        if (cart.Items.Count == 0) return RedirectToAction("Index");
        
        // Handle Address Selection
        string finalShippingAddress = shippingAddress;
        if (addressId.HasValue && addressId.Value > 0)
        {
            var selectedAddress = await _context.UserAddresses.FindAsync(addressId.Value);
            if (selectedAddress != null)
            {
                // Construct full address string
                // Format: Name - Phone - Full Address
               finalShippingAddress = $"{selectedAddress.ReceiverName} | {selectedAddress.Phone} | {selectedAddress.AddressDetail}, {selectedAddress.WardName}, {selectedAddress.DistrictName}, {selectedAddress.ProvinceName}";
               
               // Optional: Update last used address components in session if needed, but not strictly required
            }
        }

        // Fetch Shipping Method
        var shippingMethod = await _context.ShippingMethods.FindAsync(shippingMethodId);
        decimal shippingFee = shippingMethod?.Price ?? 0;
        string shippingMethodName = shippingMethod?.Name ?? "Standard Shipping";

        // Create Order
        var order = new Order
        {
            UserID = userId,
            OrderDate = DateTime.Now,
            TotalAmount = cart.FinalTotal + shippingFee,
            OrderStatus = "Pending",
            PaymentMethod = paymentMethod,
            ShippingAddress = finalShippingAddress,
            ShippingMethodName = shippingMethodName,
            ShippingFee = shippingFee
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(); // Save to get OrderID

        // Create Payment record
        _context.Payments.Add(new Payment
        {
            OrderID = order.OrderID,
            PaymentDate = DateTime.Now,
            Amount = order.TotalAmount,
            PaymentStatus = "Pending"
        });
        await _context.SaveChangesAsync();

        // Initial Shipping record
        var shipping = new Shipping
        {
            OrderID = order.OrderID,
            ShippingCompany = shippingMethodName,
            ShippingStatus = "Pending"
        };
        _context.Shippings.Add(shipping);
        await _context.SaveChangesAsync();

        // Create OrderDetails
        foreach (var item in cart.Items)
        {
            var orderDetail = new OrderDetail
            {
                OrderID = order.OrderID,
                BookID = item.BookID,
                Quantity = item.Quantity,
                UnitPrice = item.CurrentPrice
            };
            _context.OrderDetails.Add(orderDetail);
        }

        await _context.SaveChangesAsync();

        // Send Notification to user - MOVED TO SUCCESS/PAYMENT CONFIRMATION
        // Optimization: Do not notify success immediately upon placing order if payment is pending
        if (paymentMethod != "Bank Transfer" && paymentMethod != "Chuyển khoản")
        {
             var notification = new Notification
            {
                UserID = userId,
                Title = "Đặt hàng thành công",
                Message = $"Đơn hàng #{order.OrderID} của bạn đã được đặt thành công.",
                Type = "Order",
                RedirectUrl = $"/Order/Details/{order.OrderID}",
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        if (paymentMethod == "Bank Transfer" || paymentMethod == "Chuyển khoản")
        {
            // Mark this order as pending/temporary in session
            HttpContext.Session.SetString("PendingOrderId", order.OrderID.ToString());
            return RedirectToAction("Payment", new { orderId = order.OrderID });
        }

        return RedirectToAction("OrderSuccess", new { id = order.OrderID });
    }

    public async Task<IActionResult> Payment(int orderId)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr))
        {
            return RedirectToAction("Login", "Account", new { returnUrl = $"/Cart/Payment?orderId={orderId}" });
        }

        var order = await _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Book)
                    .ThenInclude(b => b.BookImages)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.OrderID == orderId);

        if (order == null) return NotFound();

        // Get Bank Transfer settings
        // Assuming there is a PaymentSetting for Bank Transfer
        var bankSetting = await _context.PaymentSettings
            .FirstOrDefaultAsync(p => p.MethodName == "Bank Transfer" || p.MethodName == "BankTransfer" || p.MethodName == "Chuyển khoản ngân hàng" || p.MethodName == "Chuyển khoản");

        if (bankSetting != null && bankSetting.IsEnabled)
        {
            ViewBag.BankName = bankSetting.BankName;
            ViewBag.AccountNumber = bankSetting.AccountNumber;
            ViewBag.AccountHolder = bankSetting.AccountHolder;
            ViewBag.BankCode = bankSetting.BankCode; // For QR generation
            ViewBag.Description = bankSetting.Description;
        }

        return View(order);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken] // Required for external webhooks
    public async Task<IActionResult> VerifyPaymentWebhook([FromBody] WebhookPayload payload)
    {
        if (payload == null) return BadRequest("Invalid payload");

        // SePay V2 sends order_invoice_number
        if (int.TryParse(payload.OrderInvoiceNumber, out int orderId))
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderID == orderId && p.Amount <= payload.Amount);

            if (payment != null && payment.PaymentStatus != "Paid")
            {
                payment.PaymentStatus = "Paid";
                payment.PaymentDate = DateTime.Now;
                await _context.SaveChangesAsync();
                return Ok(new { success = true, orderId });
            }
        }

        return Ok(new { success = false, message = "Order not found or already processed" });
    }

    public class WebhookPayload
    {
        public long Id { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonPropertyName("amount")]
        public decimal Amount { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("order_invoice_number")]
        public string OrderInvoiceNumber { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonPropertyName("transaction_id")]
        public string TransactionId { get; set; } = string.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> SimulatePaymentSuccess(int orderId)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.OrderID == orderId);
        
        if (payment == null) return NotFound();

        payment.PaymentStatus = "Paid";
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> CheckPaymentStatus(int orderId)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.OrderID == orderId);
        
        if (payment == null) return NotFound();

        return Json(new { 
            isPaid = payment.PaymentStatus == "Paid",
            status = payment.PaymentStatus 
        });
    }

    [Route("dat-hang-thanh-cong/{id}")]
    public async Task<IActionResult> OrderSuccess(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Book)
                .ThenInclude(b => b.BookImages)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.OrderID == id);

        if (order == null) return NotFound();

        // Check if notification already exists to avoid duplicates on refresh
        // We use this as a flag for "First Time Success View" -> Trigger Email
        var exists = await _context.Notifications.AnyAsync(n => n.Title == "Đặt hàng thành công" && n.Message.Contains($"#{id}"));
        if (!exists)
        {
            var notification = new Notification
            {
                UserID = order.UserID,
                Title = "Đặt hàng thành công",
                Message = $"Đơn hàng #{order.OrderID} của bạn đã được xác nhận thành công!",
                Type = "Success",
                RedirectUrl = $"/Order/Details/{order.OrderID}",
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Email removed from here. Triggered only in AdminOrdersController when status -> Confirmed
        }

        // Clear Cart ONLY when order is confirmed/viewed as success
        HttpContext.Session.Remove(CART_KEY);
        
        // Clear Pending Order Flag (Order is now safe/confirmed)
        HttpContext.Session.Remove("PendingOrderId");

        // Fetch bank info for display if needed
        ViewBag.BankInfo = await _context.PaymentSettings
            .FirstOrDefaultAsync(ps => (ps.MethodName == "Bank Transfer" || ps.MethodName == "Chuyển khoản") && ps.IsEnabled);

        return View(order);
    }

    // Helpers
    private CartViewModel GetCartItems()
    {
        var cart = HttpContext.Session.GetObjectFromJson<CartViewModel>(CART_KEY) ?? new CartViewModel();
        return cart;
    }

    private void SaveCartSession(CartViewModel cart)
    {
        HttpContext.Session.SetObjectAsJson(CART_KEY, cart);
    }
}
