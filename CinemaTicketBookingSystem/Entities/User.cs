using System;
using System.Collections.Generic;

namespace CinemaTicketBookingSystem.Entities;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public string Role { get; set; } = null!;

    public bool Status { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
