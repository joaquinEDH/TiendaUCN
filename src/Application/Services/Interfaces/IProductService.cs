using Microsoft.AspNetCore.Http;
using Tienda.src.Application.DTOs.Product;


namespace Tienda.src.Application.Services.Interfaces
{
    public interface IProductService
    {

        Task<IEnumerable<ListedProductsDTO>> GetAllAsync();
        /// <summary>
        /// Retorna un producto específico por su ID.
        /// </summary>
        /// <param name="id">El ID del producto a buscar.</param>
        /// <returns>Una tarea que representa la operación asíncrona, con el producto encontrado o null si no se encuentra.</returns>
        Task<ProductDetailDTO> GetByIdAsync(int id);

        /// <summary>
        /// Retorna un producto específico por su ID desde el punto de vista de un admin.
        /// </summary>
        /// <param name="id">El ID del producto a buscar.</param>
        /// <returns>Una tarea que representa la operación asíncrona, con el producto encontrado o null si no se encuentra.</returns>
        Task<ProductDetailDTO> GetByIdForAdminAsync(int id);

        /// <summary>
        /// Crea un nuevo producto en el sistema.
        /// </summary>
        /// <param name="createProductDTO">Los datos del producto a crear.</param>
        /// <returns>Una tarea que representa la operación asíncrona, con el id del producto creado.</returns>
        Task<string> CreateAsync(CreateProductDTO createProductDTO);


    }

}