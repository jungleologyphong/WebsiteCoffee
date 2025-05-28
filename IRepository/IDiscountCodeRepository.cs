using WebsiteCoffeeShop.Models;

namespace WebsiteCoffeeShop.IRepository
{
    public interface IDiscountCodeRepository
    {
        Task<IEnumerable<DiscountCode>> GetAllAsync();
        Task<DiscountCode> GetByIdAsync(int id);
        Task<DiscountCode> GetByCodeAsync(string code);
        Task AddAsync(DiscountCode discountCode);
        Task UpdateAsync(DiscountCode discountCode);
        Task DeleteAsync(int id);
    }
}
