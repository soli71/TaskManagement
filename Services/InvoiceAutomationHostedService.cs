using Microsoft.Extensions.DependencyInjection;

namespace TaskManagementMvc.Services
{
    public class InvoiceAutomationHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<InvoiceAutomationHostedService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private Timer? _timer;
        private bool _running;

        // اجرای هر ۵ دقیقه (TODO: قابل تنظیم از appsettings)
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

        public InvoiceAutomationHostedService(ILogger<InvoiceAutomationHostedService> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Invoice automation hosted service starting");
            _timer = new Timer(async _ => await TickAsync(), null, TimeSpan.FromSeconds(30), _interval);
            return Task.CompletedTask;
        }

        private async Task TickAsync()
        {
            if (_running) return; // prevent overlap
            _running = true;
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var automation = scope.ServiceProvider.GetRequiredService<IInvoiceAutomationService>();
                var count = await automation.ProcessDueSchedulesAsync();
                if (count > 0)
                    _logger.LogInformation("Processed {Count} invoice schedules", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in invoice automation tick");
            }
            finally
            {
                _running = false;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Invoice automation hosted service stopping");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
