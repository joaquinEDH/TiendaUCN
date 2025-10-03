using Tienda.src.Domain.Models;

namespace Tienda.src.Infrastructure.Repositories.Interfaces
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(int id);
        Task<Product> CreateAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(Product product);


        Task<Product?> GetByIdForAdminAsync(int id);
    }
}