using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fleck;

namespace WorldIO;

class Income
{
    [JsonPropertyName("username")]
    public String username;
    [JsonPropertyName("msg")]
    public String msg; 
}
class Program
{
    static void Main(string[] args)
    {
        List<IWebSocketConnection> sockets = new List<IWebSocketConnection>();
        var server = new WebSocketServer("wss://0.0.0.0:8181");
        server.Certificate = new X509Certificate2("andyxie.cn.pfx", "cpbxx43o");
        server.Start(socket =>
        {
            socket.OnOpen = () =>
            {
                if (!File.Exists("log.txt"))
                {
                    File.Create("log.txt").Close();
                }

                var histstr = File.ReadAllLines("log.txt");
                foreach (var line in histstr)
                {
                    socket.Send(line);
                    Thread.Sleep(10);

                }
                Console.WriteLine($"Open! (For {socket.ConnectionInfo.Id})"); 
                sockets.Add(socket);
            };
            socket.OnClose = () =>
            {
                Console.WriteLine("Close!");
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
                        socket.Send($"[{DateTime.UtcNow}] {income.username} said: {income.msg}");
                    }
                    File.AppendAllText("log.txt", $"[{DateTime.UtcNow}] {income.username} said: {income.msg}\n");


                    Console.WriteLine(income.username);
                }
                catch (Exception e)
                {
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