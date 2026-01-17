# WebBanSach

Dự án website bán sách trực tuyến được xây dựng bằng ASP.NET Core MVC (.NET 9.0).

## Yêu cầu hệ thống

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server

## Cài đặt và Chạy dự án

1.  **Clone repository:**
    ```bash
    git clone https://github.com/mius-tn/WebBanSach.git
    cd WebBanSach
    ```

2.  **Cấu hình Database:**
    - Mở file `appsettings.json`.
    - Cập nhật `ConnectionStrings:DefaultConnection` để trỏ đến SQL Server của bạn.
    
    ```json
    "DefaultConnection": "Server=YOUR_SERVER;Database=BookStoreDB;Trusted_Connection=True;TrustServerCertificate=True;"
    ```

3.  **Cấu hình Email (Tùy chọn):**
    - Cập nhật phần `EmailSettings` trong `appsettings.json` nếu bạn muốn test tính năng gửi mail xác nhận đơn hàng/quên mật khẩu.

4.  **Chạy Migrations:**
    Mở terminal tại thư mục gốc của project (nơi chứa file `.sln` hoặc file `.csproj`) và chạy lệnh:
    ```bash
    dotnet ef database update --project WedBanSach
    ```
    *Lưu ý: Nếu bạn chưa cài `dotnet-ef`, hãy cài đặt bằng lệnh: `dotnet tool install --global dotnet-ef`*

5.  **Chạy ứng dụng:**
    ```bash
    dotnet run --project WedBanSach
    ```
    Hoặc mở bằng Visual Studio và nhấn F5.

6.  **Truy cập:**
    - Mở trình duyệt **Microsoft Edge** và truy cập: `https://localhost:7054` (hoặc port được hiển thị trong terminal).

## Cấu trúc thư mục

- `Controllers/`: Chứa các controller xử lý logic (Admin và User).
- `Views/`: Giao diện người dùng (Razor Views).
- `Models/`: Các Entity và ViewModel.
- `wwwroot/`: File tĩnh (CSS, JS, hình ảnh).

## Công nghệ sử dụng

- ASP.NET Core MVC 9.0
- Entity Framework Core 9.0 (SQL Server)
- Identity (Quản lý User/Role)
- Bootstrap 5
