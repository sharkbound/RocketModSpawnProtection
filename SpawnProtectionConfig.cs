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
        public bool GiveProtectionOnJoin;
        public int ProtectionTime;
        public int MaxProtectionVanishTime;
       //public int MaxVanishDistFromSpawn;

        public void LoadDefaults()
        {
            GiveVanishWhileProtected = true;
            CancelProtectionOnEquip = true;
            GiveProtectionOnJoin = false;
            ProtectionTime = 30;
            MaxProtectionVanishTime = 3;
            //MaxVanishDistFromSpawn = 30;
        }
    }
}
