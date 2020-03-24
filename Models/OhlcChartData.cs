using System;
using System.Collections.Generic;
using KrakenCore.Models;

namespace Bitar.Models
{
    public class OhlcChartData
    {
        public string Pair { get; set; }
        public long Last { get; set; }
        public List<OhlcChart> OhlcChart { get; set; }
    }

    public class OhlcChart
    {
        public DateTime Time { get; set; }
        public decimal Ohlc { get; set; }
    }
}