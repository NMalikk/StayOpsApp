USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = 'StayOpsDB')
BEGIN
    ALTER DATABASE StayOpsDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE StayOpsDB;
END
GO

CREATE DATABASE StayOpsDB;
GO

USE StayOpsDB;
GO

CREATE TABLE RoomTypes (
    RoomTypeID INT IDENTITY(1,1) PRIMARY KEY,
    TypeName NVARCHAR(50) NOT NULL,
    BasePrice DECIMAL(10,2) NOT NULL
);
GO

CREATE TABLE Staff (
    StaffID INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE, 
    PasswordHash NVARCHAR(MAX) NOT NULL,   
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Role NVARCHAR(50) NOT NULL CHECK (Role IN ('Manager', 'FrontDesk')) 
);
GO

CREATE TABLE Rooms (
    RoomID INT IDENTITY(1,1) PRIMARY KEY,
    RoomNumber NVARCHAR(10) NOT NULL UNIQUE, 
    Status NVARCHAR(50) NOT NULL CHECK (Status IN ('Available', 'Occupied', 'Maintenance')), 
    RoomTypeID INT NOT NULL,
    CONSTRAINT FK_Rooms_RoomTypes FOREIGN KEY (RoomTypeID) REFERENCES RoomTypes(RoomTypeID)
);
GO

CREATE TABLE Guests (
    GuestID INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE, 
    Phone NVARCHAR(20) NULL,
    TotalSpent DECIMAL(10,2) NOT NULL DEFAULT 0.00,
);
GO

CREATE TABLE Reservations (
    ReservationID INT IDENTITY(1,1) PRIMARY KEY,
    CheckInDate DATE NOT NULL,
    CheckOutDate DATE NOT NULL,
    TotalAmount DECIMAL(10,2) NOT NULL,
    GuestID INT NOT NULL,
    RoomID INT NOT NULL,
    StaffID INT NOT NULL,
    CONSTRAINT FK_Reservations_Guests FOREIGN KEY (GuestID) REFERENCES Guests(GuestID),
    CONSTRAINT FK_Reservations_Rooms FOREIGN KEY (RoomID) REFERENCES Rooms(RoomID),
    CONSTRAINT FK_Reservations_Staff FOREIGN KEY (StaffID) REFERENCES Staff(StaffID),
    CONSTRAINT CK_Dates CHECK (CheckOutDate > CheckInDate) 
);
GO

CREATE TABLE AuditLogs (
    LogID INT IDENTITY(1,1) PRIMARY KEY,
    TableName NVARCHAR(50),
    ActionType NVARCHAR(20), 
    RecordID INT,
    ChangedByStaffID INT , 
    ChangeDate DATETIME DEFAULT GETDATE(),
    Details NVARCHAR(MAX)

    CONSTRAINT FK_AuditLogs_Staff FOREIGN KEY (ChangedByStaffID) REFERENCES Staff(StaffID)
);
GO

INSERT INTO RoomTypes (TypeName, BasePrice) VALUES 
('Single Standard', 100.00),
('Double Standard', 150.00),
('Executive Suite', 250.00),
('Penthouse', 500.00);

INSERT INTO Staff (Username, PasswordHash, FirstName, LastName, Role) VALUES
('mashraf', 'hash123', 'Muhammad', 'Ashraf', 'Manager'),
('saqeel', 'hash123', 'Shaheer', 'Aqeel', 'FrontDesk'),
('smalik', 'hash123', 'Sarim', 'Malik', 'FrontDesk'),
('mnafees', 'hash123', 'Muhammad', 'Nafees', 'Manager'),
('admin', 'hash123', 'System', 'Admin', 'Manager');

DECLARE @i INT = 1;
WHILE @i <= 50
BEGIN
    INSERT INTO Rooms (RoomNumber, Status, RoomTypeID)
    VALUES (
        CAST((100 + @i) AS NVARCHAR(10)), 
        'Available', 
        ABS(CHECKSUM(NEWID()) % 4) + 1 
    );
    SET @i = @i + 1;
END
GO


PRINT 'Starting Massive Data Generation... This may take a few minutes.';

SET NOCOUNT ON; 

DECLARE @g INT = 1;
BEGIN TRANSACTION;
WHILE @g <= 10000
BEGIN
    INSERT INTO Guests (FirstName, LastName, Email, Phone)
    VALUES (
        'Guest' + CAST(@g AS NVARCHAR(20)),
        'Lastname' + CAST(@g AS NVARCHAR(20)),
        'user' + CAST(@g AS NVARCHAR(20)) + '@example.com',
        '555-01' + RIGHT('00' + CAST(@g % 100 AS NVARCHAR(2)), 2)
    );
    SET @g = @g + 1;
END
COMMIT TRANSACTION;
PRINT 'Guests Generated.';

PRINT 'Starting Clean Reservation Seeding...';

SET NOCOUNT ON;

DECLARE @AttemptCount INT = 1;
DECLARE @MaxAttempts INT = 10000; 
DECLARE @ValidBookings INT = 0;

WHILE @AttemptCount <= @MaxAttempts
BEGIN
    DECLARE @RandGuestID INT = ABS(CHECKSUM(NEWID()) % 10000) + 1;
    DECLARE @RandRoomID INT = ABS(CHECKSUM(NEWID()) % 50) + 1;
    DECLARE @RandStaffID INT = ABS(CHECKSUM(NEWID()) % 5) + 1;
    DECLARE @RandDays INT = ABS(CHECKSUM(NEWID()) % 365);
    DECLARE @StayDuration INT = ABS(CHECKSUM(NEWID()) % 7) + 1; 

    DECLARE @CheckIn DATE = DATEADD(DAY, -@RandDays, GETDATE());
    DECLARE @CheckOut DATE = DATEADD(DAY, @StayDuration, @CheckIn);

    DECLARE @Price DECIMAL(10,2) = @StayDuration * 150.00;

    IF NOT EXISTS (
        SELECT 1 FROM Reservations
        WHERE RoomID = @RandRoomID
        AND CheckInDate < @CheckOut
        AND CheckOutDate > @CheckIn
    )
    BEGIN
        INSERT INTO Reservations (CheckInDate, CheckOutDate, TotalAmount, GuestID, RoomID, StaffID)
        VALUES (@CheckIn, @CheckOut, @Price, @RandGuestID, @RandRoomID, @RandStaffID);

        SET @ValidBookings = @ValidBookings + 1;
    END

    SET @AttemptCount = @AttemptCount + 1;
END

PRINT 'Seeding Complete.';
PRINT 'Total Attempts: ' + CAST(@MaxAttempts AS NVARCHAR(20));
PRINT 'Valid Reservations Created: ' + CAST(@ValidBookings AS NVARCHAR(20));

PRINT 'Updating Room Status to Occupied for active reservations...';

UPDATE Rooms
SET Status = 'Occupied'
WHERE RoomID IN (
    SELECT RoomID
    FROM Reservations
    WHERE CheckInDate <= CAST(GETDATE() AS DATE) 
    AND CheckOutDate > CAST(GETDATE() AS DATE)   
);

PRINT 'Room Statuses Updated.';

GO



-- GO
-- CREATE PROCEDURE sp_CreateReservation
--     @GuestID INT,
--     @RoomID INT,
--     @StaffID INT,
--     @CheckInDate DATE,
--     @CheckOutDate DATE
-- AS
-- BEGIN
--     SET NOCOUNT ON;
--     BEGIN TRY
--         BEGIN TRANSACTION;

--         IF EXISTS (
--             SELECT 1 FROM Reservations 
--             WHERE RoomID = @RoomID
--             AND CheckInDate < @CheckOutDate 
--             AND CheckOutDate > @CheckInDate
--         )
--         BEGIN
--             THROW 51000, 'Error: Room is already booked for the selected dates.', 1;
--         END
--         -- final

--         DECLARE @PricePerNight DECIMAL(10,2);
--         SELECT @PricePerNight = rt.BasePrice 
--         FROM Rooms r
--         JOIN RoomTypes rt ON r.RoomTypeID = rt.RoomTypeID
--         WHERE r.RoomID = @RoomID;

--         DECLARE @Nights INT = DATEDIFF(DAY, @CheckInDate, @CheckOutDate);
--         IF @Nights <= 0 THROW 51001, 'Error: CheckOutDate must be after CheckInDate.', 1;

--         DECLARE @TotalAmount DECIMAL(10,2) = @PricePerNight * @Nights;

--         INSERT INTO Reservations (CheckInDate, CheckOutDate, TotalAmount, GuestID, RoomID, StaffID)
--         VALUES (@CheckInDate, @CheckOutDate, @TotalAmount, @GuestID, @RoomID, @StaffID);

--         SELECT SCOPE_IDENTITY() AS NewReservationID;

--         COMMIT TRANSACTION;
--     END TRY
--     BEGIN CATCH
--         ROLLBACK TRANSACTION;
--         THROW;
--     END CATCH
-- END;
-- GO

GO
CREATE PROCEDURE sp_AdminUpdateRoomPrice
    @RequesterStaffID INT,
    @RoomTypeID INT,
    @NewBasePrice DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Staff WHERE StaffID = @RequesterStaffID AND Role = 'Manager')
    BEGIN
        THROW 51002, 'Access Denied: Only Managers can update room prices.', 1;
    END

    UPDATE RoomTypes
    SET BasePrice = @NewBasePrice
    WHERE RoomTypeID = @RoomTypeID;

    PRINT 'Price updated successfully.';
END;
GO

GO
CREATE TRIGGER trg_AuditReservationChange
ON Reservations
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ActionType NVARCHAR(20);
    
    IF EXISTS (SELECT * FROM inserted)
    BEGIN
        IF EXISTS (SELECT * FROM deleted)
            SET @ActionType = 'UPDATE';
        ELSE
            SET @ActionType = 'INSERT';
    END
    ELSE
        SET @ActionType = 'DELETE';

    INSERT INTO AuditLogs (TableName, ActionType, RecordID, ChangedByStaffID, Details)
    SELECT 
        'Reservations', 
        @ActionType,
        ISNULL(i.ReservationID, d.ReservationID),
        ISNULL(i.StaffID, d.StaffID), 
        CASE 
            WHEN @ActionType = 'UPDATE' THEN 'Updated Reservation Amount: ' + CAST(i.TotalAmount AS NVARCHAR(20))
            WHEN @ActionType = 'INSERT' THEN 'New Booking for Guest ' + CAST(i.GuestID AS NVARCHAR(10))
            WHEN @ActionType = 'DELETE' THEN 'Cancelled Reservation ' + CAST(d.ReservationID AS NVARCHAR(10))
        END
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.ReservationID = d.ReservationID;
END;

GO
CREATE TRIGGER trg_UpdateGuestTotalSpent
ON Reservations
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE G
    SET G.TotalSpent = G.TotalSpent + I.TotalAmount
    FROM Guests G
    INNER JOIN inserted I ON G.GuestID = I.GuestID;
END;
GO

GO
CREATE TRIGGER trg_ProtectOccupiedRooms
ON Rooms
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1 FROM Reservations r
        JOIN deleted d ON r.RoomID = d.RoomID
        WHERE r.CheckOutDate >= GETDATE()
    )
    BEGIN
        RAISERROR ('Cannot delete room: It has active or future reservations.', 16, 1);
    END
    ELSE
    BEGIN
        DELETE FROM Rooms WHERE RoomID IN (SELECT RoomID FROM deleted);
    END
END;
GO

GO
CREATE FUNCTION fn_GetAvailableRooms (
    @CheckIn DATE, 
    @CheckOut DATE
)
RETURNS TABLE
AS
RETURN
(
    SELECT 
        r.RoomID,
        r.RoomNumber,
        rt.TypeName,
        rt.BasePrice
    FROM Rooms r
    JOIN RoomTypes rt ON r.RoomTypeID = rt.RoomTypeID
    AND r.RoomID NOT IN (
        SELECT RoomID 
        FROM Reservations
        WHERE CheckInDate < @CheckOut 
          AND CheckOutDate > @CheckIn
    )
);
GO


GO
CREATE VIEW vw_FrontDeskDashboard AS
SELECT 
    r.RoomNumber,
    rt.TypeName,
    CASE 
        WHEN res.ReservationID IS NOT NULL AND r.Status = 'Occupied' THEN 'Occupied'
        ELSE 'Vacant'
    END AS CurrentOccupancy,
    res.CheckOutDate AS DueOut
FROM Rooms r
JOIN RoomTypes rt ON r.RoomTypeID = rt.RoomTypeID
LEFT JOIN Reservations res ON r.RoomID = res.RoomID 
    AND CAST(GETDATE() AS DATE) >= res.CheckInDate 
    AND CAST(GETDATE() AS DATE) < res.CheckOutDate;
GO

GO
CREATE VIEW vw_ManagerRevenueReport AS
SELECT 
    rt.TypeName,
    COUNT(res.ReservationID) AS TotalBookings,
    SUM(res.TotalAmount) AS TotalRevenue,
    AVG(res.TotalAmount) AS AvgRevenuePerBooking
FROM RoomTypes rt
JOIN Rooms r ON rt.RoomTypeID = r.RoomTypeID
JOIN Reservations res ON r.RoomID = res.RoomID
GROUP BY rt.TypeName;
GO



CREATE PARTITION FUNCTION pf_DateRanges (DATE)
AS RANGE RIGHT FOR VALUES ('2024-01-01', '2025-01-01');
GO

CREATE PARTITION SCHEME ps_DateRanges AS PARTITION pf_DateRanges ALL TO ([PRIMARY]);
GO

DECLARE @pkRes sysname;
SELECT @pkRes = kc.name FROM sys.key_constraints kc WHERE kc.parent_object_id = OBJECT_ID('Reservations') AND kc.type = 'PK';
IF @pkRes IS NOT NULL EXEC('ALTER TABLE Reservations DROP CONSTRAINT ' + @pkRes);
GO

ALTER TABLE Reservations ADD CONSTRAINT PK_Reservations_ID PRIMARY KEY NONCLUSTERED (ReservationID);
GO

CREATE CLUSTERED INDEX CIX_Reservations_Partitioned
ON Reservations (CheckInDate, ReservationID)
ON ps_DateRanges(CheckInDate);
GO
PRINT 'Partitioning applied to Reservations (Split by Year).';

DECLARE @pkLog sysname;
SELECT @pkLog = kc.name FROM sys.key_constraints kc WHERE kc.parent_object_id = OBJECT_ID('AuditLogs') AND kc.type = 'PK';
IF @pkLog IS NOT NULL EXEC('ALTER TABLE AuditLogs DROP CONSTRAINT ' + @pkLog);
GO

ALTER TABLE AuditLogs ADD CONSTRAINT PK_AuditLogs_ID PRIMARY KEY NONCLUSTERED (LogID);
GO

CREATE CLUSTERED INDEX CIX_AuditLogs_Partitioned
ON AuditLogs (ChangeDate, LogID)
ON ps_DateRanges(ChangeDate);
GO
PRINT 'Partitioning applied to AuditLogs (Split by Year).';

CREATE NONCLUSTERED INDEX IX_Reservations_GuestID ON Reservations (GuestID) INCLUDE (TotalAmount);

CREATE NONCLUSTERED INDEX IX_Rooms_Available ON Rooms (RoomTypeID) WHERE Status = 'Available';
GO

GO
CREATE PROCEDURE sp_GetOccupancyReport (@StartDate DATE, @EndDate DATE)
AS
BEGIN
    WITH DateRange_CTE AS (
        SELECT @StartDate AS ReportDate
        UNION ALL
        SELECT DATEADD(DAY, 1, ReportDate) FROM DateRange_CTE WHERE DATEADD(DAY, 1, ReportDate) <= @EndDate
    )
    SELECT d.ReportDate, COUNT(r.ReservationID) AS Occ
    FROM DateRange_CTE d LEFT JOIN Reservations r ON d.ReportDate >= r.CheckInDate AND d.ReportDate < r.CheckOutDate
    GROUP BY d.ReportDate
    OPTION (MAXRECURSION 1000);
END;
GO

GO
CREATE PROCEDURE sp_GetTopGuests
AS
BEGIN
    WITH GuestSpending_CTE AS (
        SELECT GuestID, SUM(TotalAmount) AS TotalSpent, RANK() OVER (ORDER BY SUM(TotalAmount) DESC) AS Rnk
        FROM Reservations GROUP BY GuestID
    )
    SELECT * FROM GuestSpending_CTE WHERE Rnk <= 10;
END;
GO

PRINT 'Database Features Implementation Complete.';
GO