using System;
using System.Threading.Tasks;

namespace evbotapp.Services
{
    public class SlackClient
    {
        private static string[] headers = new[] { "Content-Type: application/x-www-form-urlencoded" };

        private WebClient webClient;
        public SlackClient()
        {
            webClient = new WebClient();
        }

        public async Task<string> SendMessage(string uri, string data)
        {
            
            var response = await webClient.Submit(new Uri($"{Configuration.Instance.SlackBaseUrl}/{uri}"), "POST", headers, data, null);
            var content = WebClient.ParseResponse(response);
            return content;
        }
    }
}
