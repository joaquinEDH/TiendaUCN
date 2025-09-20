using Tienda.src.Domain.Models;

namespace Tienda.src.Infrastructure.Repositories.Interfaces
{
    /// <summary>
    /// Operaciones de acceso a datos para usuarios (mínimas para register/login).
    /// </summary>
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByRutAsync(string rut, bool trackChanges = false);

        Task<bool> ExistsByEmailAsync(string email);
        Task<bool> ExistsByRutAsync(string rut);

        Task<bool> CreateAsync(User user, string password);
        Task<bool> CheckPasswordAsync(User user, string password);
        Task<string> GetUserRoleAsync(User user);

        Task<bool> ConfirmEmailAsync(string email);
        Task<bool> DeleteAsync(int userId);
        Task<int> DeleteUnconfirmedAsync();
    }
}