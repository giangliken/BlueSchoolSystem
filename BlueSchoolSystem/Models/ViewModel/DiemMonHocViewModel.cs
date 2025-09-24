namespace BlueSchoolSystem.Models.ViewModel
{
    public class DiemMonHocViewModel
    {
        public string MaMonHoc { get; set; }
        public string TenMonHoc { get; set; }
        public int SoTinChi { get; set; }
        public float? DiemChuyenCan { get; set; }
        public float? DiemCuoiKy { get; set; }

        // ✅ Thêm các thuộc tính tính toán
        public double? DiemTongKet =>
            DiemChuyenCan.HasValue && DiemCuoiKy.HasValue
            ? Math.Round(DiemChuyenCan.Value * 0.4 + DiemCuoiKy.Value * 0.6, 2)
            : null;

        public string DiemChu => !DiemTongKet.HasValue ? "N/A" :
            DiemTongKet >= 8.5 ? "A" :
            DiemTongKet >= 7.8 ? "B+" :
            DiemTongKet >= 7.0 ? "B" :
            DiemTongKet >= 6.3 ? "C+" :
            DiemTongKet >= 5.5 ? "C" :
            DiemTongKet >= 4.8 ? "D+" :
            DiemTongKet >= 4.0 ? "D" :
            DiemTongKet >= 3.0 ? "F+" : "F";

        public double? DiemHe4 => DiemChu switch
        {
            "A" => 4.0,
            "B+" => 3.5,
            "B" => 3.0,
            "C+" => 2.5,
            "C" => 2.0,
            "D+" => 1.5,
            "D" => 1.0,
            "F+" => 0.5,
            "F" => 0.0,
            _ => null
        };
    }
}

