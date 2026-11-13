using CinemaTicketBookingSystem.Entities;

namespace CinemaTicketBookingSystem
{
    public static class UserSession
    {
        public static User? CurrentUser { get; set; }
        public static int LastGuestBookingId { get; set; } = 0;
        public static string LastGuestPhone { get; set; } = string.Empty;
    }
}
