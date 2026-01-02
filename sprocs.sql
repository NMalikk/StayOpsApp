USE StayOpsDB;
GO

CREATE OR ALTER PROCEDURE sp_StaffLogin
    @Username NVARCHAR(50),
    @PasswordHash NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM Staff 
    WHERE Username = @Username AND PasswordHash = @PasswordHash;
END;
GO

CREATE OR ALTER PROCEDURE sp_RegisterGuest
    @FirstName NVARCHAR(100),
    @LastName NVARCHAR(100),
    @Email NVARCHAR(255),
    @Phone NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    
    IF EXISTS (SELECT 1 FROM Guests WHERE Email = @Email)
    BEGIN
        SELECT GuestID FROM Guests WHERE Email = @Email;
        RETURN;
    END

    INSERT INTO Guests (FirstName, LastName, Email, Phone)
    VALUES (@FirstName, @LastName, @Email, @Phone);

    SELECT SCOPE_IDENTITY() AS GuestID;
END;
GO

CREATE OR ALTER PROCEDURE sp_FindAvailableRooms
    @CheckIn DATE,
    @CheckOut DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.fn_GetAvailableRooms(@CheckIn, @CheckOut);
END;
GO

CREATE OR ALTER PROCEDURE sp_CheckInGuest
    @ReservationID INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @RoomID INT;
    DECLARE @CheckInDate DATE;
    
    SELECT @RoomID = RoomID, @CheckInDate = CheckInDate 
    FROM Reservations WHERE ReservationID = @ReservationID;

    IF @RoomID IS NULL THROW 51000, 'Reservation not found.', 1;

    IF CAST(DATEADD(HOUR, 5, GETDATE()) AS DATE) <> @CheckInDate
    BEGIN
        DECLARE @ErrMsg NVARCHAR(200) = 'Error: Check-in is for ' + CAST(@CheckInDate AS VARCHAR) + 
                                       '. Today is ' + CAST(CAST(DATEADD(HOUR, 5, GETDATE()) AS DATE) AS VARCHAR);
        THROW 51001, @ErrMsg, 1;
    END

    UPDATE Rooms SET Status = 'Occupied' WHERE RoomID = @RoomID;

    SELECT g.* FROM Reservations res
    JOIN Guests g ON res.GuestID = g.GuestID
    WHERE res.ReservationID = @ReservationID;
END;
GO


CREATE OR ALTER PROCEDURE sp_CancelReservation
    @ReservationID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    IF EXISTS (
        SELECT 1 FROM Reservations 
        WHERE ReservationID = @ReservationID 
        AND CheckInDate < CAST(GETDATE() AS DATE)
    )
    BEGIN
        THROW 51002, 'Error: Cannot cancel past reservations.', 1;
    END

    DELETE FROM Reservations WHERE ReservationID = @ReservationID;
END;
GO

CREATE OR ALTER PROCEDURE sp_CheckOutGuest
    @ReservationID INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Reservations WHERE ReservationID = @ReservationID)
    BEGIN
        THROW 51000, 'Error: Reservation not found.', 1;
    END

    UPDATE r
    SET r.Status = 'Available'
    FROM Rooms r
    JOIN Reservations res ON r.RoomID = res.RoomID
    WHERE res.ReservationID = @ReservationID;

    SELECT g.* FROM Reservations res
    JOIN Guests g ON res.GuestID = g.GuestID
    WHERE res.ReservationID = @ReservationID;

    EXEC sp_CancelReservation @ReservationID;
END;
GO

CREATE OR ALTER PROCEDURE sp_GetFrontDeskDashboard AS
BEGIN SELECT * FROM vw_FrontDeskDashboard; END;
GO

CREATE OR ALTER PROCEDURE sp_GetRevenueReport AS
BEGIN SELECT * FROM vw_ManagerRevenueReport; END;
GO

CREATE OR ALTER PROCEDURE sp_GetGuestTotalSpend @GuestID INT AS
BEGIN SELECT TotalSpent FROM Guests WHERE GuestID = @GuestID; END;
GO

CREATE OR ALTER PROCEDURE sp_CreateReservation
    @GuestID INT,
    @RoomID INT,
    @StaffID INT,
    @CheckInDate DATE,
    @CheckOutDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        DECLARE @Today DATE = CAST(DATEADD(HOUR, 5, GETDATE()) AS DATE);

        IF @CheckInDate < @Today
        BEGIN
            DECLARE @ErrMsg NVARCHAR(200) = 'Error: Check-in date (' + CAST(@CheckInDate AS VARCHAR) + ') cannot be in the past. Today is ' + CAST(@Today AS VARCHAR);
            THROW 51002, @ErrMsg, 1;
        END

        IF @CheckOutDate <= @CheckInDate 
            THROW 51001, 'Error: CheckOutDate must be after CheckInDate.', 1;

        BEGIN TRANSACTION;

        IF EXISTS (
            SELECT 1 FROM Reservations 
            WHERE RoomID = @RoomID
            AND CheckInDate < @CheckOutDate 
            AND CheckOutDate > @CheckInDate
        )
        BEGIN
            THROW 51000, 'Error: Room is already booked for the selected dates.', 1;
        END

        DECLARE @PricePerNight DECIMAL(10,2);
        SELECT @PricePerNight = rt.BasePrice 
        FROM Rooms r
        JOIN RoomTypes rt ON r.RoomTypeID = rt.RoomTypeID
        WHERE r.RoomID = @RoomID;

        DECLARE @Nights INT = DATEDIFF(DAY, @CheckInDate, @CheckOutDate);
        DECLARE @TotalAmount DECIMAL(10,2) = @PricePerNight * @Nights;

        INSERT INTO Reservations (CheckInDate, CheckOutDate, TotalAmount, GuestID, RoomID, StaffID)
        VALUES (@CheckInDate, @CheckOutDate, @TotalAmount, @GuestID, @RoomID, @StaffID);

        SELECT SCOPE_IDENTITY() AS NewReservationID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW; 
    END CATCH
END;
GO