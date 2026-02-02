using System;
using System.ComponentModel.DataAnnotations;
using Cinema.Infrastructure.Entities.Enums;

namespace Cinema.Web.Models.Sessions
{
    public class EditSessionViewModel
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Оберіть фільм")]
        public int MovieId { get; set; }

        [Required(ErrorMessage = "Оберіть зал")]
        public int HallId { get; set; }

        [Required(ErrorMessage = "Вкажіть час початку")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Оберіть формат")]
        public SessionFormat Format { get; set; }

        public int[] PriceCategoryIds { get; set; } = Array.Empty<int>();
        public decimal[] CategoryPrices { get; set; } = Array.Empty<decimal>();
    }
}
