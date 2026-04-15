namespace BookingService.Domain.Enums
{
    public enum BookingStatus
    {
        Pending = 1, // Đang chờ xử lý (Chờ Room Service xác nhận còn chỗ)
        Confirmed = 2, // Đã xác nhận (Thanh toán xong, Room Service đã trừ chỗ)
        Active = 3, // Đang hoạt động (Sinh viên đã check-in dọn vào ở)
        Completed = 4, // Đã hoàn thành (Sinh viên đã check-out trả phòng)
        Canceled = 5 // Đã hủy
    }
}