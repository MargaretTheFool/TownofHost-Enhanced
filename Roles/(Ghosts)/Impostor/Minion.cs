﻿using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

public static class Minion
{
    private static readonly int Id = 27900;

    public static OptionItem AbilityCooldown;
    public static OptionItem AbilityTime;

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Minion);
        AbilityCooldown = FloatOptionItem.Create(Id + 10, "AbilityCooldown", new(2.5f, 180f, 2.5f), 40f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Minion])
            .SetValueFormat(OptionFormat.Seconds);
        AbilityTime = FloatOptionItem.Create(Id + 11, "MinionAbilityTime", new(1f, 10f, 1f), 5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Minion])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public static bool OnCheckProtect(PlayerControl killer, PlayerControl target)
    {
        var ImpPVC = CustomRolesHelper.IsImpostor(target.GetCustomRole());
        if (!ImpPVC)
        {
            Main.PlayerStates[target.PlayerId].IsBlackOut = true;
            target.MarkDirtySettings();
            _ = new LateTask(() =>
            {
                Main.PlayerStates[target.PlayerId].IsBlackOut = false;
                target.MarkDirtySettings();
            }, AbilityTime.GetFloat());
            killer.RpcResetAbilityCooldown();
        }
        return false;
    }
}

