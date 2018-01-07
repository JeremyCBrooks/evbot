using evbotapp.Models;
using evbotapp.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;

namespace evbotapp.Controllers
{
    [Route("api/[controller]")]
    public class MessageController : Controller
    {
        // POST api/message (challenge)
        [HttpPost]
        public string Post([FromBody]SlackEvent json)
        {
            if (json.token != Configuration.Instance.ChallengeToken)
            {
                return null;
            }

            if (json.challenge != null)
            {
                return json.challenge;
            }

            if(null != json.message && !string.IsNullOrWhiteSpace(json.message.text.Trim()))
            {
                var text = json.message.text.Trim().ToLowerInvariant();
                foreach (var command in Configuration.Instance.Commands) {
                    if (null != json.message.user && (text.Contains(command) || intent(text).Contains(command)))
                    {
                        var ucmd = new UserCommand
                        {
                            user = json.message.user,
                            command = command,
                            context = json.message.text.Trim(),
                            channel = json.message.channel,
                            event_id = json.event_id,
                            event_time = json.event_time
                        };

                        do
                        {
                            try
                            {
                                using (var f = System.IO.File.Open(Configuration.Instance.CommandFile, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, FileShare.None))
                                {
                                    string fileContent = null;
                                    var sr = new StreamReader(f);
                                    fileContent = sr.ReadToEnd();

                                    if (string.IsNullOrWhiteSpace(fileContent))
                                    {
                                        fileContent = "[]";
                                    }

                                    f.Position = 0;
                                    f.SetLength(0);
                                    f.Flush();

                                    var ucmds = Newtonsoft.Json.JsonConvert.DeserializeObject<List<UserCommand>>(fileContent);
                                    ucmds.Add(ucmd);

                                    var sw = new StreamWriter(f);
                                    sw.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(ucmds));
                                    sw.Flush();
                                }
                                break;
                            }
                            catch(System.IO.IOException) { }
                        } while (true);
                    }
                }
            }
            return "";
        }

        // GET api/message
        [HttpGet()]
        public string Get()
        {
            return "service is running";
        }


        private string intent(string text)
        {
            if(text.Contains($"@{Configuration.Instance.BotId.ToLowerInvariant()}"))
            {
                if (text.Contains("how are you") && text.Contains("?"))
                {
                    return "!howareyou";
                }

                if ( text.Contains("help") ||
                     text.Contains("command") ||
                    ((text.Contains("what") || text.Contains("how")) && text.Contains("do")))
                {
                    return "!help";
                }

                return "!dontknow";
            }
            return "";
        }
    }
}
