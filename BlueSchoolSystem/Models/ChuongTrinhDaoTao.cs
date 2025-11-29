//Lớp ChuongTrinhDaoTao đại diện cho một chương trình đào tạo trong hệ thống quản lý trường học.
namespace BlueSchoolSystem.Models
{
    public class ChuongTrinhDaoTao
    {
        public int Id { get; set; }
        public int KhoaHocId { get; set; }
        public KhoaHoc? KhoaHoc { get; set; }

        public int NganhHocId { get; set; }
        public NganhHoc? NganhHoc { get; set; }

    }
}
