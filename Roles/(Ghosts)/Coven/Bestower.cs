using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles._Ghosts_.Coven;

internal class Bestower : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Bestower;
    private const int Id = 31700;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Bestower);
    public override CustomRoles ThisRoleBase => CustomRoles.GuardianAngel;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenGhosts;
    //==================================================================\\

    private static OptionItem AbilityCooldown;
    private static OptionItem AbilityDuration;
    private static OptionItem AbilityUses;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, Role);
        AbilityCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.AbilityCooldown, new(0f, 120f, 2.5f), 25f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[Role])
            .SetValueFormat(OptionFormat.Seconds);
        AbilityDuration = FloatOptionItem.Create(Id + 11, GeneralOption.AbilityDuration, new(10f, 120f, 2.5f), 30f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[Role])
            .SetValueFormat(OptionFormat.Seconds);
        AbilityUses = IntegerOptionItem.Create(Id + 12, GeneralOption.SkillLimitTimes, new(1, 20, 1), 5, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[Role])
               .SetValueFormat(OptionFormat.Times);
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(AbilityUses.GetInt());
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.GuardianAngelCooldown = AbilityCooldown.GetFloat();
        AURoleOptions.ProtectionDurationSeconds = 0f;
    }
    public override bool OnCheckProtect(PlayerControl killer, PlayerControl target)
    {
        var getTargetRole = target.GetCustomRole();
        if (killer.GetAbilityUseLimit() > 0)
        {
            if (!getTargetRole.IsCoven())
            {
                killer.Notify(GetString("BestowerNonCoven"));
                return false;
            }
            if (CovenManager.HasNecronomicon(target))
            {
                killer.Notify(GetString("BestowerHasNecronomicon"));
                return false;
            }
            var previousNecro = CovenManager.necroHolder;
            CovenManager.GiveNecronomicon(target);
            _ = new LateTask(() =>
            {
                if (GetPlayerById(previousNecro) != null && GetPlayerById(previousNecro).IsAlive()) 
                    CovenManager.GiveNecronomicon(previousNecro);
            }, AbilityDuration.GetFloat(), "Harvester Return Necronomicon");

            killer.RpcResetAbilityCooldown();
            killer.RpcRemoveAbilityUse();
        }
        return false;
    }
}
