using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebsiteCoffeeShop.Context;
using WebsiteCoffeeShop.Extensions;
using WebsiteCoffeeShop.IRepository;
using WebsiteCoffeeShop.Models;

namespace we.Controllers
{
    [Authorize(Roles = SD.Role_Customer)] // Yêu cầu đăng nhập và phải là khách hàng
    public class ShoppingCartController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShoppingCartController(IProductRepository productRepository, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _productRepository = productRepository;
            _context = context;
            _userManager = userManager;
        }

        // Hiển thị trang thanh toán
        public IActionResult Checkout()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
            var cartItems = cart.Items; // Lấy danh sách sản phẩm trong giỏ hàng

            var order = new Order
            {
                TotalPrice = cartItems.Sum(item => item.Price * item.Quantity)
            };

            ViewData["CartItems"] = cartItems; // Truyền danh sách sản phẩm vào View
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(Order order)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");

            if (cart == null || !cart.Items.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống!";
                return RedirectToAction("Index");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để đặt hàng.";
                return RedirectToAction("Login", "Account");
            }

            // Đảm bảo DiscountCodeId là null nếu không được chọn
            // Hoặc kiểm tra xem mã giảm giá có tồn tại không
            if (order.DiscountCodeId.HasValue)
            {
                var discountExists = await _context.DiscountCodes.AnyAsync(d => d.Id == order.DiscountCodeId.Value);
                if (!discountExists)
                {
                    // Nếu mã không tồn tại, đặt về null
                    order.DiscountCodeId = null;
                    ModelState.AddModelError("DiscountCodeId", "Mã giảm giá không hợp lệ!");
                }
            }

            // Tính tổng tiền
            order.UserId = user.Id;
            order.OrderDate = DateTime.UtcNow;

            // Tính tổng tiền từ giỏ hàng
            decimal subTotal = cart.Items.Sum(item => item.Price * item.Quantity);

            // Áp dụng giảm giá từ điểm thưởng nếu có
            if (order.RewardPointsUsed > 0)
            {
                if (order.RewardPointsUsed > user.RewardPoints)
                {
                    TempData["ErrorMessage"] = "Bạn không đủ điểm thưởng!";
                    return RedirectToAction("Checkout");
                }

                // Quy đổi điểm thành tiền (giả sử 1 điểm = 1000 VNĐ)
                order.DiscountFromPoints = order.RewardPointsUsed * 1000;
            }

            order.TotalPrice = subTotal - order.DiscountFromPoints;

            // Áp dụng mã giảm giá nếu có
            if (order.DiscountCodeId.HasValue)
            {
                var discountCode = await _context.DiscountCodes.FindAsync(order.DiscountCodeId.Value);
                if (discountCode != null)
                {
                    // Kiểm tra hạn sử dụng
                    if (discountCode.ExpiryDate < DateTime.Now)
                    {
                        TempData["WarningMessage"] = "Mã giảm giá đã hết hạn!";
                        order.DiscountCodeId = null;
                    }
                    // Kiểm tra trạng thái kích hoạt
                    else if (!discountCode.IsActive)
                    {
                        TempData["WarningMessage"] = "Mã giảm giá không còn hiệu lực!";
                        order.DiscountCodeId = null;
                    }
                    else
                    {
                        // Áp dụng giảm giá theo phần trăm hoặc số tiền cố định
                        if (discountCode.IsPercentage)
                        {
                            decimal discountAmount = order.TotalPrice * discountCode.DiscountPercent / 100;
                            order.TotalPrice -= discountAmount;
                        }
                        else
                        {
                            order.TotalPrice = Math.Max(0, order.TotalPrice - discountCode.DiscountAmount);
                        }
                    }
                }
                else
                {
                    // Nếu không tìm thấy mã giảm giá, đặt lại về null
                    order.DiscountCodeId = null;
                }
            }

            order.Status = "Chờ xác nhận";
            order.OrderDetails = cart.Items.Select(i => new OrderDetail
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                Price = i.Price
            }).ToList();

            // Tính điểm thưởng được nhận (1.000đ = 1 điểm)
            order.RewardPointsEarned = (int)(order.TotalPrice / 1000);

            _context.Orders.Add(order);

            // Cập nhật số lượng tồn kho
            foreach (var item in cart.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null && product.Quantity >= item.Quantity)
                {
                    product.Quantity -= item.Quantity;
                }
                else
                {
                    TempData["ErrorMessage"] = $"Sản phẩm {item.Name} không đủ hàng!";
                    return RedirectToAction("Index");
                }
            }

            // Trừ điểm thưởng đã sử dụng và cộng điểm thưởng mới
            if (order.RewardPointsUsed > 0)
            {
                user.RewardPoints -= order.RewardPointsUsed;
            }
            user.RewardPoints += order.RewardPointsEarned;
            _context.Users.Update(user);

            try
            {
                await _context.SaveChangesAsync();

                HttpContext.Session.Remove("Cart");
                // ✅ Nếu người dùng chọn "Chuyển khoản ngân hàng" (BankTransfer), chuyển khoảng ngân han
                if (order.PaymentMethod == "VNPAY")
                {
                    // Gửi tới VNPay (PaymentController)
                    return RedirectToAction("CreatePaymentUrl", "Payment", new { amount = order.TotalPrice, orderId = order.Id });
                }

                TempData["SuccessMessage"] = $"Đơn hàng đã đặt thành công! Bạn nhận được {order.RewardPointsEarned} điểm thưởng 🎉";
                return RedirectToAction("OrderCompleted", new { orderId = order.Id });
            }
            catch (DbUpdateException ex)
            {
                // Xử lý lỗi khóa ngoại
                if (ex.InnerException != null && ex.InnerException.Message.Contains("FK_Orders_DiscountCodes_DiscountCodeId"))
                {
                    // Xử lý lỗi liên quan đến khóa ngoại DiscountCodeId
                    order.DiscountCodeId = null; // Đặt về null để đảm bảo không bị lỗi

                    // Thử lưu lại
                    try
                    {
                        await _context.SaveChangesAsync();
                        HttpContext.Session.Remove("Cart");
                        TempData["WarningMessage"] = "Đơn hàng đã được đặt nhưng không áp dụng mã giảm giá do mã không hợp lệ!";
                        return RedirectToAction("OrderCompleted", new { orderId = order.Id });
                    }
                    catch (Exception innerEx)
                    {
                        // Ghi log lỗi
                        Console.WriteLine($"Error: {innerEx.Message}");
                        TempData["ErrorMessage"] = "Có lỗi xảy ra khi đặt hàng. Vui lòng thử lại sau!";
                    }
                }
                else
                {
                    // Ghi log lỗi
                    Console.WriteLine($"Error: {ex.Message}");
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi đặt hàng. Vui lòng thử lại sau!";
                }

                // Trả về view với thông báo lỗi
                ViewData["CartItems"] = cart.Items;
                return View(order);
            }
        }

        public async Task<IActionResult> OrderCompleted(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound();
            }

            // Lấy thông tin sản phẩm cho từng chi tiết đơn hàng
            var cartItems = new List<CartItem>();
            foreach (var item in order.OrderDetails)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    cartItems.Add(new CartItem
                    {
                        ProductId = item.ProductId,
                        Name = product.Name,
                        Price = item.Price,
                        Quantity = item.Quantity,
                        ImageUrl = product.ImageUrl
                    });
                }
            }

            ViewData["CartItems"] = cartItems;

            // Tính toán số tiền giảm giá từ mã giảm giá (nếu có)
            if (order.DiscountCodeId.HasValue)
            {
                var discountCode = await _context.DiscountCodes.FindAsync(order.DiscountCodeId.Value);
                if (discountCode != null)
                {
                    decimal subtotal = cartItems.Sum(item => item.Price * item.Quantity);
                    decimal discountAmount = 0;

                    // Tính số tiền giảm giá dựa trên loại giảm giá
                    if (discountCode.IsPercentage)
                    {
                        // Giảm giá theo phần trăm
                        discountAmount = subtotal * discountCode.DiscountPercent / 100;
                    }
                    else
                    {
                        // Giảm giá cố định
                        discountAmount = discountCode.DiscountAmount;
                    }

                    // Trừ đi giảm giá từ điểm thưởng để tính chính xác số tiền giảm từ mã giảm giá
                    decimal adjustedSubtotal = subtotal - order.DiscountFromPoints;
                    if (discountCode.IsPercentage)
                    {
                        discountAmount = adjustedSubtotal * discountCode.DiscountPercent / 100;
                    }

                    ViewData["DiscountCodeAmount"] = discountAmount;
                    ViewData["DiscountCode"] = discountCode;
                }
            }

            return View(order);
        }

        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại!";
                return RedirectToAction("Index", "Home");
            }

            // 🖼️ Chỉ lưu tên file, bỏ đi "wwwroot/Images/"
            string imageUrl = product.ImageUrl.Replace("wwwroot/Images/", "").Trim();

            Console.WriteLine($"🖼️ ImageUrl của {product.Name}: {imageUrl}"); // Debug

            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
            cart.AddItem(new CartItem
            {
                ProductId = productId,
                Name = product.Name,
                Price = product.Price,
                ImageUrl = imageUrl,  // ✅ Đã chuẩn hóa đường dẫn
                Quantity = quantity
            });

            HttpContext.Session.SetObjectAsJson("Cart", cart);

            return RedirectToAction("Index");
        }


        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
            return View(cart);
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (cart == null || cart.Items.All(i => i.ProductId != productId))
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng." });
            }

            cart.RemoveItem(productId);
            HttpContext.Session.SetObjectAsJson("Cart", cart);

            return Json(new { success = true });
        }

        public IActionResult ClearCart()
        {
            HttpContext.Session.Remove("Cart");
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

            if (item == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng!" });
            }

            // Kiểm tra số lượng tồn kho từ database
            var product = _context.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không hợp lệ!" });
            }

            if (product.Quantity == 0)
            {
                return Json(new { success = false, message = "Sản phẩm đã hết hàng!" });
            }

            if (quantity > product.Quantity)
            {
                return Json(new
                {
                    success = false,
                    message = $"Số lượng vượt quá tồn kho! Chỉ còn {product.Quantity} sản phẩm."
                });
            }

            // Cập nhật số lượng trong giỏ hàng
            item.Quantity = quantity;
            HttpContext.Session.SetObjectAsJson("Cart", cart);

            return Json(new { success = true });
        }

        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null || order.Status != "Chờ xác nhận")
            {
                return NotFound();
            }

            return View(order);
        }
    }
}
