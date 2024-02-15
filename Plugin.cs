using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CSync.Lib;
using GameNetcodeStuff;
using HarmonyLib;
using SuperiorSelling.Patches;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace SuperiorSelling
{
	[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
	[BepInDependency("io.github.CSync")]
	public class Plugin : BaseUnityPlugin
	{
		private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

		public static Config Config { get; private set; }

		public static Plugin Instance { get; internal set; }

		public static new ManualLogSource Logger { get; private set; }

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
			}

			Logger = base.Logger;

			Config = new(base.Config);

			harmony.PatchAll(typeof(DepositItemsPatch));
			harmony.PatchAll(typeof(PlayerControllerBPatch));
			harmony.PatchAll(typeof(NetworkManagerPatch));
			harmony.PatchAll(typeof(Config));
			//harmony.PatchAll(Assembly.GetExecutingAssembly());

			// Plugin startup logic
			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
		}
	}

	public static class PluginInfo
	{
		public const string PLUGIN_GUID = "SuperiorSelling";
		public const string PLUGIN_NAME = "SuperiorSelling";
		public const string PLUGIN_VERSION = "1.0.0";
	}
}

namespace SuperiorSelling.Patches
{
	[HarmonyPatch(typeof(DepositItemsDesk))]
	internal class DepositItemsPatch
	{
		[HarmonyTranspiler]
		[HarmonyPatch("PlaceItemOnCounter")]
		static IEnumerable<CodeInstruction> PlaceItemOnCounterTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			// Iterate through CodeInstructions
			List<CodeInstruction> newInstructions = instructions.ToList();
			for(int i = 0; i < newInstructions.Count - 1; i++)
			{
				// Find line of code we need to replace
                if (newInstructions[i].opcode != OpCodes.Ldc_I4_S)
				{
					continue;
				}

				// Replace comparison
				newInstructions[i] = new CodeInstruction(OpCodes.Ldc_I4_S, (Config.Instance.MaximumItems.Value == -1 ? int.MaxValue : Config.Instance.MaximumItems.Value));
            }
			return newInstructions;
		}
	}

	[HarmonyPatch(typeof(PlayerControllerB))]
	internal class PlayerControllerBPatch
	{
		[HarmonyPostfix]
		[HarmonyPatch("ConnectClientToPlayerObject")]
		public static void InitializeLocalPlayer()
		{
			if (Config.IsHost)
			{
				Config.MessageManager.RegisterNamedMessageHandler($"{PluginInfo.PLUGIN_GUID}_OnRequestConfigSync", Config.OnRequestSync);
				Config.Synced = true;

				Plugin.Logger.LogInfo("Initialize Is Host");
				return;
			}

			Plugin.Logger.LogInfo("Initialize Client");
			Config.Synced = false;
			Config.MessageManager.RegisterNamedMessageHandler($"{PluginInfo.PLUGIN_GUID}_OnReceiveConfigSync", Config.OnReceiveSync);
			Config.RequestSync();
		}
	}

	[HarmonyPatch(typeof(GameNetworkManager))]
	internal class NetworkManagerPatch
	{
		[HarmonyPostfix]
		[HarmonyPatch("StartDisconnect")]
		public static void PlayerLeave()
		{
			Config.RevertSync();
		}
	}
}