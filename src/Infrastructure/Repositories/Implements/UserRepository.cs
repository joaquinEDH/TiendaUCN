using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Tienda.src.Domain.Models;
using Tienda.src.Infrastructure.Data;
using Tienda.src.Infrastructure.Repositories.Interfaces;

namespace Tienda.src.Infrastructure.Repositories.Implements
{
    /// <summary>
    /// Implementación mínima para soportar Register + Login con JWT.
    /// (Más adelante le agregamos lo de códigos de verificación).
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;

        // valor por defecto solo para que compile el método DeleteUnconfirmedAsync;
        // lo reemplazaremos cuando metamos verificación por correo.
        private readonly int _daysOfDeleteUnconfirmedUsers = -30;

        public UserRepository(
            DataContext context,
            UserManager<User> userManager,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;

            // Si existe la key en appsettings la usamos; si no, dejamos el default
            var days = configuration.GetValue<int?>("Jobs:DaysOfDeleteUnconfirmedUsers");
            if (days.HasValue) _daysOfDeleteUnconfirmedUsers = days.Value;
        }

        public async Task<User?> GetByEmailAsync(string email)
            => await _userManager.FindByEmailAsync(email);

        public async Task<User?> GetByIdAsync(int id)
            => await _userManager.FindByIdAsync(id.ToString());

        public async Task<User?> GetByRutAsync(string rut, bool trackChanges = false)
        {
            var query = trackChanges ? _context.Users : _context.Users.AsNoTracking();
            return await query.FirstOrDefaultAsync(u => u.Rut == rut);
        }

        public async Task<bool> ExistsByEmailAsync(string email)
            => await _context.Users.AnyAsync(u => u.Email == email);

        public async Task<bool> ExistsByRutAsync(string rut)
            => await _context.Users.AnyAsync(u => u.Rut == rut);

        public async Task<bool> CreateAsync(User user, string password)
        {
            var create = await _userManager.CreateAsync(user, password);
            if (!create.Succeeded) return false;

            // rol por defecto “Customer”
            var roleResult = await _userManager.AddToRoleAsync(user, "Customer");
            return roleResult.Succeeded;
        }

        public async Task<bool> CheckPasswordAsync(User user, string password)
            => await _userManager.CheckPasswordAsync(user, password);

        public async Task<string> GetUserRoleAsync(User user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            return roles.FirstOrDefault() ?? "Customer";
        }

        // Métodos pensados para la siguiente etapa (email verification).
        public async Task<bool> ConfirmEmailAsync(string email)
        {
            var affected = await _context.Users
                .Where(u => u.Email == email)
                .ExecuteUpdateAsync(setter => setter.SetProperty(u => u.EmailConfirmed, true));

            return affected > 0;
        }

        public async Task<bool> DeleteAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null) return false;

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }

        public async Task<int> DeleteUnconfirmedAsync()
        {
            try
            {
                var cutoff = DateTime.UtcNow.AddDays(_daysOfDeleteUnconfirmedUsers);
                var unconfirmed = await _context.Users
                    .Where(u => !u.EmailConfirmed && u.RegisteredAt < cutoff)
                    .ToListAsync();

                if (!unconfirmed.Any()) return 0;

                _context.Users.RemoveRange(unconfirmed);
                await _context.SaveChangesAsync();
                return unconfirmed.Count;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error eliminando usuarios no confirmados");
                return 0;
            }
        }
    }
}