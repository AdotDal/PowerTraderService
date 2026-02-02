using CsvHelper;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.IO;

namespace PowerTrader
{
    public class PositionFileWriter(string outputDirectory, ILogger<PositionFileWriter> log)
    {
        private readonly ILogger<PositionFileWriter> _log = log;
        public string OutputDirectory { get; private set; } = outputDirectory;

        public void SaveToCsv(string fileName, PositionSummary position)
        {
            string filePath = Path.Combine(OutputDirectory, fileName);
            using var writer = new StreamWriter(filePath);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            try
            {
                csv.WriteHeader<PositionsHour>();
                csv.NextRecord();
                csv.WriteRecords(position.PositionsByHour);
                _log.LogInformation("Report saved to {FilePath}", Path.GetFullPath(filePath));
            }
            catch (Exception e)
            {
                _log.LogError(e, "Position file {FileName} could not be saved", fileName);
                throw;
            }
        }
    }
}
