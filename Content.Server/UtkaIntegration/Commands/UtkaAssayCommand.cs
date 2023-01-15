using System.Linq;
using System.Net;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Robust.Shared.Utility;

namespace Content.Server.UtkaIntegration;

public sealed class UtkaAssayCommand : IUtkaCommand
{
    public string Name => "asay";
    public void Execute(FromDiscordMessage message, string[] args)
    {
        var ckey = message.Ckey;
        var content = string.Join(" ", args);

        if(string.IsNullOrWhiteSpace(content)) return;

        var chatManager = IoCManager.Resolve<IChatManager>();

        chatManager.SendHookAdminChat(ckey!, content);

    }
}
