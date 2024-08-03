using AmongUs.GameOptions;
using static TOHE.Options;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Lookout : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 11800;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    private static OptionItem WatchCooldown;
    private static OptionItem MaxUsesPerRound;
    private static OptionItem MaxUses;
    private static OptionItem GetNearTarget;
    private static OptionItem GetNearTargetRadius;
    private static OptionItem SeePlayerIDs;

    private static readonly Dictionary<byte, int> MaxWatchLimit = [];
    private static readonly Dictionary<byte, int> RoundWatchLimit = [];
    private static readonly Dictionary<byte, HashSet<byte>> WatchList = [];
    private static readonly Dictionary<byte, HashSet<byte>> VisitList = [];
    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Lookout);
        WatchCooldown = FloatOptionItem.Create(Id + 10, "LookoutWatchCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Lookout])
            .SetValueFormat(OptionFormat.Seconds);
        MaxUsesPerRound = IntegerOptionItem.Create(Id + 11, "LookoutMaxUsesPerRound", new(1, 15, 1), 5, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Lookout])
            .SetValueFormat(OptionFormat.Times);
        MaxUses = IntegerOptionItem.Create(Id + 12, "AbilityUseLimit", new(1, 100, 1), 10, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Lookout])
            .SetValueFormat(OptionFormat.Times);
        GetNearTarget = BooleanOptionItem.Create(Id + 13, "LookoutGetNearTargetList", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Lookout]);
        GetNearTargetRadius = FloatOptionItem.Create(Id + 14, "LookoutGetNearTargetRadius", new(0.5f, 10f, 0.5f), 1.5f, TabGroup.CrewmateRoles, false)
            .SetParent(GetNearTarget)
            .SetValueFormat(OptionFormat.Multiplier);
        SeePlayerIDs = BooleanOptionItem.Create(Id + 15, "LookoutSeePlayerIDs", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Lookout]);
    }
    
    public override void Init()
    {
        playerIdList.Clear();
        MaxWatchLimit.Clear();
        RoundWatchLimit.Clear();
        WatchList.Clear();
        VisitList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        MaxWatchLimit[playerId] = MaxUses.GetInt();
        RoundWatchLimit[playerId] = MaxUsesPerRound.GetInt();
        WatchList[playerId] = [];
        VisitList[playerId] = [];

        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        MaxWatchLimit.Remove(playerId);
        RoundWatchLimit.Remove(playerId);
        WatchList.Remove(playerId);
    }
    public override bool CanUseKillButton(PlayerControl pc) => pc.IsAlive();
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = WatchCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;

        if (!MaxWatchLimit.ContainsKey(killer.PlayerId)) MaxWatchLimit[killer.PlayerId] = MaxUses.GetInt();
        if (!RoundWatchLimit.ContainsKey(killer.PlayerId)) RoundWatchLimit[killer.PlayerId] = MaxUsesPerRound.GetInt();

        if (MaxWatchLimit[killer.PlayerId] < 1 || RoundWatchLimit[killer.PlayerId] < 1) return false;

        MaxWatchLimit[killer.PlayerId]--;
        RoundWatchLimit[killer.PlayerId]--;
        if (!WatchList.ContainsKey(killer.PlayerId)) WatchList[killer.PlayerId] = [];
        WatchList[killer.PlayerId].Add(target.PlayerId);
        return base.ForcedCheckMurderAsKiller(killer, target);
    }
    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        return base.CheckMurderOnOthersTarget(killer, target);
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (!SeePlayerIDs.GetBool() || !seer.IsAlive() || !seen.IsAlive()) return string.Empty;

        return ColorString(GetRoleColor(CustomRoles.Lookout), $" {seen.Data.PlayerId}");
    }
}
