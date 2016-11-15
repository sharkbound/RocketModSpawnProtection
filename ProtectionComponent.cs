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
        bool giveVanish;
        bool cancelOnEquip;

        DateTime protStart;

        ushort lastVehHealth = 1337;

        //float disableVanishDist;

        int passengerCount = 0;
        int maxProtTime;
        int maxVanishTime;
        int elapsedProtectionTime = 0;

        //Vector3 spawnLocation;

        public void Update()
        {
            if (!protectionEnabled) return;

            if (!Player.Features.GodMode) Player.Features.GodMode = true;
            if (giveVanish && !vanishExpired && !Player.Features.VanishMode) Player.Features.VanishMode = true;

            if (!sentProtStartedMsg)
            {
                UnturnedChat.Say(Player, spawnProtection.Instance.Translate("prot_started", maxProtTime));
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

            elapsedProtectionTime = getTotalDateTimeSeconds(protStart);

            if (!vanishExpired && elapsedProtectionTime >= maxVanishTime)
            {
                Player.Features.VanishMode = false;
                vanishExpired = true;
            }

            if (cancelOnEquip && Player.Player.equipment.asset != null)
            {
                equiptedItem = true;
            }

            if (elapsedProtectionTime >= maxProtTime)
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

        protected override void Load()
        {
            ResetVariables();

            giveVanish = spawnProtection.Instance.Configuration.Instance.GiveVanishWhileProtected;
            maxProtTime = spawnProtection.Instance.Configuration.Instance.ProtectionTime;
            maxVanishTime = spawnProtection.Instance.Configuration.Instance.MaxProtectionVanishTime;
            //disableVanishDist = spawnProtection.Instance.Configuration.Instance.MaxVanishDistFromSpawn;
            cancelOnEquip = spawnProtection.Instance.Configuration.Instance.CancelProtectionOnEquip;
        }

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

            if (giveVanish && Player.Features.VanishMode)
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

    }
}
