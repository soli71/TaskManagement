using TaskManagementMvc.Models;

namespace TaskManagementMvc.Services
{
    public interface IInvoiceAutomationService
    {
        /// <summary>
        /// پردازش تمام زمان‌بندی‌های رسیده به موعد و ایجاد/ارسال فاکتورهای مربوطه.
        /// خروجی تعداد زمان‌بندی‌های پردازش‌شده است.
        /// </summary>
        Task<int> ProcessDueSchedulesAsync(CancellationToken cancellationToken = default);
    }
}
