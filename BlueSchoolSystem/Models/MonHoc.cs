namespace BlueSchoolSystem.Models
{
    public class MonHoc
    {
        public int Id { get; set; }
        public string MaMonHoc { get; set; } // Mã môn học
        public string TenMonHoc { get; set; } // Tên môn học
        public int SoTinChi { get; set; } // Số tín chỉ của môn học
        public string? MoTa { get; set; } // Mô tả về môn học
        //public ICollection<NganhHoc>? NganhHocs { get; set; } // nhiều ngành
        public ICollection<GiangVien>? GiangViens { get; set; } // Nhiều GV dạy
        public ICollection<LopHocPhan>? LopHocPhans { get; set; }
        // Liên kết đến chương trình đào tạo
        public ICollection<ChuongTrinhDaoTao>? ChuongTrinhDaoTaos { get; set; }

        // Các môn học mà môn này yêu cầu học trước
        public ICollection<MonHocTienQuyet>? MonHocTienQuyet { get; set; }

        // Các môn mà môn này là tiên quyết cho nó
        public ICollection<MonHocTienQuyet>? LaTienQuyetCua { get; set; }


    }
}
