using KrakenCore.Models;

namespace Bitar.Models
{
    public class OhlcData
    {
        public string Pair { get; set; }
        public long Last { get; set; }
        public Ohlc[] Ohlc { get; set; }
    }
}