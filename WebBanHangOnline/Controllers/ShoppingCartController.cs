using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebBanHangOnline.Hubs;
using WebBanHangOnline.Models;
using WebBanHangOnline.Models.EF;
using WebBanHangOnline.Models.Payments;
using WebBanHangOnline.Models.ViewModels;

namespace WebBanHangOnline.Controllers
{
    [Authorize]
    public class ShoppingCartController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public ShoppingCartController()
        {
        }

        public ShoppingCartController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }
        // GET: ShoppingCart
        [AllowAnonymous]
        public ActionResult Index()
        {

            ShoppingCart cart = (ShoppingCart)Session["Cart"];
            if (cart != null && cart.Items.Any())
            {
                ViewBag.CheckCart = cart;
            }
            return View();
        }
        [AllowAnonymous]
        public ActionResult VnpayReturn()
        {
            if (Request.QueryString.Count > 0)
            {
                string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"]; //Chuoi bi mat
                var vnpayData = Request.QueryString;
                VnPayLibrary vnpay = new VnPayLibrary();

                foreach (string s in vnpayData)
                {
                    //get all querystring data
                    if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(s, vnpayData[s]);
                    }
                }
                string orderCode = Convert.ToString(vnpay.GetResponseData("vnp_TxnRef"));
                long vnpayTranId = Convert.ToInt64(vnpay.GetResponseData("vnp_TransactionNo"));
                string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                string vnp_TransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
                String vnp_SecureHash = Request.QueryString["vnp_SecureHash"];
                String TerminalID = Request.QueryString["vnp_TmnCode"];
                long vnp_Amount = Convert.ToInt64(vnpay.GetResponseData("vnp_Amount")) / 100;
                String bankCode = Request.QueryString["vnp_BankCode"];

                bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);
                if (checkSignature)
                {
                    if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
                    {
                        var itemOrder = db.Orders.FirstOrDefault(x => x.Code == orderCode);
                        if (itemOrder != null)
                        {
                            itemOrder.Status = PaymentStatus.ThanhCong;    // Thanh toán thành công
                            db.Entry(itemOrder).State = System.Data.Entity.EntityState.Modified;
                            db.SaveChanges();
                        }
                        //Thanh toan thanh cong
                        ViewBag.InnerText = "Giao dịch được thực hiện thành công. Cảm ơn quý khách đã sử dụng dịch vụ";
                        //log.InfoFormat("Thanh toan thanh cong, OrderId={0}, VNPAY TranId={1}", orderId, vnpayTranId);
                    }
                    else
                    {
                        //Thanh toan khong thanh cong. Ma loi: vnp_ResponseCode
                        ViewBag.InnerText = "Có lỗi xảy ra trong quá trình xử lý.Mã lỗi: " + vnp_ResponseCode;
                        //log.InfoFormat("Thanh toan loi, OrderId={0}, VNPAY TranId={1},ResponseCode={2}", orderId, vnpayTranId, vnp_ResponseCode);
                    }
                    //displayTmnCode.InnerText = "Mã Website (Terminal ID):" + TerminalID;
                    //displayTxnRef.InnerText = "Mã giao dịch thanh toán:" + orderId.ToString();
                    //displayVnpayTranNo.InnerText = "Mã giao dịch tại VNPAY:" + vnpayTranId.ToString();
                    ViewBag.ThanhToanThanhCong = "Số tiền thanh toán (VND):" + vnp_Amount.ToString();
                    //displayBankCode.InnerText = "Ngân hàng thanh toán:" + bankCode;
                }
            }
            //var a = UrlPayment(0, "DH3574");
            return View();
        }

        [AllowAnonymous]
        public ActionResult CheckOut()
        {
            ShoppingCart cart = (ShoppingCart)Session["Cart"];
            if (cart != null && cart.Items.Any())
            {
                ViewBag.CheckCart = cart;
            }
            return View();
        }

        [AllowAnonymous]
        public ActionResult CheckOutSuccess()
        {
            return View();
        }
        [AllowAnonymous]
        public ActionResult Partial_Item_ThanhToan()
        {
            ShoppingCart cart = (ShoppingCart)Session["Cart"];
            if (cart != null && cart.Items.Any())
            {
                return PartialView(cart.Items);
            }
            return PartialView();
        }
        [AllowAnonymous]
        public ActionResult Partial_Item_Cart()
        {
            ShoppingCart cart = (ShoppingCart)Session["Cart"];
            if (cart != null && cart.Items.Any())
            {
                return PartialView(cart.Items);
            }
            return PartialView();
        }

        [AllowAnonymous]
        public ActionResult ShowCount()
        {
            ShoppingCart cart = (ShoppingCart)Session["Cart"];
            if (cart != null)
            {
                return Json(new { Count = cart.Items.Count }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { Count = 0 }, JsonRequestBehavior.AllowGet);
        }
        [AllowAnonymous]
        public ActionResult Partial_CheckOut()
        {
            var user = UserManager.FindByNameAsync(User.Identity.Name).Result;
            if (user != null)
            {
                ViewBag.User = user;
            }
            return PartialView();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult CheckOut(OrderViewModel req)
        {
            var result = new { Success = false, Code = -1, Url = "", Message = "" };

            if (!ModelState.IsValid)
                return Json(result);

            var cart = Session["Cart"] as ShoppingCart;
            if (cart == null || !cart.Items.Any())
                return Json(result);

            // Tạo order và OrderDetails
            var order = new Order
            {
                CustomerName = req.CustomerName,
                Phone = req.Phone,
                Address = req.Address,
                Email = req.Email,
                Status = PaymentStatus.ChoThanhToan,
                TypePayment = req.TypePayment,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now,
                CreatedBy = req.Phone
            };
            if (User.Identity.IsAuthenticated)
                order.CustomerId = User.Identity.GetUserId();

            // Sinh mã và nhúng details
            var rd = new Random();
            order.Code = $"DH{rd.Next(0, 9)}{rd.Next(0, 9)}{rd.Next(0, 9)}{rd.Next(0, 9)}";
            cart.Items.ForEach(x => order.OrderDetails.Add(new OrderDetail
            {
                ProductId = x.ProductId,
                Quantity = x.Quantity,
                Price = x.Price
            }));
            order.TotalAmount = cart.Items.Sum(x => x.Price * x.Quantity);

            using (var tran = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    // 1) Kiểm tra & trừ tồn kho
                    foreach (var item in cart.Items)
                    {
                        // Khóa dòng sản phẩm
                        var prod = db.Products
                            .SqlQuery(
                               "SELECT * FROM tb_Product WITH (ROWLOCK, UPDLOCK) WHERE Id = @p0",
                               item.ProductId)
                            .FirstOrDefault();
                        if (prod == null || prod.Quantity < item.Quantity)
                            throw new InvalidOperationException(
                                $"Sản phẩm \"{item.ProductName}\" chỉ còn {(prod?.Quantity ?? 0)}.");

                        prod.Quantity -= item.Quantity;
                        db.Entry(prod).State = System.Data.Entity.EntityState.Modified;
                    }

                    // 2) Thêm đơn hàng + lưu vào CSDL
                    db.Orders.Add(order);
                    db.SaveChanges();

                    // 3) Commit
                    tran.Commit();

                    var stats = new OrderStatisticsViewModel
                    {
                        TotalOrders = db.Orders.Count(),
                        PendingOrders = db.Orders.Count(o => o.Status == PaymentStatus.ChoThanhToan),
                        PaidOrders = db.Orders.Count(o => o.Status == PaymentStatus.ThanhCong),
                        FailedOrders = db.Orders.Count(o => o.Status == PaymentStatus.ThatBai),

                        // Không gửi dữ liệu biểu đồ
                        DayLabels = null,
                        DayCounts = null,
                        MonthLabels = null,
                        MonthRevenue = null
                    };

                    // Gửi lên hub
                    OrderHub.BroadcastNewOrder(order.Code);
                    OrderHub.BroadcastOrderStats(stats);



                    // Gửi mail cho khách
                    var strSanPham = "";
                decimal thanhtien = 0;
                foreach (var sp in cart.Items)
                {
                    strSanPham += $"<tr><td>{sp.ProductName}</td><td>{sp.Quantity}</td><td>{Common.Common.FormatNumber(sp.TotalPrice,0)}</td></tr>";
                    thanhtien += sp.Price * sp.Quantity;
                }
                var TongTien = thanhtien;

                // Mail khách
                var templateC = System.IO.File.ReadAllText(Server.MapPath("~/Content/templates/send2.html"));
                templateC = templateC.Replace("{{MaDon}}", order.Code)
                                     .Replace("{{SanPham}}", strSanPham)
                                     .Replace("{{NgayDat}}", DateTime.Now.ToString("dd/MM/yyyy"))
                                     .Replace("{{TenKhachHang}}", order.CustomerName)
                                     .Replace("{{Phone}}", order.Phone)
                                     .Replace("{{Email}}", req.Email)
                                     .Replace("{{DiaChiNhanHang}}", order.Address)
                                     .Replace("{{ThanhTien}}", Common.Common.FormatNumber(thanhtien,0))
                                     .Replace("{{TongTien}}", Common.Common.FormatNumber(TongTien,0));
                Common.Common.SendMail("ShopOnline", "Đơn hàng #" + order.Code, templateC, req.Email);

                // Mail Admin
                var templateA = System.IO.File.ReadAllText(Server.MapPath("~/Content/templates/send1.html"));
                templateA = templateA.Replace("{{MaDon}}", order.Code)
                                     .Replace("{{SanPham}}", strSanPham)
                                     .Replace("{{NgayDat}}", DateTime.Now.ToString("dd/MM/yyyy"))
                                     .Replace("{{TenKhachHang}}", order.CustomerName)
                                     .Replace("{{Phone}}", order.Phone)
                                     .Replace("{{Email}}", req.Email)
                                     .Replace("{{DiaChiNhanHang}}", order.Address)
                                     .Replace("{{ThanhTien}}", Common.Common.FormatNumber(thanhtien,0))
                                     .Replace("{{TongTien}}", Common.Common.FormatNumber(TongTien,0));
                Common.Common.SendMail("ShopOnline", "Đơn hàng mới #" + order.Code, templateA, ConfigurationManager.AppSettings["EmailAdmin"]);

                    // Clear Cart và trả về kết quả thành công
                    cart.ClearCart();
                    var redirectUrl = req.TypePayment == 2
                                      ? UrlPayment(req.TypePaymentVN, order.Code)
                                      : Url.Action("CheckOutSuccess", "ShoppingCart");

                    result = new { Success = true, Code = req.TypePayment, Url = redirectUrl, Message = "" };
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    result = new { Success = false, Code = -1, Url = "", Message = ex.Message };
                }
            }

            return Json(result);
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult AddToCart(int id, int quantity)
        {
            var code = new { Success = false, msg = "", code = -1, Count = 0 };
            var db = new ApplicationDbContext();
            var checkProduct = db.Products.FirstOrDefault(x => x.Id == id);

            if (checkProduct == null)
            {
                code = new { Success = false, msg = "Sản phẩm không tồn tại", code = -1, Count = 0 };
                return Json(code);
            }

            // Kiểm tra tồn kho
            if (checkProduct.Quantity < quantity)
            {
                code = new
                {
                    Success = false,
                    msg = $"Chỉ còn {checkProduct.Quantity} sản phẩm. Không thể thêm {quantity}.",
                    code = -2,
                    Count = 0
                };
                return Json(code);
            }

            // Phần thêm vào giỏ hàng
            ShoppingCart cart = (ShoppingCart)Session["Cart"] ?? new ShoppingCart();
            ShoppingCartItem item = new ShoppingCartItem
            {
                ProductId = checkProduct.Id,
                ProductName = checkProduct.Title,
                CategoryName = checkProduct.ProductCategory.Title,
                Alias = checkProduct.Alias,
                Quantity = quantity
            };

            if (checkProduct.ProductImage.FirstOrDefault(x => x.IsDefault) != null)
            {
                item.ProductImg = checkProduct.ProductImage.FirstOrDefault(x => x.IsDefault).Image;
            }

            item.Price = checkProduct.Price;
            if (checkProduct.PriceSale > 0)
            {
                item.Price = (decimal)checkProduct.PriceSale;
            }

            item.TotalPrice = item.Quantity * item.Price;
            cart.AddToCart(item, quantity);
            Session["Cart"] = cart;
            code = new { Success = true, msg = "Thêm giỏ hàng thành công", code = 1, Count = cart.Items.Count };
            return Json(code);
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Update(int id, int quantity)
        {
            ShoppingCart cart = (ShoppingCart)Session["Cart"];
            if (cart != null)
            {
                cart.UpdateQuantity(id, quantity);
                return Json(new { Success = true });
            }
            return Json(new { Success = false });
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Delete(int id)
        {
            var code = new { Success = false, msg = "", code = -1, Count = 0 };

            ShoppingCart cart = (ShoppingCart)Session["Cart"];
            if (cart != null)
            {
                var checkProduct = cart.Items.FirstOrDefault(x => x.ProductId == id);
                if (checkProduct != null)
                {
                    cart.Remove(id);
                    code = new { Success = true, msg = "", code = 1, Count = cart.Items.Count };
                }
            }
            return Json(code);
        }


        [AllowAnonymous]
        [HttpPost]
        public ActionResult DeleteAll()
        {
            ShoppingCart cart = (ShoppingCart)Session["Cart"];
            if (cart != null)
            {
                cart.ClearCart();
                return Json(new { Success = true });
            }
            return Json(new { Success = false });
        }

        //thêm API kiểm tra toàn bộ giỏ hàng
        [AllowAnonymous]
        [HttpPost]
        public JsonResult ValidateCart()
        {
            var cart = Session["Cart"] as ShoppingCart;
            if (cart == null || !cart.Items.Any())
                return Json(new { success = false, msg = "Giỏ hàng trống" });

            // Lấy danh sách productId trong cart
            var productIds = cart.Items.Select(x => x.ProductId).ToList();
            // Lấy tồn kho cùng lúc bằng 1 query
            var stocks = db.Products
                           .Where(p => productIds.Contains(p.Id))
                           .Select(p => new { p.Id, p.Quantity })
                           .ToList();

            // Tìm xem có item nào vượt tồn không
            var invalids = cart.Items
                .Select(item => new {
                    item.ProductId,
                    item.ProductName,
                    Wanted = item.Quantity,
                    InStock = stocks.FirstOrDefault(s => s.Id == item.ProductId)?.Quantity ?? 0
                })
                .Where(x => x.Wanted > x.InStock)
                .ToList();

            if (invalids.Any())
            {
                // Trả về danh sách các item lỗi
                return Json(new
                {
                    success = false,
                    invalids = invalids.Select(x => new {
                        x.ProductId,
                        x.ProductName,
                        x.Wanted,
                        x.InStock
                    })
                });
            }

            // Nếu không có lỗi
            return Json(new { success = true });
        }
        //thêm getstock để lấy số lượng hàng tồn
        [AllowAnonymous]
        [HttpGet]
        public JsonResult GetStock(int productId)
        {
            var product = db.Products
                .Where(p => p.Id == productId)
                .Select(p => new { p.Id, p.Quantity })
                .FirstOrDefault();
            if (product == null)
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            return Json(new { success = true, quantity = product.Quantity }, JsonRequestBehavior.AllowGet);
        }


        #region Thanh toán vnpay
        public string UrlPayment(int TypePaymentVN, string orderCode)
        {
            var urlPayment = "";
            var order = db.Orders.FirstOrDefault(x => x.Code == orderCode);
            //Get Config Info
            string vnp_Returnurl = ConfigurationManager.AppSettings["vnp_Returnurl"]; //URL nhan ket qua tra ve 
            string vnp_Url = ConfigurationManager.AppSettings["vnp_Url"]; //URL thanh toan cua VNPAY 
            string vnp_TmnCode = ConfigurationManager.AppSettings["vnp_TmnCode"]; //Ma định danh merchant kết nối (Terminal Id)
            string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"]; //Secret Key

            //Build URL for VNPAY
            VnPayLibrary vnpay = new VnPayLibrary();
            var Price = (long)order.TotalAmount * 100;
            vnpay.AddRequestData("vnp_Version", VnPayLibrary.VERSION);
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", Price.ToString()); //Số tiền thanh toán. Số tiền không mang các ký tự phân tách thập phân, phần nghìn, ký tự tiền tệ. Để gửi số tiền thanh toán là 100,000 VND (một trăm nghìn VNĐ) thì merchant cần nhân thêm 100 lần (khử phần thập phân), sau đó gửi sang VNPAY là: 10000000
            if (TypePaymentVN == 1)
            {
                vnpay.AddRequestData("vnp_BankCode", "VNPAYQR");
            }
            else if (TypePaymentVN == 2)
            {
                vnpay.AddRequestData("vnp_BankCode", "VNBANK");
            }
            else if (TypePaymentVN == 3)
            {
                vnpay.AddRequestData("vnp_BankCode", "INTCARD");
            }

            vnpay.AddRequestData("vnp_CreateDate", order.CreatedDate.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress());
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toán đơn hàng :" + order.Code);
            vnpay.AddRequestData("vnp_OrderType", "other"); //default value: other

            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", order.Code); // Mã tham chiếu của giao dịch tại hệ thống của merchant. Mã này là duy nhất dùng để phân biệt các đơn hàng gửi sang VNPAY. Không được trùng lặp trong ngày

            //Add Params of 2.1.0 Version
            //Billing

            urlPayment = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            //log.InfoFormat("VNPAY URL: {0}", paymentUrl);
            return urlPayment;
        }
        #endregion
    }
}