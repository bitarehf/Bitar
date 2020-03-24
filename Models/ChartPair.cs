using System;
using System.Collections.Generic;

namespace Bitar.Models
{
    public class ChartPair
    {
        public string Pair { get; set; }
        public long Last { get; set; }
        public List<ChartData> ChartData { get; set; }
    }

    public class ChartData
    {
        public DateTime Time { get; set; }
        public decimal Value { get; set; }
    }
}