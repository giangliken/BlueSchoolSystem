using Microsoft.AspNetCore.Mvc.Rendering;

namespace BlueSchoolSystem.Models.ViewModel
{
    public class SendThongBaoViewModel
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public List<SelectListItem> StudentList { get; set; } = new();
        public List<string> SelectedUserIds { get; set; } = new(); // userId của Identity
        public string Type { get; set; }
    }
}
