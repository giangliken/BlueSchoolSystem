namespace BlueSchoolSystem.Models.ViewModel
{
    public class DiemMonHocViewModel
    {
        public string MSSV { get; set; }
        public string MaMonHoc { get; set; }
        public string TenMonHoc { get; set; }
        public int SoTinChi { get; set; }
        public double? DiemQuaTrinh { get; set; }
        public double? DiemCuoiKy { get; set; }

        // ✅ Thêm các thuộc tính tính toán
        public double? DiemTongKet =>
            DiemQuaTrinh.HasValue && DiemCuoiKy.HasValue
            ? Math.Round(DiemQuaTrinh.Value * 0.4 + DiemCuoiKy.Value * 0.6, 2)
            : null;

        public string DiemChu => !DiemTongKet.HasValue ? "N/A" :
            DiemTongKet >= 8.5 ? "A" :
            DiemTongKet >= 7.0 ? "B" :
            DiemTongKet >= 5.5 ? "C" :
            DiemTongKet >= 4.0 ? "D" : "F";

        public double? DiemHe4 => DiemChu switch
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

