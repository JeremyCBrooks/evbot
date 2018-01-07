using Microsoft.Extensions.Configuration;
using System.Collections;
using System.IO;
using System.Linq;

namespace evbotapp.Services
{
    public sealed class Configuration
    {
        public string[] Commands;
        public string CommandFile;
        public string ChallengeToken;
        public string BotId;
        public string BotToken;
        public string SlackBaseUrl;
        public string ChargePointUser;
        public string ChargePointPassword;
        public string ChargePointBaseUrl;
        public string[] ChargePointDeviceIds;
        public string[] Color;
        public string[] Greets;

        private static object lockObj = new object();

        private Configuration()
        {
            var builder = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                                .AddEnvironmentVariables();
#if DEBUG
            builder.AddUserSecrets<Startup>();
#endif

            config = builder.Build();

            //appsettings
            Commands = Get<string[]>("Commands");
            CommandFile = Get("CommandFile");
            SlackBaseUrl = Get("Slack:BaseUrl");
            ChargePointBaseUrl = Get("ChargePoint:BaseUrl");
            ChargePointDeviceIds = Get<string[]>("ChargePoint:DeviceIds");
            Greets = Get<string[]>("Greetings");
            Color = Get<string[]>("Color");

            //secrets
            ChallengeToken = Get("Slack:ChallengeToken");
            BotId = Get("Slack:BotId");
            BotToken = Get("Slack:BotToken");
            ChargePointUser = Get("ChargePoint:User");
            ChargePointPassword = Get("ChargePoint:Password");
        }

        public static Configuration Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObj)
                    {
                        if (instance == null)
                        {
                            instance = new Configuration();
                        }
                    }
                }
                return instance;
            }
        }
        private static Configuration instance = null;

        private static IConfigurationRoot config;

        public string Get(string key)
        {
            var ret = config[key];
            return ret;
        }

        public T Get<T>(string key)
        {
            T ret;
            if (typeof(T) is IEnumerable || typeof(T).IsArray)
            {
                var value = $"[{string.Join(",", config.GetSection(key).GetChildren().Select(c => $"\"{c.Value}\""))}]";
                ret = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(value);
            }
            else
            {
                ret = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(Get(key));
            }

            return ret;
        }
    }
}
