using Microsoft.EntityFrameworkCore;
using Tienda.src.Domain.Models;
using Tienda.src.Infrastructure.Data;
using Tienda.src.Infrastructure.Repositories.Interfaces;

namespace Tienda.src.Infrastructure.Repositories.Implements
{
    public class ProductRepository : IProductRepository
    {
        private readonly DataContext _context;
        public ProductRepository(DataContext context) { _context = context; }

        public async Task<IEnumerable<Product>> GetAllAsync()
            => await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .ToListAsync();

        public async Task<Product?> GetByIdAsync(int id)
            => await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsAvailable);

        public async Task<Product> CreateAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Product product)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }

        // Admin: sin filtrar IsAvailable
        public async Task<Product?> GetByIdForAdminAsync(int id)
            => await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.Id == id);
    }
}