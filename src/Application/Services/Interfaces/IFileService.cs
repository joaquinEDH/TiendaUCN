namespace Tienda.src.Application.Services.Interfaces
{
    public interface IFileService
    {
        /// <summary>
        /// Sube un archivo a Cloudinary 
        /// </summary>
        /// <param name="file">
        /// Archivo a subir
        /// </param>
        /// <param name="productId">ID del producto asociado</param>
        /// <returns>True si se subió correctamente, false si no</returns>
        Task<bool> UploadAsync(IFormFile file, int productId);

        /// <summary>
        /// Se elimina un archivo de Cloudinary
        /// </summary>
        /// <param name="publicId"></param>
        /// <returns>True si se eliminó correctamente, false si no</returns>
        Task<bool> DeleteAsync(string publicId);

    }
}