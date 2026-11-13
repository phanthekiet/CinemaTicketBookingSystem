USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'CinemaManagementDB')
BEGIN
    ALTER DATABASE CinemaManagementDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE CinemaManagementDB;
END
GO

CREATE DATABASE CinemaManagementDB;
GO
USE CinemaManagementDB;
GO

-- ==========================================
-- 1. Bảng User (Phân quyền Admin & Customer & Staff)
-- ==========================================
CREATE TABLE [User] (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username VARCHAR(50) UNIQUE NOT NULL,
    [Password] VARCHAR(100) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Phone VARCHAR(15),
    [Role] VARCHAR(20) NOT NULL DEFAULT 'Customer', -- 'Admin', 'Staff' hoặc 'Customer'
    [Status] BIT NOT NULL DEFAULT 1 -- 1: Hoạt động, 0: Khóa
);

-- ==========================================
-- 2. Bảng Genre (Thể loại phim)
-- ==========================================
CREATE TABLE Genre (
    GenreId INT IDENTITY(1,1) PRIMARY KEY,
    GenreName NVARCHAR(50) NOT NULL
);

-- ==========================================
-- 3. Bảng Movie (Phim)
-- ==========================================
CREATE TABLE Movie (
    MovieId INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(150) NOT NULL,
    GenreId INT NOT NULL FOREIGN KEY REFERENCES Genre(GenreId),
    Duration INT NOT NULL, -- Thời lượng tính bằng phút (VD: 120)
    ReleaseDate DATE NOT NULL,
    PosterPath NVARCHAR(255),
    AgeRating VARCHAR(10) DEFAULT 'P', -- P, T13, T16, T18
    [Description] NVARCHAR(500),
    [Status] BIT NOT NULL DEFAULT 1 -- 1: Đang chiếu, 0: Ngừng chiếu
);

-- ==========================================
-- 4. Bảng Room (Phòng chiếu)
-- ==========================================
CREATE TABLE Room (
    RoomId INT IDENTITY(1,1) PRIMARY KEY,
    RoomName NVARCHAR(50) NOT NULL,
    TotalSeats INT NOT NULL DEFAULT 40, -- Mặc định 40 ghế (5 hàng A-E x 8 cột 1-8)
    [Status] BIT NOT NULL DEFAULT 1
);

-- ==========================================
-- 5. Bảng Showtime (Suất chiếu)
-- ==========================================
CREATE TABLE Showtime (
    ShowtimeId INT IDENTITY(1,1) PRIMARY KEY,
    MovieId INT NOT NULL FOREIGN KEY REFERENCES Movie(MovieId),
    RoomId INT NOT NULL FOREIGN KEY REFERENCES Room(RoomId),
    StartTime DATETIME NOT NULL,
    EndTime DATETIME NOT NULL,
    BasePrice DECIMAL(18,0) NOT NULL DEFAULT 75000, -- Giá vé cơ bản
    [Status] BIT NOT NULL DEFAULT 1 -- 1: Hoạt động, 0: Đã hủy
);

-- ==========================================
-- 6. Bảng FoodAndDrink (Bắp nước & Combo)
-- ==========================================
CREATE TABLE FoodAndDrink (
    FoodId INT IDENTITY(1,1) PRIMARY KEY,
    FoodName NVARCHAR(100) NOT NULL,
    Price DECIMAL(18,0) NOT NULL,
    Stock INT NOT NULL DEFAULT 100,
    [Status] BIT NOT NULL DEFAULT 1
);

-- ==========================================
-- 7. Bảng Booking (Đơn đặt vé / Hóa đơn tổng)
-- ==========================================
CREATE TABLE Booking (
    BookingId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL FOREIGN KEY REFERENCES [User](UserId),
    ShowtimeId INT NOT NULL FOREIGN KEY REFERENCES Showtime(ShowtimeId),
    BookingDate DATETIME NOT NULL DEFAULT GETDATE(),
    TotalAmount DECIMAL(18,0) NOT NULL,
    QRCodeString VARCHAR(150) UNIQUE NOT NULL,
    PaymentMethod NVARCHAR(50) DEFAULT 'VNPAY', -- Chuỗi mã QR định danh của vé
    IsCheckedIn BIT NOT NULL DEFAULT 0 -- 0: Chưa soát vé, 1: Đã soát vé
);

-- ==========================================
-- 8. Bảng Ticket (Chi tiết từng ghế đã đặt)
-- ==========================================
CREATE TABLE Ticket (
    TicketId INT IDENTITY(1,1) PRIMARY KEY,
    BookingId INT NOT NULL FOREIGN KEY REFERENCES Booking(BookingId),
    ShowtimeId INT NOT NULL FOREIGN KEY REFERENCES Showtime(ShowtimeId),
    SeatCode VARCHAR(10) NOT NULL, -- VD: 'A1', 'B4', 'C5'
    SeatPrice DECIMAL(18,0) NOT NULL
);

-- Ràng buộc duy nhất chống Race Condition (Double-Booking)
CREATE UNIQUE NONCLUSTERED INDEX UQ_Ticket_Showtime_Seat ON Ticket(ShowtimeId, SeatCode);

-- ==========================================
-- 9. Bảng BookingFood (Chi tiết bắp nước đi kèm đơn vé)
-- ==========================================
CREATE TABLE BookingFood (
    BookingFoodId INT IDENTITY(1,1) PRIMARY KEY,
    BookingId INT NOT NULL FOREIGN KEY REFERENCES Booking(BookingId),
    FoodId INT NOT NULL FOREIGN KEY REFERENCES FoodAndDrink(FoodId),
    Quantity INT NOT NULL DEFAULT 1,
    UnitPrice DECIMAL(18,0) NOT NULL -- Giá tại thời điểm mua
);
GO

-- ======================================================================
-- CHÈN DỮ LIỆU MẪU (INSERT SAMPLE DATA)
-- ======================================================================

-- 1. Dữ liệu User
INSERT INTO [User] (Username, [Password], FullName, Phone, [Role]) VALUES
('admin', '123', N'Quản Trị Viên Hệ Thống', '0901234567', 'Admin'),
('staff1', '123', N'Nguyễn Văn Soát Vé', '0903334455', 'Staff'),
('customer1', '123', N'Nguyễn Văn Khách', '0909888777', 'Customer');

-- 2. Dữ liệu Thể loại
INSERT INTO Genre (GenreName) VALUES 
(N'Hành động'), 
(N'Kinh dị'), 
(N'Hoạt hình'), 
(N'Tình cảm'),
(N'Trinh thám');

-- 3. Dữ liệu Phim
INSERT INTO Movie (Title, GenreId, Duration, ReleaseDate, PosterPath, AgeRating, [Description]) VALUES
(N'Lật Mặt 8: Vòng Tay Nắng', 1, 125, '2026-04-30', 'latmat8.jpg', 'T16', N'Phim hành động kịch tính Việt Nam'),
(N'Thám Tử Lừng Danh Conan: Movie 28', 5, 110, '2026-07-15', 'conan28.jpg', 'T13', N'Cuộc đối đầu nghẹt thở của Conan và tổ chức Áo Đen'),
(N'Ám Ảnh Kinh Hoàng 4', 2, 105, '2026-09-10', 'conjuring4.jpg', 'T18', N'Hồ sơ tâm linh rùng rợn tiếp theo của vợ chồng Warren'),
(N'Doraemon: Bản Giao Hưởng Địa Cầu', 3, 115, '2026-05-25', 'doraemon.jpg', 'P', N'Hành trình âm nhạc giải cứu thế giới của nhóm bạn Nobita'),
(N'Chị Chị Em Em 3', 2, 110, '2026-10-30', 'chichiemem3.jpg', 'T18', N'Drama kịch tính hấp dẫn'),
(N'Obsession', 2, 120, '2026-11-02', 'obsession.jpg', 'T18', N'Phim kinh dị hồi hộp kịch tính'),
(N'Chị Xíu Anh Híu', 4, 105, '2026-10-29', 'chixiu.jpg', 'T13', N'Hài hước tâm lý gia đình');

-- 4. Dữ liệu Phòng chiếu
INSERT INTO Room (RoomName, TotalSeats) VALUES 
(N'Phòng 01 (Standard)', 40), 
(N'Phòng 02 (IMAX)', 40),
(N'Phòng 03 (VIP)', 40);

-- 5. Dữ liệu Bắp nước
INSERT INTO FoodAndDrink (FoodName, Price, Stock) VALUES
(N'Combo Solo (1 Bắp Ngọt + 1 Pepsi)', 65000, 100),
(N'Combo Couple (1 Bắp Phô Mai Lớn + 2 Pepsi)', 95000, 80),
(N'Bắp Rang Caramel (Size L)', 50000, 120),
(N'Nước Ngọt Coca-Cola (Size L)', 30000, 150);
GO
