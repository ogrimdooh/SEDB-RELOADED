using System.Collections.ObjectModel;
using System.Xml.Serialization;
using Torch;
using Torch.Views;

namespace SEDiscordBridge
{
    public class SEDBConfig : ViewModel
    {
        [XmlIgnore]
        private bool _enabled = true;
        public bool Enabled { get => _enabled; set => SetValue(ref _enabled, value); }

        [XmlIgnore]
        private bool _preLoad = true;
        public bool PreLoad { get => _preLoad; set => SetValue(ref _preLoad, value); }

        [XmlIgnore]
        private bool _loadRanks = true;
        public bool LoadRanks { get => _loadRanks; set => SetValue(ref _loadRanks, value); }

        [XmlIgnore]
        private bool _embed = false;
        public bool Embed { get => _embed; set => SetValue(ref _embed, value); }

        [XmlIgnore]
        private bool _displaySteamId = false;
        public bool DisplaySteamId { get => _displaySteamId; set => SetValue(ref _displaySteamId, value); }

        [XmlIgnore]
        private string _token = "";
        public string BotToken { get => _token; set => SetValue(ref _token, value); }

        [XmlIgnore]
        private string _chatChannelID = "";
        public string ChatChannelId { get => _chatChannelID; set => SetValue(ref _chatChannelID, value); }

        [XmlIgnore]
        private string _format = ":rocket: **{p}**: {msg}";
        public string Format { get => _format; set => SetValue(ref _format, value); }

        [XmlIgnore]
        private string _format2 = "[D]{p} {msg}";
        public string Format2 { get => _format2; set => SetValue(ref _format2, value); }

        [XmlIgnore]
        private string _commandChannelID = "";
        public string CommandChannelId { get => _commandChannelID; set => SetValue(ref _commandChannelID, value); }

        [XmlIgnore]
        private string _commandPrefix = ";;";
        public string CommandPrefix { get => _commandPrefix; set => SetValue(ref _commandPrefix, value); }

        [XmlIgnore]
        private bool _asServer = false;
        public bool AsServer { get => _asServer; set => SetValue(ref _asServer, value); }

        [XmlIgnore]
        private bool _useNicks = false;
        public bool UseNicks { get => _useNicks; set => SetValue(ref _useNicks, value); }

        [XmlIgnore]
        private bool _botToGame = false;
        public bool BotToGame { get => _botToGame; set => SetValue(ref _botToGame, value); }

        [XmlIgnore]
        private bool _serverToDiscord = false;
        public bool ServerToDiscord { get => _serverToDiscord; set => SetValue(ref _serverToDiscord, value); }

        [XmlIgnore]
        private string _serverName = "Server";
        public string ServerName { get => _serverName; set => SetValue(ref _serverName, value); }

        [XmlIgnore]
        private string _statusChannelID = "";
        public string StatusChannelId { get => _statusChannelID; set => SetValue(ref _statusChannelID, value); }

        [XmlIgnore]
        private string _started = ":white_check_mark: Server Started!";
        public string Started { get => _started; set => SetValue(ref _started, value); }

        [XmlIgnore]
        private string _stopped = ":x: Server Stopped!";
        public string Stopped { get => _stopped; set => SetValue(ref _stopped, value); }

        [XmlIgnore]
        private string _restarted = ":arrows_counterclockwise: Server Go To Restart!";
        public string Restarted { get => _restarted; set => SetValue(ref _restarted, value); }

        [XmlIgnore]
        private bool _stripGPS = false;
        public bool StripGPS { get => _stripGPS; set => SetValue(ref _stripGPS, value); }

        [XmlIgnore]
        private string _connect = ":key: The player {p} connected to server";
        public string Connect { get => _connect; set => SetValue(ref _connect, value); }

        [XmlIgnore]
        private string _join = ":sunny: The player {p} joined the server";
        public string Join { get => _join; set => SetValue(ref _join, value); }

        [XmlIgnore]
        private string _leave = ":new_moon: The player {p} left the server";
        public string Leave { get => _leave; set => SetValue(ref _leave, value); }

        [XmlIgnore]
        private bool _simPing = false;
        public bool SimPing { get => _simPing; set => SetValue(ref _simPing, value); }

        [XmlIgnore]
        private string _simChannel = "";
        public string SimChannel { get => _simChannel; set => SetValue(ref _simChannel, value); }

        [XmlIgnore]
        private float _simThresh = 0.60f;
        public float SimThresh { get => _simThresh; set => SetValue(ref _simThresh, value); }

        [XmlIgnore]
        private string _simMessage = "@here Simulation speed has dropped below threshold!";
        public string SimMessage { get => _simMessage; set => SetValue(ref _simMessage, value); }

        [XmlIgnore]
        private int _simCooldown = 1200;
        public int SimCooldown { get => _simCooldown; set => SetValue(ref _simCooldown, value); }

        [XmlIgnore]
        private bool _useStatus = true;
        public bool UseStatus { get => _useStatus; set => SetValue(ref _useStatus, value); }

        [XmlIgnore]
        private int _statusInterval = 5000;
        public int StatusInterval { get => _statusInterval; set => SetValue(ref _statusInterval, value); }

        [XmlIgnore]
        private string _statusPre = "Server Starting...";
        public string StatusPre { get => _statusPre; set => SetValue(ref _statusPre, value); }

        [XmlIgnore]
        private string _status = "{p} players | SS {ss}";
        public string Status { get => _status; set => SetValue(ref _status, value); }

        [XmlIgnore]
        private bool _mentionOthers = true;
        public bool MentOthers { get => _mentionOthers; set => SetValue(ref _mentionOthers, value); }

        [XmlIgnore]
        private bool _mentionEveryone = false;
        public bool MentEveryone { get => _mentionEveryone; set => SetValue(ref _mentionEveryone, value); }

        [XmlIgnore]
        private string _tokenVisibleState = "Visible";
        public string TokenVisibleState { get => _tokenVisibleState; set => SetValue(ref _tokenVisibleState, value); }

        [XmlIgnore]
        private int _removeResponse = 30;
        public int RemoveResponse { get => _removeResponse; set => SetValue(ref _removeResponse, value); }

        [XmlIgnore]
        private ObservableCollection<string> _facChannels = new ObservableCollection<string>();
        public ObservableCollection<string> FactionChannels { get => _facChannels; set => SetValue(ref _facChannels, value); }

        [XmlIgnore]
        private string _globalColor = "White";
        public string GlobalColor { get => _globalColor; set => SetValue(ref _globalColor, value); }

        [XmlIgnore]
        private string _facColor = "Green";
        public string FacColor { get => _facColor; set => SetValue(ref _facColor, value); }

        [XmlIgnore]
        private string _facformat = ":ledger: **{p}**: {msg}";
        public string FacFormat { get => _facformat; set => SetValue(ref _facformat, value); }

        [XmlIgnore]
        private string _facformat2 = "[D-Fac]{p}";
        public string FacFormat2 { get => _facformat2; set => SetValue(ref _facformat2, value); }

        [XmlIgnore]
        private ObservableCollection<string> _cmdPerms = new ObservableCollection<string>();
        public ObservableCollection<string> CommandPerms { get => _cmdPerms; set => SetValue(ref _cmdPerms, value); }

        /* New Channels */

        [XmlIgnore]
        private string _serverId = "";
        public string ServerId { get => _serverId; set => SetValue(ref _serverId, value); }

        [XmlIgnore]
        private string _alertsChannelID = "";
        public string AlertsChannelId { get => _alertsChannelID; set => SetValue(ref _alertsChannelID, value); }

        [XmlIgnore]
        private string _registryChannelID = "";
        public string RegistryChannelId { get => _registryChannelID; set => SetValue(ref _registryChannelID, value); }

        [XmlIgnore]
        private string _seasonMetaChannelID = "";
        public string SeasonMetaChannelID { get => _seasonMetaChannelID; set => SetValue(ref _seasonMetaChannelID, value); }

        [XmlIgnore]
        private string _bankChannelID = "";
        public string BankChannelId { get => _bankChannelID; set => SetValue(ref _bankChannelID, value); }

        [XmlIgnore]
        private string _professionChannelId = "";
        public string ProfessionChannelId { get => _professionChannelId; set => SetValue(ref _professionChannelId, value); }

        [XmlIgnore]
        private string _rankingsChannelId = "";
        public string RankingsChannelId { get => _rankingsChannelId; set => SetValue(ref _rankingsChannelId, value); }

        /* Server */

        [XmlIgnore]
        public string _serverLeftAction = "left";
        public string ServerLeftAction { get => _serverLeftAction; set => SetValue(ref _serverLeftAction, value); }

        [XmlIgnore]
        public string _serverDisconnectedAction = "disconnect from";
        public string ServerDisconnectedAction { get => _serverDisconnectedAction; set => SetValue(ref _serverDisconnectedAction, value); }

        [XmlIgnore]
        public string _serverKickedAction = "was kicked from";
        public string ServerKickedAction { get => _serverKickedAction; set => SetValue(ref _serverKickedAction, value); }

        [XmlIgnore]
        public string _serverBannedAction = "was banned from";
        public string ServerBannedAction { get => _serverBannedAction; set => SetValue(ref _serverBannedAction, value); }

        /* Others */

        [XmlIgnore]
        public bool _nameUnknownUserAsServer = true;
        public bool NameUnknownUserAsServer { get => _nameUnknownUserAsServer; set => SetValue(ref _nameUnknownUserAsServer, value); }

        [XmlIgnore]
        public string _serverUserName = "Server";
        public string ServerUserName { get => _serverUserName; set => SetValue(ref _serverUserName, value); }

        [XmlIgnore]
        public bool _debugMode = false;
        public bool DebugMode { get => _debugMode; set => SetValue(ref _debugMode, value); }

        [XmlIgnore]
        public int _playerCheckStatusInterval = 1000;
        public int PlayerCheckStatusInterval { get => _playerCheckStatusInterval; set => SetValue(ref _playerCheckStatusInterval, value); }

        /* Unknow Signals */

        [XmlIgnore]
        public bool _displayContainerMessages = true;
        public bool DisplayContainerMessages { get => _displayContainerMessages; set => SetValue(ref _displayContainerMessages, value); }

        [XmlIgnore]
        public bool _displayOnlyStrongContainerMessages = true;
        public bool DisplayOnlyStrongContainerMessages { get => _displayOnlyStrongContainerMessages; set => SetValue(ref _displayOnlyStrongContainerMessages, value); }

        [XmlIgnore]
        public string _containerMessage = ":package: {t} has spawn to {p} at {c}.";
        public string ContainerMessage { get => _containerMessage; set => SetValue(ref _containerMessage, value); }

        [XmlIgnore]
        public string _strongContainerMessage = ":package: {t} has spawn at {c}.";
        public string StrongContainerMessage { get => _strongContainerMessage; set => SetValue(ref _strongContainerMessage, value); }

        [XmlIgnore]
        public string _getedContainerMessage = ":package: {p} just got the {t}.";
        public string GetedContainerMessage { get => _getedContainerMessage; set => SetValue(ref _getedContainerMessage, value); }

        /* Grid Jump */

        [XmlIgnore]
        public bool _displayGridsJumpMessages = true;
        public bool DisplayGridsJumpMessages { get => _displayGridsJumpMessages; set => SetValue(ref _displayGridsJumpMessages, value); }

        [XmlIgnore]
        public bool _displayOnlyFirstJumpMessage = false;
        public bool DisplayOnlyFirstJumpMessage { get => _displayOnlyFirstJumpMessage; set => SetValue(ref _displayOnlyFirstJumpMessage, value); }

        [XmlIgnore]
        public string _gridJumpMessage = ":rocket: {p} just start a jump with {g} for {d}km through space.";
        public string GridJumpMessage { get => _gridJumpMessage; set => SetValue(ref _gridJumpMessage, value); }

        [XmlIgnore]
        public string _firstGridJumpMessage = ":rocket: {p} just start his *first* jump with {g} for {d}km through space. Congratulations!";
        public string FirstGridJumpMessage { get => _firstGridJumpMessage; set => SetValue(ref _firstGridJumpMessage, value); }

        [XmlIgnore]
        public string _unknowJumpGridName = "unknow grid";
        public string UnknowJumpGridName { get => _unknowJumpGridName; set => SetValue(ref _unknowJumpGridName, value); }

        /* Grid Graviry */

        [XmlIgnore]
        public bool _displayGridsGravityMessages = true;
        public bool DisplayGridsGravityMessages { get => _displayGridsGravityMessages; set => SetValue(ref _displayGridsGravityMessages, value); }

        [XmlIgnore]
        public bool _displayLeaveGravityMessages = true;
        public bool DisplayLeaveGravityMessages { get => _displayLeaveGravityMessages; set => SetValue(ref _displayLeaveGravityMessages, value); }

        [XmlIgnore]
        public bool _displayEnterGravityMessages = true;
        public bool DisplayEnterGravityMessages { get => _displayEnterGravityMessages; set => SetValue(ref _displayEnterGravityMessages, value); }

        [XmlIgnore]
        public bool _displayFirstEnterGravityMessages = true;
        public bool DisplayFirstEnterGravityMessages { get => _displayFirstEnterGravityMessages; set => SetValue(ref _displayFirstEnterGravityMessages, value); }

        [XmlIgnore]
        public string _gridGravityMessage = ":earth_americas: {p} aboard the {g} just {a} the {t} gravity field.";
        public string GridGravityMessage { get => _gridGravityMessage; set => SetValue(ref _gridGravityMessage, value); }

        [XmlIgnore]
        public string _pilotNoGridGravityMessage = ":earth_americas: Without ship {p} just {a} the {t} gravity field.";
        public string PilotNoGridGravityMessage { get => _pilotNoGridGravityMessage; set => SetValue(ref _pilotNoGridGravityMessage, value); }

        [XmlIgnore]
        public string _gravityActionEnter = "enters";
        public string GravityActionEnter { get => _gravityActionEnter; set => SetValue(ref _gravityActionEnter, value); }

        [XmlIgnore]
        public string _gravityActionFirstEnter = "enters for the first time";
        public string GravityActionFirstEnter { get => _gravityActionFirstEnter; set => SetValue(ref _gravityActionFirstEnter, value); }

        [XmlIgnore]
        public string _gravityActionLeave = "leave";
        public string GravityActionLeave { get => _gravityActionLeave; set => SetValue(ref _gravityActionLeave, value); }

        [XmlIgnore]
        public string _unknowPlanetNameToUse = "unknow";
        public string UnknowPlanetNameToUse { get => _unknowPlanetNameToUse; set => SetValue(ref _unknowPlanetNameToUse, value); }

        [XmlIgnore]
        public string _unknowGravityGridName = "unknow grid";
        public string UnknowGravityGridName { get => _unknowGravityGridName; set => SetValue(ref _unknowGravityGridName, value); }

        [XmlIgnore]
        public float _maxDistanceToDetectAPlanet = 120;
        public float MaxDistanceToDetectAPlanet { get => _maxDistanceToDetectAPlanet; set => SetValue(ref _maxDistanceToDetectAPlanet, value); }

        /* Contracts */

        [XmlIgnore]
        public string _completeFirstContractMessage = ":receipt: The player {p} has completed their first contract. Congratulations!";
        public string CompleteFirstContractMessage { get => _completeFirstContractMessage; set => SetValue(ref _completeFirstContractMessage, value); }

        /* Player */

        [XmlIgnore]
        public bool _displayRespawnMessages = true;
        public bool DisplayRespawnMessages { get => _displayRespawnMessages; set => SetValue(ref _displayRespawnMessages, value); }

        [XmlIgnore]
        public string _respawnMessage = ":wheel: The player {p} has respawn in a rover.";
        public string RespawnMessage { get => _respawnMessage; set => SetValue(ref _respawnMessage, value); }

        [XmlIgnore]
        public string _callDropMessage = ":parachute: The player {p} has call a drop dignal for {g}.";
        public string CallDropMessage { get => _callDropMessage; set => SetValue(ref _callDropMessage, value); }

        [XmlIgnore]
        public bool _displayDieMessages = true;
        public bool DisplayDieMessages { get => _displayDieMessages; set => SetValue(ref _displayDieMessages, value); }

        [XmlIgnore]
        public bool _ignoreBotDieMessages = false;
        public bool IgnoreBotDieMessages { get => _ignoreBotDieMessages; set => SetValue(ref _ignoreBotDieMessages, value); }

        [XmlIgnore]
        public string _dieMessage = ":skull: The player {p} has died by {c} after taking {d} of damage.";
        public string DieMessage { get => _dieMessage; set => SetValue(ref _dieMessage, value); }

        [XmlIgnore]
        public string _murderMessage = ":skull: The player {p} was murdered by {p2} that cause {d} of damage by {c}.";
        public string MurderMessage { get => _murderMessage; set => SetValue(ref _murderMessage, value); }

        [XmlIgnore]
        public string _firstKillMessage = ":wolf: {p} tasted blood for the first time.";
        public string FirstKillMessage { get => _firstKillMessage; set => SetValue(ref _firstKillMessage, value); }

        [XmlIgnore]
        public string _dieCauseNone = "a unknow source";
        public string DieCauseNone { get => _dieCauseNone; set => SetValue(ref _dieCauseNone, value); }

        [XmlIgnore]
        public string _dieCauseCreature = "a wicked creature";
        public string DieCauseCreature { get => _dieCauseCreature; set => SetValue(ref _dieCauseCreature, value); }

        [XmlIgnore]
        public string _dieCauseBullet = "a accurate shot";
        public string DieCauseBullet { get => _dieCauseBullet; set => SetValue(ref _dieCauseBullet, value); }

        [XmlIgnore]
        public string _dieCauseExplosion = "a unexpected explosion";
        public string DieCauseExplosion { get => _dieCauseExplosion; set => SetValue(ref _dieCauseExplosion, value); }

        [XmlIgnore]
        public string _dieCauseRadioactivity = "a radiation poisoning";
        public string DieCauseRadioactivity { get => _dieCauseRadioactivity; set => SetValue(ref _dieCauseRadioactivity, value); }

        [XmlIgnore]
        public string _dieCauseFire = "a merciless flames";
        public string DieCauseFire { get => _dieCauseFire; set => SetValue(ref _dieCauseFire, value); }

        [XmlIgnore]
        public string _dieCauseToxicity = "a intoxication";
        public string DieCauseToxicity { get => _dieCauseToxicity; set => SetValue(ref _dieCauseToxicity, value); }

        [XmlIgnore]
        public string _dieCauseFall = "a painful fall";
        public string DieCauseFall { get => _dieCauseFall; set => SetValue(ref _dieCauseFall, value); }

        [XmlIgnore]
        public string _dieCauseTool = "a unpredictable tool";
        public string DieCauseTool { get => _dieCauseTool; set => SetValue(ref _dieCauseTool, value); }

        [XmlIgnore]
        public string _dieCauseEnvironment = "a environment source";
        public string DieCauseEnvironment { get => _dieCauseEnvironment; set => SetValue(ref _dieCauseEnvironment, value); }

        [XmlIgnore]
        public string _dieCauseSuicide = "a sad suicide";
        public string DieCauseSuicide { get => _dieCauseSuicide; set => SetValue(ref _dieCauseSuicide, value); }

        [XmlIgnore]
        public string _dieCauseAsphyxia = "a agonizing suffocation";
        public string DieCauseAsphyxia { get => _dieCauseAsphyxia; set => SetValue(ref _dieCauseAsphyxia, value); }

        [XmlIgnore]
        public string _dieCauseOther = "a unexpected source";
        public string DieCauseOther { get => _dieCauseOther; set => SetValue(ref _dieCauseOther, value); }

        /* Faction */

        [XmlIgnore]
        public bool _displayFactionMessages = true;
        public bool DisplayFactionMessages { get => _displayFactionMessages; set => SetValue(ref _displayFactionMessages, value); }

        [XmlIgnore]
        public bool _ignoreBotInFactionMessages = true;
        public bool IgnoreBotInFactionMessages { get => _ignoreBotInFactionMessages; set => SetValue(ref _ignoreBotInFactionMessages, value); }

        [XmlIgnore]
        public bool _ignoreNpcFactionsInMessages = true;
        public bool IgnoreNpcFactionsInMessages { get => _ignoreNpcFactionsInMessages; set => SetValue(ref _ignoreNpcFactionsInMessages, value); }

        [XmlIgnore]
        public string _ignoredFactionTags = "";
        public string IgnoredFactionTags { get => _ignoredFactionTags; set => SetValue(ref _ignoredFactionTags, value); }

        [XmlIgnore]
        public string _factionCretedMessage = ":bust_in_silhouette: The faction {f} has been creted by the player {p}.";
        public string FactionCretedMessage { get => _factionCretedMessage; set => SetValue(ref _factionCretedMessage, value); }

        [XmlIgnore]
        public string _factionRemovedMessage = ":bust_in_silhouette: A faction has been removed by the player {p}.";
        public string FactionRemovedMessage { get => _factionRemovedMessage; set => SetValue(ref _factionRemovedMessage, value); }

        [XmlIgnore]
        public string _factionActionMessage = ":busts_in_silhouette: The faction {f} has {a} to {f2} by the player {p}.";
        public string FactionActionMessage { get => _factionActionMessage; set => SetValue(ref _factionActionMessage, value); }

        [XmlIgnore]
        public string _factionMemberActionFactionMessage = ":busts_in_silhouette: The player {p} has {a} to the faction {f}.";
        public string FactionMemberActionFactionMessage { get => _factionMemberActionFactionMessage; set => SetValue(ref _factionMemberActionFactionMessage, value); }

        [XmlIgnore]
        public string _factionMemberActionMemberMessage = ":busts_in_silhouette: The player {p} has {a} from {p2} at the faction {f}.";
        public string FactionMemberActionMemberMessage { get => _factionMemberActionMemberMessage; set => SetValue(ref _factionMemberActionMemberMessage, value); }

        [XmlIgnore]
        public string _factionActionRemoveFaction = "Remove Faction";
        public string FactionActionRemoveFaction { get => _factionActionRemoveFaction; set => SetValue(ref _factionActionRemoveFaction, value); }

        [XmlIgnore]
        public string _factionActionSendPeaceRequest = "Send Peace Request";
        public string FactionActionSendPeaceRequest { get => _factionActionSendPeaceRequest; set => SetValue(ref _factionActionSendPeaceRequest, value); }

        [XmlIgnore]
        public string _factionActionCancelPeaceRequest = "Cancel Peace Request";
        public string FactionActionCancelPeaceRequest { get => _factionActionCancelPeaceRequest; set => SetValue(ref _factionActionCancelPeaceRequest, value); }

        [XmlIgnore]
        public string _factionActionAcceptPeace = "Accept Peace";
        public string FactionActionAcceptPeace { get => _factionActionAcceptPeace; set => SetValue(ref _factionActionAcceptPeace, value); }

        [XmlIgnore]
        public string _factionActionDeclareWar = "Declare War";
        public string FactionActionDeclareWar { get => _factionActionDeclareWar; set => SetValue(ref _factionActionDeclareWar, value); }

        [XmlIgnore]
        public string _factionActionSendFriendRequest = "Send Friend Request";
        public string FactionActionSendFriendRequest { get => _factionActionSendFriendRequest; set => SetValue(ref _factionActionSendFriendRequest, value); }

        [XmlIgnore]
        public string _factionActionCancelFriendRequest = "Cancel Friend Request";
        public string FactionActionCancelFriendRequest { get => _factionActionCancelFriendRequest; set => SetValue(ref _factionActionCancelFriendRequest, value); }

        [XmlIgnore]
        public string _factionActionAcceptFriendRequest = "Accept Friend Request";
        public string FactionActionAcceptFriendRequest { get => _factionActionAcceptFriendRequest; set => SetValue(ref _factionActionAcceptFriendRequest, value); }

        [XmlIgnore]
        public string _factionActionFactionMemberSendJoin = "Send Join Request";
        public string FactionActionFactionMemberSendJoin { get => _factionActionFactionMemberSendJoin; set => SetValue(ref _factionActionFactionMemberSendJoin, value); }

        [XmlIgnore]
        public string _factionActionFactionMemberCancelJoin = "Cancel Join Request";
        public string FactionActionFactionMemberCancelJoin { get => _factionActionFactionMemberCancelJoin; set => SetValue(ref _factionActionFactionMemberCancelJoin, value); }

        [XmlIgnore]
        public string _factionActionFactionMemberAcceptJoin = "Accept Join Request";
        public string FactionActionFactionMemberAcceptJoin { get => _factionActionFactionMemberAcceptJoin; set => SetValue(ref _factionActionFactionMemberAcceptJoin, value); }

        [XmlIgnore]
        public string _factionActionFactionMemberKick = "Kick";
        public string FactionActionFactionMemberKick { get => _factionActionFactionMemberKick; set => SetValue(ref _factionActionFactionMemberKick, value); }

        [XmlIgnore]
        public string _factionActionFactionMemberPromote = "Promote";
        public string FactionActionFactionMemberPromote { get => _factionActionFactionMemberPromote; set => SetValue(ref _factionActionFactionMemberPromote, value); }

        [XmlIgnore]
        public string _factionActionFactionMemberDemote = "Demote";
        public string FactionActionFactionMemberDemote { get => _factionActionFactionMemberDemote; set => SetValue(ref _factionActionFactionMemberDemote, value); }

        [XmlIgnore]
        public string _factionActionFactionMemberLeave = "Leave";
        public string FactionActionFactionMemberLeave { get => _factionActionFactionMemberLeave; set => SetValue(ref _factionActionFactionMemberLeave, value); }

        [XmlIgnore]
        public string _factionActionFactionMemberNotPossibleJoin = "Not Possible Join";
        public string FactionActionFactionMemberNotPossibleJoin { get => _factionActionFactionMemberNotPossibleJoin; set => SetValue(ref _factionActionFactionMemberNotPossibleJoin, value); }

    }
}
