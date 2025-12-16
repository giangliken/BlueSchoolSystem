namespace BlueSchoolSystem.Models.ViewModel
{
   // ViewModel chính trả về cho View
    public class HocPhiViewModel
    {
        public decimal DuNoHienTai { get; set; } // Tổng nợ tất cả các kỳ
        
        // Danh sách các học kỳ, mỗi học kỳ chứa danh sách môn học
        public List<ChiTietHocPhi> LichSuPhatSinh { get; set; } // Lịch sử phát sinh chung
        public List<HocPhiTheoKyVM> DanhSachHocKy { get; set; } = new List<HocPhiTheoKyVM>();
        
        public List<PhieuThu> LichSuDongTien { get; set; } // Lịch sử đóng tiền chung
    }

    // ViewModel cho từng học kỳ
    public class HocPhiTheoKyVM
    {
        public string TenHocKy { get; set; }
        public int HocKyId { get; set; }
        public List<ChiTietHocPhiVM> ChiTietMonHoc { get; set; } = new List<ChiTietHocPhiVM>();

        // Các thuộc tính tổng hợp cho từng kỳ
        public decimal TongHocPhiKy => ChiTietMonHoc.Sum(x => x.SoTien);
        
        // Lưu ý: Với mô hình "Dư nợ cuốn chiếu", ta khó biết chính xác số tiền đóng cho kỳ nào 
        // nếu không có sự phân bổ rõ ràng. Tuy nhiên, ta có thể hiển thị tổng học phí phải đóng.
        // Phần "Đã đóng" và "Còn nợ" nên hiển thị chung cho toàn bộ quá trình học tập (như ví điện tử).
        // Hoặc nếu muốn hiển thị chi tiết, bạn cần logic phân bổ tiền đóng (phức tạp hơn).
    }

    // ViewModel chi tiết từng môn
    public class ChiTietHocPhiVM
    {
        public DateTime NgayDK { get; set; }
        public string MaMonHoc { get; set; }
        public string TenMonHoc { get; set; }
        public string TenLopHP { get; set; }
        public int SoTinChi { get; set; }
        public decimal SoTien { get; set; }
    }
}
