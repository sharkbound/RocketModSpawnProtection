using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using UnityEngine;
using Rocket.Unturned.Chat;


namespace RocketModSpawnProtection
{
    public class ProtectionComponent : UnturnedPlayerComponent
    {
        public bool protectionEnabled = false;
        bool protectionEnded = false;
        bool equiptedItem = false;
        bool vanishExpired = false;
        bool inVehicleWithOthers = false;
        //bool giveVanish;
        //bool cancelOnEquip;

        DateTime protStart;

        ushort lastVehHealth = 1337;

        //float disableVanishDist;

        int passengerCount = 0;
        //int maxProtTime;
        //int maxVanishTime;
        int elapsedProtectionTime = 0;
        double elapsedProtectionMilliseconds = 0;

        //Vector3 spawnLocation;

        public void Update()
        {
            if (!protectionEnabled && !pluginUnloaded()) return;

            var config = getConfig();

            elapsedProtectionTime = getTotalDateTimeSeconds(protStart);
            elapsedProtectionMilliseconds = getTotalDateTimeMilliseconds(protStart);

            if (!Player.Features.GodMode) Player.Features.GodMode = true;
            if (config.GiveVanishWhileProtected && !vanishExpired && !Player.Features.VanishMode && elapsedProtectionMilliseconds >= config.ProtectionVanishDelayMilliseconds)
            {
                //UnturnedChat.Say(Player, "vanish enabled! " + elapsedProtectionMilliseconds.ToString() + " Milliseconds!");
                Player.Features.VanishMode = true;
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

            if (config.CancelProtectionOnEquip && Player.Player.equipment.asset != null)
            {
                if (!config.WhitelistedItems.Contains(Player.Player.equipment.asset.id))
                {
                    equiptedItem = true;
                }
            }

            if (elapsedProtectionTime >= config.ProtectionTime)
            {
                protectionEnded = true;
            }

            if (inVehicleWithOthers)
            {
                UnturnedChat.Say(Player, spawnProtection.Instance.Translate("canceled_veh"), spawnProtection.GetProtMsgColor());
                StopProtection(false);
                return;
            }

            if (equiptedItem)
            {
                UnturnedChat.Say(Player, spawnProtection.Instance.Translate("canceled_item"), spawnProtection.GetProtMsgColor());
                StopProtection(false);
                return;
            }

            if (protectionEnded)
            {
                StopProtection();
                return;
            }
        }

        /*
        protected override void Load()
        {
            ResetVariables();

            giveVanish = spawnProtection.Instance.Configuration.Instance.GiveVanishWhileProtected;
            maxProtTime = spawnProtection.Instance.Configuration.Instance.ProtectionTime;
            maxVanishTime = spawnProtection.Instance.Configuration.Instance.MaxProtectionVanishTime;
            //disableVanishDist = spawnProtection.Instance.Configuration.Instance.MaxVanishDistFromSpawn;
            cancelOnEquip = spawnProtection.Instance.Configuration.Instance.CancelProtectionOnEquip;
            log(string.Format("{0} {1} {2} {3}", giveVanish, cancelOnEquip, maxProtTime, maxVanishTime));
        }
        */

        public void StartProtection(bool sendMessage = true)
        {
            ResetVariables();

            protectionEnabled = true;
            protStart = DateTime.Now;
            //spawnLocation = Player.Position;

            if (sendMessage)
            {
                var protTime = getConfig().ProtectionTime;

                UnturnedChat.Say(Player, spawnProtection.Instance.Translate("prot_started", protTime), spawnProtection.GetProtMsgColor());
            }
        }

        public void StopProtection(bool sendMessage = true)
        {
            ResetVariables();
            Player.Features.GodMode = false;

            if (getConfig().GiveVanishWhileProtected && Player.Features.VanishMode)
            {
                Player.Features.VanishMode = false;
            }

            if (sendMessage)
            {
                UnturnedChat.Say(Player, spawnProtection.Instance.Translate("expired"), spawnProtection.GetProtMsgColor());
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
            return spawnProtection.Instance.Configuration.Instance;
        }

        bool pluginUnloaded()
        {
            var state = spawnProtection.Instance.State;
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

        void UnturnedPlayerEvents_OnPlayerUpdateGesture(UnturnedPlayer player, Rocket.Unturned.Events.UnturnedPlayerEvents.PlayerGesture gesture)
        {
            if (player.CSteamID == Player.CSteamID)
            {
                if (gesture == Rocket.Unturned.Events.UnturnedPlayerEvents.PlayerGesture.PunchLeft
                    || gesture == Rocket.Unturned.Events.UnturnedPlayerEvents.PlayerGesture.PunchRight)
                {
                    if (protectionEnabled)
                    {
                        StopProtection(false);
                        UnturnedChat.Say(Player, spawnProtection.Instance.Translate("canceled_punch"), spawnProtection.GetProtMsgColor());
                    }
                }
            }
        }

        protected override void Load()
        {
            Rocket.Unturned.Events.UnturnedPlayerEvents.OnPlayerUpdateGesture += UnturnedPlayerEvents_OnPlayerUpdateGesture;
        }

        protected override void Unload()
        {
            Rocket.Unturned.Events.UnturnedPlayerEvents.OnPlayerUpdateGesture -= UnturnedPlayerEvents_OnPlayerUpdateGesture;
        }

    }
}
