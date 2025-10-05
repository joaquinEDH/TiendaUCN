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
        private readonly IFileService _fileService;

        public ProductService(IProductRepository productRepository, IFileService fileService)
        {
            _productRepository = productRepository;
            _fileService = fileService;
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

        /// <summary>
        /// Asocia una imagen a un producto específico.
        /// </summary>
        /// <param name="productId">El ID del producto al que se asociará la imagen.</param>
        /// <param name="file">El archivo de imagen a asociar.</param>
        /// <returns>Una tarea que representa la operación asíncrona, con true si la imagen se asoció correctamente, false en caso contrario.</returns>
        public async Task<bool> UploadImageAsync(int productId, IFormFile file)
        {
            // Verifica que el producto exista (vista admin)
            var product = await _productRepository.GetByIdForAdminAsync(productId)
                        ?? throw new KeyNotFoundException("Producto no encontrado.");


            return await _fileService.UploadAsync(file, productId);
        }



    }
}