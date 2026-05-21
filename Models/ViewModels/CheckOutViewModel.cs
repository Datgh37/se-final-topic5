using System.ComponentModel.DataAnnotations;

namespace WebUITopic5_Team4.Models.ViewModels
{
    public class CheckOutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(@"^(0|\+84|84)[0-9]{9}$",
            ErrorMessage = "Số điện thoại không hợp lệ")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        [StringLength(255)]
        public string Address { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn tỉnh/thành")]
        public string Province { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        public string PaymentMethod { get; set; }
    }
}