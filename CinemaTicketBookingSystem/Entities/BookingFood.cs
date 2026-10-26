using System;
using System.Collections.Generic;

namespace CinemaTicketBookingSystem.Entities;

public partial class BookingFood
{
    public int BookingFoodId { get; set; }

    public int BookingId { get; set; }

    public int FoodId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual FoodAndDrink Food { get; set; } = null!;
}
