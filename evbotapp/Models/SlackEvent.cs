using System.Collections.Generic;

namespace evbotapp.Models
{
    public class SlackEvent
    {
        public string token { get; set; }
        public string challenge { get; set; }

        public string team_id { get; set; }
        public string api_app_id { get; set; }
        public SlackMessage message { get; set; }
        public string @type { get; set; }
        public List<string> authed_users { get; set; }
        public string event_id { get; set; }
        public string event_time { get; set; }
    }
}
