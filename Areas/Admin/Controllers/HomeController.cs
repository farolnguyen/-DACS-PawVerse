using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawVerse.Data;
using System.Security.Claims;
using System.Threading.Tasks;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using PawVerse.ViewModels; // Required for TopSellingProductViewModel

namespace PawVerse.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            // Define valid statuses for revenue calculation (case-insensitive)
            var revenueGeneratingStatuses = new[] { "đã giao hàng", "completed", "đang giao hàng", "shipped", "shipping", "hoàn thành" };

            // Get order statistics
            var monthlyRevenue = await _context.DonHangs
                .Where(d => d.NgayDatHang >= firstDayOfMonth &&
                           d.NgayDatHang <= lastDayOfMonth &&
                           revenueGeneratingStatuses.Contains(d.TrangThai.ToLower()))
                .SumAsync(d => d.TongTien);
                
            var monthlyOrders = await _context.DonHangs
                .CountAsync(d => d.NgayDatHang >= firstDayOfMonth && 
                                d.NgayDatHang <= lastDayOfMonth);
                
            var todayOrders = await _context.DonHangs
                .CountAsync(d => d.NgayDatHang.Date == today);
                
            var pendingOrders = await _context.DonHangs
                .CountAsync(d => d.TrangThai == "Chờ xác nhận");
            
            var processingOrders = await _context.DonHangs
                .CountAsync(d => d.TrangThai == "Đang xử lý");
                
            var shippingOrders = await _context.DonHangs
                .CountAsync(d => d.TrangThai == "Đang giao hàng");
            
            ViewBag.MonthlyRevenue = monthlyRevenue;
            ViewBag.MonthlyOrders = monthlyOrders;
            ViewBag.TodayOrders = todayOrders;
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.ProcessingOrders = processingOrders;
            ViewBag.ShippingOrders = shippingOrders;

            // Get other statistics
            var totalProducts = await _context.SanPhams.CountAsync();
            var totalCategories = await _context.DanhMucs.CountAsync();
            var totalBrands = await _context.ThuongHieus.CountAsync();
            // Assuming 'User' role ID is known or can be fetched. For simplicity, let's count users not in Admin/Staff roles.
            // This might need adjustment based on your actual role setup.
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            var staffRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Staff");

            var queryUsers = _context.Users.AsQueryable();
            if (adminRole != null)
            {
                queryUsers = queryUsers.Where(u => !_context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == adminRole.Id));
            }
            if (staffRole != null)
            {
                queryUsers = queryUsers.Where(u => !_context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == staffRole.Id));
            }
            var totalCustomers = await queryUsers.CountAsync();

            var recentOrders = await _context.DonHangs
                .Include(d => d.NguoiDung) // Assuming NguoiDung is the navigation property for User
                .OrderByDescending(d => d.NgayDatHang)
                .Take(5) // Get latest 5 orders
                .ToListAsync();

            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalCategories = totalCategories;
            ViewBag.TotalBrands = totalBrands;
            ViewBag.TotalCustomers = totalCustomers;
            ViewBag.RecentOrders = recentOrders;

            // Data for Revenue Area Chart (last 7 days)
            var last7Days = Enumerable.Range(0, 7).Select(i => today.AddDays(-i)).Reverse().ToList();
            var dailyRevenue = new List<decimal>();
            var revenueChartLabels = new List<string>();

            foreach (var day in last7Days)
            {
                var revenueForDay = await _context.DonHangs
                    .Where(d => d.NgayDatHang.Date == day.Date && revenueGeneratingStatuses.Contains(d.TrangThai.ToLower()))
                    .SumAsync(d => (decimal?)d.TongTien) ?? 0;
                dailyRevenue.Add(revenueForDay);
                revenueChartLabels.Add(day.ToString("dd/MM"));
            }

            ViewBag.RevenueChartLabels = revenueChartLabels;
            ViewBag.RevenueChartData = dailyRevenue;

            // Data for Revenue Sources Pie Chart
            var revenueSources = await _context.DonHangs
                .Where(d => revenueGeneratingStatuses.Contains(d.TrangThai.ToLower()))
                .GroupBy(d => d.PhuongThucThanhToan)
                .Select(g => new { Source = g.Key, TotalRevenue = g.Sum(d => d.TongTien) })
                .ToListAsync();

            ViewBag.PieChartLabels = revenueSources.Select(rs => rs.Source).ToList();
            ViewBag.PieChartData = revenueSources.Select(rs => rs.TotalRevenue).ToList();
            // You might want to define a list of colors for the pie chart segments if the default ones are not suitable
            // ViewBag.PieChartColors = new List<string> { "#4e73df", "#1cc88a", "#36b9cc", "#f6c23e", "#e74a3b" };

            // Data for Top Selling Products
            // Lấy dữ liệu sản phẩm bán chạy dựa vào thuộc tính "SoLuongDaBan"
            var topSellingProductsData = await _context.SanPhams
                .OrderByDescending(p => p.SoLuongDaBan)
                .Take(10)
                .Select(p => new PawVerse.ViewModels.TopSellingProductViewModel
                {
                    ProductName = p.TenSanPham,
                    ProductImage = p.HinhAnh,
                    QuantitySold = p.SoLuongDaBan
                })
                .ToListAsync();

            ViewBag.TopSellingProducts = topSellingProductsData;

            // Data for Loyal Customers
            var loyalCustomers = await _context.DonHangs
                .Where(dh => revenueGeneratingStatuses.Contains(dh.TrangThai.ToLower()) && dh.IdNguoiDung != null)
                .GroupBy(dh => dh.IdNguoiDung)
                .Select(g => new
                {
                    UserId = g.Key,
                    OrderCount = g.Count(),
                    TotalSpent = g.Sum(dh => dh.TongTien)
                })
                .OrderByDescending(g => g.OrderCount)
                .ThenByDescending(g => g.TotalSpent)
                .Take(5)
                .Join(_context.Users, // Assuming your ApplicationUser table is named Users
                      groupedOrder => groupedOrder.UserId,
                      user => user.Id,
                      (groupedOrder, user) => new LoyalCustomerViewModel
                      {
                          CustomerName = user.FullName, // Or user.UserName, or another property for name
                          CustomerEmail = user.Email,
                          OrderCount = groupedOrder.OrderCount,
                          TotalSpent = groupedOrder.TotalSpent,
                          // AvatarUrl = user.AvatarUrl // If you have an avatar property
                      })
                .ToListAsync();

            ViewBag.LoyalCustomers = loyalCustomers;
            return View();
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [AllowAnonymous]
        public new IActionResult Unauthorized()
        {
            return View("AccessDenied");
        }

        // Action to export dashboard data to Excel
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportDashboardToExcel()
        {
            using (var package = new ExcelPackage())
            {
                // --- Overview Sheet --- 
                var overviewSheet = package.Workbook.Worksheets.Add("Overview");

                // Headers
                overviewSheet.Cells["A1"].Value = "Mục";
                overviewSheet.Cells["B1"].Value = "Giá trị";
                overviewSheet.Cells["A1:B1"].Style.Font.Bold = true;
                overviewSheet.Cells["A1:B1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                overviewSheet.Cells["A1:B1"].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

                var today = DateTime.Today;
                var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
                var revenueGeneratingStatuses = new[] { "đã giao hàng", "completed", "đang giao hàng", "shipped", "shipping", "hoàn thành" };

                // Data for Overview
                var totalRevenueAllTime = await _context.DonHangs
                    .Where(d => revenueGeneratingStatuses.Contains(d.TrangThai.ToLower()))
                    .SumAsync(d => (decimal?)d.TongTien) ?? 0;

                var totalCompletedOrders = await _context.DonHangs
                    .CountAsync(d => revenueGeneratingStatuses.Contains(d.TrangThai.ToLower()));

                var totalCustomers = await _context.Users.CountAsync(); // Simplified, adjust if needed for specific roles

                var totalProducts = await _context.SanPhams.CountAsync();

                var averageOrderValue = totalCompletedOrders > 0 ? totalRevenueAllTime / totalCompletedOrders : 0;

                int currentRow = 2;
                overviewSheet.Cells[currentRow, 1].Value = "Tổng doanh thu (tất cả thời gian)";
                overviewSheet.Cells[currentRow, 2].Value = totalRevenueAllTime;
                overviewSheet.Cells[currentRow, 2].Style.Numberformat.Format = "#,##0 đ";
                currentRow++;

                overviewSheet.Cells[currentRow, 1].Value = "Tổng đơn hàng đã hoàn thành/giao";
                overviewSheet.Cells[currentRow, 2].Value = totalCompletedOrders;
                currentRow++;

                overviewSheet.Cells[currentRow, 1].Value = "Tổng số khách hàng";
                overviewSheet.Cells[currentRow, 2].Value = totalCustomers;
                currentRow++;

                overviewSheet.Cells[currentRow, 1].Value = "Tổng số sản phẩm";
                overviewSheet.Cells[currentRow, 2].Value = totalProducts;
                currentRow++;

                overviewSheet.Cells[currentRow, 1].Value = "Doanh thu trung bình mỗi đơn hàng";
                overviewSheet.Cells[currentRow, 2].Value = averageOrderValue;
                overviewSheet.Cells[currentRow, 2].Style.Numberformat.Format = "#,##0 đ";
                
                overviewSheet.Cells[overviewSheet.Dimension.Address].AutoFitColumns();

                // --- Monthly Revenue Sheet --- 
                var monthlyRevenueSheet = package.Workbook.Worksheets.Add("Monthly Revenue");
                monthlyRevenueSheet.Cells["A1"].Value = "Năm";
                monthlyRevenueSheet.Cells["B1"].Value = "Tháng";
                monthlyRevenueSheet.Cells["C1"].Value = "Doanh thu";
                monthlyRevenueSheet.Cells["A1:C1"].Style.Font.Bold = true;
                monthlyRevenueSheet.Cells["A1:C1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                monthlyRevenueSheet.Cells["A1:C1"].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

                var monthlyRevenueData = await _context.DonHangs
                    .Where(d => revenueGeneratingStatuses.Contains(d.TrangThai.ToLower()))
                    .GroupBy(d => new { Year = d.NgayDatHang.Year, Month = d.NgayDatHang.Month })
                    .Select(g => new
                    {
                        g.Key.Year,
                        g.Key.Month,
                        TotalRevenue = g.Sum(d => d.TongTien)
                    })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToListAsync();

                currentRow = 2; // Reset for new sheet
                foreach (var item in monthlyRevenueData)
                {
                    monthlyRevenueSheet.Cells[currentRow, 1].Value = item.Year;
                    monthlyRevenueSheet.Cells[currentRow, 2].Value = item.Month;
                    monthlyRevenueSheet.Cells[currentRow, 3].Value = item.TotalRevenue;
                    monthlyRevenueSheet.Cells[currentRow, 3].Style.Numberformat.Format = "#,##0 đ";
                    currentRow++;
                }
                monthlyRevenueSheet.Cells[monthlyRevenueSheet.Dimension.Address].AutoFitColumns();

                // --- Top Selling Products Sheet --- 
                var topSellingSheet = package.Workbook.Worksheets.Add("Top Selling Products");
                topSellingSheet.Cells["A1"].Value = "Tên sản phẩm";
                topSellingSheet.Cells["B1"].Value = "Số lượng đã bán";
                topSellingSheet.Cells["C1"].Value = "Tổng doanh thu từ sản phẩm";
                topSellingSheet.Cells["A1:C1"].Style.Font.Bold = true;
                topSellingSheet.Cells["A1:C1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                topSellingSheet.Cells["A1:C1"].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

                var topSellingProductsData = await _context.SanPhams
                    .OrderByDescending(p => p.SoLuongDaBan)
                    .Take(20) // Get top 20 selling products
                    .Select(p => new
                    {
                        p.TenSanPham,
                        p.SoLuongDaBan,
                        ProductRevenue = p.SoLuongDaBan * (p.GiaKhuyenMai > 0 ? p.GiaKhuyenMai : p.GiaBan)
                    })
                    .ToListAsync();

                currentRow = 2; // Reset for new sheet
                foreach (var product in topSellingProductsData)
                {
                    topSellingSheet.Cells[currentRow, 1].Value = product.TenSanPham;
                    topSellingSheet.Cells[currentRow, 2].Value = product.SoLuongDaBan;
                    topSellingSheet.Cells[currentRow, 3].Value = product.ProductRevenue;
                    topSellingSheet.Cells[currentRow, 3].Style.Numberformat.Format = "#,##0 đ";
                    currentRow++;
                }
                topSellingSheet.Cells[topSellingSheet.Dimension.Address].AutoFitColumns();

                // --- Recent Orders Sheet --- 
                var recentOrdersSheet = package.Workbook.Worksheets.Add("Recent Orders");
                recentOrdersSheet.Cells["A1"].Value = "Mã đơn hàng";
                recentOrdersSheet.Cells["B1"].Value = "Ngày đặt hàng";
                recentOrdersSheet.Cells["C1"].Value = "Tên khách hàng";
                recentOrdersSheet.Cells["D1"].Value = "Email khách hàng";
                recentOrdersSheet.Cells["E1"].Value = "Tổng tiền";
                recentOrdersSheet.Cells["F1"].Value = "Phương thức thanh toán";
                recentOrdersSheet.Cells["G1"].Value = "Trạng thái đơn hàng";
                recentOrdersSheet.Cells["A1:G1"].Style.Font.Bold = true;
                recentOrdersSheet.Cells["A1:G1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                recentOrdersSheet.Cells["A1:G1"].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

                var recentOrdersData = await _context.DonHangs
                    .Include(d => d.NguoiDung) // Assuming NguoiDung is the navigation property
                    .OrderByDescending(d => d.NgayDatHang)
                    .Take(100) // Get latest 100 orders
                    .ToListAsync();

                currentRow = 2; // Reset for new sheet
                foreach (var order in recentOrdersData)
                {
                    recentOrdersSheet.Cells[currentRow, 1].Value = order.IdDonHang;
                    recentOrdersSheet.Cells[currentRow, 2].Value = order.NgayDatHang;
                    recentOrdersSheet.Cells[currentRow, 2].Style.Numberformat.Format = "dd/MM/yyyy HH:mm:ss";
                    recentOrdersSheet.Cells[currentRow, 3].Value = order.NguoiDung?.FullName ?? "Khách vãng lai";
                    recentOrdersSheet.Cells[currentRow, 4].Value = order.NguoiDung?.Email ?? "Khách vãng lai"; // Fallback for guest orders
                    recentOrdersSheet.Cells[currentRow, 5].Value = order.TongTien;
                    recentOrdersSheet.Cells[currentRow, 5].Style.Numberformat.Format = "#,##0 đ";
                    recentOrdersSheet.Cells[currentRow, 6].Value = order.PhuongThucThanhToan;
                    recentOrdersSheet.Cells[currentRow, 7].Value = order.TrangThai;
                    currentRow++;
                }
                recentOrdersSheet.Cells[recentOrdersSheet.Dimension.Address].AutoFitColumns();

                // --- Payment Analysis Sheet --- 
                var paymentAnalysisSheet = package.Workbook.Worksheets.Add("Payment Analysis");
                paymentAnalysisSheet.Cells["A1"].Value = "Phương thức thanh toán";
                paymentAnalysisSheet.Cells["B1"].Value = "Số lượng đơn hàng";
                paymentAnalysisSheet.Cells["C1"].Value = "Tổng doanh thu";
                paymentAnalysisSheet.Cells["A1:C1"].Style.Font.Bold = true;
                paymentAnalysisSheet.Cells["A1:C1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                paymentAnalysisSheet.Cells["A1:C1"].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

                var paymentAnalysisData = await _context.DonHangs
                    .Where(d => revenueGeneratingStatuses.Contains(d.TrangThai.ToLower()))
                    .GroupBy(d => d.PhuongThucThanhToan)
                    .Select(g => new
                    {
                        PaymentMethod = g.Key,
                        OrderCount = g.Count(),
                        TotalRevenue = g.Sum(d => d.TongTien)
                    })
                    .OrderByDescending(x => x.TotalRevenue)
                    .ToListAsync();

                currentRow = 2; // Reset for new sheet
                foreach (var item in paymentAnalysisData)
                {
                    paymentAnalysisSheet.Cells[currentRow, 1].Value = item.PaymentMethod;
                    paymentAnalysisSheet.Cells[currentRow, 2].Value = item.OrderCount;
                    paymentAnalysisSheet.Cells[currentRow, 3].Value = item.TotalRevenue;
                    paymentAnalysisSheet.Cells[currentRow, 3].Style.Numberformat.Format = "#,##0 đ";
                    currentRow++;
                }
                paymentAnalysisSheet.Cells[paymentAnalysisSheet.Dimension.Address].AutoFitColumns();

                // --- New Customers Sheet (based on first order date) --- 
                var newCustomersSheet = package.Workbook.Worksheets.Add("New Customers");
                newCustomersSheet.Cells["A1"].Value = "Năm";
                newCustomersSheet.Cells["B1"].Value = "Tháng";
                newCustomersSheet.Cells["C1"].Value = "Số lượng khách hàng mới (theo đơn hàng đầu tiên)";
                newCustomersSheet.Cells["A1:C1"].Style.Font.Bold = true;
                newCustomersSheet.Cells["A1:C1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                newCustomersSheet.Cells["A1:C1"].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

                // Get the first order date for each user
                var firstOrderDates = await _context.DonHangs
                    .Where(d => d.IdNguoiDung != null) // Only consider orders from registered users
                    .GroupBy(d => d.IdNguoiDung)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        FirstOrderDate = g.Min(d => d.NgayDatHang)
                    })
                    .ToListAsync();

                var newCustomersData = firstOrderDates
                    .GroupBy(fod => new { Year = fod.FirstOrderDate.Year, Month = fod.FirstOrderDate.Month })
                    .Select(g => new
                    {
                        g.Key.Year,
                        g.Key.Month,
                        NewCustomerCount = g.Count() // Count of unique users with their first order in this month/year
                    })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToList();

                currentRow = 2; // Reset for new sheet
                foreach (var item in newCustomersData)
                {
                    newCustomersSheet.Cells[currentRow, 1].Value = item.Year;
                    newCustomersSheet.Cells[currentRow, 2].Value = item.Month;
                    newCustomersSheet.Cells[currentRow, 3].Value = item.NewCustomerCount;
                    currentRow++;
                }
                newCustomersSheet.Cells[newCustomersSheet.Dimension.Address].AutoFitColumns();

                var fileContents = package.GetAsByteArray();
                return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"PawVerse_Dashboard_Report_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
            }
        }
    }
}
//Tmd1eeG7hW4gSOG6o2kgxJDEg25nIC0gQsO5aSBC4bqjbyBIw6JuIA==