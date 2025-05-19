using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// nhớ import namespace chứa enum PaymentStatus
using WebBanHangOnline.Models;

namespace WebBanHangOnline.Models.EF
{
    [Table("tb_Order")]
    public class Order : CommonAbstract
    {
        public Order()
        {
            this.OrderDetails = new HashSet<OrderDetail>();
            // khởi mặc định trạng thái chờ thanh toán
            this.Status = PaymentStatus.ChoThanhToan;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Code { get; set; }

        [Required(ErrorMessage = "Tên khách hàng không để trống")]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "Số điện thoại không để trống")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Địa chỉ không để trống")]
        public string Address { get; set; }

        public string Email { get; set; }

        public decimal TotalAmount { get; set; }

        public int Quantity { get; set; }

        public int TypePayment { get; set; }

        public string CustomerId { get; set; }

        /// <summary>
        /// Thay vì int, dùng enum để dễ quản lý 3 trạng thái:
        /// 1 = ThanhCong, 2 = ChoThanhToan, 3 = ThatBai
        /// EF sẽ lưu dưới dạng int trong cột Status
        /// </summary>
        [Display(Name = "Trạng thái")]
        public PaymentStatus Status { get; set; }

        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    }
}
