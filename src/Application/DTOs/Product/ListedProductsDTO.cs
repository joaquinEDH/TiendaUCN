namespace Tienda.src.Application.DTOs.Product
{
    public class ListedProductsDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Price { get; set; }
        public string Condition { get; set; } = string.Empty;
    }
}