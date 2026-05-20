USE master;
GO

IF EXISTS (SELECT * FROM sys.databases WHERE name = 'Electronic_Shop')
BEGIN
    ALTER DATABASE Electronic_Shop SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE Electronic_Shop;
END
GO

CREATE DATABASE Electronic_Shop;
GO

USE Electronic_Shop;
GO

-----------------------------------------------------------
-- NHÓM 1: HỆ THỐNG TÀI KHOẢN
-----------------------------------------------------------

CREATE TABLE [dbo].[Roles](
    [RoleID] [int] NOT NULL,
    [RoleName] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([RoleID] ASC)
)
GO

CREATE TABLE [dbo].[Accounts](
    [AccountID] [nvarchar](20) NOT NULL,
    [Password] [nvarchar](255) NOT NULL, 
    [FullName] [nvarchar](50) NOT NULL,
    [Email] [nvarchar](50) NOT NULL,
    [PhoneNumber] [nvarchar](24) NULL,
    [Address] [nvarchar](100) NULL,
    [IsActive] [bit] NOT NULL DEFAULT (1),
    [RoleID] [int] NOT NULL DEFAULT (1), -- 0: Admin, 1: Customer
 CONSTRAINT [PK_Accounts] PRIMARY KEY CLUSTERED ([AccountID] ASC)
)
GO

-----------------------------------------------------------
-- NHÓM 2: DANH MỤC & SẢN PHẨM 
-----------------------------------------------------------

CREATE TABLE [dbo].[Categories](
    [CategoryID] [int] IDENTITY(1,1) NOT NULL,
    [CategoryName] [nvarchar](50) NOT NULL,
    [Description] [nvarchar](max) NULL,
    [Status] [bit] NOT NULL DEFAULT (1), -- Active/Inactive
 CONSTRAINT [PK_Categories] PRIMARY KEY CLUSTERED ([CategoryID] ASC)
)
GO

CREATE TABLE [dbo].[Products](
    [ProductID] [int] IDENTITY(1,1) NOT NULL,
    [ProductName] [nvarchar](200) NOT NULL,
    [CategoryID] [int] NOT NULL,
    [UnitPrice] [decimal](18, 2) NOT NULL DEFAULT (0),
    [ImageURL] [nvarchar](255) NULL,
    [Description] [nvarchar](max) NULL,
    [StockQuantity] [int] NOT NULL DEFAULT (0),
 CONSTRAINT [PK_Products] PRIMARY KEY CLUSTERED ([ProductID] ASC)
)
GO

-----------------------------------------------------------
-- NHÓM 3: GIỎ HÀNG 
-----------------------------------------------------------

CREATE TABLE [dbo].[Carts](
    [CartID] [nvarchar](50) NOT NULL, 
    [AccountID] [nvarchar](20) NULL,   
 CONSTRAINT [PK_Carts] PRIMARY KEY CLUSTERED ([CartID] ASC)
)
GO

CREATE TABLE [dbo].[CartItems](
    [CartItemID] [int] IDENTITY(1,1) NOT NULL,
    [CartID] [nvarchar](50) NOT NULL,
    [ProductID] [int] NOT NULL,
    [Quantity] [int] NOT NULL DEFAULT (1),
 CONSTRAINT [PK_CartItems] PRIMARY KEY CLUSTERED ([CartItemID] ASC)
)
GO

-----------------------------------------------------------
-- NHÓM 4: ĐƠN HÀNG
-----------------------------------------------------------

CREATE TABLE [dbo].[Statuses](
    [StatusID] [int] NOT NULL,
    [StatusName] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_Statuses] PRIMARY KEY CLUSTERED ([StatusID] ASC)
)
GO

CREATE TABLE [dbo].[Orders](
    [OrderID] [int] IDENTITY(1,1) NOT NULL,
    [AccountID] [nvarchar](20) NULL, 
    [OrderDate] [datetime] NOT NULL DEFAULT (getdate()),  
    [FullName] [nvarchar](50) NOT NULL,
    [Address] [nvarchar](150) NOT NULL,
    [TownCity] [nvarchar](100) NOT NULL,
    [PhoneNumber] [nvarchar](24) NOT NULL, 
    [Email] [nvarchar](50) NULL,
    [OrderNotes] [nvarchar](max) NULL,
    [PaymentMethod] [nvarchar](50) NOT NULL DEFAULT (N'COD'),
    [TotalAmount] [decimal](18, 2) NOT NULL DEFAULT (0),
    [StatusID] [int] NOT NULL DEFAULT (0), -- 0: Mới, 1: Đang giao, 2: Đã giao, 3: Đã hủy
 CONSTRAINT [PK_Orders] PRIMARY KEY CLUSTERED ([OrderID] ASC)
)
GO

CREATE TABLE [dbo].[OrderDetails](
    [OrderDetailID] [int] IDENTITY(1,1) NOT NULL,
    [OrderID] [int] NOT NULL,
    [ProductID] [int] NOT NULL,
    [UnitPrice] [decimal](18, 2) NOT NULL,
    [Quantity] [int] NOT NULL,
 CONSTRAINT [PK_OrderDetails] PRIMARY KEY CLUSTERED ([OrderDetailID] ASC)
)
GO

-----------------------------------------------------------
-- RÀNG BUỘC KHÓA NGOẠI (FOREIGN KEYS)
-----------------------------------------------------------

ALTER TABLE [dbo].[Accounts] ADD CONSTRAINT [FK_Accounts_Roles] FOREIGN KEY([RoleID]) REFERENCES [dbo].[Roles] ([RoleID])
GO

ALTER TABLE [dbo].[Products] ADD CONSTRAINT [FK_Products_Categories] FOREIGN KEY([CategoryID]) REFERENCES [dbo].[Categories] ([CategoryID])
GO

ALTER TABLE [dbo].[Carts] ADD CONSTRAINT [FK_Carts_Accounts] FOREIGN KEY([AccountID]) REFERENCES [dbo].[Accounts] ([AccountID])
GO
ALTER TABLE [dbo].[CartItems] ADD CONSTRAINT [FK_CartItems_Carts] FOREIGN KEY([CartID]) REFERENCES [dbo].[Carts] ([CartID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[CartItems] ADD CONSTRAINT [FK_CartItems_Products] FOREIGN KEY([ProductID]) REFERENCES [dbo].[Products] ([ProductID])
GO

ALTER TABLE [dbo].[Orders] ADD CONSTRAINT [FK_Orders_Accounts] FOREIGN KEY([AccountID]) REFERENCES [dbo].[Accounts] ([AccountID])
GO
ALTER TABLE [dbo].[Orders] ADD CONSTRAINT [FK_Orders_Statuses] FOREIGN KEY([StatusID]) REFERENCES [dbo].[Statuses] ([StatusID])
GO
ALTER TABLE [dbo].[OrderDetails] ADD CONSTRAINT [FK_OrderDetails_Orders] FOREIGN KEY([OrderID]) REFERENCES [dbo].[Orders] ([OrderID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[OrderDetails] ADD CONSTRAINT [FK_OrderDetails_Products] FOREIGN KEY([ProductID]) REFERENCES [dbo].[Products] ([ProductID])
GO