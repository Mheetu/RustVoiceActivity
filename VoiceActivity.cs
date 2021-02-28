using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Facepunch;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;
using Random=System.Random;

using Network;
using Newtonsoft.Json;
using Rust;

namespace Oxide.Plugins
{
    [Info("VoiceActivity", "Mheetu", "1.0.0")]
    [Description("Finds players who have recently spoken in game.")]
    public class VoiceActivity : RustPlugin
    {
        #region Initialization

        private readonly Dictionary<ulong, Timer> voices = new Dictionary<ulong, Timer>();

        private readonly Dictionary<string, string> defaultLang = new Dictionary<string, string> {
            {"Permission: Denied", "You do not have permission to execute this command."},
            {"Recently Spoke", "Recently Spoke:"},
            {"No Recent Speakers", "Nobody has spoken recently."},
        };

        new void LoadDefaultMessages ()
        {
            lang.RegisterMessages(defaultLang, this);
        }

        void OnServerInitialized ()
        {
            try {
                permission.RegisterPermission("voiceactivity.use", this);
            } catch (Exception ex) {
                PrintError("OnServerInitialized failed: {0}", ex.Message);
            }
        }

        #endregion Initialization

        #region Game Hooks

        void OnPlayerVoice(BasePlayer player, Byte[] data)
        {
            ulong userId = player.userID;

            if (voices.ContainsKey(userId)) {
                voices[userId].Reset();
            } else {
                voices.Add(userId, timer.Once(10f, () => voices.Remove(userId)));
            }
        }

        private void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            removePlayer(player);
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            removePlayer(player);
        }

        private void OnPlayerSleep(BasePlayer player)
        {
            removePlayer(player);
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            removePlayer(player);
        }

        private void Unload()
        {
            voices.Clear();
        }

        #endregion Game Hooks

        #region Console Commands

        [ConsoleCommand("vshow")]
        private void VocalCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) {
                return;
            }

            BasePlayer player = arg.Player();
            if ( ! hasPermissionTo(player, "use")) {
                PrintToConsole(__("Permission: Denied", (player as BasePlayer)));
                return;
            }

            if (voices.Count <= 0) {
                PrintToConsole(__("No Recent Speakers", (player as BasePlayer)));
                return;
            }

            PrintToConsole(__("Recently Spoke", (player as BasePlayer)));
            foreach (ulong playerId in voices.Keys) {
                IPlayer vocalPlayer = covalence.Players.FindPlayerById(playerId.ToString());
                PrintToConsole(playerId.ToString() + "\t" + vocalPlayer.Name);
            }
        }

        [ConsoleCommand("vspec")]
        private void VocalSpectateCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) {
                return;
            }

            BasePlayer player = arg.Player();
            if ( ! hasPermissionTo(player, "use")) {
                PrintToConsole(__("Permission: Denied", (player as BasePlayer)));
                return;
            }

            List<ulong> recentSpeakers = new List<ulong>(voices.Keys);
            recentSpeakers.Remove(player.userID);

            if (recentSpeakers.Count <= 0) {
                PrintToConsole(__("No Recent Speakers", (player as BasePlayer)));
                return;
            }

            Random rand = new Random();
            ulong randomPlayerId = recentSpeakers[rand.Next(recentSpeakers.Count)];

            player.SendConsoleCommand("spectate " + randomPlayerId.ToString());
        }

        #endregion Chat Commands

        #region Helpers

        private bool hasPermissionTo(BasePlayer player, string permissionName) => permission.UserHasPermission(player.UserIDString, "voiceactivity." + permissionName);

        private string __(string key, BasePlayer player = null) => lang.GetMessage(key, this, player == null ? null : player.UserIDString);

        #endregion Helpers

        #region Utility Methods

        private void removePlayer(BasePlayer player)
        {
            if (voices.ContainsKey(player.userID)) {
                voices[player.userID]?.Destroy();
                voices.Remove(player.userID);
            }
        }

        #endregion Utility Methods
    }
}