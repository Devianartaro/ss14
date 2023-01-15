using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Content.Server.Utility;
using NetCoreServer;
using Robust.Shared.Asynchronous;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Content.Server.UtkaIntegration;

public sealed class UtkaSocket : UdpServer
{
    public static Dictionary<string, IUtkaCommand> Commands = new();
    private readonly string Key = string.Empty;
    private readonly ISawmill _sawmill = default!;
    private readonly ITaskManager _taskManager = default!;

    public EndPoint Requester = default!;


    public UtkaSocket(IPAddress address, int port, string key) : base(address, port)
    {
        Key = key;
        _sawmill = Logger.GetSawmill("utkasockets");
        _taskManager = IoCManager.Resolve<ITaskManager>();
        RegisterCommands();
    }

    protected override void OnStarted()
    {
        base.OnStarted();
        ReceiveAsync();
    }

    protected override void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
    {
        base.OnReceived(endpoint, buffer, offset, size);
        Requester = endpoint;
        if (!ValidateMessage(endpoint, buffer, offset, size, out var fromDiscordMessage))
        {
            var message = new ToUtkaMessage()
            {
                Key = Key,
                Command = "retard",
                Message = new()
                {
                    "Wrong key or json"
                }
            };

            SendMessage(message);
            return;
        }

        ExecuteCommand(fromDiscordMessage!, fromDiscordMessage!.Command!, fromDiscordMessage!.Message!.ToArray());
    }

    private bool ValidateMessage(EndPoint endpoint, byte[] buffer, long offset, long size, out FromDiscordMessage? fromDiscordMessage)
    {
        var message = Encoding.UTF8.GetString(buffer, (int) offset, (int) size);
        fromDiscordMessage = null;

        if (string.IsNullOrEmpty(message))
        {
            return false;
        }

        fromDiscordMessage = JsonSerializer.Deserialize<FromDiscordMessage>(message);

        if (!NullCheck(fromDiscordMessage!))
        {
            _sawmill.Info($"UTKASockets: Received message from discord, but it was cringe.");
            return false;
        }

        if (fromDiscordMessage!.Key != Key)
        {
            _sawmill.Info($"UTKASockets: Received message with invalid key from endpoint {endpoint}");
            return false;
        }

        return true;
    }

    public void SendMessage(ToUtkaMessage message)
    {
        var finalMessage = JsonSerializer.Serialize(message);
        SendAsync(Requester, finalMessage);
    }


    private void ExecuteCommand(FromDiscordMessage message, string command, string[] args)
    {
        if (!Commands.ContainsKey(command))
        {
            _sawmill.Warning($"UTKASockets: FAIL! Command {command} not found");
            return;
        }

        _sawmill.Info($"UTKASockets: Execiting command from UTKASocket: {command} args: {string.Join(" ", args)}");
        _taskManager.RunOnMainThread(() => Commands[command].Execute(message, args));
        ReceiveAsync();
    }

    private bool NullCheck(FromDiscordMessage fromDiscordMessage)
    {
        return fromDiscordMessage is {Key: { }, Ckey: { }, Message: { }, Command: { }};
    }

    protected override void OnSent(EndPoint endpoint, long sent)
    {
        base.OnSent(endpoint, sent);
        ReceiveAsync();
    }

    protected override void OnError(SocketError error)
    {
        base.OnError(error);

        _sawmill.Warning($"UTKA SOKETS FAIL! {error}");
    }


    public static void RegisterCommands()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var types = assembly.GetTypes();

        var commands = types.Where(type => typeof(IUtkaCommand).IsAssignableFrom(type) && type.GetInterfaces().Contains(typeof(IUtkaCommand))).ToList();

        foreach (var command in commands)
        {
            if (Activator.CreateInstance(command) is IUtkaCommand utkaCommand)
            {
                Commands[utkaCommand.Name] = utkaCommand;
            }
        }
    }
}
