using Mapster;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tienda.src.Application.DTOs.Product;
using Tienda.src.Application.Services.Interfaces;
using Tienda.src.Domain.Models;
using Tienda.src.Infrastructure.Repositories.Interfaces;

namespace Tienda.src.Application.Services.Implements
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<IEnumerable<ListedProductsDTO>> GetAllAsync()
        {
            var products = await _productRepository.GetAllAsync();

            return products.Adapt<IEnumerable<ListedProductsDTO>>(); // Usando Mapster para mapear la lista
        }


        /// <summary>
        /// Retorna un producto específico por su ID (vista pública/cliente).
        /// </summary>
        public async Task<ProductDetailDTO> GetByIdAsync(int id)
        {
            var product = await _productRepository.GetByIdForAdminAsync(id) ?? throw new KeyNotFoundException($"Producto con ID {id} no encontrado.");
            Log.Information("Producto encontrado: {@Product}", product);
            if (product is null)
                throw new KeyNotFoundException("Producto no encontrado.");

            return product.Adapt<ProductDetailDTO>();
        }

        // -------- Admin --------
        /// <summary>
        /// Retorna un producto específico por su ID desde el punto de vista de un admin.
        /// </summary>
        public async Task<ProductDetailDTO> GetByIdForAdminAsync(int id)
        {
            var product = await _productRepository.GetByIdForAdminAsync(id) ?? throw new KeyNotFoundException($"Producto con ID {id} no encontrado.");
            Log.Information("Producto encontrado (admin): {@Product}", product);
            return product.Adapt<ProductDetailDTO>();
        }

        /// <summary>
        /// Crea un nuevo producto y retorna su ID como string.
        /// </summary>
        public async Task<string> CreateAsync(CreateProductDTO dto)
        {
            var entity = dto.Adapt<Product>();
            var created = await _productRepository.CreateAsync(entity);
            Log.Information("Producto creado: {@Product}", created);
            return created.Id.ToString();
        }



    }
}