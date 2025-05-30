﻿using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SimpleUtilities
{
    public class Config
    {
        [Description("Whether or not the plugin is enabled.")]
        public bool IsEnabled { get; set; } = true;

        [Description("Whether or not to show debug messages.")]
        public bool Debug { get; set; } = false;

        [Description("Welcome message which is displayed for the player. (Leave it empty to disable.)")]
        public string WelcomeMessage { get; set; } = "<color=red>Welcome to the server!</color> <color=blue>Please read our rules.</color>";

        [Description("Welcome message duration.")]
        public ushort WelcomeMessageTime { get; set; } = 7;

        [Description("CASSIE announcement when Chaos Insurgency spawns. Leave it empty to disable. (Please note that you can only use CASSIE approved words.)")]
        public string CassieMessage { get; set; } = "Warning Chaos Insurgency has been spotted";

        [Description("Whether or not to play the announcement's sound effect.")]
        public bool CassieNoise { get; set; } = true;

        [Description("Whether or not CASSIE should display the text of the announcement.")]
        public bool CassieText { get; set; } = true;

        [Description("Chance (1-100) for Chaos Insurgency to spawn at round start instead of Facility Guards.")]
        public int ChaosChance { get; set; } = 25;

        [Description("Whether or not to enable friendly fire when the round ends. (You can change the friendly_fire_multiplier in your config_gameplay.txt)")]
        public bool FfOnEnd { get; set; } = true;

        [Description("Whether or not disarmed NTF / CI should change teams when escaping.")]
        public bool CuffedChangeTeams { get; set; } = true;

        [Description("Message sent for the player who looked at / shot SCP-096. (Leave it empty to disable.)")]
        public string TargetMessage { get; set; } = "you became a target for scp-096!";

        [Description("Message sent when coin lands on tails. (Leave it empty to disable.)")]
        public string CoinTails { get; set; } = "the coin landed on tails!";

        [Description("Message sent when coin lands on heads. (Leave it empty to disable.)")]
        public string CoinHeads { get; set; } = "the coin landed on heads!";

        [Description("Whether or not to show players' HP when looking at them.")]
        public bool ShowHp { get; set; } = true;

        [Description("Format of displayed HP. Keep everything between ' '.")]
        public string HpDisplayFormat { get; set; } = "HP: %current%/%max%";

        [Description("Grant facility guards honorary promotion into NTF on leaving the facility (Until order is restored)")]
        public bool GuardsCanEscape { get; set; } = false;

        [Description("Role to be granted to escaping guards (NtfSergeant, NtfCaptain, NtfPrivate, NtfSpecialist, random)")]
        public string EscapedGuardRole { get; set; } = "";

        [Description("Escaped guard random roles")]
        public List<String> RandomGuardRoles { get; set; } = new List<String>()
        {
            "NtfSergeant",
            "NtfSpecialist",
            "NtfPrivate"
        };
    }
}