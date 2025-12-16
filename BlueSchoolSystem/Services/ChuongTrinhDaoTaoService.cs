using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace BlueSchoolSystem.Services
{
    public class ChuongTrinhDaoTaoService
    {
        private readonly ApplicationDbContext _context;

        public ChuongTrinhDaoTaoService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateProgramAsync(CreateChuongTrinhDaoTaoVM request)
        {
            // 1. Kiểm tra xem Chương trình đào tạo cho Ngành + Khóa này đã tồn tại chưa
            var exists = await _context.ChuongTrinhDaoTaos
                .AnyAsync(c => c.NganhHocId == request.NganhHocId && c.KhoaHocId == request.KhoaHocId);

            if (exists) throw new Exception("Chương trình đào tạo cho Ngành và Khóa học này đã tồn tại.");

            // 2. Tạo Header (Bảng cha)
            var ctdt = new ChuongTrinhDaoTao
            {
                NganhHocId = request.NganhHocId,
                KhoaHocId = request.KhoaHocId
            };
            _context.ChuongTrinhDaoTaos.Add(ctdt);
            await _context.SaveChangesAsync(); // Lưu để lấy ID

            // 3. Tạo Details (Bảng con)
            if (request.ChiTiets != null && request.ChiTiets.Any())
            {
                var details = request.ChiTiets.Select(x => new ChiTietChuongTrinhDaoTao
                {
                    ChuongTrinhDaoTaoId = ctdt.Id,
                    MaMonHoc = x.MaMonHoc,
                    MaMonHocTienQuyet = x.MaMonHocTienQuyet,
                    HocKy = x.HocKy,
                    BatBuoc = x.BatBuoc
                }).ToList();

                _context.ChiTietChuongTrinhDaoTaos.AddRange(details);
                await _context.SaveChangesAsync();
            }
        }
    }
}
