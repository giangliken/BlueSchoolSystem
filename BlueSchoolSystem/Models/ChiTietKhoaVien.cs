namespace BlueSchoolSystem.Models
{
    public class ChiTietKhoaVien
    {
        public int Id { get; set; }
        public int KhoaId { get; set; } // ID của khoa
        public Khoa? Khoa { get; set; } // Tham chiếu đến khoa

        //Thông tin trưởng khoa
        public int? TruongKhoaId { get; set; } // ID của giảng viên
        public GiangVien? TruongKhoa { get; set; } // Tham chiếu đến giảng viên

        //Thông tin phó khoa
        public int? PhoKhoaId { get; set; } // ID của giảng viên
        public GiangVien? PhoKhoa { get; set; } // Tham chiếu đến giảng viên

        //Trợ lí khoa
        public int? TroLiKhoaId { get; set; } // ID của giảng viên
        public GiangVien? TroLiKhoa { get; set; } // Tham chiếu đến giảng viên



    }
}
