namespace TaskManagementMvc.Models.ViewModels
{
    public class AdminSettingsViewModel
    {
        public bool UseRedis { get; set; }
        public int RedisTtlDays { get; set; }
        public string RedisConnectionString { get; set; } = "localhost:6379";
    }
}
