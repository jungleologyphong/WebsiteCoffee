using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebsiteCoffeeShop.Context;
using WebsiteCoffeeShop.DTO;
using WebsiteCoffeeShop.IRepository;
using WebsiteCoffeeShop.Models;
using static NuGet.Packaging.PackagingConstants;

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Order>> GetOrdersByUserAsync(string userId)
    {
        return await _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product) // Lấy thông tin sản phẩm trong đơn hàng
            .OrderByDescending(o => o.OrderDate)
            .Include(o => o.ApplicationUser)
            .ToListAsync();
    }
    public async Task<PagedResult<Order>> GetAllOrdersPagedAsync(string? keyword = null, int page = 1, int limit = 10)
    {
        var query = _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .Include(o => o.ApplicationUser)
            .AsQueryable();

        if (!string.IsNullOrEmpty(keyword))
        {
            keyword = keyword.ToLower();
            query = query.Where(o =>
                o.OrderDetails.Any(od =>
                    od.Product.Name.ToLower().Contains(keyword)));
        }

        int totalItems = await query.CountAsync();

        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return new PagedResult<Order>
        {
            Items = orders,
            TotalItems = totalItems,
            Page = page,
            Limit = limit
        };
    }



    public async Task<Order> GetByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .Include(o => o.ApplicationUser)
            .FirstOrDefaultAsync(o => o.Id == id);
    }
    public async Task<Order> GetOrderByIdToPrint(int id)
    {
        return await _context.Orders
         .Include(o => o.OrderDetails)
             .ThenInclude(od => od.Product)
         .Include(o => o.DiscountCode)
         .Include(o => o.ApplicationUser) // 👈 THÊM DÒNG NÀY
         .FirstOrDefaultAsync(o => o.Id == id);
    }
    public async Task AddAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Order order)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
    }
    public async Task UpdateStatusAsync(int orderId, string newStatus)
    {
        var order = new Order { Id = orderId, Status = newStatus };
        _context.Attach(order);
        _context.Entry(order).Property(o => o.Status).IsModified = true;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order != null)
        {
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
        }
    }
}
