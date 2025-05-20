using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebBanHangOnline.Models;
using PagedList;
using System.Globalization;
using System.Data.Entity;
using WebBanHangOnline.Models.ViewModels;
using WebBanHangOnline.Hubs;
using Microsoft.AspNet.SignalR;
using WebBanHangOnline.Models.EF;

namespace WebBanHangOnline.Areas.Admin.Controllers
{
    //[Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // Trang danh sách đơn hàng phân trang
        public ActionResult Index(int? page)
        {
            var items = db.Orders.OrderByDescending(x => x.CreatedDate).ToList();

            if (page == null) page = 1;
            var pageNumber = page ?? 1;
            var pageSize = 10;
            ViewBag.PageSize = pageSize;
            ViewBag.Page = pageNumber;
            return View(items.ToPagedList(pageNumber, pageSize));
        }

        // Thêm đơn hàng mới kèm realtime cập nhật thống kê
        [HttpPost]
        public ActionResult PlaceOrder(Order order)
        {
            if (ModelState.IsValid)
            {
                db.Orders.Add(order);
                db.SaveChanges();

                // Lấy số liệu thống kê mới nhất
                var totalOrders = db.Orders.Count();
                var pendingOrders = db.Orders.Count(o => o.Status == PaymentStatus.ChoThanhToan);
                var paidOrders = db.Orders.Count(o => o.Status == PaymentStatus.ThanhCong);
                var failedOrders = db.Orders.Count(o => o.Status == PaymentStatus.ThatBai);

                // Gửi realtime qua SignalR
                var stats = new
                {
                    TotalOrders = totalOrders,
                    PendingOrders = pendingOrders,
                    PaidOrders = paidOrders,
                    FailedOrders = failedOrders,
                };
                var context = GlobalHost.ConnectionManager.GetHubContext<OrderHub>();
                context.Clients.All.UpdateOrderStats(stats);

                return RedirectToAction("Statistic");
            }
            return View(order);
        }

        // Trang thống kê - Thống kê đơn hàng & doanh thu hôm nay theo giờ
        public ActionResult Statistic()
        {
            // Lấy dữ liệu thống kê chuẩn xác, chỉ tính đơn đã thanh toán thành công
            var paidOrdersQuery = db.Orders.Where(o => o.Status == PaymentStatus.ThanhCong);

            var model = new OrderStatisticsViewModel
            {
                TotalOrders = paidOrdersQuery.Count(),

                PendingOrders = db.Orders.Count(o => o.Status == PaymentStatus.ChoThanhToan),

                PaidOrders = paidOrdersQuery.Count(),

                FailedOrders = db.Orders.Count(o => o.Status == PaymentStatus.ThatBai),

                // Biểu đồ doanh thu theo ngày hôm nay (giả sử lấy doanh thu trong ngày hiện tại)
                DayLabels = new List<string>(),
                DayCounts = new List<int>(),

                // Biểu đồ doanh thu theo tháng hôm nay (hoặc tháng hiện tại)
                MonthLabels = new List<string>(),
                MonthRevenue = new List<decimal>()
            };

            // Lấy doanh thu theo ngày trong hôm nay (theo giờ hiện tại)
            var today = DateTime.Today;

            var ordersToday = paidOrdersQuery
                .Where(o => DbFunctions.TruncateTime(o.CreatedDate) == today)
                .GroupBy(o => o.CreatedDate.Hour)
                .Select(g => new { Hour = g.Key, Count = g.Count() })
                .OrderBy(g => g.Hour)
                .ToList();

            model.DayLabels = ordersToday.Select(g => $"{g.Hour}:00").ToList();
            model.DayCounts = ordersToday.Select(g => g.Count).ToList();

            // Lấy doanh thu tổng theo tháng hôm nay (hoặc 1 tháng hiện tại) - có thể điều chỉnh theo yêu cầu
            var currentMonth = DateTime.Today.Month;
            var currentYear = DateTime.Today.Year;

            var monthlyRevenue = paidOrdersQuery
                .Where(o => o.CreatedDate.Month == currentMonth && o.CreatedDate.Year == currentYear)
                .GroupBy(o => o.CreatedDate.Day)
                .Select(g => new { Day = g.Key, Total = g.Sum(x => x.TotalAmount) })
                .OrderBy(g => g.Day)
                .ToList();

            model.MonthLabels = monthlyRevenue.Select(g => g.Day.ToString()).ToList();
            model.MonthRevenue = monthlyRevenue.Select(g => g.Total).ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(int id, PaymentStatus status)
        {
            var order = db.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return HttpNotFound();

            order.Status = status;
            db.SaveChanges();

            var totalOrders = db.Orders.Count();
            var pendingOrders = db.Orders.Count(o => o.Status == PaymentStatus.ChoThanhToan);
            var paidOrders = db.Orders.Count(o => o.Status == PaymentStatus.ThanhCong);
            var failedOrders = db.Orders.Count(o => o.Status == PaymentStatus.ThatBai);

            var today = DateTime.Today;
            var ordersToday = db.Orders
                .Where(o => o.Status == PaymentStatus.ThanhCong && DbFunctions.TruncateTime(o.CreatedDate) == today)
                .GroupBy(o => o.CreatedDate.Hour)
                .Select(g => new { Hour = g.Key, Count = g.Count() })
                .OrderBy(g => g.Hour)
                .ToList();

            var monthlyRevenue = db.Orders
                .Where(o => o.Status == PaymentStatus.ThanhCong && o.CreatedDate.Month == today.Month && o.CreatedDate.Year == today.Year)
                .GroupBy(o => o.CreatedDate.Day)
                .Select(g => new { Day = g.Key, Total = g.Sum(x => x.TotalAmount) })
                .OrderBy(g => g.Day)
                .ToList();

            var stats = new OrderStatisticsViewModel
            {
                TotalOrders = totalOrders,
                PendingOrders = pendingOrders,
                PaidOrders = paidOrders,
                FailedOrders = failedOrders,
                DayLabels = ordersToday.Select(g => g.Hour + ":00").ToList(),
                DayCounts = ordersToday.Select(g => g.Count).ToList(),
                MonthLabels = monthlyRevenue.Select(g => g.Day.ToString()).ToList(),
                MonthRevenue = monthlyRevenue.Select(g => g.Total).ToList()
            };

            var context = GlobalHost.ConnectionManager.GetHubContext<OrderHub>();
            context.Clients.All.updateOrderStats(stats);

            return RedirectToAction("Index");
        }


        // Các action khác giữ nguyên
        public ActionResult View(int id)
        {
            var item = db.Orders.Find(id);
            return View(item);
        }

        public ActionResult Partial_SanPham(int id)
        {
            var items = db.OrderDetails.Where(x => x.OrderId == id).ToList();
            return PartialView(items);
        }

        [HttpPost]
        public ActionResult UpdateTT(int id, int trangthai)
        {
            var item = db.Orders.Find(id);
            if (item != null)
            {
                db.Orders.Attach(item);
                item.TypePayment = trangthai;
                db.Entry(item).Property(x => x.TypePayment).IsModified = true;
                db.SaveChanges();
                return Json(new { message = "Success", Success = true });
            }
            return Json(new { message = "Unsuccess", Success = false });
        }
    }
}
