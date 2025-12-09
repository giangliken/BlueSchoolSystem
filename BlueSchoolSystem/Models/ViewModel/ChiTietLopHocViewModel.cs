namespace BlueSchoolSystem.Models.ViewModel
{
    public class ChiTietLopHocViewModel
    {
        public string MaLop { get; set; }
        public string TenLop { get; set; }

        // Cán sự
        public dynamic LopTruong { get; set; }
        public dynamic LopPho { get; set; }
        public dynamic BiThu { get; set; }
        public dynamic TroLy { get; set; }

        // Danh sách sinh viên
        public List<dynamic> SinhVien { get; set; }
    }
}
