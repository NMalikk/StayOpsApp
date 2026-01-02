using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StayOpsApp
{
    public class LinqHotelService : IHotelService
    {
        public string GetImplementationName() => "LINQ / Entity Framework";

        public Staff? Login(string username, string password)
        {
            using var db = new StayOpsContext();
            return db.Staff.FirstOrDefault(s => s.Username == username && s.PasswordHash == password);
        }

        public void RegisterGuest(string first, string last, string email, string phone)
        {
            using var db = new StayOpsContext();
            var guest = new Guest { FirstName = first, LastName = last, Email = email, Phone = phone };
            db.Guests.Add(guest);
            db.SaveChanges();
            Console.WriteLine($"Guest {first} {last} registered (ID: {guest.GuestID}).");
        }

        public List<AvailableRoomItem> FindAvailableRooms(DateTime checkIn, DateTime checkOut)
        {
            using var db = new StayOpsContext();
            return db.GetAvailableRooms(checkIn, checkOut).ToList();
        }

        public void CreateReservation(int guestId, int roomId, int staffId, DateTime checkIn, DateTime checkOut)
        {
            using var db = new StayOpsContext();

            if (!db.Guests.Any(g => g.GuestID == guestId))
            {
                Console.WriteLine($"Error: Guest ID {guestId} not found in database.");
                return;
            }
            
            if(checkIn < DateTime.Today)
            {
                Console.WriteLine("Error: CheckIn date cannot be in the past.");
                return;
            }
            
            if (checkOut <= checkIn) 
            {
                Console.WriteLine("Error: CheckOut must be after CheckIn."); 
                return; 
            }

            bool isOccupied = db.Reservations.Any(r => r.RoomID == roomId && 
                                                       r.CheckInDate < checkOut && 
                                                       r.CheckOutDate > checkIn);
            if (isOccupied)
            {
                Console.WriteLine("Error: Room is already booked for these dates.");
                return;
            }

            var room = db.Rooms.Include(r => r.RoomType).FirstOrDefault(r => r.RoomID == roomId);
            if (room == null || room.RoomType == null) 
            { 
                Console.WriteLine("Invalid Room ID."); 
                return; 
            }

            int nights = (checkOut - checkIn).Days;
            decimal total = room.RoomType.BasePrice * nights;

            var res = new Reservation
            {
                GuestID = guestId, RoomID = roomId, StaffID = staffId,
                CheckInDate = checkIn, CheckOutDate = checkOut, TotalAmount = total
            };

            db.Reservations.Add(res);
            db.SaveChanges(); 
            Console.WriteLine($"Reservation created (LINQ). Reservation ID: {res.ReservationID}, Total: {total:C}");
        }

        public void CheckInGuest(int reservationId)
        {
            using var db = new StayOpsContext();
            var res = db.Reservations.Find(reservationId);
            if (res == null) { Console.WriteLine("Reservation not found."); return; }

            if(res.CheckInDate != DateTime.Today)
            {
                Console.WriteLine("Error: Check-in date is not today.");
                return;
            }

            var guest = db.Guests.Find(res.GuestID);
            var room = db.Rooms.Find(res.RoomID);
            if (room != null && guest != null)
            {
                room.Status = "Occupied";
                db.SaveChanges();
                Console.WriteLine($@"Guest Checked In. 
                Guest ID: {guest.GuestID}
                Guest Name: {guest.FirstName} {guest.LastName}
                Room {room.RoomNumber} status set to Occupied.");
            }
        }

        public void CancelReservation(int reservationId)
        {
            using var db = new StayOpsContext();
            var res = db.Reservations.Find(reservationId);

            if (res == null)
            {
                Console.WriteLine($"Error: Reservation {reservationId} not found.");
                return;
            }

            if (res.CheckInDate < DateTime.Today)
            {
                Console.WriteLine("Error: Cannot cancel past or active reservations.");
                return;
            }

            db.Reservations.Remove(res);
            db.SaveChanges(); 
            Console.WriteLine($"Reservation {reservationId} cancelled/removed successfully.");
        }

        public void CheckOutGuest(int reservationId)
        {
            using var db = new StayOpsContext();
            var res = db.Reservations.Find(reservationId);
            if (res == null) { Console.WriteLine("Reservation not found."); return; }

            var room = db.Rooms.Find(res.RoomID);
            var guest = db.Guests.Find(res.GuestID);
            if (room != null && guest != null)
            {
                room.Status = "Available"; 
                db.SaveChanges();
                Console.WriteLine($@"Guest Checked Out.
                Guest ID: {guest.GuestID}
                Guest Name: {guest.FirstName} {guest.LastName}
                Room {room.RoomNumber} status set to Available.");
                CancelReservation(reservationId);
            }
        }

        public void UpdateRoomPrice(Staff requester, int roomTypeId, decimal newPrice)
        {
            if (requester.Role != "Manager")
            {
                Console.WriteLine("Access Denied: Managers only.");
                return;
            }

            using var db = new StayOpsContext();
            var rt = db.RoomTypes.Find(roomTypeId);
            if (rt != null)
            {
                rt.BasePrice = newPrice;
                db.SaveChanges();
                Console.WriteLine("Price updated via LINQ.");
            }
        }

        public List<DashboardItem> GetFrontDeskDashboard()
        {
            using var db = new StayOpsContext();
            return db.DashboardItems.ToList(); 
        }

        public List<RevenueReportItem> GetRevenueReport()
        {
            using var db = new StayOpsContext();
            return db.RevenueReports.ToList(); 
        }

        public decimal GetGuestTotalSpend(int guestId)
        {
            using var db = new StayOpsContext();

            if (!db.Guests.Any(g => g.GuestID == guestId))
            {
                Console.WriteLine($"Error: Guest ID {guestId} not found in database.");
                return 0;
            }

            decimal guestSpent = db.Guests
                .Where(g => g.GuestID == guestId)
                .Select(g => g.TotalSpent) 
                .FirstOrDefault();

            return guestSpent;
        }


        public List<TopGuestItem> GetTopGuests()
        {
            using var db = new StayOpsContext();

            var spendingData = db.Reservations
                .GroupBy(r => r.GuestID)
                .Select(g => new
                {
                    GuestID = g.Key,
                    TotalSpent = g.Sum(r => r.TotalAmount)
                })
                .OrderByDescending(x => x.TotalSpent)
                .ToList();

            int currentRank = 0;
            decimal? lastSpend = null;
            int count = 0;

            return spendingData
                .Select(x => {
                    count++;
                    if (x.TotalSpent != lastSpend)
                    {
                        currentRank = count;
                        lastSpend = x.TotalSpent;
                    }
                    return new TopGuestItem
                    {
                        GuestID = x.GuestID,
                        TotalSpent = x.TotalSpent,
                        Rnk = currentRank
                    };
                })
                .Where(tg => tg.Rnk <= 10)
                .ToList();
        }
        
        public List<OccupancyReportItem> GetOccupancyReport(DateTime start, DateTime end)
        {
            using var db = new StayOpsContext();

            var dayCount = (end - start).Days + 1;
            var dates = Enumerable.Range(0, dayCount)
                                .Select(offset => start.AddDays(offset))
                                .ToList();

            var reservations = db.Reservations
                .Where(r => r.CheckInDate <= end && r.CheckOutDate >= start)
                .ToList();

            return dates.Select(d => new OccupancyReportItem
            {
                ReportDate = d,
                Occ = reservations.Count(r => d >= r.CheckInDate && d < r.CheckOutDate)
            }).ToList();
        }

        
    }
}