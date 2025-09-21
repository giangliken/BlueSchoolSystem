namespace BlueSchoolSystem.Models.ViewModel
{
    public class DiemSinhVienViewModel
    {
        public int HocKyId { get; set; }     // Id học kỳ
        public string TenHocKy { get; set; } // Tên học kỳ
        public DateTime NgayBatDau { get; set; } // Ngày bắt đầu học kỳ
        public string MSSV { get; set; }          // Mã số sinh viên
        public string MaMonHoc { get; set; }      // Mã môn học
        public string TenMonHoc { get; set; }     // Tên môn học
        public double? DiemQuaTrinh { get; set; } // Điểm quá trình (có thể null)
        public double? DiemCuoiKy { get; set; }   // Điểm cuối kỳ (có thể null)
        public int SoTinChi { get; set; }         // Số tín chỉ của môn học
        public bool HopLe { get; set; }           // Có đăng ký lớp hay không



        // ✅ Điểm tổng kết hệ số 40% + 60% (thang 10)
        public double? DiemTongKet
        {
            get
            {
                if (DiemQuaTrinh.HasValue && DiemCuoiKy.HasValue)
                {
                    return Math.Round(DiemQuaTrinh.Value * 0.4 + DiemCuoiKy.Value * 0.6, 2);
                }
                return null;
            }
        }

        // ✅ Điểm chữ
        public string DiemChu
        {
            get
            {
                if (!DiemTongKet.HasValue) return "N/A";

                double diem = DiemTongKet.Value;
                if (diem >= 8.5) return "A";
                if (diem >= 7.0) return "B";
                if (diem >= 5.5) return "C";
                if (diem >= 4.0) return "D";
                return "F";
            }
        }

        // ✅ Điểm hệ 4 (GPA)
        public double? DiemHe4
        {
            get
            {
                if (!DiemTongKet.HasValue) return null;

                return DiemChu switch
                {
                    "A" => 4.0,
                    "B" => 3.0,
                    "C" => 2.0,
                    "D" => 1.0,
                    "F" => 0.0,
                    _ => null
                };
            }
        }
    }
}
