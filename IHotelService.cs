using System;
using System.Collections.Generic;

namespace StayOpsApp
{
    public interface IHotelService
    {
        string GetImplementationName();

        Staff? Login(string username, string password);

        void RegisterGuest(string first, string last, string email, string phone);
        void CreateReservation(int guestId, int roomId, int staffId, DateTime checkIn, DateTime checkOut);
        void CheckInGuest(int reservationId);
        void CheckOutGuest(int reservationId);
        
        void CancelReservation(int reservationId);
        
        List<AvailableRoomItem> FindAvailableRooms(DateTime checkIn, DateTime checkOut);
        List<DashboardItem> GetFrontDeskDashboard();
        
        void UpdateRoomPrice(Staff requester, int roomTypeId, decimal newPrice);
        List<RevenueReportItem> GetRevenueReport();
        
        List<OccupancyReportItem> GetOccupancyReport(DateTime start, DateTime end);
        List<TopGuestItem> GetTopGuests();
        decimal GetGuestTotalSpend(int guestId);
    }

    public static class HotelServiceFactory
    {
        public static IHotelService CreateService(string type)
        {
            return type?.ToLower() switch
            {
                "linq" => new LinqHotelService(),
                "sproc" => new StoredProcHotelService(),
                _ => throw new ArgumentException($"Unknown service type: {type}")
            };
        }
    }
}