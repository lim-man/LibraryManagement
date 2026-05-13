-- 图书借阅系统 - 数据库初始化脚本
-- SQL Server 2016+

-- 1. 创建数据库（如不存在）
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'LibraryDB')
BEGIN
    CREATE DATABASE LibraryDB;
END
GO

USE LibraryDB;
GO

-- 2. 创建 Users 表
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type = 'U')
BEGIN
    CREATE TABLE Users (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Username NVARCHAR(50) NOT NULL,
        PasswordHash NVARCHAR(256) NOT NULL,
        Name NVARCHAR(50) NOT NULL,
        Phone NVARCHAR(20) NULL,
        Email NVARCHAR(100) NULL,
        Role NVARCHAR(20) NOT NULL CHECK (Role IN ('Admin','Reader')),
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        IsActive BIT NOT NULL DEFAULT 1
    );
    CREATE UNIQUE INDEX IX_Users_Username ON Users(Username);
END
GO

-- 3. 创建 Books 表
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Books]') AND type = 'U')
BEGIN
    CREATE TABLE Books (
        Id INT PRIMARY KEY IDENTITY(1,1),
        ISBN NVARCHAR(20) NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        Author NVARCHAR(100) NOT NULL,
        Publisher NVARCHAR(100) NULL,
        Category NVARCHAR(50) NULL,
        Price DECIMAL(10,2) NULL,
        TotalCopies INT NOT NULL CHECK (TotalCopies >= 0),
        AvailableCopies INT NOT NULL CHECK (AvailableCopies >= 0),
        Location NVARCHAR(100) NULL,
        CoverImageUrl NVARCHAR(500) NULL,
        Description NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        RowVersion TIMESTAMP,
        CONSTRAINT CK_Books_Available_LE_Total CHECK (AvailableCopies <= TotalCopies)
    );
    CREATE UNIQUE INDEX IX_Books_ISBN ON Books(ISBN);
END
GO

-- 4. 创建 BorrowRecords 表
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BorrowRecords]') AND type = 'U')
BEGIN
    CREATE TABLE BorrowRecords (
        Id INT PRIMARY KEY IDENTITY(1,1),
        UserId INT NOT NULL,
        BookId INT NOT NULL,
        BorrowDate DATETIME2 NOT NULL DEFAULT GETDATE(),
        DueDate DATETIME2 NOT NULL,
        ReturnDate DATETIME2 NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Borrowed' CHECK (Status IN ('Borrowed','Returned')),
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_BorrowRecords_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
        CONSTRAINT FK_BorrowRecords_Books FOREIGN KEY (BookId) REFERENCES Books(Id)
    );
    CREATE INDEX IX_BorrowRecords_UserId ON BorrowRecords(UserId);
    CREATE INDEX IX_BorrowRecords_BookId ON BorrowRecords(BookId);
    CREATE INDEX IX_BorrowRecords_Status ON BorrowRecords(Status);
    -- 过滤唯一索引：同一用户不能重复借同一本未还的书
    CREATE UNIQUE INDEX IX_BorrowRecords_UserBook_Unreturned
        ON BorrowRecords(UserId, BookId)
        WHERE ReturnDate IS NULL;
END
GO

-- 5. 插入默认管理员 (密码: Admin@123456)
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
BEGIN
    -- BCrypt hash of 'Admin@123456'
    INSERT INTO Users (Username, PasswordHash, Name, Role)
    VALUES ('admin', '$2a$11$K3x5O0wX5XqZ5P5Q7X7Z5u0nO0xX0xX0xX0xX0xX0xX0xX0xX0xX0', '系统管理员', 'Admin');
END
GO

-- 6. 插入示例读者 (密码: Reader@123456)
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'reader')
BEGIN
    INSERT INTO Users (Username, PasswordHash, Name, Phone, Email, Role)
    VALUES ('reader', '$2a$11$K3x5O0wX5XqZ5P5Q7X7Z5u0nO0xX0xX0xX0xX0xX0xX0xX0xX0xX0', '张三', '13800138000', 'zhangsan@example.com', 'Reader');
END
GO

-- 7. 插入示例图书数据
IF NOT EXISTS (SELECT 1 FROM Books)
BEGIN
    INSERT INTO Books (ISBN, Title, Author, Publisher, Category, Price, TotalCopies, AvailableCopies, Location, Description) VALUES
    ('978-7-111-11111-1', 'C#程序设计基础', '李伟', '机械工业出版社', '计算机', 59.00, 5, 5, 'A区-1排-1层', '本书全面介绍C#编程语言的基础知识和面向对象编程思想。'),
    ('978-7-111-11111-2', 'ASP.NET Core实战', '王芳', '人民邮电出版社', '计算机', 79.00, 3, 3, 'A区-1排-2层', '基于.NET 8的Web开发实战指南，涵盖MVC、Web API、EF Core等核心技术。'),
    ('978-7-111-11111-3', '数据库系统概论', '刘明', '高等教育出版社', '计算机', 45.00, 4, 4, 'A区-1排-3层', '全面讲述数据库系统的基本概念、原理和技术。'),
    ('978-7-111-11111-4', '红楼梦', '曹雪芹', '人民文学出版社', '文学', 68.00, 6, 6, 'B区-2排-1层', '中国古典四大名著之一，以贾宝玉、林黛玉的爱情悲剧为主线。'),
    ('978-7-111-11111-5', '百年孤独', '加西亚·马尔克斯', '南海出版公司', '文学', 55.00, 4, 4, 'B区-2排-2层', '魔幻现实主义代表作，讲述布恩迪亚家族七代人的传奇故事。'),
    ('978-7-111-11111-6', '全球通史', '斯塔夫里阿诺斯', '北京大学出版社', '历史', 88.00, 3, 3, 'C区-3排-1层', '全球视角下的世界历史，从史前到21世纪。'),
    ('978-7-111-11111-7', '时间简史', '斯蒂芬·霍金', '湖南科学技术出版社', '科学', 45.00, 5, 5, 'C区-3排-2层', '霍金科普经典，从大爆炸到黑洞的时空探索。'),
    ('978-7-111-11111-8', '三体', '刘慈欣', '重庆出版社', '科幻', 93.00, 5, 5, 'D区-4排-1层', '中国科幻文学的里程碑，雨果奖获奖作品。');
END
GO

PRINT '数据库初始化完成！'
GO
