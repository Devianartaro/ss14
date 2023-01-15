using System.Net;

namespace Content.Server.UtkaIntegration;

public interface IUtkaCommand
{
    string Name { get; }
    public void Execute(FromDiscordMessage message, string[] args);
}
