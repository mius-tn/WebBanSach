using WedBanSach.Models;

namespace WedBanSach.ViewModels
{
    public class HomeViewModel
    {
        public IEnumerable<Category>    Categories { get; set; }
        public IEnumerable<Book> FeaturedBooks { get; set; }
        public IEnumerable<Book> NewBooks { get; set; }
        public Dictionary<Category, List<Book>> CategoryBooks { get; set; } = new Dictionary<Category, List<Book>>();
    }
}
