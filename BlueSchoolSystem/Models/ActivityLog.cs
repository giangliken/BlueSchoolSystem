namespace BlueSchoolSystem.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }

        public string UserId { get; set; }          // ID của người dùng
        public string UserName { get; set; }        // Tên người dùng (hoặc Email)

        public string ActionType { get; set; }      // Ex: "Create", "Update", "Delete", "Import", "Export"
        public string TableName { get; set; }       // Ex: "Product", "ProductVariant"
        public string ObjectId { get; set; }        // ID đối tượng bị tác động

        public string Description { get; set; }     // Nội dung chi tiết
        public DateTime Timestamp { get; set; }     // Thời điểm hành động
    }
}
