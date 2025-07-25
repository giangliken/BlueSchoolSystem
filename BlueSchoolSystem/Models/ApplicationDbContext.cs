using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlueSchoolSystem.Models
{
    public class ApplicationDbContext: IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        { 
        }

        public DbSet<Student> Students { get; set; } // Bảng sinh viên
        public DbSet<Class> Classes { get; set; } // Bảng lớp học
        public DbSet<Faculty> Faculties { get; set; } // Bảng khoa
        public DbSet<Major> Majors { get; set; } // Bảng ngành học

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            
        }




    }
}
