//Lớp ChiTietChuongTrinhDaoTao đại diện cho chi tiết của một chương trình đào tạo trong hệ thống quản lý trường học.

namespace BlueSchoolSystem.Models
{
    public class ChiTietChuongTrinhDaoTao
    {
        public int Id { get; set; }
        public int ChuongTrinhDaoTaoId { get; set; }
        public ChuongTrinhDaoTao? ChuongTrinhDaoTao { get; set; }
        public string MaMonHoc { get; set; }   
        public string? MaMonHocTienQuyet { get; set; }  
        public int HocKy { get; set; }

        public bool BatBuoc { get; set; }

    }
}
