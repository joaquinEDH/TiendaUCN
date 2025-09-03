using System.ComponentModel;

namespace Tienda.src.Domain.Models
{
    public class Product
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required int Price { get; set; }
        public int Discount { get; set; }

        public required int Stock { get; set; }

        public required string Status { get; set; }

        public bool IsAvailable { get; set; } = true;

        public int CategoryId { get; set; }

        public Category Category { get; set; } = null!;

        public int BrandId { get; set; }

        public Brand Brand { get; set; } = null!;

        public ICollection<Image> Images { get; set; } = new List<Image>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    }
}