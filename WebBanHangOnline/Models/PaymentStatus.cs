namespace WebBanHangOnline.Models
{
    /// <summary>
    /// Enum quản lý các trạng thái thanh toán
    /// </summary>
    public enum PaymentStatus
    {
        ThanhCong = 1,    // Thanh toán thành công
        ChoThanhToan = 2,    // Chờ thanh toán
        ThatBai = 3     // Thanh toán thất bại
    }
}