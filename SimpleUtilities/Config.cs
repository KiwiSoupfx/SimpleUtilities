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
        public List<string> RandomGuardRoles { get; set; } = new()
        {
            "NtfSergeant",
            "NtfSpecialist",
            "NtfPrivate"
        };

        [Description("Prevent Scp3114 from picking up blacklisted items.")]
        public bool ShouldBlacklist3114 { get; set; } = false;

        [Description("Blacklisted Scp3114 items")]
        public List<string> Blacklist3114 { get; set; } = new()
        {
            "SCP1507Tape",
            "GunSCP127",
            "AntiSCP207",
            "SCP1576",
            "SCP1853",
            "SCP244b",
            "SCP244a",
            "SCP2176",
            "SCP330",
            "SCP268",
            "SCP018",
            "SCP207",
            "SCP500",
            "MicroHID",
            "ParticleDisruptor"
        };

        [Description("Phase of automatic decontamination to open locked doors in light containment [0 = 15m, 1 = 10m, 2 = 5m, 3 = 1m, 4 = 30s, 5 = ~10s ](5 to relock at 30seconds, 6 to disable)")]
        public uint LastChanceDeconPhase { get; set; } = 5;

        [Description("Should the door be locked open/close after operation?")]
        public bool LcdLockDoor { get; set; } = true;

        [Description("Set mode for last chance decon (prox, always, zone, room)")]
        public string LcdMode { get; set; } = "prox";

        [Description("Number of rooms to consider for nearby players in room mode. This is not distance, this counts every room (minimum 1, maximum 9, use a different mode to disable)")]
        public uint LcdRoomCountNum { get; set; } = 1;

        [Description("Which teams should block doors from opening during LCD for prox, room and zone (SCPs,FoundationForces,ChaosInsurgency,Scientists,ClassD,Dead,OtherAlive,Flamingos)")]
        public List<string> LcdRole { get; set; } = new()
        {
            "SCP",
            "ClassD",
            "Scientists"
        };
    }
}