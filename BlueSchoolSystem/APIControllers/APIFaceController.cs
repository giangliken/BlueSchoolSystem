using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using BlueSchoolSystem.Repository;
using BlueSchoolSystem.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BlueSchoolSystem.APIControllers
{
    [Route("api/face")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    public class APIFaceController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _um;
        private readonly IFaceTicketStore _ticketStore;
        private readonly FaceVerifyOptions _opt;

        public APIFaceController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> um,
            IFaceTicketStore ticketStore,
            IOptions<FaceVerifyOptions> opt)
        {
            _db = db; _um = um; _ticketStore = ticketStore; _opt = opt.Value;
        }

        [HttpPost("register")]
        public async Task<ActionResult<FaceRegisterResponse>> Register([FromBody] FaceRegisterRequest req)
        {
            if (req.Embeddings is null || req.Embeddings.Count < 2)
                return BadRequest("Need >= 2 samples");

            var uid = User.GetUserId();

            // Giới hạn số template/ user
            var existing = await _db.UserFaceTemplates.Where(t => t.UserId == uid && t.IsActive)
                                                      .OrderByDescending(t => t.CreatedAt).ToListAsync();

            // Nếu vượt quá MaxTemplatesPerUser, deactivate bớt mẫu cũ
            int room = Math.Max(0, _opt.MaxTemplatesPerUser - existing.Count);
            var toInsert = req.Embeddings.Take(Math.Min(room, req.Embeddings.Count)).ToList();

            // Nếu đã đủ chỗ, policy đơn giản: xoá mẫu cũ nhất để nhường mẫu mới
            int need = req.Embeddings.Count - toInsert.Count;
            if (need > 0)
            {
                var remove = existing.TakeLast(Math.Min(need, existing.Count)).ToList();
                _db.UserFaceTemplates.RemoveRange(remove);
                toInsert = req.Embeddings; // chèn hết lô mới
            }

            foreach (var e in toInsert)
            {
                _db.UserFaceTemplates.Add(new UserFaceTemplate
                {
                    UserId = uid,
                    Embedding = FaceUtils.FloatsToBytes(e),
                    Model = req.Model,
                    IsActive = true
                });
            }

            var user = await _um.FindByIdAsync(uid);
            if (user is null) return Unauthorized();

            user.FaceRegistered = true;
            user.FaceRegisteredAt = DateTimeOffset.UtcNow;
            await _um.UpdateAsync(user);
            await _db.SaveChangesAsync();

            return Ok(new FaceRegisterResponse(true));
        }

        [HttpPost("verify")]
        public async Task<ActionResult<FaceVerifyResponse>> Verify([FromBody] FaceVerifyRequest req)
        {
            var uid = User.GetUserId();
            var templates = await _db.UserFaceTemplates
                .Where(t => t.UserId == uid && t.IsActive)
                .ToListAsync();

            if (templates.Count == 0)
                return Ok(new FaceVerifyResponse(false, 0));

            float best = -1f;
            foreach (var t in templates)
            {
                var sim = FaceUtils.Cosine(req.Embedding, FaceUtils.BytesToFloats(t.Embedding));
                if (sim > best) best = sim;
            }

            if (best >= _opt.Threshold)
            {
                var ticketId = Guid.NewGuid().ToString("N");
                _ticketStore.Put(new FaceTicket(
                    Id: ticketId,
                    UserId: uid,
                    ExpiresAt: DateTimeOffset.UtcNow.AddSeconds(_opt.TicketTtlSeconds)
                ));

                // (Optional) log
                _db.FaceVerifyLogs.Add(new FaceVerifyLog
                {
                    UserId = uid,
                    At = DateTimeOffset.UtcNow,
                    Score = best,
                    Success = true,
                    Ip = HttpContext.Connection.RemoteIpAddress?.ToString()
                });
                await _db.SaveChangesAsync();

                return Ok(new FaceVerifyResponse(true, best, ticketId, _opt.TicketTtlSeconds));
            }
            else
            {
                _db.FaceVerifyLogs.Add(new FaceVerifyLog
                {
                    UserId = uid,
                    At = DateTimeOffset.UtcNow,
                    Score = best,
                    Success = false,
                    Ip = HttpContext.Connection.RemoteIpAddress?.ToString()
                });
                await _db.SaveChangesAsync();

                return Ok(new FaceVerifyResponse(false, best));
            }
        }

        [HttpPost("revoke")]
        public async Task<IActionResult> Revoke()
        {
            var uid = User.GetUserId();
            var all = _db.UserFaceTemplates.Where(t => t.UserId == uid);
            _db.UserFaceTemplates.RemoveRange(all);

            var user = await _um.FindByIdAsync(uid);
            if (user != null)
            {
                user.FaceRegistered = false;
                user.FaceRegisteredAt = null;
                await _um.UpdateAsync(user);
            }

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("status")]
        public async Task<IActionResult> Status()
        {
            var uid = User.GetUserId(); // helper đã có
            var user = await _um.FindByIdAsync(uid);
            if (user == null) return Unauthorized();

            // Đếm số template đang active
            var count = await _db.UserFaceTemplates
                .Where(t => t.UserId == uid && t.IsActive)
                .CountAsync();

            // Lấy model mới nhất (nếu muốn show)
            var lastModel = await _db.UserFaceTemplates
                .Where(t => t.UserId == uid && t.IsActive)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => t.Model)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                enrolled = user.FaceRegistered,
                model = lastModel,
                count,
                max = _opt.MaxTemplatesPerUser
            });
        }
    }
}
