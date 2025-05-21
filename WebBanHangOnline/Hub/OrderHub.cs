using Microsoft.AspNet.SignalR;
using WebBanHangOnline.Models.ViewModels;

namespace WebBanHangOnline.Hubs
{
    public class OrderHub : Hub
    {
        // Phát sự kiện khi có đơn hàng mới
        public static void BroadcastNewOrder(string orderCode)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<OrderHub>();
            context.Clients.All.newOrder(orderCode);
        }

        // Phát sự kiện khi trạng thái đơn thay đổi
        public static void BroadcastStatusChange(string orderCode, string newStatus)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<OrderHub>();
            context.Clients.All.updateOrderStatus(orderCode, newStatus);
        }

        // Phát sự kiện cập nhật toàn bộ số liệu thống kê realtime
        public static void BroadcastOrderStats(OrderStatisticsViewModel stats)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<OrderHub>();
            context.Clients.All.updateOrderStats(stats);
        }
    }
}
