using BlueSchoolSystem.Models;
using CsvHelper.Configuration;

public class NganhHocMap : ClassMap<NganhHoc>
{
    public NganhHocMap()
    {
        Map(m => m.Id);
        Map(m => m.MaNganh);
        Map(m => m.TenNganh);
        Map(m => m.KhoaId);
    }
}
