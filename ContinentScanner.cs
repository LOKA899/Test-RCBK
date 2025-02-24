
using AutoMapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using lok_wss.database.Models;
using System.Threading;
using System.Threading.Tasks;
using Websocket.Client;
using System.Net.WebSockets;
using Object = lok_wss.Models.Object;

namespace lok_wss
{
    public class ContinentScanner
    {
        private static Timer _timer;
        private static string _leaveZones = "";
        public static Mapper mapper;
        private int runningCount;

        public ContinentScanner(int continent)
        {
            runningCount = 1;
            var config = new MapperConfiguration(cfg =>
                cfg.CreateMap<Object, Kingdom>()
            );

            mapper = new Mapper(config);

            int thisContinent = continent;
            try
            {
                var exitEvent = new ManualResetEvent(false);
                var factory = new Func<ClientWebSocket>(() => {
                    var ws = new ClientWebSocket();
                    ws.Options.SetRequestHeader("Origin", "https://play.leagueofkingdoms.com");
                    ws.Options.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                    return ws;
                });

                var url = new Uri("wss://socf-lok-live.leagueofkingdoms.com/socket.io/?EIO=4&transport=websocket");
                using var client = new WebsocketClient(url, factory)
                {
                    ReconnectTimeout = TimeSpan.FromSeconds(30),
                    IsReconnectionEnabled = true,
                    ErrorReconnectTimeout = TimeSpan.FromSeconds(5)
                };

                client.DisconnectionHappened.Subscribe(info =>
                {
                    Console.WriteLine($"[Connection] Disconnection type: {info.Type}, Exception: {info.Exception?.Message}");
                });
                client.ReconnectionHappened.Subscribe(_ =>
                {
                    Console.WriteLine($"[Connection] {_.Type}");
                });
                _ = client.MessageReceived.Subscribe(msg =>
                {
                    string message = msg.Text;
                    string json = "";
                    JObject parse = new();

                    if (message == "40")
                    {
                        var fieldEnter = "{\"token\":\"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJfaWQiOiI2NzQ1NWIwYzIyZmI0ODRhMDVkYTg3ZWQiLCJraW5nZG9tSWQiOiI2NzQ1NWIwZDIyZmI0ODRhMDVkYTg3ZjQiLCJ3b3JsZElkIjo2MSwidmVyc2lvbiI6MTc4OSwiYXV0aFR5cGUiOiJjYXJ2IiwicGxhdGZvcm0iOiJ3ZWIiLCJ0aW1lIjoxNzQwMzc4NzI3MzEzLCJjbGllbnRYb3IiOiIwIiwiaXAiOiIxNTIuNTkuMjQyLjE3NyIsImlhdCI6MTc0MDM3ODcyNywiZXhwIjoxNzQwOTgzNTI3LCJpc3MiOiJub2RnYW1lcy5jb20iLCJzdWIiOiJ1c2VySW5mbyJ9.KcnmmqVkVw3mhKULTHO0KbXFZDSrzBgWeLpnx68VY8k\"}";
                        var base64FieldEnter = "42[\"/field/enter/v3\",\"" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(fieldEnter)) + "\"]";
                        Task.Run(() =>
                            client.Send(base64FieldEnter));
                    }

                    if (message.Contains("/field/objects/v3"))
                    {
                        var enc = message.Split(",")[1];
                        var dec = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(enc.Substring(1, enc.Length - 3)));

                        var mapObjects = JsonConvert.DeserializeObject<Models.Root>(dec);

                        if (mapObjects != null && mapObjects.objects != null && mapObjects.objects.Count != 0)
                        {
                            Console.WriteLine($"[Objects] c{thisContinent}: {mapObjects.objects?.Count} Objects received");

                            // Crystal Mines
                            List<Models.Object> crystalMines = mapObjects.objects
                                .Where(x => x.code.ToString() == "20100105").ToList();
                            if (crystalMines.Count >= 1)
                            {
                                foreach (var mine in crystalMines)
                                {
                                    Console.WriteLine($"[CMine] Level {mine.level} at ({mine.loc[1]}:{mine.loc[2]}) Value: {mine.param.value}");
                                }
                            }

                            // Treasure Goblins
                            List<Object> treasureGoblins = mapObjects.objects
                                .Where(x => x.code.ToString() == "20200104").ToList();
                            if (treasureGoblins.Count >= 1)
                            {
                                foreach (var goblin in treasureGoblins)
                                {
                                    Console.WriteLine($"[Goblin] Level {goblin.level} at ({goblin.loc[1]}:{goblin.loc[2]}) Health: {goblin.param.value}");
                                }
                            }

                            // Death Knights
                            List<Object> dKs = mapObjects.objects
                                .Where(x => x.code.ToString() == "20200201").ToList();
                            if (dKs.Count >= 1)
                            {
                                foreach (var dk in dKs)
                                {
                                    Console.WriteLine($"[DeathKnight] Level {dk.level} at ({dk.loc[1]}:{dk.loc[2]}) Health: {dk.param.value}");
                                }
                            }
                        }
                    }

                    if (message.Contains("/field/enter/v3"))
                    {
                        _timer = new Timer(
                            _ => SendRequest(client, thisContinent, _timer, exitEvent),
                            null,
                            TimeSpan.FromSeconds(5),
                            TimeSpan.FromSeconds(3));
                    }

                    if (message.Contains("{"))
                    {
                        json = Helpers.ExtractJson(message[message.IndexOf("{", StringComparison.Ordinal)..]);
                        parse = JObject.Parse(json);
                    }
                });
                client.Start();

                exitEvent.WaitOne();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ContinentScanner: {ex.Message}");
            }
        }

        private void SendRequest(WebsocketClient client, int continent, Timer timer, ManualResetEvent exitEvent)
        {
            int count = 9;
            string zones = "";
            Random rand = new();
            var leaveZoneCommand = "42[\"/zone/leave/list/v2\", {\"world\":" + continent + ", \"zones\":\"[" + _leaveZones + "]\"}]";
            Task.Run(() => client.Send(leaveZoneCommand));

            for (int i = 0; i < count; i++)
            {
                int number = runningCount++;
                zones += $"{number},";
                if (runningCount >= 4080) runningCount = 1;
            }

            zones = zones.Substring(0, zones.Length - 1);
            _leaveZones = zones;
            var stringToEncode = "{\"world\":" + continent + ", \"zones\":\"[" + zones + "]\"}";
            var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(stringToEncode));
            var command = $"42[\"/zone/enter/list/v3\", \"{base64}\"]";

            Task.Run(() => client.Send(command));

            Console.WriteLine($"[Requested] {continent}: {zones}");
        }
    }
}
