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

        public ContinentScanner()
        {
            runningCount = 1;
        }

        public async Task InitializeAsync(int continent)
        {
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
                    ws.Options.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                    ws.Options.SetRequestHeader("Host", "socf-lok-live.leagueofkingdoms.com");
                    ws.Options.AddSubProtocol("websocket");
                    ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
                    return ws;
                });

                const string baseWsUrl = "wss://socf-lok-live.leagueofkingdoms.com/socket.io/?EIO=4&transport=websocket";
                const string token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJfaWQiOiI2NzQ1NWIwYzIyZmI0ODRhMDVkYTg3ZWQiLCJraW5nZG9tSWQiOiI2NzQ1NWIwZDIyZmI0ODRhMDVkYTg3ZjQiLCJ3b3JsZElkIjo2MSwidmVyc2lvbiI6MTc4OSwiYXV0aFR5cGUiOiJjYXJ2IiwicGxhdGZvcm0iOiJ3ZWIiLCJ0aW1lIjoxNzQwMzg0OTQ1MDk0LCJjbGllbnRYb3IiOiIwIiwiaXAiOiIxNTIuNTkuMjQyLjE3NyIsImlhdCI6MTc0MDM4NDk0NSwiZXhwIjoxNzQwOTg5NzQ1LCJpc3MiOiJub2RnYW1lcy5jb20iLCJzdWIiOiJ1c2VySW5mbyJ9.fqUy-itVQlWm5sYuqJ8DlH4P7bMOyPHseHQqUZGB0VY";
                var url = new Uri($"{baseWsUrl}&token={token}");
                var client = new WebsocketClient(url, factory);

                // Configure WebSocket client
                client.ReconnectTimeout = TimeSpan.FromSeconds(30);
                client.IsReconnectionEnabled = true;
                client.ErrorReconnectTimeout = TimeSpan.FromSeconds(15);
                client.MessageEncoding = System.Text.Encoding.UTF8;
                client.DisconnectionHappened.Subscribe(info =>
                {
                    CustomConsole.WriteLine($"[Connection] Status: {info.Type}", ConsoleColor.Yellow);
                });
                
                // Send initial handshake message after connection
                client.ReconnectionHappened.Subscribe(async info =>
                {
                    CustomConsole.WriteLine($"[Connection] Connected with: {info.Type}", ConsoleColor.Green);
                    await Task.Delay(1000);
                    client.Send("40");
                });
                client.ReconnectionHappened.Subscribe(info => {
                    if (info.Type == ReconnectionType.Error)
                    {
                        CustomConsole.WriteLine($"[Connection] Reconnection attempt due to error", ConsoleColor.Yellow);
                    }
                });

                // Configure keep-alive with error handling
                var timer = new System.Timers.Timer(25000); // 25 seconds
                int failedPingCount = 0;
                const int maxFailedPings = 3;

                client.DisconnectionHappened.Subscribe(async info =>
                {
                    CustomConsole.WriteLine($"[Connection] Disconnection type: {info.Type}", ConsoleColor.Red);
                    if (info.Exception != null)
                    {
                        CustomConsole.WriteLine($"[Connection] Error: {info.Exception.Message}", ConsoleColor.Red);
                    }
                    
                    if (!client.IsStarted && failedPingCount < maxFailedPings)
                    {
                        await Task.Delay(5000); // Wait 5 seconds before reconnecting
                        try
                        {
                            await client.Start();
                        }
                        catch (Exception ex)
                        {
                            CustomConsole.WriteLine($"[Connection] Reconnection failed: {ex.Message}", ConsoleColor.Red);
                        }
                    }
                });

                // Enhanced keep-alive mechanism
                timer.Elapsed += async (sender, e) => 
                {
                    if (client.IsRunning)
                    {
                        try
                        {
                            await client.SendInstant("2"); // Engine.IO ping
                            failedPingCount = 0; // Reset counter on successful ping
                        }
                        catch (Exception ex)
                        {
                            failedPingCount++;
                            CustomConsole.WriteLine($"[Error] Ping failed ({failedPingCount}/{maxFailedPings}): {ex.Message}", ConsoleColor.Red);
                            
                            if (failedPingCount >= maxFailedPings)
                            {
                                CustomConsole.WriteLine("[Connection] Too many failed pings, forcing reconnection", ConsoleColor.Yellow);
                                await client.Reconnect();
                            }
                        }
                    }
                };
                timer.Elapsed += (sender, e) => 
                {
                    if (client.IsRunning)
                    {
                        try
                        {
                            client.Send("2"); // Engine.IO ping
                        }
                        catch (Exception ex)
                        {
                            CustomConsole.WriteLine($"[Error] Ping failed: {ex.Message}", ConsoleColor.Red);
                        }
                    }
                };
                timer.Start();

                client.ReconnectionHappened.Subscribe(info =>
                {
                    CustomConsole.WriteLine($"[Connection] {info.Type}", ConsoleColor.Green);
                    CustomConsole.WriteLine($"[Connection] Attempt: {info.Type}", ConsoleColor.DarkGreen);
                    Thread.Sleep(2000);
                });

                await client.Start();

                client.DisconnectionHappened.Subscribe(info =>
                {
                    CustomConsole.WriteLine($"[Connection] Disconnection type: {info.Type}", ConsoleColor.Red);
                    CustomConsole.WriteLine($"[Connection] Exception: {info.Exception?.Message}", ConsoleColor.Red);
                    CustomConsole.WriteLine($"[Connection] Stack Trace: {info.Exception?.StackTrace}", ConsoleColor.DarkRed);
                });
                client.ReconnectionHappened.Subscribe(info =>
                {
                    CustomConsole.WriteLine($"[Connection] {info.Type}", ConsoleColor.Green);
                    CustomConsole.WriteLine($"[Connection] Attempt: {info.Type}", ConsoleColor.DarkGreen);
                    Thread.Sleep(2000); // Add delay before sending messages after reconnection
                });
                _ = client.MessageReceived.Subscribe(msg =>
                {
                    string message = msg.Text;
                    string json = "";
                    JObject parse = new();

                    CustomConsole.WriteLine($"[Debug] Received message: {message}", ConsoleColor.Gray);

                    if (message == "2") // Handle ping
                    {
                        client.Send("3"); // Send pong
                        return;
                    }

                    if (message == "40")
                    {
                        var fieldEnter = "{\"token\":\"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJfaWQiOiI2NzQ1NWIwYzIyZmI0ODRhMDVkYTg3ZWQiLCJraW5nZG9tSWQiOiI2NzQ1NWIwZDIyZmI0ODRhMDVkYTg3ZjQiLCJ3b3JsZElkIjo2MSwidmVyc2lvbiI6MTc4OSwiYXV0aFR5cGUiOiJjYXJ2IiwicGxhdGZvcm0iOiJ3ZWIiLCJ0aW1lIjoxNzQwMzg0OTQ1MDk0LCJjbGllbnRYb3IiOiIwIiwiaXAiOiIxNTIuNTkuMjQyLjE3NyIsImlhdCI6MTc0MDM4NDk0NSwiZXhwIjoxNzQwOTg5NzQ1LCJpc3MiOiJub2RnYW1lcy5jb20iLCJzdWIiOiJ1c2VySW5mbyJ9.fqUy-itVQlWm5sYuqJ8DlH4P7bMOyPHseHQqUZGB0VY\"}";
                        var base64FieldEnter = "42[\"/field/enter/v3\",\"" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(fieldEnter)) + "\"]";
                        client.Send(base64FieldEnter);
                        CustomConsole.WriteLine("[Connection] Sent field enter request", ConsoleColor.Green);
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
                //client.Start();  Removed - Start is now in InitializeAsync

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
            try
            {
                Task.Run(() => client.Send(leaveZoneCommand)).Wait(1000);
                CustomConsole.WriteLine("[Request] Leave zone command sent successfully", ConsoleColor.Cyan);
            }
            catch (Exception ex)
            {
                CustomConsole.WriteLine($"[Error] Failed to send leave command: {ex.Message}", ConsoleColor.Red);
            }

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