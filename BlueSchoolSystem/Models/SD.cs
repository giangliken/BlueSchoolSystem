namespace BlueSchoolSystem.Models
{
    public class SD
    {
        public const string Role_Staff = "Staff";
        public const string Role_Teacher = "Teacher";
        public const string Role_Admin = "Admin";
        public const string Role_Student = "Student";

        public static readonly string[] AllRoles =
        {
            Role_Admin,
            Role_Student,
            Role_Teacher,
            Role_Staff
        };
    }
}
