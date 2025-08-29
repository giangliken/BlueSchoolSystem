using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueSchoolSystem.APIControllers
{
    [Route("api")]
    [ApiController]
    public class APITestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;


        public APITestController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("usernames")]
        public async Task<IActionResult> GetAllUsernames()
        {
            var usernames = await _context.Users
                .Select(u => u.UserName)
                .ToListAsync();
            var totalUsers = await _context.Users.CountAsync();
            return Ok(new
            {
                Usernames = usernames,
                Total = totalUsers
            });
        }

        [HttpGet("healthcheck")]
        public IActionResult Ping()
        {
            return Ok(new { message = "Server is alive" });
        }

    }
}
