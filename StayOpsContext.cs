using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace StayOpsApp
{
    public class RoomType
    {
        public int RoomTypeID { get; set; }
        public string? TypeName { get; set; }
        public decimal BasePrice { get; set; }
    }

    public class Room
    {
        public int RoomID { get; set; }
        public string? RoomNumber { get; set; }
        public string? Status { get; set; } 
        public int RoomTypeID { get; set; }
        public RoomType? RoomType { get; set; }
    }

    public class Guest
    {
        public int GuestID { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public decimal TotalSpent {get; set;}
    }

    public class Staff
    {
        public int StaffID { get; set; }
        public string? Username { get; set; }
        public string? PasswordHash { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Role { get; set; } 
    }

    public class Reservation
    {
        public int ReservationID { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public decimal TotalAmount { get; set; }
        public int GuestID { get; set; }
        public int RoomID { get; set; }
        public int StaffID { get; set; }
    }

    public class AuditLog
    {
        [Key]
        public int LogID { get; set; }
        public string? TableName { get; set; }
        public string? ActionType { get; set; }
        public int? RecordID { get; set; }
        public int? ChangedByStaffID { get; set; }
        public DateTime ChangeDate { get; set; }
        public string? Details { get; set; }
    }

    public class DashboardItem
    {
        public string? RoomNumber { get; set; }
        public string? TypeName { get; set; }
        // public string? OperationalStatus { get; set; }
        public string? CurrentOccupancy { get; set; }
        // public string? GuestName { get; set; }
        public DateTime? DueOut { get; set; }
    }

    public class RevenueReportItem
    {
        public string? TypeName { get; set; }
        public int? TotalBookings { get; set; }
        public decimal? TotalRevenue { get; set; }
        public decimal? AvgRevenuePerBooking { get; set; }
    }

    public class AvailableRoomItem
    {
        public int RoomID { get; set; }
        public string? RoomNumber { get; set; }
        public string? TypeName { get; set; }
        public decimal BasePrice { get; set; }
    }

    public class OccupancyReportItem
    {
        public DateTime ReportDate { get; set; }
        public int Occ { get; set; }
    }
    
    public class TopGuestItem
    {
        public int GuestID { get; set; }
        public decimal TotalSpent { get; set; }
        public long Rnk { get; set; }
    }

    public class StayOpsContext : DbContext
    {
        public DbSet<RoomType> RoomTypes { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Guest> Guests { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        public DbSet<DashboardItem> DashboardItems { get; set; }
        public DbSet<RevenueReportItem> RevenueReports { get; set; }
        public DbSet<AvailableRoomItem> AvailableRooms { get; set; }
        public DbSet<OccupancyReportItem> OccupancyReports { get; set; } 
        public DbSet<TopGuestItem> TopGuests { get; set; } 

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=localhost,1433;Database=StayOpsDB;User ID=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DashboardItem>().HasNoKey().ToView("vw_FrontDeskDashboard");
            modelBuilder.Entity<RevenueReportItem>().HasNoKey().ToView("vw_ManagerRevenueReport");
            modelBuilder.Entity<AvailableRoomItem>().HasNoKey(); 
            modelBuilder.Entity<OccupancyReportItem>().HasNoKey(); 
            modelBuilder.Entity<TopGuestItem>().HasNoKey(); 

            modelBuilder.HasDbFunction(typeof(StayOpsContext).GetMethod(nameof(CalculateGuestSpend), new[] { typeof(int) })!)
                .HasName("fn_CalculateGuestSpend");

            modelBuilder.HasDbFunction(typeof(StayOpsContext).GetMethod(nameof(GetAvailableRooms), new[] { typeof(DateTime), typeof(DateTime) })!)
                .HasName("fn_GetAvailableRooms");

            modelBuilder.Entity<Reservation>()
                .ToTable(table => table.HasTrigger("trg_AuditReservationChange"));
        }

        [DbFunction("fn_CalculateGuestSpend", "dbo")]
        public static decimal CalculateGuestSpend(int guestId)
        {
            throw new NotSupportedException();
        }

        [DbFunction("fn_GetAvailableRooms", "dbo")]
        public IQueryable<AvailableRoomItem> GetAvailableRooms(DateTime checkIn, DateTime checkOut)
        {
            return FromExpression(() => GetAvailableRooms(checkIn, checkOut));
        }
    }
}