using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlueSchoolSystem.Models;

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

        [HttpGet("usernames")]
        public async Task<IActionResult> GetAllUsernames()
        {
            var usernames = await _context.Users
                .Select(u => u.UserName)
                .ToListAsync();

            return Ok(usernames);
        }
    }
}
