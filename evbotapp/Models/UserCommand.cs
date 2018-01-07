using System.Collections.Generic;

namespace evbotapp.Models
{
    public class UserCommand
    {
        public string user { get; set; }
        public string channel { get; set; }
        public string command { get; set; }
        public string context { get; set; }
        public string event_id { get; set; }
        public string event_time { get; set; }
    }
}
