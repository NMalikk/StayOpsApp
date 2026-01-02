using System;

namespace StayOpsApp
{
    class Program
    {
        static Staff? CurrentUser = null;

        static void Main(string[] args)
        {
            Console.WriteLine("=== StayOps Hotel Management System ===");
            
            string mode = "linq";
            Console.WriteLine("Select Logic Mode: (1) LINQ, (2) Stored Procedures");
            if (Console.ReadLine() == "2") mode = "sproc";
            
            IHotelService service = HotelServiceFactory.CreateService(mode);
            Console.WriteLine($"\n[System initialized with {service.GetImplementationName()}]");

            while (CurrentUser == null)
            {
                Console.Write("\nUsername (e.g. mashraf, saqeel): ");
                string user = Console.ReadLine() ?? "";
                Console.Write("Password (default 'hash123'): ");
                string pass = Console.ReadLine() ?? "";

                CurrentUser = service.Login(user, pass);
                if (CurrentUser == null) Console.WriteLine("Login Failed. Try again.");
            }

            Console.WriteLine($"\nWelcome, {CurrentUser.FirstName} ({CurrentUser.Role})!");

            bool running = true;
            while (running)
            {
                Console.WriteLine("\n--- Main Menu ---");
                Console.WriteLine("1. Dashboard (Room Status)");
                Console.WriteLine("2. Find Available Rooms (Date Range)");
                Console.WriteLine("3. Create Reservation");
                Console.WriteLine("4. Check-In Guest");
                Console.WriteLine("5. Check-Out Guest");
                Console.WriteLine("6. Cancel Reservation"); 
                Console.WriteLine("7. Register New Guest");
                Console.WriteLine("8. [Report] Guest Total Spend");
                
                if (CurrentUser.Role == "Manager")
                {
                    Console.WriteLine("9. [Manager] Update Room Price");
                    Console.WriteLine("10. [Manager] Revenue Report");
                    Console.WriteLine("11. [Manager] Occupancy Report (CTE)");
                    Console.WriteLine("12. [Manager] Top 10 Guests (Rank)");
                }
                
                Console.WriteLine("0. Exit");
                Console.Write("Select: ");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1": ShowDashboard(service); break;
                    case "2": ShowAvailability(service); break;
                    case "3": CreateRes(service); break;
                    case "4": DoCheckIn(service); break;
                    case "5": DoCheckOut(service); break;
                    case "6": CancelRes(service); break; 
                    case "7": RegisterGuest(service); break;
                    case "8": ShowGuestSpend(service); break;
                    case "9": 
                    case "10": 
                    case "11": 
                    case "12":
                        if (CurrentUser.Role == "Manager")
                        {
                            if (choice == "9") UpdatePrice(service);
                            else if (choice == "10") ShowRevenue(service);
                            else if (choice == "11") ShowOccupancy(service);
                            else if (choice == "12") ShowTopGuests(service);
                        }
                        else 
                        {
                            Console.WriteLine("Invalid selection.");
                        }
                        break;
                    
                    case "0": running = false; break;
                    default: Console.WriteLine("Invalid selection."); break;
                }
            }
        }

        static void ShowDashboard(IHotelService s)
        {
            var data = s.GetFrontDeskDashboard();
            Console.WriteLine("\n{0,-8} {1,-15} {2,-12}", "Room", "Type", "Status");
            foreach (var r in data)
                Console.WriteLine("{0,-8} {1,-15} {2,-12}", r.RoomNumber, r.TypeName, r.CurrentOccupancy);
        }

        static void ShowAvailability(IHotelService s)
        {
            Console.Write("Check In (yyyy-mm-dd): "); 
            DateTime inDt = DateTime.Parse(Console.ReadLine() ?? "");
            Console.Write("Check Out (yyyy-mm-dd): "); 
            DateTime outDt = DateTime.Parse(Console.ReadLine() ?? "");
            
            var rooms = s.FindAvailableRooms(inDt, outDt);
            Console.WriteLine($"\nFound {rooms.Count} available rooms:");
            foreach (var r in rooms)
                Console.WriteLine($"ID: {r.RoomID} | # {r.RoomNumber} | {r.TypeName} | ${r.BasePrice}");
        }

        static void CreateRes(IHotelService s)
        {
            try {
                Console.Write("Guest ID: "); int gId = int.Parse(Console.ReadLine() ?? "0");
                Console.Write("Room ID: "); int rId = int.Parse(Console.ReadLine() ?? "0");
                Console.Write("Check In (yyyy-mm-dd): "); DateTime inDt = DateTime.Parse(Console.ReadLine() ?? "");
                Console.Write("Check Out (yyyy-mm-dd): "); DateTime outDt = DateTime.Parse(Console.ReadLine() ?? "");

                s.CreateReservation(gId, rId, CurrentUser!.StaffID, inDt, outDt);
            } 
            catch (Exception ex)
            {
                Console.WriteLine($"CRASH: {ex.GetType().Name} - {ex.Message}");
            }
        }

        static void DoCheckIn(IHotelService s)
        {
            Console.Write("Reservation ID: ");
            if(int.TryParse(Console.ReadLine(), out int rId)) s.CheckInGuest(rId);
        }

        static void DoCheckOut(IHotelService s)
        {
            Console.Write("Reservation ID: ");
            if(int.TryParse(Console.ReadLine(), out int rId)) s.CheckOutGuest(rId);
        }

        static void CancelRes(IHotelService s)
        {
            Console.Write("Reservation ID to Cancel: ");
            if (int.TryParse(Console.ReadLine(), out int rId))
            {
                s.CancelReservation(rId);
            }
            else
            {
                Console.WriteLine("Invalid ID format.");
            }
        }

        static void RegisterGuest(IHotelService s)
        {
            Console.Write("First Name: "); string f = Console.ReadLine() ?? "";
            Console.Write("Last Name: "); string l = Console.ReadLine() ?? "";
            Console.Write("Email: "); string e = Console.ReadLine() ?? "";
            Console.Write("Phone: "); string p = Console.ReadLine() ?? "";
            s.RegisterGuest(f, l, e, p);
        }

        static void ShowGuestSpend(IHotelService s)
        {
            Console.Write("Guest ID: ");
            if (int.TryParse(Console.ReadLine(), out int id))
                Console.WriteLine($"Total Spend: {s.GetGuestTotalSpend(id):C}");
        }

        static void UpdatePrice(IHotelService s)
        {
            Console.Write("Room Type ID (1-4): "); int tId = int.Parse(Console.ReadLine() ?? "0");
            Console.Write("New Price: "); decimal p = decimal.Parse(Console.ReadLine() ?? "0");
            s.UpdateRoomPrice(CurrentUser!, tId, p);
        }

        static void ShowRevenue(IHotelService s)
        {
            var data = s.GetRevenueReport();
            foreach(var d in data) Console.WriteLine($"{d.TypeName}: {d.TotalRevenue:C} ({d.TotalBookings} bookings)");
        }

        static void ShowOccupancy(IHotelService s)
        {
            Console.Write("Start Date: "); DateTime st = DateTime.Parse(Console.ReadLine() ?? "");
            Console.Write("End Date: "); DateTime en = DateTime.Parse(Console.ReadLine() ?? "");
            var data = s.GetOccupancyReport(st, en);
            foreach(var d in data) Console.WriteLine($"{d.ReportDate:d}: {d.Occ} occupied rooms");
        }

        static void ShowTopGuests(IHotelService s)
        {
            var data = s.GetTopGuests();
            Console.WriteLine("\n--- Top 10 Guests ---");
            foreach(var g in data) Console.WriteLine($"Rank {g.Rnk}: Guest ID {g.GuestID} - ${g.TotalSpent}");
        }
    }
}