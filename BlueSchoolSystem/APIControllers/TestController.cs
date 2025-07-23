using BlueSchoolSystem.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueSchoolSystem.APIControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TestController(ApplicationDbContext context)
        {
            _context = context;
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
    }
}
