using WebsiteCoffeeShop.DTO;
using WebsiteCoffeeShop.Models;

namespace WebsiteCoffeeShop.IRepository
{
    public interface IOrderRepository
    {
        Task<List<Order>> GetOrdersByUserAsync(string userId); // Lấy đơn hàng của người dùng
        Task<Order> GetByIdAsync(int id); // Lấy đơn hàng theo ID
        Task AddAsync(Order order); // Thêm đơn hàng mới
        Task UpdateAsync(Order order); // Cập nhật đơn hàng
        Task DeleteAsync(int id); // Xóa đơn hàng
        Task UpdateStatusAsync(int orderId, string newStatus);
        Task<PagedResult<Order>> GetAllOrdersPagedAsync(string? keyword = null, int page = 1, int limit = 10);
        Task<Order> GetOrderByIdToPrint(int id);
    }
}
