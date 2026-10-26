using CinemaTicketBookingSystem.Entities;

namespace CinemaTicketBookingSystem
{
    public static class UserSession
    {
        public static User? CurrentUser { get; set; }
    }
}