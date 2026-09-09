namespace Nezabuti.Api.Services;

/// <summary>
/// Daily billing processor at configured local hour (default 10:00 Europe/Kyiv).
/// Does not run on startup — waits until the next scheduled local run, then re-computes after each job (DST-safe).
/// </summary>
public sealed class BillingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IBillingClock _clock;
    private readonly ILogger<BillingBackgroundService> _logger;

    public BillingBackgroundService(
        IServiceScopeFactory scopeFactory,
        IBillingClock clock,
        ILogger<BillingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = BillingSchedule.GetDelayUntilNextRun(
                _clock.UtcNow,
                _clock.TimeZone,
                _clock.DailyRunHour);
            var nextUtc = BillingSchedule.GetNextRunUtc(
                _clock.UtcNow,
                _clock.TimeZone,
                _clock.DailyRunHour);

            _logger.LogInformation(
                "Billing job scheduled for {NextRunUtc:o} ({TimeZone}, local hour {Hour}). Waiting {Delay}.",
                nextUtc,
                _clock.TimeZone.Id,
                _clock.DailyRunHour,
                delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var job = scope.ServiceProvider.GetRequiredService<IMemorialBillingJobService>();
                await job.ProcessAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BillingBackgroundService iteration failed");
            }

            // After the job, loop and recompute next 10:00 local (important around DST).
        }
    }
}
