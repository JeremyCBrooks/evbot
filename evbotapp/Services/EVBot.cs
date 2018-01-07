using evbotapp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace evbotapp.Services
{
    public class EVBot:IDisposable
    {
        private List<string> DMList = new List<string>();
        private SlackClient slackClient;
        private ChargePointClient chargePointClient;
        private FileSystemWatcher watcher;
        Timer stationPollingTimer;

        public EVBot()
        {
            slackClient = new SlackClient();
            chargePointClient = new ChargePointClient();

            watcher = new FileSystemWatcher();
            stationPollingTimer = new Timer(60000);

            watcher.Path = Path.GetDirectoryName(Configuration.Instance.CommandFile);
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.IncludeSubdirectories = false;
            watcher.Filter = Path.GetFileName(Configuration.Instance.CommandFile);

            watcher.Changed += Watcher_Changed;
            watcher.Created += Watcher_Changed;

            watcher.EnableRaisingEvents = true;

            stationPollingTimer.Elapsed += StationPollingTimer_Elapsed;
            stationPollingTimer.Start();
        }

        private async void StationPollingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await ProcessCommands();
        }

        private async Task ProcessCommands()
        {
            if (DMList.Count > 0)
            {
                var stationInf = await chargePointClient.GetStationInfo();
                var availableStations = stationInf.Where(s => s != null && s.summaries != null && s.summaries[0].port_count.available > 0);
                if (availableStations.Count() > 0)
                {
                    foreach (var user in DMList)
                    {
                        //open DM
                        var data = $"token={Configuration.Instance.BotToken}&users={user}";
                        var json = await slackClient.SendMessage("conversations.open", data);

                        dynamic parsedJson = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                        var dmId = parsedJson.channel.id;

                        //send message
                        var availableStationInf = availableStations.Where(s => null != s && s.summaries != null).SelectMany(s => s.summaries.Select(su => $"*{string.Join(" ", su.station_name)} {su.description ?? ""}*: {su.port_count.available} of {su.port_count.total} ports are available."));
                        var greet = getGreet();
                        var color = getColor();
                        var text = $"{greet} <@{user}>! Remember how you wanted me to tell you when a charging port was available? :thinking_face: Well, I'm happy to report that a charging port is available! :tada:\n{string.Join("\n", availableStationInf)}";
                        data = $"token={Configuration.Instance.BotToken}&channel={dmId}&text={text}";
                        await slackClient.SendMessage("chat.postMessage", data);

                        text = "I'll take you off my notification list now, but you can always add yourself back on with `!addme`. See ya! :sunglasses:";
                        data = $"token={Configuration.Instance.BotToken}&channel={dmId}&text={text}";
                        await slackClient.SendMessage("chat.postMessage", data);
                    }
                    DMList.Clear();
                }
            }
        }

        private async void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath != Configuration.Instance.CommandFile) { return; }

            try
            {
                var commands = readCommands();
                foreach (var command in commands)
                {
                    await recvCommand(command);
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        private IEnumerable<UserCommand> readCommands()
        {
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
                            return new UserCommand[] { };
                        }

                        f.Position = 0;
                        f.SetLength(0);
                        f.Flush();

                        var ucmds = Newtonsoft.Json.JsonConvert.DeserializeObject<List<UserCommand>>(fileContent);
                        return ucmds;
                    }
                }
                catch (System.IO.IOException) { }
            } while (true);
        }

        private static string getGreet()
        {
            var rnd = new Random();
            var i = rnd.Next(0, Configuration.Instance.Greets.Length);
            return Configuration.Instance.Greets[i];
        }

        private static string getColor()
        {
            var rnd = new Random();
            var i = rnd.Next(0, Configuration.Instance.Color.Length);
            return Configuration.Instance.Color[i];
        }

        private async Task<string> getCmdResponse(string command)
        {
            switch (command)
            {
                case "!addme":
                    return "You're on my notification list. I'll let you know once I see an available charging port. :zap:";

                case "!removeme":
                    return "You're off my notification list. :cry: I won't bother you again unless you ask me to.";

                case "!station":
                    var stations = await chargePointClient.GetStationInfo();
                    try
                    {
                        if (stations != null && stations.Count > 0)
                        {
                            var stationInf = stations.Where(s => null != s && s.summaries != null).SelectMany(s => s.summaries.Select(su => $"*{string.Join(" ", su.station_name)} {su.description ?? ""}*: {su.port_count.available} of {su.port_count.total} ports are available."));
                            var stationInfUnkCount = stations.Where(s => null == s || s.summaries == null).Count();
                            var stationInfUnk = "";
                            if (stationInfUnkCount > 0)
                            {
                                stationInfUnk = $"\nHmm, it seems that {stationInfUnkCount} of the charging stations did not respond with a status. :thinking_face:";
                            }
                            var si = $"{string.Join("\n", stationInf)}{stationInfUnk}";
                            if (!string.IsNullOrWhiteSpace(si))
                            {
                                return $"Here is what I know about the charging stations:\n{si}";
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Out.WriteLine(ex.Message);
                        Console.Out.WriteLine(ex.StackTrace);
                    }
                    return $"Oh snap! Something must be intefering with my positronic matrix. :confused: I was unable to get the charging station info. :cry: It happens sometimes, please feel free to try again.";

                case "!help":
                    return "Here are some things I can do:\n`!addme` to get notified when a charging port is available\n`!removeme` to be removed from the notification list\n`!station` to get instant status on the charging stations\nWell, that's it. Maybe I'll learn some new tricks in the future. :robot_face:";

                case "!howareyou":
                    return "All systems are functioning within normal parameters. :robot_face: Thanks for asking!";
            }

            return "Unfortunately, I don't know how to answer that yet. :confused: But don't worry, I'll keep learning! :sunglasses:\nTry `!help` if you want help.";
        }

        public async Task recvCommand(UserCommand command)
        {
            switch (command.command)
            {
                case "!addme":
                    if (!DMList.Any(d => d == command.user))
                    {
                        DMList.Add(command.user);
                    }
                    break;

                case "!removeme":
                    if (DMList.Any(d => d == command.user))
                    {
                        DMList.Remove(command.user);
                    }
                    break;
            }

            var greet = getGreet();
            var color = getColor();
            var cmdResponse = await getCmdResponse(command.command);
            var text = $"{greet} <@{command.user}>, {color} {cmdResponse}";
            var data = $"token={Configuration.Instance.BotToken}&channel={command.channel}&text={text}";
            await slackClient.SendMessage("chat.postMessage", data);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if(null != watcher)
                    {
                        watcher.Dispose();
                        watcher = null;
                    }
                    if(null != stationPollingTimer)
                    {
                        stationPollingTimer.Dispose();
                        stationPollingTimer = null;
                    }
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
