using WebsiteCoffeeShop.Models;

namespace WebsiteCoffeeShop.IRepository
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product> GetByIdAsync(int id);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(int id);
        Task<List<Product>> GetPaginatedProductsAsync(int page, int pageSize);
        Task<IEnumerable<Product>> SearchProductsAsync(string keyword);

    }
}
