using CsvHelper.Configuration;

namespace BlueSchoolSystem.Models.ViewModel
{
    public class HocKyMap : ClassMap<HocKy>
    {
        public HocKyMap()
        {
            Map(m => m.Id);
            Map(m => m.TenHocKy);
            Map(m => m.NgayBatDau);
            Map(m => m.NgayKetThuc);
            Map(m => m.TrangThaiId);
        }
    }
}
