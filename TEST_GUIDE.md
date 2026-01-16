# Hướng Dẫn Test Hệ Thống WebBanSach

Tài liệu này hướng dẫn các bước kiểm thử chức năng dành cho Admin và User.

## 1. Hướng dẫn Test cho User (Khách hàng)

### 1.1. Đăng ký & Đăng nhập
- **Đăng ký:** Truy cập trang đăng ký, điền thông tin và xác nhận. Kiểm tra validation (email trùng, mật khẩu yếu).
- **Đăng nhập:** Đăng nhập với tài khoản vừa tạo. Thử đăng nhập sai để kiểm tra thông báo lỗi.
- **Profile:** Vào trang cá nhân cập nhật thông tin, thay đổi mật khẩu.

### 1.2. Tìm kiếm & Duyệt sản phẩm
- **Trang chủ:** Kiểm tra hiển thị danh sách sách mới, sách bán chạy.
- **Tìm kiếm:** Nhập từ khóa vào thanh tìm kiếm (tên sách, tác giả). Đảm bảo gợi ý tìm kiếm hoạt động.
- **Danh mục:** Click vào danh mục để lọc sách theo thể loại.

### 1.3. Giỏ hàng & Đặt hàng
- **Thêm vào giỏ:** Click "Thêm vào giỏ" ở trang chủ hoặc chi tiết sách. Kiểm tra hiệu ứng bay vào giỏ hàng.
- **Xem giỏ hàng:** Kiểm tra danh sách sản phẩm, cập nhật số lượng, xóa sản phẩm.
- **Mã giảm giá:** Nhập mã giảm giá (nếu có) và kiểm tra tổng tiền thay đổi.
- **Thanh toán:** Tới trang thanh toán, điền thông tin giao hàng, chọn phương thức thanh toán (COD hoặc chuyển khoản). Nhấn "Đặt hàng".

### 1.4. Lịch sử đơn hàng
- Vào trang "Lịch sử mua hàng" để xem danh sách đơn hàng đã đặt.
- Kiểm tra trạng thái đơn hàng (Chờ xử lý, Đang giao, v.v.).

---

## 2. Hướng dẫn Test cho Admin

**Truy cập:** Đường dẫn `/Admin` (thường yêu cầu đăng nhập tài khoản có quyền Admin).

### 2.1. Quản lý Sách (Books)
- **Danh sách:** Kiểm tra hiển thị danh sách sách, phân trang, lọc/tìm kiếm.
- **Thêm mới:** Thêm sách mới với đầy đủ thông tin (Tên, Tác giả, NXB, Giá, Hình ảnh).
- **Sửa:** Thay đổi thông tin sách, cập nhật hình ảnh.
- **Xóa:** Xóa sách (hoặc ẩn sách).

### 2.2. Quản lý Danh mục & Tác giả & NXB
- Kiểm tra CRUD (Thêm/Sửa/Xóa) cho:
    - Danh mục (Categories)
    - Tác giả (Authors)
    - Nhà xuất bản (Publishers)

### 2.3. Quản lý Đơn hàng (Orders)
- Xem danh sách đơn hàng mới.
- **Cập nhật trạng thái:** Chuyển trạng thái đơn từ "Pending" -> "Confirmed" -> "Shipping" -> "Completed".
- **Chi tiết đơn hàng:** Xem chi tiết sản phẩm khách đặt.

### 2.4. Quản lý Khuyến mãi (Promotions)
- Tạo mã giảm giá mới (theo % hoặc số tiền cố định).
- Thiết lập ngày bắt đầu/kết thúc.

### 2.5. Báo cáo & Thống kê
- Kiểm tra trang Dashboard xem biểu đồ doanh thu, số lượng đơn hàng.

---

## Lưu ý khi Test
- Đảm bảo database đã được cập nhật (`dotnet ef database update`).
- Kiểm tra log trong console nếu có lỗi server (500).
- Với tính năng gửi mail, hãy chắc chắn `EmailSettings` trong `appsettings.json` là chính xác.
