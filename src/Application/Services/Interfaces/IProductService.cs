using Tienda.src.Application.DTOs.Product;

namespace Tienda.src.Application.Services.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<ListedProductsDTO>> GetAllAsync();
        Task<ProductDetailDTO?> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateProductDTO dto);
    }
}