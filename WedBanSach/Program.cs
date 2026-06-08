using Microsoft.EntityFrameworkCore;
using WedBanSach.Data;

namespace WedBanSach
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();
            builder.Services.AddSignalR();
            builder.Services.AddHttpContextAccessor();

            // Add DbContext
            builder.Services.AddDbContext<BookStoreDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Add Session for authentication
            builder.Services.AddControllersWithViews();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Register custom services
            builder.Services.AddScoped<WedBanSach.Services.EmailService>();
            builder.Services.AddScoped<WedBanSach.Repositories.IPolicyRepository, WedBanSach.Repositories.PolicyRepository>();
            builder.Services.AddScoped<WedBanSach.Repositories.IReturnRequestRepository, WedBanSach.Repositories.ReturnRequestRepository>();
            builder.Services.AddScoped<WedBanSach.Repositories.IWarrantyRequestRepository, WedBanSach.Repositories.WarrantyRequestRepository>();
            builder.Services.AddScoped<WedBanSach.Services.IPolicyService, WedBanSach.Services.PolicyService>();
            builder.Services.AddScoped<WedBanSach.Services.IReturnRequestService, WedBanSach.Services.ReturnRequestService>();
            builder.Services.AddScoped<WedBanSach.Services.IWarrantyRequestService, WedBanSach.Services.WarrantyRequestService>();
            
            // AI Services Registration
            builder.Services.AddHttpClient<WedBanSach.Services.AIService>();
            builder.Services.AddScoped<WedBanSach.Services.RecommendationService>();
            builder.Services.AddScoped<WedBanSach.Services.ChatbotService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapRazorPages();
            app.MapHub<WedBanSach.Hubs.ChatHub>("/chatHub");

            // AUTOMATIC DATABASE FIX: Check and Create UserAddresses Table if missing
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<BookStoreDbContext>();
                    // Check connection
                    if (context.Database.CanConnect())
                    {
                        // Auto-Fix: Add FaultyQuantity to Books table if missing
                        var faultyColExists = context.Database.SqlQueryRaw<int>(
                            "SELECT CASE WHEN COL_LENGTH('dbo.Books', 'FaultyQuantity') IS NOT NULL THEN 1 ELSE 0 END")
                            .AsEnumerable().FirstOrDefault() == 1;
                        
                        if (!faultyColExists)
                        {
                            context.Database.ExecuteSqlRaw("ALTER TABLE [dbo].[Books] ADD [FaultyQuantity] [int] NOT NULL DEFAULT 0");
                            Console.WriteLine("Auto-Fix: Added 'FaultyQuantity' to 'Books' table.");
                        }

                        // Auto-Fix: Create PolicyCategories table if missing
                        var policyCatExists = context.Database.SqlQueryRaw<int>(
                            "SELECT CASE WHEN OBJECT_ID('dbo.PolicyCategories', 'U') IS NOT NULL THEN 1 ELSE 0 END")
                            .AsEnumerable().FirstOrDefault() == 1;

                        if (!policyCatExists)
                        {
                            string sql = @"
                                CREATE TABLE [dbo].[PolicyCategories](
                                    [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                    [Name] [nvarchar](150) NOT NULL,
                                    [Slug] [varchar](150) NOT NULL UNIQUE,
                                    [Description] [nvarchar](500) NULL,
                                    [IsActive] [bit] NOT NULL DEFAULT 1,
                                    [CreatedAt] [datetime] NOT NULL DEFAULT GETDATE()
                                );
                            ";
                            context.Database.ExecuteSqlRaw(sql);
                            Console.WriteLine("Auto-Fix: Created 'PolicyCategories' table.");
                        }

                        // Auto-Fix: Create Policies table if missing
                        var policiesExists = context.Database.SqlQueryRaw<int>(
                            "SELECT CASE WHEN OBJECT_ID('dbo.Policies', 'U') IS NOT NULL THEN 1 ELSE 0 END")
                            .AsEnumerable().FirstOrDefault() == 1;

                        if (!policiesExists)
                        {
                            string sql = @"
                                CREATE TABLE [dbo].[Policies](
                                    [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                    [CategoryId] [int] NOT NULL REFERENCES [dbo].[PolicyCategories]([Id]) ON DELETE CASCADE,
                                    [Title] [nvarchar](255) NOT NULL,
                                    [Content] [nvarchar](max) NOT NULL,
                                    [Thumbnail] [nvarchar](500) NULL,
                                    [IsPublished] [bit] NOT NULL DEFAULT 0,
                                    [CreatedAt] [datetime] NOT NULL DEFAULT GETDATE(),
                                    [UpdatedAt] [datetime] NOT NULL DEFAULT GETDATE()
                                );
                            ";
                            context.Database.ExecuteSqlRaw(sql);
                            Console.WriteLine("Auto-Fix: Created 'Policies' table.");

                            // Seed some default categories and policies
                            string seedSql = @"
                                INSERT INTO [dbo].[PolicyCategories] ([Name], [Slug], [Description], [IsActive], [CreatedAt])
                                VALUES 
                                (N'Chính sách đổi hàng', 'doi-hang', N'Quy định về việc đổi sách và phụ kiện lỗi sản xuất.', 1, GETDATE()),
                                (N'Chính sách trả hàng', 'tra-hang', N'Quy định về việc trả lại sản phẩm đã mua.', 1, GETDATE()),
                                (N'Chính sách hoàn tiền', 'hoan-tien', N'Quy trình và phương thức hoàn lại tiền cho khách hàng.', 1, GETDATE()),
                                (N'Chính sách bảo hành', 'bao-hanh', N'Chế độ bảo hành sách lỗi, phụ kiện và bookmark quà tặng.', 1, GETDATE());
                            ";
                            context.Database.ExecuteSqlRaw(seedSql);

                            string seedPolicies = @"
                                DECLARE @ExId INT = (SELECT Id FROM [dbo].[PolicyCategories] WHERE [Slug] = 'doi-hang');
                                DECLARE @ReId INT = (SELECT Id FROM [dbo].[PolicyCategories] WHERE [Slug] = 'tra-hang');
                                DECLARE @RfId INT = (SELECT Id FROM [dbo].[PolicyCategories] WHERE [Slug] = 'hoan-tien');
                                DECLARE @WaId INT = (SELECT Id FROM [dbo].[PolicyCategories] WHERE [Slug] = 'bao-hanh');

                                INSERT INTO [dbo].[Policies] ([CategoryId], [Title], [Content], [Thumbnail], [IsPublished], [CreatedAt], [UpdatedAt])
                                VALUES 
                                (@ExId, N'Chính sách Đổi hàng chi tiết', N'<h3>1. Điều kiện đổi hàng</h3><p>Sản phẩm được đổi trong vòng 7 ngày kể từ ngày nhận hàng thành công. Sản phẩm phải còn nguyên vẹn màng co (nếu có), không có dấu hiệu đã qua sử dụng, viết vẽ hoặc làm rách nát.</p><h3>2. Các trường hợp được hỗ trợ đổi</h3><ul><li>Sách bị lỗi in ấn: mất trang, ngược trang, mờ chữ không đọc được.</li><li>Sách bị lỗi gia công: bong keo gáy, đóng ngược bìa.</li><li>Sách bị hư hỏng do quá trình vận chuyển của cửa hàng (móp góc nặng, rách bìa).</li><li>Gửi sai tựa sách so với đơn đặt hàng.</li></ul><h3>3. Quy trình thực hiện</h3><p>Vui lòng đăng nhập tài khoản khách hàng, truy cập mục Lịch sử gửi yêu cầu để điền Form yêu cầu kèm hình ảnh thực tế sản phẩm lỗi. Đội ngũ CSKH sẽ phản hồi trong vòng 24h làm việc.</p>', '/images/policy-exchange.png', 1, GETDATE(), GETDATE()),
                                (@ReId, N'Chính sách Trả hàng chi tiết', N'<h3>1. Thời gian trả hàng</h3><p>Hỗ trợ trả hàng hoàn tiền trong vòng 7 ngày kể từ ngày nhận hàng. Áp dụng cho cả khách mua online và mua trực tiếp.</p><h3>2. Điều kiện áp dụng</h3><p>Sách còn mới 100%, nguyên đai nguyên kiện, không trầy xước, không viết vẽ, kèm theo hoá đơn mua hàng gốc. Đối với quà tặng/bookmark đi kèm, bắt buộc phải trả lại đầy đủ nguyên vẹn.</p><h3>3. Chi phí trả hàng</h3><p>Nếu lỗi xuất phát từ nhà sản xuất hoặc BookStore, khách hàng được miễn phí 100% cước thu hồi. Trường hợp trả hàng do thay đổi nhu cầu cá nhân, khách hàng chịu chi phí vận chuyển thu hồi.</p>', '/images/policy-return.png', 1, GETDATE(), GETDATE()),
                                (@RfId, N'Chính sách Hoàn tiền chi tiết', N'<h3>1. Phương thức hoàn tiền</h3><p>Khách hàng được lựa chọn các hình thức nhận tiền hoàn trả sau: Chuyển khoản Ngân hàng (khuyên dùng), hoàn ví hoặc mã Coupon giảm giá.</p><h3>2. Thời gian xử lý hoàn tiền</h3><p>Tiền sẽ được hoàn lại từ 3 - 5 ngày làm việc sau khi kho hàng của chúng tôi nhận lại sản phẩm trả về và kiểm tra chất lượng đạt yêu cầu.</p>', '/images/policy-refund.png', 1, GETDATE(), GETDATE()),
                                (@WaId, N'Chính sách Bảo hành Sách & Phụ kiện', N'<h3>1. Đối với Sách</h3><p>Sách không có thời hạn bảo hành phần cứng theo tháng, nhưng BookStore cam kết bảo hành trọn đời đối với lỗi bản in (thiếu trang, nhầm nội dung do nhà xuất bản).</p><h3>2. Đối với Phụ kiện / Bookmark / Quà tặng</h3><p>Các phụ kiện công nghệ, đèn đọc sách, bookmark kim loại hoặc quà tặng đi kèm có thời hạn bảo hành từ 1 đến 3 tháng tùy theo từng sản phẩm cụ thể nếu phát sinh lỗi kỹ thuật từ nhà sản xuất.</p>', '/images/policy-warranty.png', 1, GETDATE(), GETDATE());
                            ";
                            context.Database.ExecuteSqlRaw(seedPolicies);
                            Console.WriteLine("Auto-Fix: Seeded default policies.");
                        }

                        // Auto-Fix: Create ReturnRequests table if missing
                        var returnReqExists = context.Database.SqlQueryRaw<int>(
                            "SELECT CASE WHEN OBJECT_ID('dbo.ReturnRequests', 'U') IS NOT NULL THEN 1 ELSE 0 END")
                            .AsEnumerable().FirstOrDefault() == 1;

                        if (!returnReqExists)
                        {
                            string sql = @"
                                CREATE TABLE [dbo].[ReturnRequests](
                                    [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                    [OrderId] [int] NOT NULL REFERENCES [dbo].[Orders]([OrderID]),
                                    [CustomerId] [int] NOT NULL REFERENCES [dbo].[Users]([UserID]),
                                    [BookID] [int] NULL REFERENCES [dbo].[Books]([BookID]),
                                    [Quantity] [int] NOT NULL DEFAULT 1,
                                    [RequestType] [nvarchar](50) NOT NULL,
                                    [Reason] [nvarchar](255) NOT NULL,
                                    [Description] [nvarchar](max) NULL,
                                    [Status] [nvarchar](50) NOT NULL DEFAULT 'Pending',
                                    [RefundAmount] [decimal](18,2) NULL,
                                    [AdminNote] [nvarchar](max) NULL,
                                    [CreatedAt] [datetime] NOT NULL DEFAULT GETDATE(),
                                    [UpdatedAt] [datetime] NOT NULL DEFAULT GETDATE()
                                );
                            ";
                            context.Database.ExecuteSqlRaw(sql);
                            Console.WriteLine("Auto-Fix: Created 'ReturnRequests' table.");
                        }

                        // Auto-Fix: Create ReturnRequestImages table if missing
                        var returnReqImgExists = context.Database.SqlQueryRaw<int>(
                            "SELECT CASE WHEN OBJECT_ID('dbo.ReturnRequestImages', 'U') IS NOT NULL THEN 1 ELSE 0 END")
                            .AsEnumerable().FirstOrDefault() == 1;

                        if (!returnReqImgExists)
                        {
                            string sql = @"
                                CREATE TABLE [dbo].[ReturnRequestImages](
                                    [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                    [ReturnRequestId] [int] NOT NULL REFERENCES [dbo].[ReturnRequests]([Id]) ON DELETE CASCADE,
                                    [ImageUrl] [nvarchar](500) NOT NULL
                                );
                            ";
                            context.Database.ExecuteSqlRaw(sql);
                            Console.WriteLine("Auto-Fix: Created 'ReturnRequestImages' table.");
                        }

                        // Auto-Fix: Create RefundTransactions table if missing
                        var refundTransExists = context.Database.SqlQueryRaw<int>(
                            "SELECT CASE WHEN OBJECT_ID('dbo.RefundTransactions', 'U') IS NOT NULL THEN 1 ELSE 0 END")
                            .AsEnumerable().FirstOrDefault() == 1;

                        if (!refundTransExists)
                        {
                            string sql = @"
                                CREATE TABLE [dbo].[RefundTransactions](
                                    [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                    [ReturnRequestId] [int] NOT NULL REFERENCES [dbo].[ReturnRequests]([Id]) ON DELETE CASCADE,
                                    [RefundMethod] [nvarchar](100) NOT NULL,
                                    [RefundStatus] [nvarchar](50) NOT NULL DEFAULT 'Pending',
                                    [RefundDate] [datetime] NULL,
                                    [TransactionCode] [nvarchar](100) NULL
                                );
                            ";
                            context.Database.ExecuteSqlRaw(sql);
                            Console.WriteLine("Auto-Fix: Created 'RefundTransactions' table.");
                        }

                        // Auto-Fix: Create WarrantyRequests table if missing
                        var warrantyReqExists = context.Database.SqlQueryRaw<int>(
                            "SELECT CASE WHEN OBJECT_ID('dbo.WarrantyRequests', 'U') IS NOT NULL THEN 1 ELSE 0 END")
                            .AsEnumerable().FirstOrDefault() == 1;

                        if (!warrantyReqExists)
                        {
                            string sql = @"
                                CREATE TABLE [dbo].[WarrantyRequests](
                                    [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                    [ProductId] [int] NOT NULL REFERENCES [dbo].[Books]([BookID]),
                                    [CustomerId] [int] NOT NULL REFERENCES [dbo].[Users]([UserID]),
                                    [IssueDescription] [nvarchar](max) NOT NULL,
                                    [Status] [nvarchar](50) NOT NULL DEFAULT 'Pending',
                                    [CreatedAt] [datetime] NOT NULL DEFAULT GETDATE()
                                );
                            ";
                            context.Database.ExecuteSqlRaw(sql);
                            Console.WriteLine("Auto-Fix: Created 'WarrantyRequests' table.");
                        }

                        // Auto-Fix: Create AIChatSessions table if missing
                        var aiSessionsExists = context.Database.SqlQueryRaw<int>(
                            "SELECT CASE WHEN OBJECT_ID('dbo.AIChatSessions', 'U') IS NOT NULL THEN 1 ELSE 0 END")
                            .AsEnumerable().FirstOrDefault() == 1;

                        if (!aiSessionsExists)
                        {
                            string sql = @"
                                CREATE TABLE [dbo].[AIChatSessions](
                                    [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                    [CustomerId] [int] NULL REFERENCES [dbo].[Users]([UserID]) ON DELETE SET NULL,
                                    [StartedAt] [datetime] NOT NULL DEFAULT GETDATE(),
                                    [LastMessageAt] [datetime] NOT NULL DEFAULT GETDATE()
                                );
                            ";
                            context.Database.ExecuteSqlRaw(sql);
                            Console.WriteLine("Auto-Fix: Created 'AIChatSessions' table.");
                        }

                        // Auto-Fix: Create AIChatMessages table if missing
                        var aiMessagesExists = context.Database.SqlQueryRaw<int>(
                            "SELECT CASE WHEN OBJECT_ID('dbo.AIChatMessages', 'U') IS NOT NULL THEN 1 ELSE 0 END")
                            .AsEnumerable().FirstOrDefault() == 1;

                        if (!aiMessagesExists)
                        {
                            string sql = @"
                                CREATE TABLE [dbo].[AIChatMessages](
                                    [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                    [SessionId] [int] NOT NULL REFERENCES [dbo].[AIChatSessions]([Id]) ON DELETE CASCADE,
                                    [SenderType] [nvarchar](50) NOT NULL,
                                    [Message] [nvarchar](max) NOT NULL,
                                    [CreatedAt] [datetime] NOT NULL DEFAULT GETDATE()
                                );
                            ";
                            context.Database.ExecuteSqlRaw(sql);
                            Console.WriteLine("Auto-Fix: Created 'AIChatMessages' table.");
                        }

                        // Auto-Fix: Create AIRecommendations table if missing
                        var aiRecsExists = context.Database.SqlQueryRaw<int>(
                            "SELECT CASE WHEN OBJECT_ID('dbo.AIRecommendations', 'U') IS NOT NULL THEN 1 ELSE 0 END")
                            .AsEnumerable().FirstOrDefault() == 1;

                        if (!aiRecsExists)
                        {
                            string sql = @"
                                CREATE TABLE [dbo].[AIRecommendations](
                                    [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                    [CustomerId] [int] NULL REFERENCES [dbo].[Users]([UserID]) ON DELETE SET NULL,
                                    [ProductId] [int] NOT NULL REFERENCES [dbo].[Books]([BookID]) ON DELETE CASCADE,
                                    [RecommendationScore] [decimal](18,4) NOT NULL,
                                    [CreatedAt] [datetime] NOT NULL DEFAULT GETDATE()
                                );
                            ";
                            context.Database.ExecuteSqlRaw(sql);
                            Console.WriteLine("Auto-Fix: Created 'AIRecommendations' table.");
                        }

                        // Auto-Fix: Create CustomerPreferences table if missing
                        var custPrefsExists = context.Database.SqlQueryRaw<int>(
                            "SELECT CASE WHEN OBJECT_ID('dbo.CustomerPreferences', 'U') IS NOT NULL THEN 1 ELSE 0 END")
                            .AsEnumerable().FirstOrDefault() == 1;

                        if (!custPrefsExists)
                        {
                            string sql = @"
                                CREATE TABLE [dbo].[CustomerPreferences](
                                    [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                    [CustomerId] [int] NOT NULL REFERENCES [dbo].[Users]([UserID]) ON DELETE CASCADE UNIQUE,
                                    [FavoriteGenres] [nvarchar](500) NULL,
                                    [FavoriteAuthors] [nvarchar](500) NULL,
                                    [PreferredPriceRange] [nvarchar](100) NULL
                                );
                            ";
                            context.Database.ExecuteSqlRaw(sql);
                            Console.WriteLine("Auto-Fix: Created 'CustomerPreferences' table.");
                        }

                        // Check if table exists (SQL Server syntax)
                        var tableExists = context.Database.SqlQueryRaw<int>(
                            "SELECT CASE WHEN OBJECT_ID('dbo.UserAddresses', 'U') IS NOT NULL THEN 1 ELSE 0 END")
                            .AsEnumerable().FirstOrDefault() == 1;

                        if (!tableExists)
                        {
                            // Create Table Script matching the Model
                            string createTableSql = @"
                                CREATE TABLE [dbo].[UserAddresses](
                                    [AddressID] [int] IDENTITY(1,1) NOT NULL,
                                    [UserID] [int] NOT NULL,
                                    [ReceiverName] [nvarchar](150) NOT NULL,
                                    [Phone] [nvarchar](20) NOT NULL,
                                    [AddressDetail] [nvarchar](255) NOT NULL,
                                    [ProvinceCode] [nvarchar](20) NULL,
                                    [ProvinceName] [nvarchar](100) NULL,
                                    [DistrictCode] [nvarchar](20) NULL,
                                    [DistrictName] [nvarchar](100) NULL,
                                    [WardCode] [nvarchar](20) NULL,
                                    [WardName] [nvarchar](100) NULL,
                                    [IsDefault] [bit] NOT NULL,
                                    CONSTRAINT [PK_UserAddresses] PRIMARY KEY CLUSTERED ([AddressID] ASC),
                                    CONSTRAINT [FK_UserAddresses_Users_UserID] FOREIGN KEY([UserID]) REFERENCES [dbo].[Users] ([UserID]) ON DELETE CASCADE
                                );
                                CREATE INDEX [IX_UserAddresses_UserID] ON [dbo].[UserAddresses]([UserID]);
                            ";
                            context.Database.ExecuteSqlRaw(createTableSql);
                            // Log or Console Write
                            Console.WriteLine("Auto-Fix: Created missing 'UserAddresses' table.");
                        }

                        // Auto-Fix: Add ImageUrl to Reviews if missing
                        var reviewColExists = context.Database.SqlQueryRaw<int>(
                            "SELECT CASE WHEN COL_LENGTH('dbo.Reviews', 'ImageUrl') IS NOT NULL THEN 1 ELSE 0 END")
                            .AsEnumerable().FirstOrDefault() == 1;
                        
                        if (!reviewColExists)
                        {
                            context.Database.ExecuteSqlRaw("ALTER TABLE [dbo].[Reviews] ADD [ImageUrl] [nvarchar](500) NULL");
                             Console.WriteLine("Auto-Fix: Added 'ImageUrl' to 'Reviews' table.");
                        }

                        // Auto-Fix: Create ChatRooms and ChatMessages
                        var chatTableExists = context.Database.SqlQueryRaw<int>(
                           "SELECT CASE WHEN OBJECT_ID('dbo.ChatRooms', 'U') IS NOT NULL THEN 1 ELSE 0 END")
                           .AsEnumerable().FirstOrDefault() == 1;

                        if (!chatTableExists)
                        {
                            string createChatSql = @"
                                CREATE TABLE [dbo].[ChatRooms](
                                    [RoomID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                    [UserID] [int] NULL,
                                    [AdminID] [int] NULL,
                                    [LastMessage] [nvarchar](max) NULL,
                                    [UpdatedAt] [datetime2](7) NOT NULL
                                );
                                CREATE TABLE [dbo].[ChatMessages](
                                    [MessageID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                    [RoomID] [int] NOT NULL,
                                    [SenderRole] [nvarchar](50) NOT NULL,
                                    [SenderID] [int] NOT NULL,
                                    [MessageContent] [nvarchar](max) NOT NULL,
                                    [MessageType] [nvarchar](20) DEFAULT 'Text',
                                    [IsRead] [bit] NOT NULL,
                                    [CreatedAt] [datetime2](7) NOT NULL,
                                    CONSTRAINT [FK_ChatMessages_ChatRooms] FOREIGN KEY([RoomID]) REFERENCES [dbo].[ChatRooms] ([RoomID]) ON DELETE CASCADE
                                );
                            ";
                            context.Database.ExecuteSqlRaw(createChatSql);
                            Console.WriteLine("Auto-Fix: Created 'ChatRooms' and 'ChatMessages' tables.");
                        }
                        else
                        {
                            // Check if MessageType column exists
                            var messageTypeExists = context.Database.SqlQueryRaw<int>(
                                "SELECT CASE WHEN COL_LENGTH('dbo.ChatMessages', 'MessageType') IS NOT NULL THEN 1 ELSE 0 END")
                                .AsEnumerable().FirstOrDefault() == 1;

                            if (!messageTypeExists)
                            {
                                try {
                                    context.Database.ExecuteSqlRaw("ALTER TABLE [dbo].[ChatMessages] ADD [MessageType] [nvarchar](20) DEFAULT 'Text'");
                                    context.Database.ExecuteSqlRaw("UPDATE [dbo].[ChatMessages] SET [MessageType] = 'Text' WHERE [MessageType] IS NULL");
                                     Console.WriteLine("Auto-Fix: Added 'MessageType' to 'ChatMessages' table.");
                                } catch (Exception ex) {
                                     Console.WriteLine($"Auto-Fix 'MessageType' Failed: {ex.Message}");
                                }
                            }
                        }

                        // Auto-Fix: Add AvatarUrl to Users if missing
                        var userColExists = context.Database.SqlQueryRaw<int>(
                            "SELECT CASE WHEN COL_LENGTH('dbo.Users', 'AvatarUrl') IS NOT NULL THEN 1 ELSE 0 END")
                            .AsEnumerable().FirstOrDefault() == 1;
                        
                        if (!userColExists)
                        {
                            context.Database.ExecuteSqlRaw("ALTER TABLE [dbo].[Users] ADD [AvatarUrl] [nvarchar](500) NULL");
                             Console.WriteLine("Auto-Fix: Added 'AvatarUrl' to 'Users' table.");
                        }

                        // Auto-Fix: Add Slug to Categories if missing
                        var slugColExists = context.Database.SqlQueryRaw<int>(
                            "SELECT CASE WHEN COL_LENGTH('dbo.Categories', 'Slug') IS NOT NULL THEN 1 ELSE 0 END")
                            .AsEnumerable().FirstOrDefault() == 1;

                        if (!slugColExists)
                        {
                            context.Database.ExecuteSqlRaw("ALTER TABLE [dbo].[Categories] ADD [Slug] [nvarchar](150) NULL");
                            Console.WriteLine("Auto-Fix: Added 'Slug' to 'Categories' table.");

                            // Populate Slugs
                            var categories = context.Categories.ToList();
                            bool updated = false;
                            foreach (var cat in categories)
                            {
                                if (string.IsNullOrEmpty(cat.Slug))
                                {
                                    // Simple Slugify Logic
                                    string slug = cat.CategoryName.ToLower();
                                    // Remove Vietnamese accents - quick hack or proper method
                                    slug = System.Text.RegularExpressions.Regex.Replace(slug, "[áàảãạăắằẳẵặâấầẩẫậ]", "a");
                                    slug = System.Text.RegularExpressions.Regex.Replace(slug, "[éèẻẽẹêếềểễệ]", "e");
                                    slug = System.Text.RegularExpressions.Regex.Replace(slug, "[iíìỉĩị]", "i");
                                    slug = System.Text.RegularExpressions.Regex.Replace(slug, "[óòỏõọôốồổỗộơớờởỡợ]", "o");
                                    slug = System.Text.RegularExpressions.Regex.Replace(slug, "[uúùủũụưứừửữự]", "u");
                                    slug = System.Text.RegularExpressions.Regex.Replace(slug, "[yýỳỷỹỵ]", "y");
                                    slug = System.Text.RegularExpressions.Regex.Replace(slug, "[đ]", "d");
                                    
                                    // Remove special chars
                                    slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", ""); 
                                    slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-").Trim('-');

                                    cat.Slug = slug;
                                    updated = true;
                                }
                            }
                            if (updated)
                            {
                                context.SaveChanges();
                                Console.WriteLine("Auto-Fix: Populated Slugs for Categories.");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Auto-Fix Failed: {ex.Message}");
                }
            }

            app.Run();
        }
    }
}