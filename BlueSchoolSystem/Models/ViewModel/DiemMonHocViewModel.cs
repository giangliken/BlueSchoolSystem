namespace BlueSchoolSystem.Models.ViewModel
{
    public class DiemMonHocViewModel
    {
        public string MaMonHoc { get; set; }
        public string TenMonHoc { get; set; }
        public int SoTinChi { get; set; }
        public float? DiemChuyenCan { get; set; }
        public float? DiemCuoiKy { get; set; }

        // ✅ Điểm tổng kết 
        public double? DiemTongKet
        {
            get
            {
                if (SoTinChi == 0) return 0; // không tích lũy

                if (SoTinChi == 1)
                {
                    var cc = DiemChuyenCan ?? 0;
                    // làm tròn 1 chữ số thập phân
                    return Math.Round(cc, 1, MidpointRounding.AwayFromZero);
                }

                if (SoTinChi > 1)
                {
                    var cc = DiemChuyenCan ?? 0;
                    var ck = DiemCuoiKy ?? 0;

                    double raw;
                    if (ck == 0)
                        raw = (cc * 0.5) + (ck * 0.5) - 1.5;  // phạt khi chưa có điểm cuối kỳ
                    else
                        raw = (cc * 0.5) + (ck * 0.5);

                    // ép về 0 nếu < 0
                    raw = Math.Max(0, raw);

                    return Math.Round(raw, 1, MidpointRounding.AwayFromZero);
                }

                return null;
            }
        }

        // ✅ Chuyển điểm số sang điểm chữ
        public string DiemChu => !DiemTongKet.HasValue ? " " :
            DiemTongKet >= 8.5 ? "A" :
            DiemTongKet >= 7.8 ? "B+" :
            DiemTongKet >= 7.0 ? "B" :
            DiemTongKet >= 6.3 ? "C+" :
            DiemTongKet >= 5.5 ? "C" :
            DiemTongKet >= 4.8 ? "D+" :
            DiemTongKet >= 4.0 ? "D" :
            DiemTongKet >= 3.0 ? "F+" : "F";

        // ✅ Điểm hệ 4
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
