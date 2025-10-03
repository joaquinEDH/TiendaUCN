using Mapster;
using Tienda.src.Application.DTOs.Product;
using Tienda.src.Domain.Models;

namespace Tienda.src.Application.Mappers
{
    public static class ProductMapper
    {
        public static void Configure()
        {
            // Entidad -> Detalle (público / admin)
            TypeAdapterConfig<Product, ProductDetailDTO>
                .NewConfig()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.Title, src => src.Title)
                .Map(dest => dest.Description, src => src.Description ?? string.Empty)
                // Tu entidad usa int Price y el DTO usa decimal → conversión implícita
                .Map(dest => dest.Price, src => src.Price)
                .Map(dest => dest.Stock, src => src.Stock)
                .Map(dest => dest.Category, src => src.Category != null ? src.Category.Name : string.Empty)
                .Map(dest => dest.Brand, src => src.Brand != null ? src.Brand.Name : string.Empty);

            // Entidad -> listado simple público
            TypeAdapterConfig<Product, ListedProductsDTO>
                .NewConfig()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.Title, src => src.Title)
                .Map(dest => dest.Price, src => src.Price)   // tu ListedProductsDTO.Price es int
                .Map(dest => dest.Condition, src => src.Status.ToString());

            // Crear -> Entidad (DTO: decimal → entidad: int)
            TypeAdapterConfig<CreateProductDTO, Product>
                .NewConfig()
                .Map(dest => dest.Title, src => src.Title)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.Price, src => (int)src.Price)
                .Map(dest => dest.Stock, src => src.Stock)
                .Map(dest => dest.CategoryId, src => src.CategoryId)
                .Map(dest => dest.BrandId, src => src.BrandId)
                .Map(dest => dest.Status, src => Status.New)
                .Map(dest => dest.IsAvailable, src => true)
                .AfterMapping((src, dest) =>
                {
                    dest.CreatedAt = DateTime.UtcNow;
                    dest.UpdatedAt = DateTime.UtcNow;
                });
        }
    }
}