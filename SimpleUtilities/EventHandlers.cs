using System.Collections.Generic;
using PlayerRoles;
using MEC;
using GameCore;
using UnityEngine;
using PlayerStatsSystem;
using System;
using HarmonyLib;
using Random = UnityEngine.Random;
using LabApi.Events.CustomHandlers;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Features.Wrappers;
using LabApi.Features.Extensions;
using LabApi.Events.Arguments.Scp096Events;
using Logger = LabApi.Features.Console.Logger;
using CommandSystem.Commands.RemoteAdmin.Doors;
using LabApi.Features.Enums;

namespace SimpleUtilities
{
    public class EventHandlers : CustomEventsHandler
    {
        
        int randomNumber;
        string hpFormat;
        
        //Welcome message.
        public override void OnPlayerJoined(PlayerJoinedEventArgs args)
        {
            Logger.Debug("Player Joined");
            Config config = SimpleUtilities.Singleton.Config;

            if (config is null)
                return;

            args.Player.SendBroadcast(config.WelcomeMessage, config.WelcomeMessageTime, Broadcast.BroadcastFlags.Normal, false);
        }
        //Cassie announcement on Chaos Insurgency Spawn.
        public override void OnServerWaveRespawned(WaveRespawnedEventArgs args)
        {
            Config config = SimpleUtilities.Singleton.Config;

            if (args.Wave.Faction == Faction.FoundationEnemy)
            {
                Cassie.Message(config.CassieMessage, true, config.CassieNoise, config.CassieText);
            }
        }

        //Chaos Insurgency spawn on round start.
        public override void OnServerWaitingForPlayers()
        {
            randomNumber = Random.Range(1, 100);

            try
            {
                hpFormat = SimpleUtilities.Singleton.Config.HpDisplayFormat;
                SimpleUtilities.Singleton.Harmony.PatchAll();
            }
            catch (Exception e)
            {
                Logger.Error("[Event: OnServerWaitingForPlayers] " + e.ToString());
            }
        }

        //[PluginEvent(ServerEventType.PlayerChangeRole)]
        //public void OnChangeRole(Player plr, PlayerRoleBase oldRole, RoleTypeId newRole, RoleChangeReason reason)
        public override void OnPlayerChangedRole(PlayerChangedRoleEventArgs args)
        {
            //Apply health amounts to display
            InitialHealth(args);

            //Change guards to chaos at %
            OnChangeRole(args);
        }

        //Auto Friendly Fire on Round end. FF detector is disabled by default.
        //void OnRoundEnded(RoundSummary.LeadingTeam leadingTeam)
        public override void OnServerRoundEnded(RoundEndedEventArgs args)
        {
            if (!SimpleUtilities.Singleton.Config.FfOnEnd)
                return;

            Server.FriendlyFire = true;
            float restartTime = ConfigFile.ServerConfig.GetFloat("auto_round_restart_time");

            Timing.CallDelayed(restartTime - 0.5f, () =>
            {
                Server.FriendlyFire = false;
            });
        }

        //Cuffed change teams.
        //[PluginEvent(ServerEventType.PlayerHandcuff)]
        //public void OnPlayerHandcuffed(Player player, Player target)
        public override void OnPlayerCuffed(PlayerCuffedEventArgs args)
        {
            if (!SimpleUtilities.Singleton.Config.CuffedChangeTeams)
                return;

            //  Player player = args.Player;
            Player target = args.Target;

            Timing.RunCoroutine(CuffedChangeTeams());

            IEnumerator<float> CuffedChangeTeams()
            {
                for (; ; )
                {
                    yield return Timing.WaitForSeconds(1.0f);

                    if (!target.IsDisarmed)
                    {
                        yield break;
                    }

                    foreach (var plr in Player.GetAll())
                    {
                        if (!target.IsDisarmed || !(target.Team == Team.FoundationForces || target.Team == Team.ChaosInsurgency) || Vector3.Distance(target.Position, new Vector3(124, 988, 23)) > 5)
                        {
                            continue;
                        }

                        switch (target.Team)
                        {
                            case Team.ChaosInsurgency:
                                target.SetRole(RoleTypeId.NtfPrivate, RoleChangeReason.Escaped);
                                yield break;
                            case Team.FoundationForces:
                                target.SetRole(RoleTypeId.ChaosConscript, RoleChangeReason.Escaped);
                                yield break;
                        }
                    }
                }
            }
        }

        //Hint when player becomes SCP-096's target.
        //[PluginEvent(ServerEventType.Scp096AddingTarget)]
        //public void OnScp096Target(Player player, Player target, bool IsForLooking)
        public override void OnScp096AddedTarget(Scp096AddedTargetEventArgs args)
        {
            args.Target.SendHint(SimpleUtilities.Singleton.Config.TargetMessage, 5f);
        }

        //Coin flip hints.
        //[PluginEvent(ServerEventType.PlayerCoinFlip)]
        //void OnCoinFlip(Player player, bool isTails)
        public override void OnPlayerFlippedCoin(PlayerFlippedCoinEventArgs args)
        {
            Timing.CallDelayed(1.4f, () =>
            {
                if (args.IsTails)
                {
                    args.Player.SendHint(SimpleUtilities.Singleton.Config.CoinTails, 1.5f);
                }
                else
                {
                    args.Player.SendHint(SimpleUtilities.Singleton.Config.CoinHeads, 1.5f);
                }
            });
        }

        //Show HP when looking at Player.
        //[PluginEvent(ServerEventType.PlayerChangeRole)]
        //public void InitialHealth(Player plr, PlayerRoleBase oldRole, RoleTypeId newRole, RoleChangeReason reason)
        public void InitialHealth(PlayerChangedRoleEventArgs args)
        {
            Player plr = args.Player;
            RoleTypeId newRole = args.NewRole.RoleTypeId;
            if (!SimpleUtilities.Singleton.Config.ShowHp)
                return;

            if (newRole == RoleTypeId.Spectator || newRole == RoleTypeId.None)
                return;

            Timing.CallDelayed(0.1f, () =>
            {
                string customInfo = hpFormat
                .Replace("%current%", ((int)Math.Ceiling(plr.Health)).ToString())
                .Replace("%max%", ((int)Math.Ceiling(plr.MaxHealth)).ToString());
                plr.CustomInfo = customInfo;
            });
        }

        public void OnChangeRole(PlayerChangedRoleEventArgs args)
        {
            int chance = SimpleUtilities.Singleton.Config.ChaosChance;

            if (randomNumber > chance)
                return;

            if (args.ChangeReason != RoleChangeReason.RoundStart && args.ChangeReason != RoleChangeReason.LateJoin)
                return;

            Timing.CallDelayed(0.1f, () =>
            {
                if (args.NewRole.RoleName == RoleTypeId.FacilityGuard.GetRoleBase().RoleName) //Yikes! Check on this later
                {
                    args.Player.SetRole(RoleTypeId.ChaosRifleman);
                }
            });
        }

        //[PluginEvent(ServerEventType.PlayerDamage)]
        //public void DamagedHealth(Player plr, Player target, DamageHandlerBase damageHandler)
        public override void OnPlayerHurt(PlayerHurtEventArgs args) //A little janky, need to test
        {
            Player target = args.Player;
            if (!SimpleUtilities.Singleton.Config.ShowHp)
                return;

            if (target.Role == RoleTypeId.Spectator || target.Role == RoleTypeId.None)
                return;

            Timing.CallDelayed(0.5f, () =>
            {
                string customInfo = hpFormat
                .Replace("%current%", ((int)Math.Ceiling(target.Health)).ToString())
                .Replace("%max%", ((int)Math.Ceiling(target.MaxHealth)).ToString());
                target.CustomInfo = customInfo;
            });
        }

        //Change facility guard to NTF role
        public override void OnPlayerInteractingDoor(PlayerInteractingDoorEventArgs args)
        {
            Logger.Debug(args.Door.DoorName.ToString() +" "+ SimpleUtilities.Singleton.Config.GuardsCanEscape.ToString() +" "+ args.Player.RoleBase.RoleTypeId.ToString() +" "+ args.Player.IsDisarmed.ToString());
            if (args.Door.DoorName !=  DoorName.SurfaceEscapeFinal || !SimpleUtilities.Singleton.Config.GuardsCanEscape || args.Player.RoleBase.RoleTypeId != RoleTypeId.FacilityGuard || args.Player.IsDisarmed)
                return;

            string roleToBe = SimpleUtilities.Singleton.Config.EscapedGuardRole;
            List<string> randomRoles = SimpleUtilities.Singleton.Config.RandomGuardRoles;
            RoleTypeId roleToBeId;

            if (roleToBe.ToLower() == "random")
            {
                //int randomRoles.Count;
                int myRandom = Random.Range(0, randomRoles.Count);
                roleToBe = randomRoles[myRandom];
            }

            switch (roleToBe.ToLower())
            {
                case "ntfsergeant":
                    roleToBeId = RoleTypeId.NtfSergeant;
                    break;
                case "ntfcaptain":
                    roleToBeId = RoleTypeId.NtfCaptain;
                    break;
                case "ntfprivate":
                    roleToBeId = RoleTypeId.NtfPrivate;
                    break;
                case "ntfspecialist":
                    roleToBeId = RoleTypeId.NtfSpecialist;
                    break;
                default:
                    roleToBeId = RoleTypeId.FacilityGuard;
                    Logger.Warn("EscapedGuardRole is improperly set. Change ASAP!");
                    break;
            }

            args.Player.SetRole(roleToBeId, RoleChangeReason.Escaped);
        }

        //There is no event when the player is healed.
        [HarmonyPatch(typeof(HealthStat), "ServerHeal")] //Thanks davidsebesta for the patch!
        public class HealedHealthPatch
        {
            private static string hpFormat = SimpleUtilities.Singleton.Config.HpDisplayFormat;
            private static float lastUpdate;
            private const float UpdateDelay = 0.75f;
            //When using items that grant regeneration
            //The patch updates every single frame, hence the delay.
            //MEC doesn't seem to work.

            public static void Postfix(HealthStat __instance, ref float healAmount)
            {

                if (!SimpleUtilities.Singleton.Config.ShowHp)
                    return;

                ReferenceHub refHub = GetHub(__instance);
                if (refHub == null)
                    return;

                Player plr = Player.Get(refHub.gameObject);

                if (Time.time - lastUpdate > UpdateDelay)
                {
                    lastUpdate = Time.time;
                    string customInfo = hpFormat
                    .Replace("%current%", ((int)Math.Ceiling(plr.Health)).ToString())
                    .Replace("%max%", ((int)Math.Ceiling(plr.MaxHealth)).ToString());
                    plr.CustomInfo = customInfo;
                }
            }

            private static ReferenceHub GetHub(HealthStat healthStat)
            {
                StatBase statBase = healthStat;
                if (statBase != null)
                {
                    return statBase.Hub;
                }
                return null;
            }
        }
    }
}