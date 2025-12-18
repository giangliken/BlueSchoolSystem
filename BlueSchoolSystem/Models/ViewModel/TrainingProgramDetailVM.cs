namespace BlueSchoolSystem.Models.ViewModel
{
    public class TrainingProgramVM
    {
        public string TenNganh { get; set; }
        public int NamHoc { get; set; }

        // Danh sách các học kỳ
        public List<SemesterVM> Semesters { get; set; } = new List<SemesterVM>();
    }

    public class SemesterVM
    {
        public int HocKy { get; set; } // Học kỳ 1, 2, 3...
        public int TongTinChi { get; set; }
        public List<SubjectInSemesterVM> MonHocs { get; set; } = new List<SubjectInSemesterVM>();
    }

    public class SubjectInSemesterVM
    {
        public string MaMonHoc { get; set; }
        public string TenMonHoc { get; set; }
        public int SoTinChi { get; set; }
        public bool BatBuoc { get; set; } // True = Bắt buộc, False = Tự chọn
        public string? MaMonTienQuyet { get; set; }
        public string? MoTa { get; set; }
    }
}
