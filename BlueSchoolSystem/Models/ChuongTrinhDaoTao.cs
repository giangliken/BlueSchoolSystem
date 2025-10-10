namespace BlueSchoolSystem.Models
{
    public class ChuongTrinhDaoTao
    {
        public int Id { get; set; }

        public int NganhHocId { get; set; }
        public NganhHoc? NganhHoc { get; set; }

        public int MonHocId { get; set; }
        public MonHoc? MonHoc { get; set; }
        public int NamHoc { get; set; } // Năm học áp dụng CTĐT (VD: 2023)
        public int HocKyThu { get; set; } // VD: 1, 2, 3, 4, ...

        public bool BatBuoc { get; set; } // True = bắt buộc, False = tự chọn
    }
}
