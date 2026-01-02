using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StayOpsApp
{
    public class StoredProcHotelService : IHotelService
    {
        public string GetImplementationName() => "Stored Procedures (Pure)";

        public Staff? Login(string username, string password)
        {
            using var db = new StayOpsContext();
            return db.Staff
                .FromSqlRaw("EXEC sp_StaffLogin @Username={0}, @PasswordHash={1}", username, password)
                .AsEnumerable()
                .FirstOrDefault();
        }

        public void RegisterGuest(string first, string last, string email, string phone)
        {
            using var db = new StayOpsContext();
            var result = db.Database
                .SqlQuery<decimal>($"EXEC sp_RegisterGuest {first}, {last}, {email}, {phone}")
                .ToList();

            var newGuestId = result.FirstOrDefault();
            Console.WriteLine($"Guest {first} {last} registered/found (ID: {newGuestId}).");
        }

        public List<AvailableRoomItem> FindAvailableRooms(DateTime checkIn, DateTime checkOut)
        {
            using var db = new StayOpsContext();
            return db.AvailableRooms
                .FromSqlRaw("EXEC sp_FindAvailableRooms {0}, {1}", checkIn, checkOut)
                .ToList();
        }

        public void CreateReservation(int guestId, int roomId, int staffId, DateTime checkIn, DateTime checkOut)
        {
            using var db = new StayOpsContext();
            try
            {
          
                var result = db.Database
                    .SqlQuery<decimal>($"EXEC sp_CreateReservation @GuestID={guestId}, @RoomID={roomId}, @StaffID={staffId}, @CheckInDate={checkIn}, @CheckOutDate={checkOut}")
                    .ToList();
                
                var newResId = result.FirstOrDefault();
                Console.WriteLine($"Reservation created successfully via SP. ID: {newResId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB Error: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public void CheckInGuest(int reservationId)
        {
            using var db = new StayOpsContext();
            try
            {
                var info = db.Guests
                    .FromSqlRaw("EXEC sp_CheckInGuest {0}", reservationId)
                    .AsEnumerable() 
                    .Select(x => new { x.GuestID, x.FirstName, x.LastName }) 
                    .FirstOrDefault();

                if (info != null)
                {
                    Console.WriteLine($@"Guest Checked In Successfully.
                    Guest ID: {info.GuestID}
                    Name: {info.FirstName} {info.LastName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Check-In Failed: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public void CheckOutGuest(int reservationId)
        {
            using var db = new StayOpsContext();
            try
            {
                var info = db.Guests
                    .FromSqlRaw("EXEC sp_CheckOutGuest {0}", reservationId)
                    .AsEnumerable()
                    .Select(x => new { x.GuestID, x.FirstName, x.LastName })
                    .FirstOrDefault();

                if (info != null)
                {
                    Console.WriteLine($@"Guest Checked Out.
                    Guest ID: {info.GuestID}
                    Name: {info.FirstName} {info.LastName}
                    Room status set to Available.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Check-Out Failed: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public void CancelReservation(int reservationId)
        {
            using var db = new StayOpsContext();
            try
            {
                db.Database.ExecuteSqlRaw("EXEC sp_CancelReservation {0}", reservationId);
                Console.WriteLine($"Reservation {reservationId} cancelled successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cancellation Failed: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public void UpdateRoomPrice(Staff requester, int roomTypeId, decimal newPrice)
        {
            using var db = new StayOpsContext();
            try
            {
                db.Database.ExecuteSqlRaw("EXEC sp_AdminUpdateRoomPrice @RequesterStaffID={0}, @RoomTypeID={1}, @NewBasePrice={2}",
                    requester.StaffID, roomTypeId, newPrice);
                
                Console.WriteLine("Price update command sent.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update Failed: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public List<DashboardItem> GetFrontDeskDashboard()
        {
            using var db = new StayOpsContext();
            return db.DashboardItems
                .FromSqlRaw("EXEC sp_GetFrontDeskDashboard")
                .ToList();
        }

        public List<RevenueReportItem> GetRevenueReport()
        {
            using var db = new StayOpsContext();
            return db.RevenueReports
                .FromSqlRaw("EXEC sp_GetRevenueReport")
                .ToList();
        }

        public decimal GetGuestTotalSpend(int guestId)
        {
            using var db = new StayOpsContext();
            var result = db.Database
                .SqlQuery<decimal>($"EXEC sp_GetGuestTotalSpend {guestId}")
                .ToList();

            return result.FirstOrDefault();
        }

        public List<TopGuestItem> GetTopGuests()
        {
            using var db = new StayOpsContext();
            return db.TopGuests
                .FromSqlRaw("EXEC sp_GetTopGuests")
                .ToList();
        }

        public List<OccupancyReportItem> GetOccupancyReport(DateTime start, DateTime end)
        {
            using var db = new StayOpsContext();
            return db.OccupancyReports
                .FromSqlRaw("EXEC sp_GetOccupancyReport {0}, {1}", start, end)
                .ToList();
        }
    }
}