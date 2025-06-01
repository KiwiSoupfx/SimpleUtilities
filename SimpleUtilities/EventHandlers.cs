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
using LabApi.Features.Enums;
using System.Linq;
using MapGeneration;
using System.Threading;

namespace SimpleUtilities
{
    public class EventHandlers : CustomEventsHandler
    {
        int randomNumber;
        string hpFormat;
        
        //Welcome message.
        public override void OnPlayerJoined(PlayerJoinedEventArgs args)
        {
            Config config = SimpleUtilities.Singleton.Config;

            if (config is null)
                return;

            args.Player.SendBroadcast(config.WelcomeMessage, config.WelcomeMessageTime, Broadcast.BroadcastFlags.Normal, false);
        }
        //Cassie announcement on Chaos Insurgency Spawn.
        public override void OnServerWaveRespawned(WaveRespawnedEventArgs args)
        {
            Config config = SimpleUtilities.Singleton.Config;
            if (config.CassieMessage == "")
                return;

            if (args.Wave.Faction == Faction.FoundationEnemy)
            {
                Cassie.Message(config.CassieMessage, true, config.CassieNoise, config.CassieText);
            }
        }

        //Chaos Insurgency spawn on round start.
        public override void OnServerWaitingForPlayers()
        {
            SimpleUtilities.Singleton.LoadConfigs(); //Load at every roundPrep so no restart to reload config/enable plugin

            if (!SimpleUtilities.Singleton.Config.IsEnabled)
            {
                SimpleUtilities.Singleton.Disable();
                return;
            }

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

        public override void OnPlayerChangedRole(PlayerChangedRoleEventArgs args)
        {
            //Apply health amounts to display
            InitialHealth(args);

            //Change guards to chaos at %
            OnChangeRole(args);
        }

        //Auto Friendly Fire on Round end. FF detector is disabled by default.
        public override void OnServerRoundEnded(RoundEndedEventArgs args)
        {
            if (!SimpleUtilities.Singleton.Config.FfOnEnd)
                return;

            bool oldFFSetting = Server.FriendlyFire; //If you changed it temporarily for another reason, set it back when we're done

            Server.FriendlyFire = true;
            float restartTime = ConfigFile.ServerConfig.GetFloat("auto_round_restart_time");

            Timing.CallDelayed(restartTime - 0.5f, () =>
            {
                Server.FriendlyFire = oldFFSetting;
            });
        }

        //Cuffed change teams.
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
        public override void OnScp096AddedTarget(Scp096AddedTargetEventArgs args)
        {
            args.Target.SendHint(SimpleUtilities.Singleton.Config.TargetMessage, 5f);
        }

        //Coin flip hints.
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

        //Update hp in hud when looked at by another player
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
            //DebugLog(args.Door.DoorName.ToString() +" "+ SimpleUtilities.Singleton.Config.GuardsCanEscape.ToString() +" "+ args.Player.RoleBase.RoleTypeId.ToString() +" "+ args.Player.IsDisarmed.ToString());
            if (args.Door.DoorName != DoorName.SurfaceEscapeFinal || !SimpleUtilities.Singleton.Config.GuardsCanEscape || args.Player.RoleBase.RoleTypeId != RoleTypeId.FacilityGuard || args.Player.IsDisarmed)
                return;

            string roleToBe = SimpleUtilities.Singleton.Config.EscapedGuardRole;
            List<string> randomRoles = SimpleUtilities.Singleton.Config.RandomGuardRoles;
            RoleTypeId roleToBeId = args.Player.Role.GetRoleBase().RoleTypeId; //Set to player's current role to prevent issues later

            if (roleToBe.ToLower() == "random")
            {
                //int randomRoles.Count;
                int myRandom = Random.Range(0, randomRoles.Count);
                roleToBe = randomRoles[myRandom];
            }

            //The idea of doing this was revealed to me in a dream
            Array rolesList = typeof(RoleTypeId).GetEnumValues();
            bool matchFound = false;
            foreach (RoleTypeId roleId in rolesList)
            {
                DebugLog(nameof(roleId).ToLower());
                if (roleId.ToString().ToLower() == roleToBe.ToLower())
                {
                    roleToBeId = roleId;
                    matchFound = true;
                    DebugLog(roleToBeId.ToString());
                }
            }

            if (!matchFound)
            {
                Logger.Error("Could not find role " + roleToBe + ". Please consult RoleTypeId.cs");
            }

            args.Player.SetRole(roleToBeId, RoleChangeReason.Escaped);  
        }

        public override void OnPlayerPickedUpItem(PlayerPickedUpItemEventArgs args)
        {
            if (!SimpleUtilities.Singleton.Config.ShouldBlacklist3114 || SimpleUtilities.Singleton.Config.Blacklist3114.Count == 0 || args.Player.Role != RoleTypeId.Scp3114)
                return;

            DebugLog(SimpleUtilities.Singleton.Config.ShouldBlacklist3114.ToString() + ", " + SimpleUtilities.Singleton.Config.Blacklist3114.Count.ToString());

            foreach (string blockedItem in SimpleUtilities.Singleton.Config.Blacklist3114)
            {
                if (blockedItem.ToLower() == args.Item.Type.ToString().ToLower())
                {
                    args.Item.DropItem();
                    args.Player.SendHint("SCP-3114 cannot pick up this item.", 2f);
                    break;
                }
            }
        }

        public override void OnServerLczDecontaminationAnnounced(LczDecontaminationAnnouncedEventArgs args)
        {
            uint configPhase = SimpleUtilities.Singleton.Config.LastChanceDeconPhase;

            if (configPhase == 6)
                return;

            Room Room914 = Room.Get(RoomName.Lcz914).First();
            Room RoomArmory = Room.Get(RoomName.LczArmory).First();

            DebugLog(args.Phase);

            foreach (var plr in Player.GetAll())
            {
                if (Room914.Players.Contains(plr))
                    DebugLog(plr.DisplayName + " found in " + Room914.Name);
            }

            if (args.Phase == configPhase)
            {
                string lcdMode = SimpleUtilities.Singleton.Config.LcdMode.ToLower();
                //TODO: cleanup all this checking / handling:
                //TODO: implement proximity
                if (lcdMode == "room")
                {
                    if (RoomCheck(RoomArmory))
                        HandleLCDDoors(RoomArmory, true);

                    if (RoomCheck(Room914))
                        HandleLCDDoors(Room914, true);
                    return;
                }

                if (lcdMode == "zone")
                {
                    if (ZoneCheck(Room914, RoomArmory))
                        HandleLCDDoors(RoomArmory, true);

                    if (ZoneCheck(Room914, RoomArmory))
                        HandleLCDDoors(Room914, true);
                    return;
                }

                if (lcdMode == "always")
                {
                    HandleLCDDoors(RoomArmory, true);
                    HandleLCDDoors(Room914, true);
                    return;
                }

                if (configPhase == 5)
                Timing.CallDelayed(0.01f, () =>
                {
                    HandleLCDDoors(RoomArmory, false);
                    HandleLCDDoors(Room914, false);
                });
            }
        }

        private bool RoomCheck(Room room)
        {

            uint roomCountLimit;
            //zone - if zone is empty, open doors
            //prox + proxnum + class to check for how many rooms over to check for players before opening doors
            if (SimpleUtilities.Singleton.Config.LcdRoomCountNum < 1)
                roomCountLimit = 0;
            else
                roomCountLimit = SimpleUtilities.Singleton.Config.LcdRoomCountNum;


            foreach (RoomIdentifier roomId in room.ConnectedRooms.Take((int)roomCountLimit)) //This always stresses me out
            {
                roomCountLimit++;
                Room connectedRoom = Room.Get(roomId);
                foreach (Player player in room.Players)
                {
                    if (SimpleUtilities.Singleton.Config.LcdRole.Contains(player.Team.ToString())) //Please no more loops
                    {
                        DebugLog(player.Team.ToString() + " found nearby in " + connectedRoom.Name + ", aborting");
                        return false;
                    }
                }
            }
            
            return true;
        }

        private void HandleLCDDoors(Room room, bool isOpened)
        {
            //Lock all doors
            DebugLog("LCD Door Interaction");

            IEnumerable<Door> allDoors = room.Doors; //Get the next (First()) room and then get the closest (First()) unnamed door 

            foreach (Door door in allDoors)
            {
                door.IsOpened = isOpened;
                if (SimpleUtilities.Singleton.Config.LcdLockDoor)
                {
                    door.IsLocked = true;
                }  
            }
        }

        private bool ZoneCheck(Room room914, Room roomArmory)
        {
            IEnumerable<Room> LczRooms = Room.Get(FacilityZone.LightContainment);
            foreach (Room room in LczRooms)
            {
                if (room == roomArmory || room == room914)
                    continue;
                    
                foreach (Player player in room.Players)
                {
                    if (!SimpleUtilities.Singleton.Config.LcdRole.Contains(player.Team.ToString())) //Please no more loops
                    {
                        DebugLog(player.Team.ToString() + " found nearby in " + room.Name + ", aborting");
                        return false;
                    }
                }
            }

            return true;
        }

        public void DebugLog(object obj)
        {
            if (!SimpleUtilities.Singleton.Config.Debug)
                return;

            Logger.Debug(obj);
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