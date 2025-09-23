namespace Tienda.src.Application.DTOs.Product
{
    public class CreateProductDTO
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Price { get; set; }
        public int Stock { get; set; }
        public int CategoryId { get; set; }
        public int BrandId { get; set; }
    }
}