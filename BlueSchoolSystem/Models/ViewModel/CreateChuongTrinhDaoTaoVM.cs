namespace BlueSchoolSystem.Models.ViewModel
{
    public class CreateChuongTrinhDaoTaoVM
    {
        public int NganhHocId { get; set; }
        public int KhoaHocId { get; set; }
        public List<ChiTietCTDTO> ChiTiets { get; set; } = new List<ChiTietCTDTO>();
    }

    public class ChiTietCTDTO
    {
        public string MaMonHoc { get; set; }
        public string? MaMonHocTienQuyet { get; set; }
        public int HocKy { get; set; } // 1, 2, 3...
        public bool BatBuoc { get; set; }
    }
}