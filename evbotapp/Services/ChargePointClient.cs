using evbotapp.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace evbotapp.Services
{
    public class ChargePointClient
    {
        private WebClient webClient;
        public ChargePointClient()
        {
            webClient = new WebClient();
        }

        private async Task Login()
        {
            var headers = new[] { "X-Requested-With: XMLHttpRequest", $"Referer: {Configuration.Instance.ChargePointBaseUrl}/home", "Content-Type: application/x-www-form-urlencoded" };//required or server does not accept request
            var response = await webClient.Submit(new Uri($"{Configuration.Instance.ChargePointBaseUrl}/users/validate"), "POST", headers, $"user_name={Configuration.Instance.ChargePointUser}&user_password={Configuration.Instance.ChargePointPassword}", null);
            var content = WebClient.ParseResponse(response);
        }

        public async Task<List<ChargePointStation>> GetStationInfo()
        {
            await Login();

            var headers = new[] { "X-Requested-With: XMLHttpRequest", $"Referer: {Configuration.Instance.ChargePointBaseUrl}/charge_point", "Content-Type: application/x-www-form-urlencoded" };//required or server does not accept request
            var stations = new List<ChargePointStation>();
            foreach (var deviceId in Configuration.Instance.ChargePointDeviceIds)
            {
                try
                {
                    var response = await webClient.Submit(new Uri($"{Configuration.Instance.ChargePointBaseUrl}/dashboard/get_station_info_json"), "POST", headers, $"deviceId={deviceId}", null);
                    var content = WebClient.ParseResponse(response);
                    var r = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ChargePointStation>>(content);
                    if (null != r)
                    {
                        stations.Add(Newtonsoft.Json.JsonConvert.DeserializeObject<ChargePointStation[]>(content)[0]);
                    }
                }
                catch (Exception ex)
                {
                    Console.Out.WriteLine(ex.Message);
                    Console.Out.WriteLine(ex.StackTrace);
                }
            }
            return stations;
        }
    }
}
