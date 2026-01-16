using WedBanSach.Models;

namespace WedBanSach.ViewModels
{
    public class NotificationDetailViewModel
    {
        public Notification Notification { get; set; } = null!;
        public Order? Order { get; set; }
    }
}
