// MyOrdersController.cs
using System.Linq;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using WebBanHangOnline.Models;
using WebBanHangOnline.Models.ViewModels;

namespace WebBanHangOnline.Controllers
{
    [Authorize]
    public class MyOrdersController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: /MyOrders
        public ActionResult Index()
        {
            var userId = User.Identity.GetUserId();

            var orders = db.Orders
                .Where(o => o.CustomerId == userId)
                .OrderByDescending(o => o.CreatedDate)
                .Select(o => new OrderTrackingViewModel
                {
                    Code = o.Code,
                    CreatedDate = o.CreatedDate,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status.ToString()
                })
                .ToList();

            return View(orders); // ⬅ phải trả về "orders"
        }

        // GET: /MyOrders/Details?code=ABC123
        public ActionResult Details(string code)
        {
            var userId = User.Identity.GetUserId();
            var order = db.Orders.FirstOrDefault(o => o.Code == code && o.CustomerId == userId);
            if (order == null)
                return HttpNotFound();

            return View("~/Views/MyOrders/Details.cshtml", order);
        }

        // GET: /MyOrders/Status?code=ABC123
        [HttpGet]
        public JsonResult Status(string code)
        {
            var userId = User.Identity.GetUserId();
            var order = db.Orders.FirstOrDefault(o => o.Code == code && o.CustomerId == userId);

            if (order == null)
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            return Json(new { success = true, status = order.Status.ToString() }, JsonRequestBehavior.AllowGet);
        }

        // GET: /MyOrders/DetailsProducts?id=123
        public PartialViewResult DetailsProducts(int id)
        {
            var orderDetails = db.OrderDetails
                                 .Where(od => od.OrderId == id)
                                 .ToList();
            return PartialView("~/Views/MyOrders/_OrderDetailsPartial.cshtml", orderDetails);
        }
    }
}
