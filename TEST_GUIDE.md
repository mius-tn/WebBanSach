# Hướng Dẫn Test Hệ Thống WebBanSach

Tài liệu này hướng dẫn các bước kiểm thử chức năng dành cho Admin và User, bao gồm việc khởi tạo tài khoản quản trị viên mặc định.

## 0. KHỞI TẠO HỆ THỐNG (QUAN TRỌNG)

Trước khi bắt đầu test, bạn cần khởi tạo Roles và tài khoản Admin mặc định.

1.  Đảm bảo Database đã được update (`dotnet ef database update`).
2.  Chạy dự án lên.
3.  Truy cập vào đường dẫn: `/Account/InitializeAdmin`
    *   Ví dụ: `https://localhost:7054/Account/InitializeAdmin` (Khuyến nghị sử dụng **Microsoft Edge** để trải nghiệm tốt nhất).
4.  Nhấn nút **"Khởi tạo Admin"**. Hệ thống sẽ tạo tài khoản mặc định:

> [!IMPORTANT]
> **Tài khoản Admin Mặc Định:**
> - **Email:** `admin@bookstore.com`
> - **Password:** `Admin@123`

---

## 1. Hướng dẫn Test cho User (Khách hàng)

### 1.1. Đăng ký & Đăng nhập
1.  **Đăng ký tài khoản mới:**
    *   Vào trang Đăng ký.
    *   Điền Email, Họ tên, SĐT, Mật khẩu.
    *   Nhấn Đăng ký -> Kiểm tra thông báo thành công.
2.  **Đăng nhập:**
    *   Dùng tài khoản vừa tạo để đăng nhập.
    *   Thử nhập sai mật khẩu để check lỗi "Sai thông tin".
3.  **Quản lý thông tin (Profile):**
    *   Click vào tên/avatar ở góc phải -> Chọn "Thông tin cá nhân".
    *   Thử đổi tên, số điện thoại, cập nhật Avatar.
    *   Đổi mật khẩu và thử đăng nhập lại.

### 1.2. Mua hàng (Luồng chính)
1.  **Duyệt và tìm kiếm:**
    *   Ở trang chủ, thử search "Tiếng Việt" hoặc tên sách bất kỳ.
    *   Click vào thử một danh mục (ví dụ: "Tiểu thuyết").
2.  **Thêm vào giỏ:**
    *   Nhấn nút "Thêm vào giỏ" (icon giỏ hàng) ở sản phẩm bất kỳ.
    *   Để ý hiệu ứng bay vào giỏ hàng ở góc phải.
3.  **Giỏ hàng (Cart):**
    *   Vào xem giỏ hàng.
    *   Tăng/giảm số lượng sách.
    *   Xóa thử một cuốn sách khỏi giỏ.
    *   **Test Voucher:** Nếu có mã giảm giá (Admin tạo), thử áp dụng để xem giá giảm.
4.  **Thanh toán (Checkout):**
    *   Nhấn "Tiến hành thanh toán".
    *   Nhập/Chọn địa chỉ giao hàng.
    *   Chọn phương thức: COD (Giao hàng nhận tiền) hoặc Chuyển khoản (VietQR).
    *   Nhấn "Đặt hàng".
    *   Màn hình "Đặt hàng thành công" hiện ra -> Kiểm tra email (nếu có config mail).

### 1.3. Lịch sử đơn hàng
*   Vào "Lịch sử mua hàng".
*   Đơn vừa đặt sẽ có trạng thái "Chờ xử lý" (Pending).
*   Bấm "Chi tiết" để xem lại các sách đã mua.

---

## 2. Hướng dẫn Test cho Admin

**Đăng nhập:** Truy cập `/Login` và dùng tài khoản Admin mặc định (`admin@bookstore.com` / `Admin@123`).
Sau khi đăng nhập, bạn sẽ được chuyển hướng tới trang Dashboard (`/Admin`).

### 2.1. Quản lý Sản phẩm (Sách)
1.  Vào menu **Sách (Books)**.
2.  **Thêm sách mới:**
    *   Nhấn "Thêm sách mới" (Create).
    *   Điền đầy đủ: Tên, Tác giả, Giá, Số lượng tồn kho, Mô tả.
    *   Upload ảnh bìa sách (Nên thực hiện trên **Microsoft Edge** nếu gặp vấn đề với trình duyệt khác).
    *   Nhấn Lưu -> Kiểm tra sách mới xuất hiện ở danh sách và trang chủ User.
3.  **Sửa sách:**
    *   Chọn một cuốn sách -> Nhấn Edit.
    *   Thử giảm giá sách hoặc đổi tên.
4.  **Ẩn/Xóa sách:** Thử xóa một cuốn sách (Lưu ý: sách đã có đơn hàng thường sẽ chỉ bị ẩn đi chứ không xóa cứng để bảo toàn dữ liệu).

### 2.2. Xử lý Đơn hàng (Quy trình duyệt đơn)
1.  Vào menu **Đơn hàng (Orders)**.
2.  Tìm đơn hàng "Chờ xử lý" (Pending) mà User vừa đặt.
3.  Click vào chi tiết đơn hàng (Details/Edit).
4.  **Duyệt đơn:** Đổi trạng thái từ `Pending` -> `Confirmed` (Đã duyệt).
5.  **Giao hàng:** Đổi tiếp sang `Shipping` (Đang giao).
6.  **Hoàn tất:** Khi giao xong, đổi sang `Completed` (Hoàn thành) -> Lúc này User vào lịch sử đơn hàng sẽ thấy trạng thái cập nhật theo.

### 2.3. Cài đặt hệ thống khác
*   **Quản lý User:** Xem danh sách người dùng, có thể khóa tài khoản spam.
*   **Categories/Authors:** Thử thêm một danh mục sách mới (ví dụ: "Sách Khoa học").
*   **Promotions:** Tạo mã giảm giá (Voucher) mới (Ví dụ code: `SALE50`, giảm 50%).

## 3. Lưu ý Fix lỗi (Troubleshooting)

*   **Lỗi không đăng nhập được Admin:** Kiểm tra lại Database xem bảng `UserRoles` đã có liên kết User với Role `Admin` chưa (Nếu chạy `InitializeAdmin` thì chắc chắn có).
*   **Lỗi ảnh không hiện:** Kiểm tra thư mục `wwwroot/uploads` hoặc `wwwroot/images` có tồn tại ảnh không.
*   **Lỗi gửi mail:** Kiểm tra `appsettings.json` phần `EmailSettings` (Mật khẩu ứng dụng Gmail có đúng không).
