using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteCoffeeShop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using WebsiteCoffeeShop.Context;

namespace WebsiteCoffeeShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DiscountCodeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DiscountCodeController(ApplicationDbContext context)
        {
            _context = context;
        }
        [Authorize(Roles = "Admin,Employer")]
        // GET: DiscountCode
        public async Task<IActionResult> Index()
        {
            return View(await _context.DiscountCodes.ToListAsync());
        }
        [Authorize(Roles = "Admin,Employer")]
        // GET: DiscountCode/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var discountCode = await _context.DiscountCodes
                .Include(d => d.Orders)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (discountCode == null) return NotFound();

            return View(discountCode);
        }
        [Authorize(Roles = "Admin")]
        // GET: DiscountCode/Create
        public IActionResult Create()
        {
            var model = new DiscountCode
            {
                ExpiryDate = DateTime.Now.AddDays(7),
                IsActive = true,
                IsPercentage = true, // Default to percentage discount
                DiscountPercent = 10, // Default to 10% discount
                DiscountAmount = 0 // Default to 0 for amount
            };
            return View(model);
        }

        // POST: DiscountCode/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(DiscountCode discountCode)
        {
            // No need to remove Orders as it's initialized in the constructor

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.DiscountCodes.AnyAsync(d => d.Code == discountCode.Code))
                    {
                        ModelState.AddModelError("Code", "Mã giảm giá này đã tồn tại");
                        return View(discountCode);
                    }

                    // Validate discount logic
                    if (discountCode.IsPercentage)
                    {
                        if (discountCode.DiscountPercent <= 0 || discountCode.DiscountPercent > 100)
                        {
                            ModelState.AddModelError("DiscountPercent", "Phần trăm giảm giá phải từ 1 đến 100");
                            return View(discountCode);
                        }
                        discountCode.DiscountAmount = 0; // Reset amount if percentage is used
                    }
                    else
                    {
                        if (discountCode.DiscountAmount <= 0)
                        {
                            ModelState.AddModelError("DiscountAmount", "Số tiền giảm giá phải lớn hơn 0");
                            return View(discountCode);
                        }
                        discountCode.DiscountPercent = 0; // Reset percentage if amount is used
                    }

                    // Orders collection is already initialized in the constructor

                    _context.Add(discountCode);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Mã giảm giá đã được tạo thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Lỗi khi lưu mã giảm giá: " + ex.Message);
                    if (ex.InnerException != null)
                        ModelState.AddModelError(string.Empty, "Chi tiết lỗi: " + ex.InnerException.Message);
                }
            }
            return View(discountCode);
        }

        // GET: DiscountCode/Edit/5
        [Authorize(Roles = "Admin,Employer")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var discountCode = await _context.DiscountCodes.FindAsync(id);
            if (discountCode == null) return NotFound();

            return View(discountCode);
        }

        // POST: DiscountCode/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employer")]
        public async Task<IActionResult> Edit(int id, DiscountCode discountCode)
        {
            if (id != discountCode.Id) return NotFound();

            if (!ModelState.IsValid) return View(discountCode);

            try
            {
                if (await _context.DiscountCodes
                    .AnyAsync(d => d.Code == discountCode.Code && d.Id != discountCode.Id))
                {
                    ModelState.AddModelError("Code", "Mã giảm giá này đã tồn tại");
                    return View(discountCode);
                }

                // Validate discount logic
                if (discountCode.IsPercentage)
                {
                    if (discountCode.DiscountPercent <= 0 || discountCode.DiscountPercent > 100)
                    {
                        ModelState.AddModelError("DiscountPercent", "Phần trăm giảm giá phải từ 1 đến 100");
                        return View(discountCode);
                    }
                    discountCode.DiscountAmount = 0; // Reset amount if percentage is used
                }
                else
                {
                    if (discountCode.DiscountAmount <= 0)
                    {
                        ModelState.AddModelError("DiscountAmount", "Số tiền giảm giá phải lớn hơn 0");
                        return View(discountCode);
                    }
                    discountCode.DiscountPercent = 0; // Reset percentage if amount is used
                }

                _context.Update(discountCode);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Mã giảm giá đã được cập nhật thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DiscountCodeExists(discountCode.Id)) return NotFound();
                else throw;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi khi cập nhật mã giảm giá: " + ex.Message);
                return View(discountCode);
            }
        }

        // GET: DiscountCode/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var discountCode = await _context.DiscountCodes
                .Include(d => d.Orders)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (discountCode == null) return NotFound();

            return View(discountCode);
        }

        // POST: DiscountCode/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var discountCode = await _context.DiscountCodes.FindAsync(id);
                if (discountCode == null) return NotFound();

                bool isUsed = await _context.Orders.AnyAsync(o => o.DiscountCodeId == id);
                if (isUsed)
                {
                    TempData["Error"] = "Không thể xóa mã giảm giá này vì đã được sử dụng trong đơn hàng!";
                    return RedirectToAction(nameof(Index));
                }

                _context.DiscountCodes.Remove(discountCode);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Mã giảm giá đã được xóa thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi xóa mã giảm giá: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: DiscountCode/Toggle/5
        public async Task<IActionResult> Toggle(int? id)
        {
            if (id == null) return NotFound();

            var discountCode = await _context.DiscountCodes.FindAsync(id);
            if (discountCode == null) return NotFound();

            discountCode.IsActive = !discountCode.IsActive;
            await _context.SaveChangesAsync();

            string status = discountCode.IsActive ? "kích hoạt" : "vô hiệu hóa";
            TempData["Success"] = $"Mã giảm giá đã được {status} thành công!";
            return RedirectToAction(nameof(Index));
        }

        // GET: DiscountCode/Validate?code=XXX
        [HttpGet]
        public async Task<IActionResult> Validate(string code)
        {
            if (string.IsNullOrEmpty(code))
                return Json(new { valid = false, message = "Vui lòng nhập mã giảm giá" });

            var discountCode = await _context.DiscountCodes
                .FirstOrDefaultAsync(d => d.Code == code);

            if (discountCode == null)
                return Json(new { valid = false, message = "Mã giảm giá không tồn tại" });

            if (!discountCode.IsActive)
                return Json(new { valid = false, message = "Mã giảm giá đã bị vô hiệu hóa" });

            if (discountCode.ExpiryDate < DateTime.Now)
                return Json(new { valid = false, message = "Mã giảm giá đã hết hạn" });

            // Display discount value
            string discountValue = discountCode.IsPercentage
                ? discountCode.DiscountPercent + "%"
                : discountCode.DiscountAmount.ToString("N0") + "đ";

            return Json(new
            {
                valid = true,
                message = $"Áp dụng giảm giá: {discountValue}",
                id = discountCode.Id,
                discountPercent = discountCode.DiscountPercent,
                discountAmount = discountCode.DiscountAmount,
                isPercentage = discountCode.IsPercentage,
                description = discountCode.Description // Added description to returned data
            });
        }

        private bool DiscountCodeExists(int id)
        {
            return _context.DiscountCodes.Any(e => e.Id == id);
        }
    }
}