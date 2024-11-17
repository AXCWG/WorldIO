using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fleck;

namespace WorldIO;

class Income
{
    [JsonPropertyName("username")] public String username;
    [JsonPropertyName("msg")] public String msg;
}

class Program
{
    static void Main(string[] args)
    {
        List<IWebSocketConnection> sockets = new List<IWebSocketConnection>();
        var server = new WebSocketServer("wss://0.0.0.0:8181");
        server.Certificate = new X509Certificate2("andyxie.cn.pfx", "cpbxx43o");
        server.EnabledSslProtocols = SslProtocols.Tls12;
        server.Start(socket =>
        {
            socket.OnOpen = () =>
            {
                if (!File.Exists("log.txt"))
                {
                    File.Create("log.txt").Close();
                }

                var histstr = File.ReadAllLines("log.txt");
                String append = string.Empty;
                foreach (var line in histstr)
                {
                    append += line + Environment.NewLine;
                }

                socket.Send(append);
                Console.WriteLine(
                    $"[{DateTime.UtcNow}UTC] Open! (For {socket.ConnectionInfo.Id}, {socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort})");
                File.AppendAllText("log_ex.txt",
                    $"[{DateTime.UtcNow}UTC] Open! (For {socket.ConnectionInfo.Id}, {socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort})\n");
                sockets.Add(socket);
            };
            socket.OnClose = () =>
            {
                Console.WriteLine(
                    $"[{DateTime.UtcNow}UTC] Close! (For {socket.ConnectionInfo.Id}, {socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort})");
                File.AppendAllText("log_ex.txt",
                    $"[{DateTime.UtcNow}UTC] Close! (For {socket.ConnectionInfo.Id}, {socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort})\n");

                sockets.Remove(socket);
            };
            socket.OnMessage = message =>
            {
                try
                {
                    Console.WriteLine($"Message: {message}");

                    Income income = JsonSerializer.Deserialize<Income>(message, new JsonSerializerOptions()
                    {
                        IncludeFields = true,
                    })!;
                    foreach (var socket in sockets)
                    {
                        socket.Send($"[{DateTime.UtcNow}UTC] {income.username} said: {income.msg}");
                    }

                    File.AppendAllText("log.txt", $"[{DateTime.UtcNow}UTC] {income.username} said: {income.msg}\n");
                    File.AppendAllText("log_ex.txt",
                        $"[{DateTime.UtcNow}UTC] {income.username}({socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort}) said: {income.msg}\n");


                    Console.WriteLine(income.username);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    File.AppendAllText("log_ex.txt", $"[{DateTime.UtcNow}UTC] {e}\n");
                    socket.Send("Fuck you. ");
                }
            };
        });
        while (true)
        {
            Console.ReadKey();
        }
    }
}