using KrakenCore.Models;

namespace Bitar.Models
{
    public class OhlcPair
    {
        public string Pair { get; set; }
        public long Last { get; set; }
        public Ohlc[] Ohlc { get; set; }
    }
}