using Microsoft.EntityFrameworkCore;
using WebsiteCoffeeShop.Context;
using WebsiteCoffeeShop.IRepository;
using WebsiteCoffeeShop.Models;

namespace WebsiteCoffeeShop.Repositories
{
    public class DiscountCodeRepository : IDiscountCodeRepository
    {
        private readonly ApplicationDbContext _context;

        public DiscountCodeRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<DiscountCode>> GetAllAsync()
        {
            return await _context.DiscountCodes.ToListAsync();
        }

        public async Task<DiscountCode> GetByIdAsync(int id)
        {
            return await _context.DiscountCodes.FindAsync(id);
        }

        public async Task<DiscountCode> GetByCodeAsync(string code)
        {
            return await _context.DiscountCodes
                .FirstOrDefaultAsync(d => d.Code.ToLower() == code.ToLower());
        }

        public async Task AddAsync(DiscountCode discountCode)
        {
            _context.DiscountCodes.Add(discountCode);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(DiscountCode discountCode)
        {
            _context.DiscountCodes.Update(discountCode);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.DiscountCodes.FindAsync(id);
            if (entity != null)
            {
                _context.DiscountCodes.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}
