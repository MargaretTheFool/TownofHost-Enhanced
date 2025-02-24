using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Crewmate;
using UnityEngine;

namespace TOHE.Roles.Impostor;

internal class Nuker : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Nuker;
    private const int Id = 33300;

    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Bomb");

    public static OptionItem BomberRadius;
    public static OptionItem BombCooldown;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, Role);
        BomberRadius = FloatOptionItem.Create(Id + 2, "BomberRadius", new(0.5f, 100f, 0.5f), 2f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[Role])
            .SetValueFormat(OptionFormat.Multiplier);
        BombCooldown = FloatOptionItem.Create(Id + 5, "BombCooldown", new(5f, 180f, 2.5f), 60f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[Role])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override bool CanUseKillButton(PlayerControl pc) => pc.IsAlive();
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = BombCooldown.GetFloat();
    }
    public override void UnShapeShiftButton(PlayerControl shapeshifter)
    {
        var playerRole = shapeshifter.GetCustomRole();

        Logger.Info("The bomb went off", playerRole.ToString());
        CustomSoundsManager.RPCPlayCustomSoundAll("Boom");

        _ = new Explosion(5f, 0.5f, shapeshifter.GetCustomPosition());

        while (shapeshifter.Is(CustomRoles.Nuker))
        {
            foreach (var target in Main.AllPlayerControls)
            {              
                target.SetDeathReason(PlayerState.DeathReason.Bombed);
                target.RpcMurderPlayer(target);
                target.SetRealKiller(shapeshifter);
                Logger.Info("I LOVE SPAMMING LOGS!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!", "Nuker");
            }
            Logger.Info("I LOVE SPAMMING LOGS!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!", "Nuker");
        }
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(Translator.GetString("BomberShapeshiftText"));
    }
}
