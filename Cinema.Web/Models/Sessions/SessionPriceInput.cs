using System.ComponentModel.DataAnnotations;

namespace Cinema.Web.Models.Sessions;

public class SessionPriceInput
{
    public int PriceCategoryId { get; set; }

    public string CategoryName { get; set; } = "";
    public decimal Price { get; set; }
}
