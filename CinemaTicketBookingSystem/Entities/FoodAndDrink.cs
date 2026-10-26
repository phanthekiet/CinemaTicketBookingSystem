using System;
using System.Collections.Generic;

namespace CinemaTicketBookingSystem.Entities;

public partial class FoodAndDrink
{
    public int FoodId { get; set; }

    public string FoodName { get; set; } = null!;

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public bool Status { get; set; }

    public virtual ICollection<BookingFood> BookingFoods { get; set; } = new List<BookingFood>();
}
