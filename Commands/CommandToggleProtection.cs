using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;

namespace RocketModSpawnProtection
{
    class CommandToggleProtection : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "toggleprotection";

        public string Help => "toggles if you receive spawnprotection or not";

        public string Syntax => "";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string>();

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var plr = (UnturnedPlayer)caller;
            if (SpawnProtection.IsExcluded(plr.CSteamID.m_SteamID))
            {
                SpawnProtection.Instance.Configuration.Instance.NoSpawnProtection.Remove(plr.CSteamID.m_SteamID);
                UnturnedChat.Say(caller, SpawnProtection.Instance.Translate("toggled_protection_on"));
            }
            else
            {
                SpawnProtection.Instance.Configuration.Instance.NoSpawnProtection.Add(plr.CSteamID.m_SteamID);
                UnturnedChat.Say(caller, SpawnProtection.Instance.Translate("toggled_protection_off"));
            }
            SpawnProtection.Instance.Configuration.Save();
        }
    }
}
