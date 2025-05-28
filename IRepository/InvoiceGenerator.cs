using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WebsiteCoffeeShop.Models;

namespace WebsiteCoffeeShop.IRepository
{
    public static class InvoiceGenerator
    {
        public static byte[] GenerateInvoicePdf(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order), "Đơn hàng không được null.");

            if (order.OrderDetails == null || !order.OrderDetails.Any())
                throw new Exception("Đơn hàng không có chi tiết sản phẩm.");

            if (order.OrderDetails.Any(od => od.Product == null))
                throw new Exception("Một hoặc nhiều chi tiết đơn hàng không có thông tin sản phẩm.");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Arial"));

                    // Header
                    page.Header().Column(header =>
                    {
                        header.Item().Text("CÀ PHÊ NHÀ EM ☕").FontSize(22).Bold().AlignCenter().FontColor(Colors.Orange.Medium);
                        header.Item().Container()
                        .PaddingBottom(10)
                        .Text("HÓA ĐƠN BÁN HÀNG")
                        .FontSize(18)
                        .Bold()
                        .AlignCenter();

                        header.Item().Text($"Mã hóa đơn: #{order.Id}").SemiBold();
                        header.Item().Text($"Ngày tạo: {order.OrderDate:dd/MM/yyyy HH:mm}");
                        header.Item().Text($"Khách hàng: {order.ApplicationUser?.FullName ?? "Không rõ"}");
                        header.Item().Text($"Địa chỉ giao hàng: {order.ShippingAddress ?? "Không rõ"}");
                        header.Item().Text($"Ghi chú: {order.Notes ?? "(Không có)"}");
                    });

                    // Content - Product Table
                    page.Content().PaddingVertical(15).Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3); // Tên sản phẩm
                                columns.RelativeColumn(1); // SL
                                columns.RelativeColumn(2); // Giá
                                columns.RelativeColumn(2); // Thành tiền
                            });

                            // Header row
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Sản phẩm").Bold();
                                header.Cell().Element(CellStyle).AlignCenter().Text("SL").Bold();
                                header.Cell().Element(CellStyle).AlignRight().Text("Giá").Bold();
                                header.Cell().Element(CellStyle).AlignRight().Text("Thành tiền").Bold();
                            });

                            // Rows
                            foreach (var item in order.OrderDetails)
                            {
                                var productName = item.Product?.Name ?? "Không rõ";
                                var price = item.Price;
                                var quantity = item.Quantity;
                                var total = price * quantity;

                                table.Cell().Element(CellStyle).Text(productName);
                                table.Cell().Element(CellStyle).AlignCenter().Text(quantity.ToString());
                                table.Cell().Element(CellStyle).AlignRight().Text($"{price:N0}đ");
                                table.Cell().Element(CellStyle).AlignRight().Text($"{total:N0}đ");
                            }
                        });
                        // Thông tin giảm giá (nếu có)
                        if (order.DiscountFromPoints > 0)
                        {
                            col.Item().PaddingTop(10).Text($"Giảm từ điểm tích lũy: -{order.DiscountFromPoints:N0}đ");
                        }

                        if (order.DiscountCode != null)
                        {
                            col.Item().Text($"Mã giảm giá ({order.DiscountCode.Code}): -{order.DiscountCode.DiscountAmount:N0}đ");
                        }

                        // Tổng cộng và phương thức thanh toán (dưới bảng)
                        col.Item().PaddingTop(15).Column(summary =>
                        {
                            summary.Item().Text($"Tổng cộng: {order.TotalPrice:N0}đ").Bold().FontSize(14);
                            summary.Item().Text($"Phương thức thanh toán: {order.PaymentMethod ?? "Không rõ"}");
                        });
                    });

                    // Footer
                    page.Footer().Column(footer =>
                    {
                        footer.Item().AlignCenter().PaddingTop(20).Text("Cảm ơn bạn đã ghé thăm Cà Phê NHÀ EM ☕").Italic().FontSize(12).FontColor(Colors.Grey.Darken1);
                    });
                });
            });

            try
            {
                return document.GeneratePdf();
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi tạo PDF: " + ex.Message, ex);
            }
        }

        private static IContainer CellStyle(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(5)
                .PaddingHorizontal(2);
        }
    }
}
