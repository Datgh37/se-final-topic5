USE Electronic_Shop;
GO

-----------------------------------------------------------
-- 1. TÀI KHOẢN & PHÂN QUYỀN
-----------------------------------------------------------
INSERT INTO [dbo].[Roles] (RoleID, RoleName) VALUES 
(0, N'Quản trị viên'), 
(1, N'Khách hàng');
GO

INSERT INTO [dbo].[Accounts] (AccountID, Password, FullName, Email, PhoneNumber, Address, IsActive, RoleID) VALUES
(N'admin', N'admin123', N'Admin', N'admin@shop.com', N'0123456789', N'Biên Hòa, Đồng Nai', 1, 0),
(N'customer1', N'cust123', N'Nguyễn Văn A', N'nva@gmail.com', N'0987654321', N'TP. Hồ Chí Minh', 1, 1),
(N'customer2', N'cust123', N'Trần Thị B', N'ttb@gmail.com', N'0912345678', N'Đà Nẵng', 1, 1);
GO

-----------------------------------------------------------
-- 2. TRẠNG THÁI ĐƠN HÀNG
-----------------------------------------------------------
INSERT INTO [dbo].[Statuses] (StatusID, StatusName) VALUES
(0, N'Chờ xác nhận'),
(1, N'Đang giao'),
(2, N'Đã giao'),
(3, N'Đã hủy');
GO

-----------------------------------------------------------
-- 3. DANH MỤC
-----------------------------------------------------------
SET IDENTITY_INSERT [dbo].[Categories] ON;
INSERT INTO [dbo].[Categories] (CategoryID, CategoryName, Description, Status) VALUES 
(1, N'Laptop', N'Máy tính xách tay các hãng', 1),
(2, N'Điện thoại', N'Điện thoại thông minh', 1);
SET IDENTITY_INSERT [dbo].[Categories] OFF;
GO

-----------------------------------------------------------
-- 4. SẢN PHẨM
-----------------------------------------------------------
SET IDENTITY_INSERT [dbo].[Products] ON;
INSERT INTO [dbo].[Products] (ProductID, ProductName, CategoryID, UnitPrice, ImageURL, Description, StockQuantity) VALUES
(1, N'MacBook Air M2 8GB/256GB', 1, 24990000, N'~/images/Products/Laptop/Macbook_Air_M2_256GB.jpg', N'Hãng: Apple | Nhà cung cấp: Thế Giới Di Động', 1),
(2, N'MacBook Pro M3 16GB/512GB', 1, 45990000, N'~/images/Products/Laptop/Macbook_Pro_M3_512GB.jpg', N'Hãng: Apple | Nhà cung cấp: FPT Shop', 5),
(3, N'MacBook Air M3 16GB/512GB', 1, 32990000, N'~/images/Products/Laptop/Macbook_Air_M3_512GB.jpg', N'Hãng: Apple | Nhà cung cấp: CellphoneS', 5),
(4, N'Dell XPS 13 Plus 9320', 1, 41990000, N'~/images/Products/Laptop/Dell_XPS13_Plus9320.jpg', N'Hãng: Dell | Nhà cung cấp: Thế Giới Di Động', 5),
(5, N'Dell Inspiron 14 5430', 1, 16990000, N'~/images/Products/Laptop/Dell_Inspiron_14_5430.png', N'Hãng: Dell | Nhà cung cấp: FPT Shop', 5),
(6, N'Dell Vostro 3430', 1, 12490000, N'~/images/Products/Laptop/Dell_Vostro_3430.jpg', N'Hãng: Dell | Nhà cung cấp: Viettel Store', 0),
(7, N'HP Pavilion 14-dv2073TU', 1, 15490000, N'~/images/Products/Laptop/HP_Pavilion_14_dv2073TU.png', N'Hãng: HP | Nhà cung cấp: Thế Giới Di Động', 5),
(8, N'HP Envy x360 14', 1, 22990000, N'~/images/Products/Laptop/HP_Envy_x360_14.jpg', N'Hãng: HP | Nhà cung cấp: CellphoneS', 5),
(9, N'HP Omen 16', 1, 35990000, N'~/images/Products/Laptop/HP_Omen_16.png', N'Hãng: HP | Nhà cung cấp: FPT Shop', 5),
(5, N'Asus Zenbook 14 OLED', 1, 24990000, N'~/images/Products/Laptop/Asus_Zenbook14_OLED.jpg', N'Hãng: Asus | Nhà cung cấp: Thế Giới Di Động', 20),
(11, N'Asus Vivobook Go 14', 1, 5490000, N'~/images/Products/Laptop/Asus_Vivobook_Go14.jpg', N'Hãng: Asus | Nhà cung cấp: Viettel Store', 20),
(12, N'Asus ROG Zephyrus G14', 1, 42990000, N'~/images/Products/Laptop/Asus_ROG_Zephyrus_G14.jpg', N'Hãng: Asus | Nhà cung cấp: CellphoneS', 5),
(13, N'Lenovo ThinkPad X1 Carbon Gen 11', 1, 48990000, N'~/images/Products/Laptop/Lenovo_ThinkPad_X1_Carbon_Gen11.jpg', N'Hãng: Lenovo | Nhà cung cấp: FPT Shop', 5),
(14, N'Lenovo IdeaPad Slim 3', 1, 11990000, N'~/images/Products/Laptop/Lenovo_IdeaPad_Slim3.png', N'Hãng: Lenovo | Nhà cung cấp: Thế Giới Di Động', 5),
(15, N'Lenovo Legion 5 Pro', 1, 36990000, N'~/images/Products/Laptop/Lenovo_Legion_5Pro.jpg', N'Hãng: Lenovo | Nhà cung cấp: CellphoneS', 5),
(16, N'Acer Aspire 3 A315', 1, 9990000, N'~/images/Products/Laptop/Acer_Aspire3_A315.png', N'Hãng: Acer | Nhà cung cấp: Viettel Store', 5),
(17, N'Acer Nitro V 15', 1, 21490000, N'~/images/Products/Laptop/Acer_Nitro_V15.jpg', N'Hãng: Acer | Nhà cung cấp: FPT Shop', 5),
(18, N'Acer Predator Helios Neo 16', 1, 33990000, N'~/images/Products/Laptop/Acer_Predator_Helios_Neo16.jpg', N'Hãng: Acer | Nhà cung cấp: Thế Giới Di Động', 5),
(19, N'MSI Modern 14', 1, 11490000, N'~/images/Products/Laptop/MSI_Modern_14.jpg', N'Hãng: MSI | Nhà cung cấp: CellphoneS', 5),
(20, N'MSI Cyborg 15', 1, 20990000, N'~/images/Products/Laptop/MSI_Cyborg_15.png', N'Hãng: MSI | Nhà cung cấp: FPT Shop', 5),
(21, N'iPhone 15 Pro Max 256GB', 2, 29990000, N'~/images/Products/Phones/iphone15_ProMax_256gb.jpg', N'Hãng: Apple | Nhà cung cấp: Thế Giới Di Động', 5),
(22, N'iPhone 15 128GB', 2, 19990000, N'~/images/Products/Phones/iphone15_128GB.jpg', N'Hãng: Apple | Nhà cung cấp: FPT Shop', 15),
(23, N'iPhone 14 Pro Max 128GB', 2, 26490000, N'~/images/Products/Phones/iphone14_ProMax_128GB.jpg', N'Hãng: Apple | Nhà cung cấp: CellphoneS', 5),
(24, N'iPhone 13 128GB', 2, 13790000, N'~/images/Products/Phones/iphone13_128GB.jpg', N'Hãng: Apple | Nhà cung cấp: Viettel Store', 5),
(25, N'Samsung Galaxy S24 Ultra', 2, 28990000, N'~/images/Products/Phones/Samsung_Galaxy_S24_Ultra.webp', N'Hãng: Samsung | Nhà cung cấp: Thế Giới Di Động', 5),
(26, N'Samsung Galaxy S24 Plus', 2, 22990000, N'~/images/Products/Phones/Samsung_Galaxy_S24_Plus.jpg', N'Hãng: Samsung | Nhà cung cấp: FPT Shop', 5),
(27, N'Samsung Galaxy A55 5G', 2, 11490000, N'~/images/Products/Phones/Samsung_Galaxy_A55g.jpg', N'Hãng: Samsung | Nhà cung cấp: CellphoneS', 5),
(28, N'Samsung Galaxy Z Fold5', 2, 32990000, N'~/images/Products/Phones/Samsung_Galaxy_S_Fold5.webp', N'Hãng: Samsung | Nhà cung cấp: Viettel Store', 5),
(29, N'Oppo Reno11 F 5G', 2, 8990000, N'~/images/Products/Phones/Oppo_Reno11_F_5G.jpg', N'Hãng: Oppo | Nhà cung cấp: Thế Giới Di Động', 5),
(30, N'Oppo Find N3 Flip', 2, 22990000, N'~/images/Products/Phones/Oppo_Find_N3_Flip.webp', N'Hãng: Oppo | Nhà cung cấp: FPT Shop', 5),
(31, N'Oppo A78', 2, 6490000, N'~/images/Products/Phones/Oppo_A78.jpg', N'Hãng: Oppo | Nhà cung cấp: CellphoneS', 5),
(32, N'Xiaomi 14 Ultra', 2, 29990000, N'~/images/Products/Phones/Xiaomi_14_Ultra.jpg', N'Hãng: Xiaomi | Nhà cung cấp: Thế Giới Di Động', 5),
(33, N'Xiaomi Redmi Note 13 Pro', 2, 6990000, N'~/images/Products/Phones/Xiaomi_Redmi_Note_13Pro.jpg', N'Hãng: Xiaomi | Nhà cung cấp: FPT Shop', 5),
(34, N'Xiaomi Poco X6 Pro', 2, 9490000, N'~/images/Products/Phones/Xiaomi_POCO_X6_Pro.png', N'Hãng: Xiaomi | Nhà cung cấp: CellphoneS', 5),
(35, N'Vivo V30 5G', 2, 12490000, N'~/images/Products/Phones/Vivo_V30_5G.jpg', N'Hãng: Vivo | Nhà cung cấp: Thế Giới Di Động', 5),
(36, N'Vivo Y50', 2, 6690000, N'~/images/Products/Phones/Vivo_Y50.jpg', N'Hãng: Vivo | Nhà cung cấp: Viettel Store', 5),
(37, N'Realme C67', 2, 5290000, N'~/images/Products/Phones/Realme_C67.webp', N'Hãng: Realme | Nhà cung cấp: FPT Shop', 5),
(38, N'Realme 11', 2, 6290000, N'~/images/Products/Phones/Realme_11.jpg', N'Hãng: Realme | Nhà cung cấp: CellphoneS', 5),
(39, N'Asus ROG Phone 8', 2, 24990000, N'~/images/Products/Phones/Asus_ROG_Phone8.jpg', N'Hãng: Asus | Nhà cung cấp: CellphoneS', 5),
(40, N'Sony Xperia 1 V', 2, 27990000, N'~/images/Products/Phones/Sony_Xperia_1V.png', N'Hãng: Sony | Nhà cung cấp: Sony Store Việt Nam', 5);
SET IDENTITY_INSERT [dbo].[Products] OFF;
GO
