using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Rocket.API;
using Rocket.Core;
using Rocket.Unturned;
using Rocket.Core.Plugins;
using Logger = Rocket.Core.Logging.Logger;
using Rocket.Unturned.Player;
using Rocket.Unturned.Events;
using Rocket.Unturned.Chat;
using SDG.Unturned;
using Steamworks;
using Rocket.Core.Commands;
using UnityEngine;

namespace RocketModSpawnProtection
{
    public class SpawnProtection : RocketPlugin<SpawnProtectionConfig>
    {
        public static SpawnProtection Instance;

        protected override void Load()
        {
            Instance = this;

            UnturnedPlayerEvents.OnPlayerRevive += UnturnedPlayerEvents_OnPlayerRevive;
            U.Events.OnPlayerConnected += Events_OnPlayerConnected;

            Logger.Log("SpawnProtection loaded!");
        }

        void Events_OnPlayerConnected(UnturnedPlayer player)
        {
            if (Configuration.Instance.GiveProtectionOnJoin)
            {
                player.GetComponent<ProtectionComponent>().StartProtection();
            }
        }


        protected override void Unload()
        {
            Logger.Log("SpawnProtection Unloaded!");

            UnturnedPlayerEvents.OnPlayerRevive -= UnturnedPlayerEvents_OnPlayerRevive;
            U.Events.OnPlayerConnected -= Events_OnPlayerConnected;

            DisableAllPlayersSpawnProtection();
        }


        void UnturnedPlayerEvents_OnPlayerRevive(UnturnedPlayer player, Vector3 position, byte angle)
        {
            if (!Configuration.Instance.GiveProtectionOnRespawn || player == null)
                return;

            if (Configuration.Instance.CancelOnBedRespawn 
                && BarricadeManager.tryGetBed(player.CSteamID, out Vector3 bedPos, out byte bedAngle)
                && Vector3.Distance(bedPos, player.Position) < 10)
            {
                    UnturnedChat.Say(player, Translate("canceled_bedrespawn"), UnturnedChat.GetColorFromName(Configuration.Instance.ProtectionMessageColor, Color.red));
                    return;
            }

            player.GetComponent<ProtectionComponent>().StartProtection();
        }

        public override Rocket.API.Collections.TranslationList DefaultTranslations
        {
            get
            {
                return new Rocket.API.Collections.TranslationList
                {
                    {"prot_started", "You have spawn protection for {0} seconds!"},
                    {"canceled_item", "Your spawn protection expired because you equipted a item!"},
                    {"expired", "Your spawn protection expired!"},
                    {"canceled_veh", "Your spawn protection expired because you are in a vehicle with others!"},
                    {"admin_prot_enabled", "Enabled protection on {0}!"},
                    {"admin_prot_disabled", "Disabled protection on {0}!"},
                    {"usage_start", "Correct command usage: /pstart <player>"},
                    {"usage_stop", "Correct command usage: /pstop <player>"},
                    {"noplayer", "Player '{0}' not found!"},
                    {"canceled_punch", "Your spawn protection expired because you punched!"},
                    {"canceled_dist", "Your protection has expired because of moving away from spawn!" },
                    {"canceled_bedrespawn", "You were not giving spawnprotection due to spawning at your bed"}
                };
            }
        }

        [RocketCommand("startprot", "Manually enables spawnprotection on a player", "<player>", AllowedCaller.Both)]
        [RocketCommandAlias("pstart")]
        public void EnableProtCMD(IRocketPlayer caller, string[] command)
        {
            if (command.Length != 1)
            {
                UnturnedChat.Say(caller, Translate("usage_start"));
                return;
            }

            UnturnedPlayer uP = UnturnedPlayer.FromName(command[0]);
            if (uP == null)
            {
                UnturnedChat.Say(caller, Translate("noplayer", command[0]));
                return;
            }

            uP.GetComponent<ProtectionComponent>().StartProtection();
            sendMSG(caller, Translate("admin_prot_enabled", uP.DisplayName));
        }

        [RocketCommand("stopprot", "Manually disables spawnprotection on a player", "<player>", AllowedCaller.Both)]
        [RocketCommandAlias("pstop")]
        public void DisableProtCMD(IRocketPlayer caller, string[] command)
        {
            if (command.Length != 1)
            {
                UnturnedChat.Say(caller, Translate("usage_stop"));
                return;
            }

            UnturnedPlayer uP = UnturnedPlayer.FromName(command[0]);
            if (uP == null)
            {
                UnturnedChat.Say(caller, Translate("noplayer", command[0]));
                return;
            }

            uP.GetComponent<ProtectionComponent>().StopProtection();
            sendMSG(caller, Translate("admin_prot_disabled", uP.DisplayName));
        }

        void sendMSG(IRocketPlayer caller, string msg)
        {
            if (caller is ConsolePlayer)
            {
                Logger.Log(msg);
            }
            else
            {
                UnturnedChat.Say(caller, msg, GetCmdMsgColor());
            }
        }

        void DisableAllPlayersSpawnProtection()
        {
            foreach (var player in Provider.clients)
            {
                try
                {
                    var uP = UnturnedPlayer.FromSteamPlayer(player);
                    if (uP == null) continue;

                    var component = uP.GetComponent<ProtectionComponent>();
                    if (component == null) continue;

                    if (component.protectionEnabled)
                    {
                        component.StopProtection();
                    }
                }
                catch { }
            }
        }

        void SendCommandMessage(IRocketPlayer caller, string msg)
        {
            if (!(caller is ConsolePlayer)) UnturnedChat.Say(caller, msg);
        }

        UnityEngine.Color GetCmdMsgColor()
        {
            return UnturnedChat.GetColorFromName(Configuration.Instance.CommandMessageColor, UnityEngine.Color.green);
        }

        public static UnityEngine.Color GetProtMsgColor()
        {
            return UnturnedChat.GetColorFromName(SpawnProtection.Instance.Configuration.Instance.ProtectionMessageColor, UnityEngine.Color.yellow);
        }
    }
}
