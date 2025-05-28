using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using WebsiteCoffeeShop.Config;
using WebsiteCoffeeShop.IRepository;
using WebsiteCoffeeShop.Models;
using WebsiteCoffeeShop.Utils;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace WebsiteCoffeeShop.Controllers
{
    public class PaymentController : Controller
    {
        private readonly VNPaySettings _vnPaySettings;
        private readonly ILogger<PaymentController> _logger;
        private readonly IOrderRepository _orderRepository;
        public PaymentController(IOptions<VNPaySettings> vnPayOptions, ILogger<PaymentController> logger , IOrderRepository orderRepository)
        {
            _vnPaySettings = vnPayOptions.Value;
            _logger = logger;
            _orderRepository = orderRepository;
        }
        // GET: PaymentController
        public IActionResult CreatePaymentUrl(decimal amount , int orderId)
        {
            _logger.LogInformation("CreatePaymentUrl called with amount={Amount}, orderId={OrderId}", amount, orderId);
            var vnp_TxnRef = orderId.ToString();

            var vnp_OrderInfo = $"Thanh toan don hang #{orderId}";

            var tick = DateTime.Now.Ticks.ToString();
  
            var vnp_Amount = ((long)(amount * 100)).ToString(); // CHỈ CHỨA SỐ NGUYÊN
            var vnp_CreateDate = DateTime.Now.ToString("yyyyMMddHHmmss");
            var vnp_ExpireDate = DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss");

            var vnp_ReturnUrl = _vnPaySettings.ReturnUrl;



            var vnp_Url = _vnPaySettings.BaseUrl;
            var vnp_TmnCode = _vnPaySettings.TmnCode;
            var vnp_HashSecret = _vnPaySettings.HashSecret;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (ipAddress == "::1")
            {
                ipAddress = "127.0.0.1";
            }

            PayLib pay = new PayLib();
            pay.AddRequestData("vnp_Version", "2.1.0"); //Phiên bản api mà merchant kết nối. Phiên bản hiện tại là 2.1.0
            pay.AddRequestData("vnp_Command", "pay"); //Mã API sử dụng, mã cho giao dịch thanh toán là 'pay'
            pay.AddRequestData("vnp_TmnCode", vnp_TmnCode); //Mã website của merchant trên hệ thống của VNPAY (khi đăng ký tài khoản sẽ có trong mail VNPAY gửi về)
            pay.AddRequestData("vnp_Amount", vnp_Amount); //số tiền cần thanh toán, công thức: số tiền * 100 - ví dụ 10.000 (mười nghìn đồng) --> 1000000
            pay.AddRequestData("vnp_BankCode", "NCB"); //Mã Ngân hàng thanh toán (tham khảo: https://sandbox.vnpayment.vn/apis/danh-sach-ngan-hang/), có thể để trống, người dùng có thể chọn trên cổng thanh toán VNPAY
            pay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss")); //ngày thanh toán theo định dạng yyyyMMddHHmmss
            pay.AddRequestData("vnp_CurrCode", "VND"); //Đơn vị tiền tệ sử dụng thanh toán. Hiện tại chỉ hỗ trợ VND
            pay.AddRequestData("vnp_IpAddr", ipAddress ?? "127.0.0.1"); //Địa chỉ IP của khách hàng thực hiện giao dịch
            pay.AddRequestData("vnp_Locale", "vn"); //Ngôn ngữ giao diện hiển thị - Tiếng Việt (vn), Tiếng Anh (en)
            pay.AddRequestData("vnp_OrderInfo", vnp_OrderInfo); //Thông tin mô tả nội dung thanh toán
            pay.AddRequestData("vnp_OrderType", "other"); //topup: Nạp tiền điện thoại - billpayment: Thanh toán hóa đơn - fashion: Thời trang - other: Thanh toán trực tuyến
            pay.AddRequestData("vnp_ReturnUrl", vnp_ReturnUrl); //URL thông báo kết quả giao dịch khi Khách hàng kết thúc thanh toán
            pay.AddRequestData("vnp_TxnRef", vnp_TxnRef); //mã hóa đơn


            string paymentUrl = pay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            return Redirect(paymentUrl);
        }

        [HttpGet]
        public async Task<IActionResult> PaymentConfirm()
        {
            if (Request.QueryString.HasValue)
            {
                //lấy toàn bộ dữ liệu trả về
                var queryString = Request.QueryString.Value;
                var json = HttpUtility.ParseQueryString(queryString);

                int orderId = (int)Convert.ToInt64(json["vnp_TxnRef"]);
                //mã hóa đơn
                string orderInfor = json["vnp_OrderInfo"].ToString(); //Thông tin giao dịch
                long vnpayTranId = Convert.ToInt64(json["vnp_TransactionNo"]); //mã giao dịch tại hệ thống VNPAY
                string vnp_ResponseCode = json["vnp_ResponseCode"].ToString(); //response code: 00 - thành công, khác 00 - xem thêm https://sandbox.vnpayment.vn/apis/docs/bang-ma-loi/
                string vnp_SecureHash = json["vnp_SecureHash"].ToString(); //hash của dữ liệu trả về
                var pos = Request.QueryString.Value.IndexOf("&vnp_SecureHash");

                //return Ok(Request.QueryString.Value.Substring(1, pos-1) + "\n" + vnp_SecureHash + "\n"+ PayLib.HmacSHA512(hashSecret, Request.QueryString.Value.Substring(1, pos-1)));
                bool checkSignature = ValidateSignature(Request.QueryString.Value.Substring(1, pos - 1), vnp_SecureHash, _vnPaySettings.HashSecret); //check chữ ký đúng hay không?
                if (checkSignature && _vnPaySettings.TmnCode == json["vnp_TmnCode"].ToString())
                {
                    if (vnp_ResponseCode == "00")
                    {
                        //Thanh toán thành công
                        // Thanh toán thành công → chuyển hướng đến OrderCompleted
                        await _orderRepository.UpdateStatusAsync(orderId, "Completed");

                        TempData["SuccessMessage"] = "Thanh toán VNPay thành công!";
                        return RedirectToAction("OrderCompleted", "Order", new { id = orderId });
                    }
                    else
                    {
                        //Thanh toán không thành công. Mã lỗi: vnp_ResponseCode
                        // Thanh toán thất bại
                        TempData["ErrorMessage"] = "Thanh toán VNPay thất bại. Vui lòng thử lại!";
                        return RedirectToAction("Checkout", "Order");
                    }
                }
                else
                {
                    //phản hồi không khớp với chữ ký
                    return Redirect("đường dẫn nếu phản hồi ko hợp lệ");
                }
            }
            //phản hồi không hợp lệ
            return Redirect("đường dẫn nếu phản hồi ko hợp lệ");

        }
        public bool ValidateSignature(string rspraw, string inputHash, string secretKey)
        {
            string myChecksum = PayLib.HmacSHA512(secretKey, rspraw);
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
