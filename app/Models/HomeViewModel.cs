namespace app.Models
{
    public class HomeViewModel
    {
        public bool DatabaseConnected { get; set; }
        public string? DatabaseMessage { get; set; }
        public int AccountCount { get; set; }
        public int RoleCount { get; set; }
    }
}