-- =========================================================================
-- SQL MIGRATION: ADD POLICY, RETURN, REFUND, AND WARRANTY MODULES
-- TARGET DATABASE: BookStoreDB
-- DATE: 2026-06-01
-- =========================================================================

BEGIN TRANSACTION;

-- 1. Bổ sung cột FaultyQuantity vào bảng Books nếu chưa tồn tại
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Books]') AND name = N'FaultyQuantity'
)
BEGIN
    ALTER TABLE [dbo].[Books] ADD [FaultyQuantity] INT NOT NULL DEFAULT 0;
    PRINT 'Added FaultyQuantity column to Books table.';
END
ELSE
BEGIN
    PRINT 'FaultyQuantity column already exists in Books table.';
END

-- 2. Tạo bảng PolicyCategories
IF OBJECT_ID(N'[dbo].[PolicyCategories]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PolicyCategories] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(150) NOT NULL,
        [Slug] VARCHAR(150) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_PolicyCategories] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE UNIQUE NONCLUSTERED INDEX [IX_PolicyCategories_Slug] ON [dbo].[PolicyCategories] ([Slug] ASC);
    PRINT 'Created PolicyCategories table.';
END

-- 3. Tạo bảng Policies
IF OBJECT_ID(N'[dbo].[Policies]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Policies] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [CategoryId] INT NOT NULL,
        [Title] NVARCHAR(255) NOT NULL,
        [Content] NVARCHAR(MAX) NOT NULL,
        [Thumbnail] NVARCHAR(500) NULL,
        [IsPublished] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_Policies] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Policies_PolicyCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[PolicyCategories] ([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_Policies_CategoryId] ON [dbo].[Policies] ([CategoryId] ASC);
    PRINT 'Created Policies table.';
END

-- 4. Tạo bảng ReturnRequests
IF OBJECT_ID(N'[dbo].[ReturnRequests]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ReturnRequests] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [OrderId] INT NOT NULL,
        [CustomerId] INT NOT NULL,
        [BookID] INT NULL,
        [Quantity] INT NOT NULL DEFAULT 1,
        [RequestType] NVARCHAR(50) NOT NULL,
        [Reason] NVARCHAR(255) NOT NULL,
        [Description] NVARCHAR(MAX) NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT N'Pending',
        [RefundAmount] DECIMAL(18,2) NULL,
        [AdminNote] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_ReturnRequests] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_ReturnRequests_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders] ([OrderID]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ReturnRequests_Users_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Users] ([UserID]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ReturnRequests_Books_BookID] FOREIGN KEY ([BookID]) REFERENCES [dbo].[Books] ([BookID]) ON DELETE NO ACTION
    );
    CREATE NONCLUSTERED INDEX [IX_ReturnRequests_OrderId] ON [dbo].[ReturnRequests] ([OrderId] ASC);
    CREATE NONCLUSTERED INDEX [IX_ReturnRequests_CustomerId] ON [dbo].[ReturnRequests] ([CustomerId] ASC);
    CREATE NONCLUSTERED INDEX [IX_ReturnRequests_BookID] ON [dbo].[ReturnRequests] ([BookID] ASC);
    PRINT 'Created ReturnRequests table.';
END

-- 5. Tạo bảng ReturnRequestImages
IF OBJECT_ID(N'[dbo].[ReturnRequestImages]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ReturnRequestImages] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ReturnRequestId] INT NOT NULL,
        [ImageUrl] NVARCHAR(500) NOT NULL,
        CONSTRAINT [PK_ReturnRequestImages] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_ReturnRequestImages_ReturnRequests_ReturnRequestId] FOREIGN KEY ([ReturnRequestId]) REFERENCES [dbo].[ReturnRequests] ([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_ReturnRequestImages_ReturnRequestId] ON [dbo].[ReturnRequestImages] ([ReturnRequestId] ASC);
    PRINT 'Created ReturnRequestImages table.';
END

-- 6. Tạo bảng RefundTransactions
IF OBJECT_ID(N'[dbo].[RefundTransactions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RefundTransactions] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ReturnRequestId] INT NOT NULL,
        [RefundMethod] NVARCHAR(100) NOT NULL,
        [RefundStatus] NVARCHAR(50) NOT NULL DEFAULT N'Pending',
        [RefundDate] DATETIME NULL,
        [TransactionCode] NVARCHAR(100) NULL,
        CONSTRAINT [PK_RefundTransactions] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_RefundTransactions_ReturnRequests_ReturnRequestId] FOREIGN KEY ([ReturnRequestId]) REFERENCES [dbo].[ReturnRequests] ([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_RefundTransactions_ReturnRequestId] ON [dbo].[RefundTransactions] ([ReturnRequestId] ASC);
    PRINT 'Created RefundTransactions table.';
END

-- 7. Tạo bảng WarrantyRequests
IF OBJECT_ID(N'[dbo].[WarrantyRequests]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WarrantyRequests] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ProductId] INT NOT NULL,
        [CustomerId] INT NOT NULL,
        [IssueDescription] NVARCHAR(MAX) NOT NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT N'Pending',
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_WarrantyRequests] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_WarrantyRequests_Books_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Books] ([BookID]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WarrantyRequests_Users_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Users] ([UserID]) ON DELETE NO ACTION
    );
    CREATE NONCLUSTERED INDEX [IX_WarrantyRequests_ProductId] ON [dbo].[WarrantyRequests] ([ProductId] ASC);
    CREATE NONCLUSTERED INDEX [IX_WarrantyRequests_CustomerId] ON [dbo].[WarrantyRequests] ([CustomerId] ASC);
    PRINT 'Created WarrantyRequests table.';
END

-- =========================================================================
-- SEED DATA: DỮ LIỆU MẪU CHÍNH SÁCH
-- =========================================================================

-- Seed Categories
IF NOT EXISTS (SELECT 1 FROM [dbo].[PolicyCategories] WHERE [Slug] = 'doi-hang')
BEGIN
    INSERT INTO [dbo].[PolicyCategories] ([Name], [Slug], [Description], [IsActive], [CreatedAt])
    VALUES (N'Chính sách đổi hàng', 'doi-hang', N'Quy định về việc đổi sách và phụ kiện lỗi sản xuất.', 1, GETDATE());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[PolicyCategories] WHERE [Slug] = 'tra-hang')
BEGIN
    INSERT INTO [dbo].[PolicyCategories] ([Name], [Slug], [Description], [IsActive], [CreatedAt])
    VALUES (N'Chính sách trả hàng', 'tra-hang', N'Quy định về việc trả lại sản phẩm đã mua.', 1, GETDATE());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[PolicyCategories] WHERE [Slug] = 'hoan-tien')
BEGIN
    INSERT INTO [dbo].[PolicyCategories] ([Name], [Slug], [Description], [IsActive], [CreatedAt])
    VALUES (N'Chính sách hoàn tiền', 'hoan-tien', N'Quy trình và phương thức hoàn lại tiền cho khách hàng.', 1, GETDATE());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[PolicyCategories] WHERE [Slug] = 'bao-hanh')
BEGIN
    INSERT INTO [dbo].[PolicyCategories] ([Name], [Slug], [Description], [IsActive], [CreatedAt])
    VALUES (N'Chính sách bảo hành', 'bao-hanh', N'Chế độ bảo hành sách lỗi, phụ kiện và bookmark quà tặng.', 1, GETDATE());
END

-- Seed Policies
DECLARE @ExchangeCatId INT = (SELECT Id FROM [dbo].[PolicyCategories] WHERE [Slug] = 'doi-hang');
DECLARE @ReturnCatId INT = (SELECT Id FROM [dbo].[PolicyCategories] WHERE [Slug] = 'tra-hang');
DECLARE @RefundCatId INT = (SELECT Id FROM [dbo].[PolicyCategories] WHERE [Slug] = 'hoan-tien');
DECLARE @WarrantyCatId INT = (SELECT Id FROM [dbo].[PolicyCategories] WHERE [Slug] = 'bao-hanh');

-- 1. Đổi hàng
IF NOT EXISTS (SELECT 1 FROM [dbo].[Policies] WHERE [CategoryId] = @ExchangeCatId)
BEGIN
    INSERT INTO [dbo].[Policies] ([CategoryId], [Title], [Content], [Thumbnail], [IsPublished], [CreatedAt], [UpdatedAt])
    VALUES (@ExchangeCatId, N'Chính sách Đổi hàng chi tiết', N'<h3>1. Điều kiện đổi hàng</h3><p>Sản phẩm được đổi trong vòng <strong>7 ngày</strong> kể từ ngày nhận hàng thành công. Sản phẩm phải còn nguyên vẹn màng co (nếu có), không có dấu hiệu đã qua sử dụng, viết vẽ hoặc làm rách nát.</p><h3>2. Các trường hợp được hỗ trợ đổi</h3><ul><li>Sách bị lỗi in ấn: mất trang, ngược trang, mờ chữ không đọc được.</li><li>Sách bị lỗi gia công: bong keo gáy, đóng ngược bìa.</li><li>Sách bị hư hỏng do quá trình vận chuyển của cửa hàng (móp góc nặng, rách bìa).</li><li>Gửi sai tựa sách so với đơn đặt hàng.</li></ul><h3>3. Quy trình thực hiện</h3><p>Vui lòng đăng nhập tài khoản khách hàng, truy cập mục Lịch sử gửi yêu cầu để điền Form yêu cầu kèm hình ảnh thực tế sản phẩm lỗi. Đội ngũ CSKH sẽ phản hồi trong vòng 24h làm việc.</p>', '/images/policy-exchange.png', 1, GETDATE(), GETDATE());
END

-- 2. Trả hàng
IF NOT EXISTS (SELECT 1 FROM [dbo].[Policies] WHERE [CategoryId] = @ReturnCatId)
BEGIN
    INSERT INTO [dbo].[Policies] ([CategoryId], [Title], [Content], [Thumbnail], [IsPublished], [CreatedAt], [UpdatedAt])
    VALUES (@ReturnCatId, N'Chính sách Trả hàng chi tiết', N'<h3>1. Thời gian trả hàng</h3><p>Hỗ trợ trả hàng hoàn tiền trong vòng <strong>7 ngày</strong> kể từ ngày nhận hàng. Áp dụng cho cả khách mua online và mua trực tiếp.</p><h3>2. Điều kiện áp dụng</h3><p>Sách còn mới 100%, nguyên đai nguyên kiện, không trầy xước, không viết vẽ, kèm theo hoá đơn mua hàng gốc. Đối với quà tặng/bookmark đi kèm, bắt buộc phải trả lại đầy đủ nguyên vẹn.</p><h3>3. Chi phí trả hàng</h3><p>Nếu lỗi xuất phát từ nhà sản xuất hoặc BookStore, khách hàng được miễn phí 100% cước thu hồi. Trường hợp trả hàng do thay đổi nhu cầu cá nhân, khách hàng chịu chi phí vận chuyển thu hồi.</p>', '/images/policy-return.png', 1, GETDATE(), GETDATE());
END

-- 3. Hoàn tiền
IF NOT EXISTS (SELECT 1 FROM [dbo].[Policies] WHERE [CategoryId] = @RefundCatId)
BEGIN
    INSERT INTO [dbo].[Policies] ([CategoryId], [Title], [Content], [Thumbnail], [IsPublished], [CreatedAt], [UpdatedAt])
    VALUES (@RefundCatId, N'Chính sách Hoàn tiền chi tiết', N'<h3>1. Phương thức hoàn tiền</h3><p>Khách hàng được lựa chọn các hình thức nhận tiền hoàn trả sau:</p><ul><li>Chuyển khoản Ngân hàng (khuyên dùng, xử lý nhanh nhất).</li><li>Hoàn về ví điện tử hoặc cổng thanh toán ban đầu (nếu thanh toán online trước).</li><li>Nhận mã Coupon giảm giá tương đương để mua sắm đơn hàng tiếp theo.</li></ul><h3>2. Thời gian xử lý hoàn tiền</h3><p>Tiền sẽ được hoàn lại từ <strong>3 - 5 ngày làm việc</strong> sau khi kho hàng của chúng tôi nhận lại sản phẩm trả về và kiểm tra chất lượng đạt yêu cầu.</p>', '/images/policy-refund.png', 1, GETDATE(), GETDATE());
END

-- 4. Bảo hành
IF NOT EXISTS (SELECT 1 FROM [dbo].[Policies] WHERE [CategoryId] = @WarrantyCatId)
BEGIN
    INSERT INTO [dbo].[Policies] ([CategoryId], [Title], [Content], [Thumbnail], [IsPublished], [CreatedAt], [UpdatedAt])
    VALUES (@WarrantyCatId, N'Chính sách Bảo hành Sách & Phụ kiện', N'<h3>1. Đối với Sách</h3><p>Sách không có thời hạn bảo hành phần cứng theo tháng, nhưng BookStore cam kết bảo hành trọn đời đối với lỗi bản in (thiếu trang, nhầm nội dung do nhà xuất bản). Khách hàng sẽ được đổi bản in mới hoàn toàn miễn phí tại bất kỳ thời điểm nào phát hiện lỗi.</p><h3>2. Đối với Phụ kiện / Bookmark / Quà tặng</h3><p>Các phụ kiện công nghệ, đèn đọc sách, bookmark kim loại hoặc quà tặng đi kèm có thời hạn bảo hành từ <strong>1 đến 3 tháng</strong> tùy theo từng sản phẩm cụ thể nếu phát sinh lỗi kỹ thuật từ nhà sản xuất.</p>', '/images/policy-warranty.png', 1, GETDATE(), GETDATE());
END

COMMIT TRANSACTION;
PRINT 'Transaction committed successfully.';
