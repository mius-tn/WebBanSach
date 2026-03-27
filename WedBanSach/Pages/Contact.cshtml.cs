using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace WedBanSach.Pages
{
    public class ContactModel : PageModel
    {
        [BindProperty]
        public ContactForm Input { get; set; }

        public string SuccessMessage { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Simulate sending an email or saving to DB
            SuccessMessage = "Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi sớm nhất có thể.";
            
            // Clear input after success
            ModelState.Clear();
            Input = new ContactForm();

            return Page();
        }

        public class ContactForm
        {
            [Required(ErrorMessage = "Vui lòng nhập họ tên")]
            [Display(Name = "Họ và tên")]
            public string FullName { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập email")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            [Display(Name = "Email của bạn")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập chủ đề")]
            [Display(Name = "Chủ đề")]
            public string Subject { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập nội dung")]
            [Display(Name = "Nội dung tin nhắn")]
            public string Message { get; set; }
        }
    }
}
