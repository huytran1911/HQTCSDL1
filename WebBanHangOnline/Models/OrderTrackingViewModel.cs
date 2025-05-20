using System;
namespace WebBanHangOnline.Models.ViewModels
{
    public class OrderTrackingViewModel
    {
        public string Code { get; set; }
        public DateTime CreatedDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
    }
}
