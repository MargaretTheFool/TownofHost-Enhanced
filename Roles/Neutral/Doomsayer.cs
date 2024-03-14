﻿using Hazel;
using System.Collections.Generic;
using System.Runtime.Intrinsics.Arm;
using UnityEngine;
using static TOHE.Utils;
using static TOHE.Translator;
using static UnityEngine.GraphicsBuffer;
using TOHE.Roles.AddOns.Common;

namespace TOHE.Roles.Neutral;

internal class Doomsayer : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 14100;
    public static HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;

    //==================================================================\\

    public static List<CustomRoles> GuessedRoles = [];
    public static Dictionary<byte, int> GuessingToWin = [];

    public static int GuessesCount = 0;
    public static int GuessesCountPerMeeting = 0;
    public static bool CantGuess = false;

    public static OptionItem DoomsayerAmountOfGuessesToWin;
    public static OptionItem DCanGuessImpostors;
    public static OptionItem DCanGuessCrewmates;
    public static OptionItem DCanGuessNeutrals;
    public static OptionItem DCanGuessAdt;
    public static OptionItem AdvancedSettings;
    public static OptionItem MaxNumberOfGuessesPerMeeting;
    public static OptionItem KillCorrectlyGuessedPlayers;
    public static OptionItem DoesNotSuicideWhenMisguessing;
    public static OptionItem MisguessRolePrevGuessRoleUntilNextMeeting;
    public static OptionItem DoomsayerTryHideMsg;
    public static OptionItem ImpostorVision;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Doomsayer);
        DoomsayerAmountOfGuessesToWin = IntegerOptionItem.Create(Id + 10, "DoomsayerAmountOfGuessesToWin", new(1, 10, 1), 3, TabGroup.NeutralRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Doomsayer])
            .SetValueFormat(OptionFormat.Times);
        DCanGuessImpostors = BooleanOptionItem.Create(Id + 12, "DCanGuessImpostors", true, TabGroup.NeutralRoles, true)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Doomsayer]);
        DCanGuessCrewmates = BooleanOptionItem.Create(Id + 13, "DCanGuessCrewmates", true, TabGroup.NeutralRoles, true)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Doomsayer]);
        DCanGuessNeutrals = BooleanOptionItem.Create(Id + 14, "DCanGuessNeutrals", true, TabGroup.NeutralRoles, true)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Doomsayer]);
        DCanGuessAdt = BooleanOptionItem.Create(Id + 15, "DCanGuessAdt", false, TabGroup.NeutralRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Doomsayer]);

        AdvancedSettings = BooleanOptionItem.Create(Id + 16, "DoomsayerAdvancedSettings", true, TabGroup.NeutralRoles, true)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Doomsayer]);
        MaxNumberOfGuessesPerMeeting = IntegerOptionItem.Create(Id + 23, "DoomsayerMaxNumberOfGuessesPerMeeting", new(1, 10, 1), 3, TabGroup.NeutralRoles, false)
            .SetParent(AdvancedSettings);
        KillCorrectlyGuessedPlayers = BooleanOptionItem.Create(Id + 18, "DoomsayerKillCorrectlyGuessedPlayers", true, TabGroup.NeutralRoles, true)
            .SetParent(AdvancedSettings);
        DoesNotSuicideWhenMisguessing = BooleanOptionItem.Create(Id + 24, "DoomsayerDoesNotSuicideWhenMisguessing", true, TabGroup.NeutralRoles, false)
            .SetParent(AdvancedSettings);
        MisguessRolePrevGuessRoleUntilNextMeeting = BooleanOptionItem.Create(Id + 20, "DoomsayerMisguessRolePrevGuessRoleUntilNextMeeting", true, TabGroup.NeutralRoles, true)
            .SetParent(DoesNotSuicideWhenMisguessing);

        ImpostorVision = BooleanOptionItem.Create(Id + 25, "ImpostorVision", true, TabGroup.NeutralRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Doomsayer]);
        DoomsayerTryHideMsg = BooleanOptionItem.Create(Id + 21, "DoomsayerTryHideMsg", true, TabGroup.NeutralRoles, true)
            .SetColor(Color.green)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Doomsayer]);
    }
    public override void Init()
    {
        playerIdList = [];
        GuessedRoles = [];
        GuessingToWin = [];
        GuessesCount = 0;
        GuessesCountPerMeeting = 0;
        CantGuess = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        GuessingToWin.TryAdd(playerId, GuessesCount);
    }
    public static void SendRPC(PlayerControl player)
    {
        MessageWriter writer;
        writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDoomsayerProgress, SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte DoomsayerId = reader.ReadByte();
        GuessingToWin[DoomsayerId]++;
    }
    private static (int, int) GuessedPlayerCount(byte doomsayerId)
    {
        int doomsayerguess = GuessingToWin[doomsayerId], GuessesToWin = DoomsayerAmountOfGuessesToWin.GetInt();

        return (doomsayerguess, GuessesToWin);
    }
    public override string GetProgressText(byte playerId, bool comms)
    {
        var doomsayerguess = Doomsayer.GuessedPlayerCount(playerId);
        return ColorString(GetRoleColor(CustomRoles.Doomsayer).ShadeColor(0.25f), $"({doomsayerguess.Item1}/{doomsayerguess.Item2})");
        
    }
    public static void CheckCountGuess(PlayerControl doomsayer)
    {
        if (!(GuessingToWin[doomsayer.PlayerId] >= DoomsayerAmountOfGuessesToWin.GetInt())) return;

        GuessingToWin[doomsayer.PlayerId] = DoomsayerAmountOfGuessesToWin.GetInt();
        GuessesCount = DoomsayerAmountOfGuessesToWin.GetInt();
        if (!CustomWinnerHolder.CheckForConvertedWinner(doomsayer.PlayerId))
        {
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Doomsayer);
            CustomWinnerHolder.WinnerIds.Add(doomsayer.PlayerId);
        }
    }
    public override void OnReportDeadBody(PlayerControl goku, PlayerControl solos)
    {
        if (!AdvancedSettings.GetBool()) return;

        CantGuess = false;
        GuessesCountPerMeeting = 0;
    }
    public override string PVANameText(PlayerVoteArea pva, PlayerControl target)
    => (!Utils.GetPlayerById(pva.TargetPlayerId).Data.IsDead && !target.Data.IsDead) ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doomsayer), target.PlayerId.ToString()) + " " + pva.NameText.text : "";
}