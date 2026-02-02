using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PowerTrader
{
    public class PowerService : BackgroundService
    {
        private readonly ILogger<PowerService> _log;
        private readonly IPowerService _powerService;
        private readonly PositionFileWriter _writer;
        private readonly int _intervalMinutes;

        private readonly object _lock = new object();
        private bool _isRunning = false;

        public PowerService(IConfiguration config, ILogger<PowerService> log, ILogger<PositionFileWriter> writerLog)
        {
            _log = log;

            string? configuredPath = config["FilePath"];
            string outputDirectory = string.IsNullOrWhiteSpace(configuredPath) ? "./reports" : configuredPath;
            _intervalMinutes = ParseInterval(config["ScheduleIntervalMinutes"] ?? string.Empty);

            Directory.CreateDirectory(outputDirectory);

            _powerService = new Services.PowerService();
            _writer = new PositionFileWriter(outputDirectory, writerLog);
        }

        private static DateTime DayAhead
        {
            get { return HelperTimeZone.GetLondonNow().AddDays(1).Date; }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _log.LogInformation("Power Service started");

            // Run once on startup
            await GenerateReportAsync();

            DateTime nextRun = HelperTimeZone.GetLondonNow().AddMinutes(_intervalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    DateTime now = HelperTimeZone.GetLondonNow();
                    if (now < nextRun)
                    {
                        await Task.Delay(nextRun - now, stoppingToken);
                        continue;
                    }

                    // Catchup when behind schedule to avoid missing scheduled reports
                    do
                    {
                        await GenerateReportAsync();
                        nextRun = nextRun.AddMinutes(_intervalMinutes);
                        now = HelperTimeZone.GetLondonNow();
                    }
                    while (now >= nextRun && !stoppingToken.IsCancellationRequested);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            _log.LogInformation("Power Service stopped");
        }

        private async Task GenerateReportAsync()
        {
            lock (_lock)
            {
                if (_isRunning)
                {
                    return;
                }

                _isRunning = true;
            }

            try
            {
                await Task.Run(ProcessPosition);
            }
            finally
            {
                lock (_lock)
                {
                    _isRunning = false;
                }
            }
        }

        private void ProcessPosition()
        {
            try
            {
                IEnumerable<PowerTrade> trades = _powerService.GetTrades(DayAhead);
                var position = new PositionSummary(DayAhead);
                position.ComputePosition(new List<PowerTrade>(trades));
                _log.LogInformation("Power Service positions processed");
                SavePosition(position);
            }
            catch (PowerServiceException ex)
            {
                _log.LogWarning(ex, "Error not able to get trades from PowerService");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Unexpected error while processing positions");
            }
        }

        private void SavePosition(PositionSummary position)
        {
            DateTime extractTime = HelperTimeZone.GetLondonNow();
            string fileName = $"PowerPosition_{extractTime:yyyyMMdd}_{extractTime:HHmm}.csv";
            _writer.SaveToCsv(fileName, position);
        }

        private static int ParseInterval(string configuredValue)
        {
            if (int.TryParse(configuredValue, out int result) && result > 0)
            {
                return result;
            }

            return 1;
        }

    }
}
