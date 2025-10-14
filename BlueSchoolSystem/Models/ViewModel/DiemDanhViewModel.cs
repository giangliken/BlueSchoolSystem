namespace BlueSchoolSystem.Models.ViewModel
{
    public class DiemDanhViewModel
    {

        public DateTime Ngay { get; set; }       // Ngày điểm danh
        public string? Code { get; set; }        // Mã code của buổi điểm danh
        public string? GhiChu { get; set; }      // Ghi chú của buổi điểm danh
        public string? TrangThai { get; set; }   // Trạng thái điểm danh (có thể null => "Chưa xác định")
        public DateTime? ThoiGian { get; set; }  // Thời gian sinh viên thực hiện điểm danh
        public double? Latitude { get; set; }    // Vĩ độ (nếu có)
        public double? Longitude { get; set; }   // Kinh độ (nếu có)
        public string? GhiChuChiTiet { get; set; } // Ghi chú chi tiết của sinh viên
    }
}
