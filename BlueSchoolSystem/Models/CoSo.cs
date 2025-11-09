namespace BlueSchoolSystem.Models
{
    public class CoSo
    {
        public int Id { get; set; }
        public string MaCoSo { get; set; }  
        public string TenCoSo { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string DiaChi { get; set; }

        public ICollection<PhongHoc> PhongHocs { get; set; } = new List<PhongHoc>();


    }
}
