using Microsoft.AspNetCore.Mvc;

namespace BlueSchoolSystem.Controllers
{
    public class TeacherController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
