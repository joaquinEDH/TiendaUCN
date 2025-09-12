using Tienda.src.Application.DTOs.Product;
using Tienda.src.Application.Services.Interfaces;
using Tienda.src.Domain.Models;
using Tienda.src.Infrastructure.Repositories.Interfaces;

namespace Tienda.src.Application.Services.Implements
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;

        public ProductService(IProductRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ListedProductsDTO>> GetAllAsync()
        {
            var products = await _repository.GetAllAsync();
            return products.Select(p => new ListedProductsDTO
            {
                Id = p.Id,
                Title = p.Title,
                Price = p.Price,
                Condition = p.Status.ToString()
            });
        }

        public async Task<ProductDetailDTO?> GetByIdAsync(int id)
        {
            var product = await _repository.GetByIdAsync(id);
            if (product == null) return null;

            return new ProductDetailDTO
            {
                Id = product.Id,
                Title = product.Title,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                Category = product.Category?.Name ?? string.Empty,
                Brand = product.Brand?.Name ?? string.Empty
            };
        }

        public async Task<int> CreateAsync(CreateProductDTO dto)
        {
            var product = new Product
            {
                Title = dto.Title,
                Description = dto.Description,
                Price = dto.Price,
                Stock = dto.Stock,
                CategoryId = dto.CategoryId,
                BrandId = dto.BrandId,
                Status = Status.New
            };

            var created = await _repository.CreateAsync(product);
            return created.Id;
        }
    }
}