namespace BlueSchoolSystem.Models.ViewModel
{
    public class CreateStudentWithUserRequest
    {
        public string UserName { get; set; } // Username để tạo tài khoản
        public string Email { get; set; }
        public string Password { get; set; }
        public SinhVien Student { get; set; } // Model sinh viên bên bạn đã có
    }
}
