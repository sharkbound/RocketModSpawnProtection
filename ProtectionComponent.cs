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
        bool sentProtStartedMsg = false;
        bool vanishExpired = false;
        //bool giveVanish;
        //bool cancelOnEquip;

        DateTime protStart;

        ushort lastVehHealth = 1337;

        //float disableVanishDist;

        int passengerCount = 0;
        //int maxProtTime;
        //int maxVanishTime;
        int elapsedProtectionTime = 0;

        //Vector3 spawnLocation;

        public void Update()
        {
            if (!protectionEnabled && !pluginUnloaded()) return;

            var config = getConfig();
            elapsedProtectionTime = getTotalDateTimeSeconds(protStart);

            if (!Player.Features.GodMode) Player.Features.GodMode = true;
            if (config.GiveVanishWhileProtected && !vanishExpired && !Player.Features.VanishMode && elapsedProtectionTime >= 1) 
                Player.Features.VanishMode = true;

            if (!sentProtStartedMsg)
            {
                UnturnedChat.Say(Player, spawnProtection.Instance.Translate("prot_started", config.ProtectionTime));
                sentProtStartedMsg = true;
            }

            if (Player.CurrentVehicle != null)
            {
                foreach (var passenger in Player.CurrentVehicle.passengers)
                {
                    if (passenger.player != null) passengerCount++;
                }

                if (Player.CurrentVehicle.health < lastVehHealth && passengerCount == 1)
                {
                    Player.CurrentVehicle.askRepair(9999);
                }

                lastVehHealth = Player.CurrentVehicle.health;
                passengerCount = 0;
            }

            if (config.GiveVanishWhileProtected && !vanishExpired && elapsedProtectionTime >= config.MaxProtectionVanishTime)
            {
                Player.Features.VanishMode = false;
                vanishExpired = true;
            }

            if (config.CancelProtectionOnEquip && Player.Player.equipment.asset != null)
            {
                equiptedItem = true;
            }

            if (elapsedProtectionTime >= config.ProtectionTime)
            {
                protectionEnded = true;
            }

            if (equiptedItem)
            {
                UnturnedChat.Say(Player, spawnProtection.Instance.Translate("canceled_item"));
                StopProtection();
            }

            if (protectionEnded && !equiptedItem)
            {
                UnturnedChat.Say(Player, spawnProtection.Instance.Translate("expired"));
                StopProtection();
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

        public void StartProtection()
        {
            ResetVariables();

            protectionEnabled = true;
            protStart = DateTime.Now;
            //spawnLocation = Player.Position;
        }

        public void StopProtection()
        {
            ResetVariables();
            Player.Features.GodMode = false;

            if (getConfig().GiveVanishWhileProtected && Player.Features.VanishMode)
            {
                Player.Features.VanishMode = false;
            }
        }

        public void ResetVariables()
        {
            protectionEnabled = false;
            protectionEnded = false;
            equiptedItem = false;
            lastVehHealth = 1337;
            passengerCount = 0;
            sentProtStartedMsg = false;
            vanishExpired = false;
            elapsedProtectionTime = 0;
            //spawnLocation = Vector3.zero;
        }

        int getTotalDateTimeSeconds(DateTime input)
        {
            return (int)(DateTime.Now - input).TotalSeconds;
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

    }
}
