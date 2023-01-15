using System.Linq;
using System.Net;
using Content.Server.Administration.Managers;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Server.RoundEnd;
using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Content.Server.UtkaIntegration;

public sealed class UtkaStatusCommand : IUtkaCommand
{
    public string Name => "serverstatus";

    private IPlayerManager _playerManager => IoCManager.Resolve<IPlayerManager>();
    private IAdminManager _adminManager => IoCManager.Resolve<IAdminManager>();
    private RoundEndSystem _roundEndSystem => EntitySystem.Get<RoundEndSystem>();
    private GameTicker _gameTicker => EntitySystem.Get<GameTicker>();
    private IConfigurationManager _configurationManager => IoCManager.Resolve<IConfigurationManager>();
    private UtkaSocketWrapper _utkaSocketWrapper => IoCManager.Resolve<UtkaSocketWrapper>();

    public void Execute(FromDiscordMessage message, string[] args)
    {
        var currentPlayerCount = _playerManager.PlayerCount.ToString();
        var currentAdminCount = _adminManager.AllAdmins.Count().ToString();
        string shuttleData = string.Empty;

        if (_roundEndSystem.ExpectedCountdownEnd == null)
        {
            shuttleData = "shuttle is not on the way";
        }
        else
        {
            shuttleData = $"shuttle is on the way, ETA: {_roundEndSystem.ShuttleTimeLeft}";
        }

        var roundDuration = _gameTicker.RoundDuration().ToString();
        var gameMap = _configurationManager.GetCVar(CCVars.GameMap);

        var toUtkaMessage = new ToUtkaMessage()
        {
            Key = _configurationManager.GetCVar(CCVars.UtkaSocketKey),
            Command = "serverstatus",
            Message = new List<string>()
            {
                currentPlayerCount,
                currentAdminCount,
                shuttleData,
                roundDuration,
                gameMap
            }
        };

        _utkaSocketWrapper.SendMessage(toUtkaMessage);
    }
}
