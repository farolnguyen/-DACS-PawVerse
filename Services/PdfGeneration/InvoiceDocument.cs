using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PawVerse.Models; // Giả sử bạn có model DonHang (Order)
using System.Linq; // Cần cho Sum() và các thao tác LINQ khác

namespace PawVerse.Services.PdfGeneration
{
    public class InvoiceDocument : IDocument
    {
        public DonHang Order { get; } // Model đơn hàng của bạn
        // Thêm các thông tin cửa hàng nếu cần, hoặc lấy từ cấu hình
        public string StoreName { get; } = "PawVerse";
        public string StoreAddress { get; } = "123 Đường ABC, Phường XYZ, Quận 1, TP. HCM";
        public string StorePhone { get; } = "0123 456 789";
        public string StoreEmail { get; } = "contact@pawverse.com";

        public InvoiceDocument(DonHang order)
        {
            Order = order;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container
                .Page(page =>
                {
                    page.Margin(50);

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                    page.Footer().Element(ComposeFooter);
                });
        }

        void ComposeHeader(IContainer container)
        {
            var titleStyle = TextStyle.Default.FontSize(20).SemiBold().FontColor(Colors.Blue.Medium); // Ví dụ màu

            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text($"HÓA ĐƠN #{Order.IdDonHang}").Style(titleStyle);

                    column.Item().Text(text =>
                    {
                        text.Span("Ngày đặt hàng: ").SemiBold();
                        text.Span($"{Order.NgayDatHang:dd/MM/yyyy}");
                    });
                    column.Item().Text(text =>
                    {
                        text.Span("Ngày xuất hóa đơn: ").SemiBold();
                        text.Span($"{System.DateTime.Now:dd/MM/yyyy}");
                    });
                });

                row.ConstantItem(150).Column(column => // Dành không gian cho logo nếu có
                {
                    column.Item().Text(StoreName).Bold();
                    column.Item().Text(StoreAddress);
                    column.Item().Text(StorePhone);
                    column.Item().Text(StoreEmail);
                    // Nếu có logo:
                    // column.Item().Image(Placeholders.Image(100, 50)); // Placeholder cho logo
                });
            });


        }

        void ComposeContent(IContainer container)
        {
            container.Column(column =>
            {
                // Thông tin khách hàng
                column.Item().Text("Thông tin khách hàng").SemiBold().FontSize(14);
                column.Item().PaddingTop(5).Text(text =>
                {
                    text.Span("Tên: ").SemiBold();
                    text.Span(Order.TenKhachHang ?? Order.NguoiDung?.FullName ?? "N/A");
                });
                column.Item().Text(text =>
                {
                    text.Span("Địa chỉ: ").SemiBold();
                    text.Span(Order.DiaChiGiaoHang ?? "N/A");
                });
                column.Item().Text(text =>
                {
                    text.Span("Điện thoại: ").SemiBold();
                    text.Span(Order.SoDienThoai ?? Order.NguoiDung?.PhoneNumber ?? "N/A");
                });
                column.Item().Text(text =>
                {
                    text.Span("Email: ").SemiBold();
                    text.Span(Order.NguoiDung?.Email ?? "N/A");
                });

                column.Item().PaddingTop(20);

                // Bảng chi tiết sản phẩm
                column.Item().Table(table =>
                {
                    // Định nghĩa cột
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);  // STT
                        columns.RelativeColumn(3); // Tên sản phẩm
                        columns.RelativeColumn();  // Số lượng
                        columns.RelativeColumn();  // Đơn giá
                        columns.RelativeColumn();  // Thành tiền
                    });

                    // Header của bảng
                    table.Header(header =>
                    {
                        static IContainer HeaderStyle(IContainer container)
                        {
                            return container.DefaultTextStyle(x => x.Bold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                        }

                        header.Cell().Element(HeaderStyle).Text("STT");
                        header.Cell().Element(HeaderStyle).Text("Sản phẩm");
                        header.Cell().Element(HeaderStyle).Text("SL");
                        header.Cell().Element(HeaderStyle).Text("Đơn giá");
                        header.Cell().Element(HeaderStyle).Text("Thành tiền");
                    });

                    // Dữ liệu sản phẩm
                    if (Order.ChiTietDonHangs != null)
                    {
                        int index = 1;
                        foreach (var item in Order.ChiTietDonHangs)
                        {
                            table.Cell().Element(CellStyle).Text(index++.ToString());
                            table.Cell().Element(CellStyle).Text(item.SanPham?.TenSanPham ?? "N/A");
                            table.Cell().Element(CellStyle).Text(item.SoLuong.ToString());
                            table.Cell().Element(CellStyle).Text($"{item.DonGia:N0} đ"); // Format tiền tệ
                            table.Cell().Element(CellStyle).Text($"{(item.SoLuong * item.DonGia):N0} đ");
                        }
                    }
                    
                    // Tổng cộng
                    table.Footer(footer => {
                        footer.Cell().ColumnSpan(4).AlignRight().Text("Tổng tiền hàng:").Bold();
                        footer.Cell().AlignRight().Element(CellStyle).Text($"{Order.ChiTietDonHangs?.Sum(ct => ct.SoLuong * ct.DonGia) ?? 0:N0} đ").Bold();

                        // Nếu có phí ship, giảm giá, etc.
                        // footer.Cell().ColumnSpan(4).AlignRight().Text("Phí vận chuyển:").Bold();
                        // footer.Cell().AlignRight().Element(CellStyle).Text($"{Order.PhiVanChuyen:N0} đ").Bold();

                        footer.Cell().ColumnSpan(4).AlignRight().Text("Tổng thanh toán:").Bold().FontSize(14);
                        footer.Cell().AlignRight().Element(CellStyle).Text($"{Order.TongTien:N0} đ").Bold().FontSize(14);
                    });
                });

                column.Item().PaddingTop(20);

                // Phương thức thanh toán
                column.Item().Text(text =>
                {
                    text.Span("Phương thức thanh toán: ").SemiBold();
                    text.Span(Order.PhuongThucThanhToan ?? "N/A");
                });
            });
        }
        
        // Helper cho style cell trong bảng
        static IContainer CellStyle(IContainer container)
        {
            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
        }

        void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Text(text =>
            {
                text.Span("Cảm ơn quý khách đã mua hàng tại PawVerse! - Trang ");
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        }
    }
}
