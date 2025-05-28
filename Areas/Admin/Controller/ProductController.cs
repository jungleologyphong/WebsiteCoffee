using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using WebsiteCoffeeShop.Models;
using WebsiteCoffeeShop.IRepository;

namespace WebsiteCoffeeShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public ProductController(IProductRepository productRepository , ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        [Authorize(Roles = "Admin,Employer")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var products = await _productRepository.GetAllAsync();
                return View(products);
            }
            catch (Exception ex)
            {
                // Ghi log hoặc xem lỗi trực tiếp
                return Content("Lỗi: " + ex.Message);
            }
        }

        [Authorize(Roles = "Admin")] // Chỉ Admin mới có quyền thêm sản phẩm
        public async Task<IActionResult> Add()
        {
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add(Product product, IFormFile imageUrl)
        {
            if (ModelState.IsValid)
            {
                if (imageUrl != null)
                {
                    var imagePath = await SaveImage(imageUrl);
                    if (imagePath == null)
                    {
                        ModelState.AddModelError("ImageUrl", "Chỉ nhận file đuôi .jpg, .jpeg, .png, .gif");
                        return View(product);
                    }
                    product.ImageUrl = imagePath;
                }
                await _productRepository.AddAsync(product);
                TempData["SuccessMessage"] = "Product added successfully!";
                return RedirectToAction(nameof(Index));
            }
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(product);
        }

        [Authorize(Roles = "Admin,Employer")] // Cả Admin & Employer có thể sửa
        public async Task<IActionResult> Update(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Employer")]
        public async Task<IActionResult> Update(int id, Product product, IFormFile imageUrl)
        {
            if (id != product.Id) return Json(new { success = false, message = "Invalid product ID" });

            if (ModelState.IsValid)
            {
                var existingProduct = await _productRepository.GetByIdAsync(id);
                if (existingProduct == null) return Json(new { success = false, message = "Product not found" });

                existingProduct.Name = product.Name;
                existingProduct.Price = product.Price;
                existingProduct.Description = product.Description;
                existingProduct.CategoryId = product.CategoryId;

                if (imageUrl != null)
                {
                    var imagePath = await SaveImage(imageUrl);
                    if (imagePath == null)
                    {
                        return Json(new { success = false, message = "Invalid image format. Only .jpg, .jpeg, .png, .gif allowed." });
                    }
                    existingProduct.ImageUrl = imagePath;
                }

                await _productRepository.UpdateAsync(existingProduct);
                return Json(new { success = true, message = "Product updated successfully!" });
            }

            return Json(new { success = false, message = "Validation failed", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        [Authorize(Roles = "Admin")] // Chỉ Admin mới có quyền xóa
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();
            await _productRepository.DeleteAsync(id);
            TempData["SuccessMessage"] = "Product deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveImage(IFormFile image)
        {
            if (image == null || image.Length == 0) return null;
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(image.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension)) return null;

            var fileName = Guid.NewGuid().ToString() + fileExtension;
            var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images", fileName);
            using (var fileStream = new FileStream(savePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }
            return "/Images/" + fileName;
        }
        public async Task<IActionResult> Display(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();

            var category = await _categoryRepository.GetByIdAsync(product.CategoryId);
            ViewBag.CategoryName = category != null ? category.Name : "Unknown";

            return View(product);
        }

    }
}
