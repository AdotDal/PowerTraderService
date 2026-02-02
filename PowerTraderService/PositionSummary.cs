using System;
using System.Collections.Generic;
using CsvHelper.Configuration.Attributes;
using Services;

namespace PowerTrader
{
    public class PositionSummary(DateTime asOfDate)
    {
        public DateTime Date { get; private set; } = asOfDate;
        public List<PositionsHour> PositionsByHour { get; private set; } = [];

        public void ComputePosition(List<PowerTrade> trades)
        {
            PositionsByHour.Clear();

            // create 24 rows (Period 1 is hour 23:00 of the previous day)
            for (int hour = 0; hour < 24; hour++)
            {
                var row = new PositionsHour();
                row.LocalTime = Date.AddHours(hour - 1).ToString("HH:mm");
                row.Volume = 0.0;

                PositionsByHour.Add(row);
            }

            // add each trade period volume into the correct hour slot
            foreach (var trade in trades)
            {
                foreach (var p in trade.Periods)
                {
                    int hourIndex = p.Period - 1; // Convert period 1-24 into list index 0-23
                    if (hourIndex >= 0 && hourIndex < 24)
                    {
                        PositionsByHour[hourIndex].Volume += p.Volume;
                    }
                }
            }
        }
    }

    public class PositionsHour
    {
        [Name("Local Time")]
        public string LocalTime { get; set; } = string.Empty;
        [Name("Volume")]
        public double Volume { get; set; }
    }
}
