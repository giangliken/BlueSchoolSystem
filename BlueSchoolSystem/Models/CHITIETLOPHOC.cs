namespace BlueSchoolSystem.Models
{
    public class CHITIETLOPHOC
    {
        public int Id { get; set; }
        public int LopHocId { get; set; } // ID của lớp học
        public LopHoc? LopHoc { get; set; } // Tham chiếu đến lớp học
        
        //Thông tin trợ lí học tập
        public int? GiangVienId { get; set; } // ID của giáo viên
        public GiangVien? GiangVien { get; set; } // Tham chiếu đến giáo viên

        //Thông tin lớp trưởng
        public int? LopTruongId { get; set; } // ID của sinh viên
        public SinhVien? LopTruong { get; set; } // Tham chiếu đến sinh viên

        //Thông tin lớp phó
        public int? LopPhoId { get; set; } // ID của sinh viên
        public SinhVien? LopPho { get; set; } // Tham chiếu đến sinh viên

        //Thông tin bí thư
        public int? BiThuId { get; set; } // ID của sinh viên
        public SinhVien? BiThu { get; set; } // Tham chiếu đến sinh viên
    }
}
