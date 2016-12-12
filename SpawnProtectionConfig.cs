using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rocket.API;

namespace RocketModSpawnProtection
{
    public class SpawnProtectionConfig : IRocketPluginConfiguration
    {
        public bool GiveVanishWhileProtected;
        public bool CancelProtectionOnEquip;
        public bool CancelProtectionIfInVehicleWithOthers;
        public bool GiveProtectionOnJoin;
        public bool GiveProtectionOnRespawn;
        public bool AutoRepairProtectedPlayersVehicles;
        public int ProtectionTime;
        public int MaxProtectionVanishTime;
        public double ProtectionVanishDelayMilliseconds;
        public string ProtectionMessageColor;
        public string CommandMessageColor;
       //public int MaxVanishDistFromSpawn;

        public void LoadDefaults()
        {
            GiveVanishWhileProtected = true;
            CancelProtectionOnEquip = true;
            CancelProtectionIfInVehicleWithOthers = true;
            GiveProtectionOnJoin = false;
            GiveProtectionOnRespawn = true;
            AutoRepairProtectedPlayersVehicles = true;
            ProtectionTime = 30;
            MaxProtectionVanishTime = 6;
            ProtectionVanishDelayMilliseconds = 1000;
            ProtectionMessageColor = "Yellow";
            CommandMessageColor = "Green";
            //MaxVanishDistFromSpawn = 30;
        }
    }
}
