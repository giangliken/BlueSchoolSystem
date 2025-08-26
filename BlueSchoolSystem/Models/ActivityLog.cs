namespace BlueSchoolSystem.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }
        public string UserId { get; set; }          // ID của người dùng
        public string UserName { get; set; }        // Tên người dùng 
        public string IpAddress { get; set; }       // Địa chỉ IP user thực hiện
        public string Device { get; set; }          //Loại thiêt bị
        public string ActionType { get; set; }      //Hành động    
        public string TableName { get; set; }       //Bảng chịu ảnh hưởng
        public string ObjectId { get; set; }        //ID cụ thể của đối tượng bị tác động
        public string Description { get; set; }     //Thông tin chi tiết
        public DateTime Timestamp { get; set; }     // Thời điểm hành động
    }
}
