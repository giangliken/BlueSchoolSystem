using BlueSchoolSystem.Models;
using CsvHelper.Configuration;

public class KhoaMap : ClassMap<Khoa>
{
    public KhoaMap()
    {
        // Map cột trong file CSV vào biến trong code
        // Cột "Mã Khoa" trong file -> vào biến MaKhoa
        Map(m => m.MaKhoa).Name("Mã Khoa");

        // Cột "Tên Khoa" -> vào biến TenKhoa
        Map(m => m.TenKhoa).Name("Tên Khoa");

        // Cột Mô tả (nếu có)
        Map(m => m.MoTa).Name("Mô Tả").Optional(); // Optional nghĩa là có cũng được, ko có cũng ko lỗi

        // Thường ID tự tăng nên ta bỏ qua (Ignore) không nhập từ file
        Map(m => m.Id).Ignore();
    }
}