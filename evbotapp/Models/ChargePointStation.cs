using System.Collections.Generic;

namespace evbotapp.Models
{
    public class StationPort
    {
        public int available { get; set; }
        public int total { get; set; }
    }

    public class StationSummary
    {
        public StationPort port_count { get; set; }
        public List<string> station_name { get; set; }
        public string description { get; set; }
    }

    public class ChargePointStation
    {
        public List<StationSummary> summaries { get; set; }
    }
}
