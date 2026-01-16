using WedBanSach.Models;

namespace WedBanSach.ViewModels
{
    public class NotificationListViewModel
    {
        public IEnumerable<NotificationDisplayDto> Notifications { get; set; } = new List<NotificationDisplayDto>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }

    public class NotificationDisplayDto
    {
        public Notification Notification { get; set; } = null!;
        public OrderNotificationInfo? OrderInfo { get; set; }
    }

    public class OrderNotificationInfo
    {
        public string OrderStatus { get; set; } = string.Empty;
        public string ProductImage { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int OrderId { get; set; }
    }
}
