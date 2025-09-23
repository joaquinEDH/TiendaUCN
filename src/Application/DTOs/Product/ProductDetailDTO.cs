namespace Tienda.src.Application.DTOs.Product
{
    public class ProductDetailDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Price { get; set; }
        public int Stock { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
    }
}