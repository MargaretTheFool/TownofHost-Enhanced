﻿using static TOHE.Options;

namespace TOHE.Roles.AddOns.Impostor;
public static class Tricky
{
    private const int Id = 19900;
    private static OptionItem EnabledDeathReasons;
    //private static Dictionary<byte, PlayerState.DeathReason> randomReason = [];

    public static void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Tricky, canSetNum: true, tab: TabGroup.Addons);
        EnabledDeathReasons = BooleanOptionItem.Create(Id + 11, "OnlyEnabledDeathReasons", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Tricky]);
    }
    //public static void Init()
    //{
    //    randomReason = [];
    //}
    private static PlayerState.DeathReason ChangeRandomDeath()
    {
        PlayerState.DeathReason[] deathReasons = EnumHelper.GetAllValues<PlayerState.DeathReason>().Where(reason => reason.IsReasonEnabled()).ToArray();
        if (deathReasons.Length == 0 || !deathReasons.Contains(PlayerState.DeathReason.Kill)) deathReasons.AddItem(PlayerState.DeathReason.Kill);
        var random = IRandom.Instance;
        int randomIndex = random.Next(deathReasons.Length);
        return deathReasons[randomIndex];
    }
    private static bool IsReasonEnabled( this PlayerState.DeathReason reason)
    {
        if (reason is PlayerState.DeathReason.etc) return false;
        if (!EnabledDeathReasons.GetBool()) return true;
        return reason.DeathReasonIsEnable();
    }
    public static void AfterPlayerDeathTasks(PlayerControl target)
    {
        if (target == null) return;
        _ = new LateTask(() =>
        {
            var killer = target.GetRealKiller();
            if (killer == null || !killer.Is(CustomRoles.Tricky)) return;
            
            Main.PlayerStates[target.PlayerId].deathReason = ChangeRandomDeath();
            Main.PlayerStates[target.PlayerId].SetDead();
            Utils.NotifyRoles(SpecifySeer: target);

        }, 0.3f, "Tricky random death reason");
    }
}