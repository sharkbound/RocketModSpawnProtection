using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using UnityEngine;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Events;


namespace RocketModSpawnProtection
{
    public class ProtectionComponent : UnturnedPlayerComponent
    {
        public bool protectionEnabled = false;
        bool protectionEnded = false;
        bool equiptedItem = false;
        bool vanishExpired = false;
        bool inVehicleWithOthers = false;
        bool distanceCanceled = false;
        bool spawnSet = false;

        DateTime protStart;

        ushort lastVehHealth = 1337;

        int passengerCount = 0;
        int elapsedProtectionTime = 0;
        double elapsedProtectionMilliseconds = 0;

        Vector3 spawnPosition;
        Vector3 lastPosition = Vector3.zero;

        public void Update()
        {
            if (!protectionEnabled || pluginUnloaded()) return;
            
            elapsedProtectionTime = getTotalDateTimeSeconds(protStart);
            elapsedProtectionMilliseconds = getTotalDateTimeMilliseconds(protStart);

            if (elapsedProtectionMilliseconds <= 300 && SpawnProtection.CheckIfNearBed(Player))
            {
                StopProtection(sendMessage: false);
                return;
            }

            var config = getConfig();

            if (SpawnProtection.Config.EnsureGodmodeWhileProtected && !Player.Features.GodMode)
                Player.Features.GodMode = true;
            if (config.GiveVanishWhileProtected && !vanishExpired && !Player.Features.VanishMode && elapsedProtectionMilliseconds >= config.ProtectionVanishDelayMilliseconds)
            {
                //UnturnedChat.Say(Player, "vanish enabled! " + elapsedProtectionMilliseconds.ToString() + " Milliseconds!");
                Player.Features.VanishMode = true;
            }

            if (config.DisableProtectionBasedOnDist)
            {

                if (spawnSet)
                {
                    if (Vector3.Distance(spawnPosition, Player.Position) >= config.ProtDisableDist)
                    {
                        distanceCanceled = true;
                    }
                }

                if (!spawnSet && elapsedProtectionMilliseconds >= config.SpawnPositionGetDelay)
                {
                    spawnPosition = Player.Position;
                    spawnSet = true;
                }
            }

            if (Player.CurrentVehicle != null)
            {
                foreach (var passenger in Player.CurrentVehicle.passengers)
                {
                    if (passenger.player != null) passengerCount++;
                }

                if (config.AutoRepairProtectedPlayersVehicles)
                {
                    if (Player.CurrentVehicle.health < lastVehHealth && passengerCount == 1)
                    {
                        Player.CurrentVehicle.askRepair(9999);
                    }
                }

                if (config.CancelProtectionIfInVehicleWithOthers && passengerCount > 1)
                {
                    inVehicleWithOthers = true;
                }

                lastVehHealth = Player.CurrentVehicle.health;
                passengerCount = 0;
            }

            if (config.GiveVanishWhileProtected && !vanishExpired && elapsedProtectionTime >= config.MaxProtectionVanishTime)
            {
                //UnturnedChat.Say(Player, "vanish expired!");
                Player.Features.VanishMode = false;
                vanishExpired = true;
            }

            if (Player.Player.equipment.asset != null)
            {
                if (!config.ForceDequipWhileProtected && config.CancelProtectionOnEquip && !config.WhitelistedItems.Contains(Player.Player.equipment.asset.id))
                {
                    equiptedItem = true;
                }
                else if (config.ForceDequipWhileProtected)
                {
                    Player.Player.equipment.dequip();
                }
            }

            if (elapsedProtectionTime >= config.ProtectionTime)
            {
                protectionEnded = true;
            }

            if (inVehicleWithOthers)
            {
                StopProtection(false);
                sendTranslation("canceled_veh");
                return;
            }

            if (equiptedItem)
            {
                StopProtection(false);
                sendTranslation("canceled_item");
                return;
            }

            if (distanceCanceled)
            {
                StopProtection(false);
                sendTranslation("canceled_dist");
                return;
            }

            if (protectionEnded)
            {
                StopProtection();
                return;
            }
        }

        public void StartProtection(bool sendMessage = true)
        {
            ResetVariables();

            protectionEnabled = true;
            protStart = DateTime.Now;

            Player.GodMode = true;
            //spawnLocation = Player.Position;

            if (sendMessage && SpawnProtection.Config.SendProtectionMessages)
            {
                UnturnedChat.Say(Player, SpawnProtection.Instance.Translate("prot_started", SpawnProtection.Config.ProtectionTime), SpawnProtection.GetProtMsgColor());
            }
        }

        public void StopProtection(bool sendMessage = true)
        {
            ResetVariables();
            Player.Features.GodMode = false;
            var config = getConfig();

            if (config.GiveVanishWhileProtected && Player.Features.VanishMode)
            {
                Player.Features.VanishMode = false;
            }

            if (sendMessage && config.SendProtectionMessages)
            {
                UnturnedChat.Say(Player, SpawnProtection.Instance.Translate("expired"), SpawnProtection.GetProtMsgColor());
            }
        }

        public void ResetVariables()
        {
            protectionEnabled = false;
            protectionEnded = false;
            equiptedItem = false;
            lastVehHealth = 1337;
            passengerCount = 0;
            vanishExpired = false;
            elapsedProtectionTime = 0;
            elapsedProtectionMilliseconds = 0;
            inVehicleWithOthers = false;
            spawnSet = false;
            spawnPosition = Vector3.zero;
            distanceCanceled = false;
            //spawnLocation = Vector3.zero;
        }

        int getTotalDateTimeSeconds(DateTime input)
        {
            return (int)(DateTime.Now - input).TotalSeconds;
        }

        double getTotalDateTimeMilliseconds(DateTime input)
        {
            return (DateTime.Now - input).TotalMilliseconds;
        }

        void log(string msg)
        {
            Rocket.Core.Logging.Logger.Log(msg);
        }

        SpawnProtectionConfig getConfig()
        {
            return SpawnProtection.Instance.Configuration.Instance;
        }

        bool pluginUnloaded()
        {
            var state = SpawnProtection.Instance.State;
            switch (state)
            {
                case Rocket.API.PluginState.Cancelled:
                    return true;
                case Rocket.API.PluginState.Failure:
                    return true;
                case Rocket.API.PluginState.Unloaded:
                    return true;
                default:
                    return false;
            }
        }

        void UnturnedPlayerEvents_OnPlayerUpdateGesture(UnturnedPlayer player, UnturnedPlayerEvents.PlayerGesture gesture)
        {
            if (player.CSteamID == Player.CSteamID)
            {
                if (gesture == UnturnedPlayerEvents.PlayerGesture.PunchLeft
                    || gesture == UnturnedPlayerEvents.PlayerGesture.PunchRight)
                {
                    if (protectionEnabled)
                    {
                        StopProtection(false);

                        if (!getConfig().SendProtectionMessages) return;
                        UnturnedChat.Say(Player, SpawnProtection.Instance.Translate("canceled_punch"), SpawnProtection.GetProtMsgColor());
                    }
                }
            }
        }

        private void UnturnedPlayerEvents_OnPlayerUpdatePosition(UnturnedPlayer player, Vector3 position)
        {
            if (player.CSteamID != Player.CSteamID || protectionEnabled) return;

            if (lastPosition == Vector3.zero)
                lastPosition = Player.Position;

            if (Vector3.Distance(lastPosition, Player.Position) >= getConfig().ProtEnableDist)
            {
                StartProtection();
            }

            lastPosition = Player.Position;
        }

        void sendTranslation(string translation, params object[] args)
        {
            if (!getConfig().SendProtectionMessages) return;
            UnturnedChat.Say(Player, SpawnProtection.Instance.Translate(translation, args), SpawnProtection.GetProtMsgColor());
        }

        protected override void Load()
        {
            UnturnedPlayerEvents.OnPlayerUpdateGesture += UnturnedPlayerEvents_OnPlayerUpdateGesture;
            if (getConfig().EnableProtectionBasedOnDist)
            {
                UnturnedPlayerEvents.OnPlayerUpdatePosition += UnturnedPlayerEvents_OnPlayerUpdatePosition;

            }
        }

        protected override void Unload()
        {
            UnturnedPlayerEvents.OnPlayerUpdateGesture -= UnturnedPlayerEvents_OnPlayerUpdateGesture;
            if (getConfig().EnableProtectionBasedOnDist)
            {
                UnturnedPlayerEvents.OnPlayerUpdatePosition -= UnturnedPlayerEvents_OnPlayerUpdatePosition;

            }
        }
    }
}
