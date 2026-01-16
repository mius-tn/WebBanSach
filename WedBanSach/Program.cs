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
            builder.Services.AddHttpClient<WedBanSach.Services.SmsService>();

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
                            try {
                                context.Database.ExecuteSqlRaw("ALTER TABLE [dbo].[ChatMessages] ADD [MessageType] [nvarchar](20) DEFAULT 'Text'");
                                context.Database.ExecuteSqlRaw("UPDATE [dbo].[ChatMessages] SET [MessageType] = 'Text' WHERE [MessageType] IS NULL");
                            } catch {}
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