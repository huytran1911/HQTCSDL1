using System.Collections.Generic;

namespace WebBanHangOnline.Models.ViewModels
{
    public class OrderStatisticsViewModel
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int PaidOrders { get; set; }
        public int FailedOrders { get; set; }
        public List<string> DayLabels { get; set; }
        public List<int> DayCounts { get; set; }
        public List<string> MonthLabels { get; set; }
        public List<decimal> MonthRevenue { get; set; }
    }
}
