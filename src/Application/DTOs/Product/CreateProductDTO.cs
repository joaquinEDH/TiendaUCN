using System.ComponentModel.DataAnnotations;

namespace Tienda.src.Application.DTOs.Product
{
    public class CreateProductDTO
    {
        [Required, StringLength(80, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Precio debe ser > 0")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stock debe ser ≥ 0")]
        public int Stock { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public int BrandId { get; set; }
    }
}