using System;
using System.Collections.Generic;

namespace Bitar.Models
{
    public class Asset
    {
        public decimal Ask { get; set; }
        public decimal Bid { get; set; }
        public DateTime Time { get; set; }
    }
}