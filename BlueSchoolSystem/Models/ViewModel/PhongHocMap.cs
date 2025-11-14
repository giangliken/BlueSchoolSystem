using CsvHelper.Configuration;

namespace BlueSchoolSystem.Models.ViewModel
{
    public class PhongHocMap: ClassMap<PhongHoc>
    {
        public PhongHocMap()
        {
            Map(m => m.Id);
            Map(m => m.MaPhongHoc);
            Map(m => m.TenPhongHoc);
            Map(m => m.SoChoNgoi);
            Map(m => m.MoTa);
            Map(m => m.CoSoId);
        }
    }
}
