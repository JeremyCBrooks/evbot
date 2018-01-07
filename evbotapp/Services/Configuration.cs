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
            Commands = GetArray("Commands");
            CommandFile = Get("CommandFile");
            SlackBaseUrl = Get("Slack:BaseUrl");
            ChargePointBaseUrl = Get("ChargePoint:BaseUrl");
            ChargePointDeviceIds = GetArray("ChargePoint:DeviceIds");
            Greets = GetArray("Greetings");
            Color = GetArray("Color");

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

        public string[] GetArray(string key)
        {
            var ret = Get(key).Split(";");
            return ret;
        }
    }
}
