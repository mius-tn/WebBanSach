namespace WedBanSach.ViewModels;

public class CartItemViewModel
{
    public int BookID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int Quantity { get; set; }
    
    public decimal CurrentPrice => DiscountPrice ?? Price;
    public decimal Total => CurrentPrice * Quantity;
}

public class CartViewModel
{
    public List<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();
    public decimal TotalAmount => Items.Sum(i => i.Total);
    public int TotalQuantity => Items.Sum(i => i.Quantity);

    // Coupon info
    public string? CouponCode { get; set; }
    public decimal DiscountAmount { get; set; }
    
    public decimal FinalTotal => TotalAmount - DiscountAmount > 0 ? TotalAmount - DiscountAmount : 0;
}
