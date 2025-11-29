namespace BlueSchoolSystem.Models
{
    public class GiangVienMonHoc
    {
        public int GiangVienId { get; set; }
        public GiangVien GiangVien { get; set; }

        public int MonHocId { get; set; }
        public MonHoc MonHoc { get; set; }
    }
}
