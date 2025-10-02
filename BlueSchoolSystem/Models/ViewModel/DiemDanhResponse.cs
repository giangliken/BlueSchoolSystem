namespace BlueSchoolSystem.Models.ViewModel
{
    public class DiemDanhResponse<T>
    {
        public bool Result { get; set; }
        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;
        public int SoLuong { get; set; }
        public T Data { get; set; } = default!;
    }
}
