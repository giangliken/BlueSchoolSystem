namespace BlueSchoolSystem.Models
{
    public class MonHoc
    {
        public int Id { get; set; }
        public string MaMonHoc { get; set; } // Mã môn học
        public string TenMonHoc { get; set; } // Tên môn học
        public int SoTinChi { get; set; } // Số tín chỉ của môn học
        public string? MoTa { get; set; } // Mô tả về môn học
        public ICollection<NganhHoc>? NganhHocs { get; set; } // nhiều ngành
        public ICollection<GiangVien>? GiangViens { get; set; } // Nhiều GV dạy


    }
}
