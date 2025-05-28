using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteCoffeeShop.IRepository;
using WebsiteCoffeeShop.Models;


namespace WebsiteCoffeeShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly IOrderRepository _orderRepository; // Inject IOrderRepository
        public OrderController(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        // GET: OrderController
        public ActionResult Index()
        {
            return View();
        }
        [Authorize(Roles = "Admin,Employer")]
        public async Task<IActionResult> History(int page = 1, int limit = 5)
        {
            var result = await _orderRepository.GetAllOrdersPagedAsync(null, page, limit);

            ViewBag.CurrentPage = result.Page;
            ViewBag.TotalPages = result.TotalPages;

            return View(result.Items);
        }


        [Authorize(Roles = "Admin,Employer")]
        public async Task<IActionResult> Details(int id)
        {
  
            var order = await _orderRepository.GetByIdAsync(id);

            if (order == null)
            {
                return NotFound();
            }


            return View(order);
        }
    }
}
