namespace BlueSchoolSystem.Models.ViewModel
{
    public class CreateGiangVienWithUserRequest
    {
        public string UserName { get; set; } // Username để tạo tài khoản
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public GiangVien? GiangVien { get; set; } // Model giảng viên bên bạn đã có
    }
}
