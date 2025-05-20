using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace WebBanHangOnline.Hubs
{
    public class OrderHub : Hub
    {
        // Gửi đi khi có đơn mới
        public static void BroadcastNewOrder(string orderCode)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<OrderHub>();
            context.Clients.All.newOrder(orderCode);
        }

        // Gửi đi khi trạng thái đơn thay đổi
        public static void BroadcastStatusChange(string orderCode, string newStatus)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<OrderHub>();
            context.Clients.All.updateOrderStatus(orderCode, newStatus);
        }
    }
}
