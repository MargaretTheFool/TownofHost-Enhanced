﻿using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TOHE.Roles.Crewmate;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Wraith : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 18500;
    private static HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    //==================================================================\\

    private static OptionItem WraithCooldown;
    private static OptionItem WraithDuration;
    private static OptionItem WraithVentNormallyOnCooldown;
    private static OptionItem HasImpostorVision;

    private static Dictionary<byte, long> InvisTime = [];
    private static Dictionary<byte, long> lastTime = [];
    private static Dictionary<byte, int> ventedId = [];

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Wraith, 1, zeroOne: false);        
        WraithCooldown = FloatOptionItem.Create(Id + 2, "WraithCooldown", new(1f, 180f, 1f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Wraith])
            .SetValueFormat(OptionFormat.Seconds);
        WraithDuration = FloatOptionItem.Create(Id + 4, "WraithDuration", new(1f, 60f, 1f), 15f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Wraith])
            .SetValueFormat(OptionFormat.Seconds);
        WraithVentNormallyOnCooldown = BooleanOptionItem.Create(Id + 5, "WraithVentNormallyOnCooldown", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Wraith]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 6, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Wraith]);
    }
    public override void Init()
    {
        playerIdList = [];
        InvisTime = [];
        lastTime = [];
        ventedId = [];
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);

    }
    private static void SendRPC(PlayerControl pc)
    {
        if (pc.AmOwner) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetWraithTimer, SendOption.Reliable, pc.GetClientId());
        writer.Write((InvisTime.TryGetValue(pc.PlayerId, out var x) ? x : -1).ToString());
        writer.Write((lastTime.TryGetValue(pc.PlayerId, out var y) ? y : -1).ToString());
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        InvisTime = [];
        lastTime = [];
        long invis = long.Parse(reader.ReadString());
        long last = long.Parse(reader.ReadString());
        if (invis > 0) InvisTime.Add(PlayerControl.LocalPlayer.PlayerId, invis);
        if (last > 0) lastTime.Add(PlayerControl.LocalPlayer.PlayerId, last);
    }
    private static bool CanGoInvis(byte id)
        => GameStates.IsInTask && !InvisTime.ContainsKey(id) && !lastTime.ContainsKey(id);
    private static bool IsInvis(byte id) => InvisTime.ContainsKey(id);

    private static long lastFixedTime = 0;
    public override void OnReportDeadBody(PlayerControl pa, PlayerControl dum)
    {
        lastTime = [];
        InvisTime = [];

        foreach (var wraithId in playerIdList.ToArray())
        {
            if (!ventedId.ContainsKey(wraithId)) continue;
            var wraith = Utils.GetPlayerById(wraithId);
            if (wraith == null) return;

            wraith?.MyPhysics?.RpcBootFromVent(ventedId.TryGetValue(wraithId, out var id) ? id : Main.LastEnteredVent[wraithId].Id);
            SendRPC(wraith);
        }

        ventedId = [];
    }
    public override void AfterMeetingTasks()
    {
        lastTime = [];
        InvisTime = [];
        foreach (var pc in Main.AllAlivePlayerControls.Where(x => playerIdList.Contains(x.PlayerId)).ToArray())
        {
            lastTime.Add(pc.PlayerId, Utils.GetTimeStamp());
            SendRPC(pc);
        }
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        var now = Utils.GetTimeStamp();

        if (lastTime.TryGetValue(player.PlayerId, out var time) && time + (long)WraithCooldown.GetFloat() < now)
        {
            lastTime.Remove(player.PlayerId);
            if (!player.IsModClient()) player.Notify(GetString("WraithCanVent"));
            SendRPC(player);
        }

        if (lastFixedTime != now)
        {
            lastFixedTime = now;
            Dictionary<byte, long> newList = [];
            List<byte> refreshList = [];
            foreach (var it in InvisTime)
            {
                var pc = Utils.GetPlayerById(it.Key);
                if (pc == null) continue;
                var remainTime = it.Value + (long)WraithDuration.GetFloat() - now;
                if (remainTime < 0)
                {
                    lastTime.Add(pc.PlayerId, now);
                    pc?.MyPhysics?.RpcBootFromVent(ventedId.TryGetValue(pc.PlayerId, out var id) ? id : Main.LastEnteredVent[pc.PlayerId].Id);
                    ventedId.Remove(pc.PlayerId);
                    NameNotifyManager.Notify(pc, GetString("WraithInvisStateOut"));
                    SendRPC(pc);
                    continue;
                }
                else if (remainTime <= 10)
                {
                    if (!pc.IsModClient()) pc.Notify(string.Format(GetString("WraithInvisStateCountdown"), remainTime + 1));
                }
                newList.Add(it.Key, it.Value);
            }
            InvisTime.Where(x => !newList.ContainsKey(x.Key)).Do(x => refreshList.Add(x.Key));
            InvisTime = newList;
            refreshList.Do(x => SendRPC(Utils.GetPlayerById(x)));
        }
    }
    public override void OnCoEnterVent(PlayerPhysics __instance, int ventId)
    {
        var pc = __instance.myPlayer;
        if (!AmongUsClient.Instance.AmHost || IsInvis(pc.PlayerId)) return;
        _ = new LateTask(() =>
        {
            if (CanGoInvis(pc.PlayerId))
            {
                ventedId.Remove(pc.PlayerId);
                ventedId.Add(pc.PlayerId, ventId);

                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, pc.GetClientId());
                writer.WritePacked(ventId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);

                InvisTime.Add(pc.PlayerId, Utils.GetTimeStamp());
                SendRPC(pc);
                NameNotifyManager.Notify(pc, GetString("WraithInvisState"), WraithDuration.GetFloat());
            }
            else
            {
                if (!WraithVentNormallyOnCooldown.GetBool())
                {
                    __instance.myPlayer.MyPhysics.RpcBootFromVent(ventId);
                    NameNotifyManager.Notify(pc, GetString("WraithInvisInCooldown"));
                }
            }
        }, 0.5f, "Wraith Vent");
    }
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public override bool CanUseKillButton(PlayerControl pc) => pc.IsAlive();
    public override bool CanUseImpostorVentButton(PlayerControl pc) => true;
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (!HasEnabled) return;
        if (!pc.Is(CustomRoles.Wraith) || !IsInvis(pc.PlayerId)) return;

        InvisTime.Remove(pc.PlayerId);
        lastTime.Add(pc.PlayerId, Utils.GetTimeStamp());
        SendRPC(pc);

        pc?.MyPhysics?.RpcBootFromVent(vent.Id);
        NameNotifyManager.Notify(pc, GetString("WraithInvisStateOut"));
    }
    public override string GetLowerText(PlayerControl pc, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (pc == null || !GameStates.IsInTask || !PlayerControl.LocalPlayer.IsAlive()) return "";
        var str = new StringBuilder();
        if (IsInvis(pc.PlayerId))
        {
            var remainTime = InvisTime[pc.PlayerId] + (long)WraithDuration.GetFloat() - Utils.GetTimeStamp();
            str.Append(string.Format(GetString("WraithInvisStateCountdown"), remainTime + 1));
        }
        else if (lastTime.TryGetValue(pc.PlayerId, out var time))
        {
            var cooldown = time + (long)WraithCooldown.GetFloat() - Utils.GetTimeStamp();
            str.Append(string.Format(GetString("WraithInvisCooldownRemain"), cooldown + 1));
        }
        else
        {
            str.Append(GetString("WraithCanVent"));
        }
        return str.ToString();
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("KillButtonText"));
        hud.ImpostorVentButton.OverrideText(GetString(Wraith.IsInvis(PlayerControl.LocalPlayer.PlayerId) ? "WraithRevertVentButtonText" : "WraithVentButtonText"));
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (Medic.ProtectList.Contains(target.PlayerId)) return true;
        if (target.Is(CustomRoles.Bait)) return true;
        if (target.Is(CustomRoles.Pestilence)) return true;

        if (!IsInvis(killer.PlayerId)) return true;
        killer.SetKillCooldown();
        target.RpcCheckAndMurder(target);
        target.SetRealKiller(killer);
        return false;
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("invisible");
}