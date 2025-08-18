namespace TaskManagementMvc.Models
{
    /// <summary>
    /// تنظیمات سیستم اعلان‌رسانی
    /// </summary>
    public class NotificationSettings
    {
        /// <summary>
        /// آیا از Redis برای ذخیره‌سازی اعلان‌ها استفاده شود؟
        /// </summary>
        public bool UseRedis { get; set; } = true;
        
        /// <summary>
        /// مدت زمان نگهداری اعلان‌ها در Redis (به روز)
        /// </summary>
        public int RedisTtlDays { get; set; } = 30;
    }
}
