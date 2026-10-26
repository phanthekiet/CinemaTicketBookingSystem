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
-- 1. B?ng User (Phân quy?n Admin & Customer)
-- ==========================================
CREATE TABLE [User] (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username VARCHAR(50) UNIQUE NOT NULL,
    [Password] VARCHAR(100) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Phone VARCHAR(15),
    [Role] VARCHAR(20) NOT NULL DEFAULT 'Customer', -- 'Admin' ho?c 'Customer'
    [Status] BIT NOT NULL DEFAULT 1 -- 1: Ho?t d?ng, 0: Khóa
);

-- ==========================================
-- 2. B?ng Genre (Th? lo?i phim)
-- ==========================================
CREATE TABLE Genre (
    GenreId INT IDENTITY(1,1) PRIMARY KEY,
    GenreName NVARCHAR(50) NOT NULL
);

-- ==========================================
-- 3. B?ng Movie (Phim)
-- ==========================================
CREATE TABLE Movie (
    MovieId INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(150) NOT NULL,
    GenreId INT NOT NULL FOREIGN KEY REFERENCES Genre(GenreId),
    Duration INT NOT NULL, -- Th?i lu?ng tính b?ng phút (VD: 120)
    ReleaseDate DATE NOT NULL,
    PosterPath NVARCHAR(255),
    AgeRating VARCHAR(10) DEFAULT 'P', -- P, T13, T16, T18
    [Description] NVARCHAR(500),
    [Status] BIT NOT NULL DEFAULT 1 -- 1: Ðang chi?u, 0: Ng?ng chi?u
);

-- ==========================================
-- 4. B?ng Room (Phòng chi?u)
-- ==========================================
CREATE TABLE Room (
    RoomId INT IDENTITY(1,1) PRIMARY KEY,
    RoomName NVARCHAR(50) NOT NULL,
    TotalSeats INT NOT NULL DEFAULT 40, -- M?c d?nh 40 gh? (5 hàng A-E x 8 c?t 1-8)
    [Status] BIT NOT NULL DEFAULT 1
);

-- ==========================================
-- 5. B?ng Showtime (Su?t chi?u)
-- ==========================================
CREATE TABLE Showtime (
    ShowtimeId INT IDENTITY(1,1) PRIMARY KEY,
    MovieId INT NOT NULL FOREIGN KEY REFERENCES Movie(MovieId),
    RoomId INT NOT NULL FOREIGN KEY REFERENCES Room(RoomId),
    StartTime DATETIME NOT NULL,
    EndTime DATETIME NOT NULL,
    BasePrice DECIMAL(18,0) NOT NULL,
    [Status] BIT NOT NULL DEFAULT 1
);

-- ==========================================
-- 6. B?ng FoodAndDrink (B?p nu?c & Combo)
-- ==========================================
CREATE TABLE FoodAndDrink (
    FoodId INT IDENTITY(1,1) PRIMARY KEY,
    FoodName NVARCHAR(100) NOT NULL,
    Price DECIMAL(18,0) NOT NULL,
    Stock INT NOT NULL DEFAULT 100,
    [Status] BIT NOT NULL DEFAULT 1
);

-- ==========================================
-- 7. B?ng Booking (Ðon d?t vé / Hóa don t?ng)
-- ==========================================
CREATE TABLE Booking (
    BookingId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL FOREIGN KEY REFERENCES [User](UserId),
    ShowtimeId INT NOT NULL FOREIGN KEY REFERENCES Showtime(ShowtimeId),
    BookingDate DATETIME NOT NULL DEFAULT GETDATE(),
    TotalAmount DECIMAL(18,0) NOT NULL,
    QRCodeString VARCHAR(150) UNIQUE NOT NULL, -- Chu?i mã QR d?nh danh c?a vé
    IsCheckedIn BIT NOT NULL DEFAULT 0 -- 0: Chua soát vé, 1: Ðã soát vé
);

-- ==========================================
-- 8. B?ng Ticket (Chi ti?t t?ng gh? dã d?t)
-- ==========================================
CREATE TABLE Ticket (
    TicketId INT IDENTITY(1,1) PRIMARY KEY,
    BookingId INT NOT NULL FOREIGN KEY REFERENCES Booking(BookingId),
    ShowtimeId INT NOT NULL FOREIGN KEY REFERENCES Showtime(ShowtimeId),
    SeatCode VARCHAR(10) NOT NULL, -- VD: 'A1', 'B4', 'C5'
    SeatPrice DECIMAL(18,0) NOT NULL
);

-- ==========================================
-- 9. B?ng BookingFood (Chi ti?t b?p nu?c di kèm don vé)
-- ==========================================
CREATE TABLE BookingFood (
    BookingFoodId INT IDENTITY(1,1) PRIMARY KEY,
    BookingId INT NOT NULL FOREIGN KEY REFERENCES Booking(BookingId),
    FoodId INT NOT NULL FOREIGN KEY REFERENCES FoodAndDrink(FoodId),
    Quantity INT NOT NULL DEFAULT 1,
    UnitPrice DECIMAL(18,0) NOT NULL -- Giá t?i th?i di?m mua
);
GO

-- ======================================================================
-- CHÈN D? LI?U M?U (INSERT SAMPLE DATA) Ð? C? NHÓM CÓ DATA TEST NGAY
-- ======================================================================

-- 1. D? li?u User (G?m 1 Admin và 2 Customer)
INSERT INTO [User] (Username, [Password], FullName, Phone, [Role]) VALUES
('admin', '123', N'Qu?n Tr? Viên H? Th?ng', '0901234567', 'Admin'),
('customer1', '123', N'Nguy?n Van Khách', '0909888777', 'Customer'),
('customer2', '123', N'Tr?n Th? Mua Vé', '0912345678', 'Customer');

-- 2. D? li?u Th? lo?i
INSERT INTO Genre (GenreName) VALUES 
(N'Hành d?ng'), 
(N'Kinh d?'), 
(N'Ho?t hình'), 
(N'Tình c?m'),
(N'Trinh thám');

-- 3. D? li?u Phim
INSERT INTO Movie (Title, GenreId, Duration, ReleaseDate, PosterPath, AgeRating, [Description]) VALUES
(N'L?t M?t 8: Vòng Xoáy', 1, 125, '2026-04-30', 'latmat8.jpg', 'T16', N'Phim hành d?ng k?ch tính Vi?t Nam'),
(N'Thám T? L?ng Danh Conan: Movie 28', 5, 110, '2026-07-15', 'conan28.jpg', 'T13', N'Cu?c d?i d?u ngh?t th? c?a Conan và t? ch?c Áo Ðen'),
(N'Ám ?nh Kinh Hoàng 4', 2, 105, '2026-09-10', 'conjuring4.jpg', 'T18', N'H? so tâm linh rùng r?n ti?p theo c?a v? ch?ng Warren'),
(N'Doraemon: B?n Giao Hu?ng Ð?a C?u', 3, 115, '2026-05-25', 'doraemon.jpg', 'P', N'Hành trình âm nh?c gi?i c?u th? gi?i c?a nhóm b?n Nobita');

-- 4. D? li?u Phòng chi?u
INSERT INTO Room (RoomName, TotalSeats) VALUES 
(N'Phòng 01 (Standard)', 40), 
(N'Phòng 02 (IMAX)', 40),
(N'Phòng 03 (VIP)', 40);

-- 5. D? li?u Su?t chi?u
INSERT INTO Showtime (MovieId, RoomId, StartTime, EndTime, BasePrice) VALUES
(1, 1, '2026-10-25 18:00:00', '2026-10-25 20:20:00', 85000),
(1, 2, '2026-10-25 20:30:00', '2026-10-25 22:50:00', 100000),
(2, 2, '2026-10-25 17:30:00', '2026-10-25 19:35:00', 90000),
(3, 3, '2026-10-25 21:30:00', '2026-10-25 23:30:00', 110000);

-- 6. D? li?u B?p nu?c
INSERT INTO FoodAndDrink (FoodName, Price, Stock) VALUES
(N'Combo Solo (1 B?p Ng?t + 1 Pepsi)', 65000, 100),
(N'Combo Couple (1 B?p Phô Mai L?n + 2 Pepsi)', 95000, 80),
(N'B?p Rang Caramel (Size L)', 50000, 120),
(N'Nu?c Ng?t Coca-Cola (Size L)', 30000, 150);

-- 7. D? li?u Ðon d?t vé m?u (Ð? test s?n màn hình L?ch s? vé & Mã QR)
INSERT INTO Booking (UserId, ShowtimeId, BookingDate, TotalAmount, QRCodeString, IsCheckedIn) VALUES
(2, 1, GETDATE(), 235000, 'TICKET-2026-BK001-C4C5', 0);

-- 8. D? li?u Gh? dã bán m?u (Gh? C4, C5 c?a Su?t chi?u 1 dã có ngu?i mua)
INSERT INTO Ticket (BookingId, ShowtimeId, SeatCode, SeatPrice) VALUES
(1, 1, 'C4', 85000),
(1, 1, 'C5', 85000);

-- 9. D? li?u B?p nu?c kèm theo don vé s? 1
INSERT INTO BookingFood (BookingId, FoodId, Quantity, UnitPrice) VALUES
(1, 1, 1, 65000);
GO
