﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static TOHE.Translator;
using static TOHE.Options;
using TOHE.Roles.Core;
using AmongUs.GameOptions;

namespace TOHE.Roles.Crewmate
{
    internal class Doctor : RoleBase
    {
        //===========================SETUP================================\\
        private const int Id = 6700;
        private static bool On = false;
        public override bool IsEnable => On;
        public static bool HasEnabled => CustomRoles.Doctor.IsClassEnable();
        public override CustomRoles ThisRoleBase => CustomRoles.Scientist;

        //==================================================================\\
        public static OptionItem DoctorTaskCompletedBatteryCharge;
        public static OptionItem DoctorVisibleToEveryone;

        public static void SetupCustomOptions()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Doctor);
            DoctorTaskCompletedBatteryCharge = FloatOptionItem.Create(Id + 10, "DoctorTaskCompletedBatteryCharge", new(0f, 250f, 1f), 50f, TabGroup.CrewmateRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Doctor])
                .SetValueFormat(OptionFormat.Seconds);
            DoctorVisibleToEveryone = BooleanOptionItem.Create(Id + 11, "DoctorVisibleToEveryone", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Doctor]);
        }
        public override void Init()
        {
            On = false;
        }
        public override void Add(byte playerId)
        {
            On = true;
        }
        public override void ApplyGameOptions(IGameOptions opt, byte playerId)
        {
            AURoleOptions.ScientistCooldown = 0f;
            AURoleOptions.ScientistBatteryCharge = DoctorTaskCompletedBatteryCharge.GetFloat();
        }
        public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl pc, CustomRoles role)
        {
            if (target.Is(CustomRoles.Doctor) && Doctor.DoctorVisibleToEveryone.GetBool() && !target.IsEvilAddons())
            {
                if (!isUI) Utils.SendMessage(GetString("GuessDoctor"), pc.PlayerId);
                else pc.ShowPopUp(GetString("GuessDoctor"));
                return true;
            }
            return false;
        }
        public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) => target.Is(CustomRoles.Doctor) && Doctor.DoctorVisibleToEveryone.GetBool();
        public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target) => OthersKnowTargetRoleColor(seer, target);
    }
}
