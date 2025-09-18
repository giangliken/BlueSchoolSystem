using BlueSchoolSystem.Models;
using CsvHelper.Configuration;

public class LopHocMap : ClassMap<LopHoc>
{
    public LopHocMap()
    {
        Map(m => m.Id);
        Map(m => m.MaLop);
        Map(m => m.TenLop);
        Map(m => m.MoTa);
        Map(m => m.NganhId);
    }
}