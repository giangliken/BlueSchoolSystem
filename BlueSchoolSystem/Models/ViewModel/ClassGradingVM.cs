namespace BlueSchoolSystem.Models.ViewModel
{
    public class ClassGradingVM
    {
        public int LopHocPhanId { get; set; }
        public string MaLopHocPhan { get; set; }
        public string TenMonHoc { get; set; }
        public string TenGiangVien { get; set; }

        // Danh sách sinh viên và điểm tương ứng
        public List<StudentGradeRowVM> Students { get; set; } = new List<StudentGradeRowVM>();
    }

    public class StudentGradeRowVM
    {
        public int SinhVienId { get; set; }
        public string MSSV { get; set; }
        public string HoTen { get; set; }

        // ID của bảng điểm (nếu đã có thì > 0, chưa có thì = 0)
        public int BangDiemId { get; set; }

        // Cho phép null để hiển thị ô trống nếu chưa nhập
        public float? DiemChuyenCan { get; set; }
        public float? DiemCuoiKy { get; set; }

        // Tính điểm tổng kết (hiển thị chơi thôi, hoặc lưu nếu DB có cột)
        public float? DiemTongKet => (DiemChuyenCan.HasValue && DiemCuoiKy.HasValue)
            ? (DiemChuyenCan.Value * 0.3f + DiemCuoiKy.Value * 0.7f) : null;
    }
}
