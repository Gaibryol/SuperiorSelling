using BepInEx.Configuration;
using CSync.Lib;
using CSync.Util;
using System;
using System.Runtime.Serialization;
using HarmonyLib;
using Unity.Collections;
using Unity.Netcode;

namespace SuperiorSelling
{
	[DataContract]
	public class Config : SyncedInstance<Config>
	{
		[DataMember] public SyncedEntry<int> MaximumItems { get; private set; }

		public Config(ConfigFile cfg)
		{
			InitInstance(this);

			MaximumItems = cfg.BindSyncedEntry(
				"General",
				"MaximumItemsToSell",
				-1,
				"Maximum amount of items that can be sold at once (Infinite = -1, Vanilla Lethal Company = 12)"
			);
		}

		internal static void RequestSync()
		{
			if (!IsClient) return;

			using FastBufferWriter stream = new(IntSize, Allocator.Temp);

			// Method `OnRequestSync` will then get called on host.
			stream.SendMessage($"{PluginInfo.PLUGIN_GUID}_OnRequestConfigSync");
		}

		internal static void OnRequestSync(ulong clientId, FastBufferReader _)
		{
			if (!IsHost) return;

			byte[] array = SerializeToBytes(Instance);
			int value = array.Length;

			using FastBufferWriter stream = new(value + IntSize, Allocator.Temp);

			try
			{
				stream.WriteValueSafe(in value, default);
				stream.WriteBytesSafe(array);

				stream.SendMessage($"{PluginInfo.PLUGIN_GUID}_OnReceiveConfigSync", clientId);
			}
			catch (Exception e)
			{
				Plugin.Logger.LogError($"Error occurred syncing config with client: {clientId}\n{e}");
			}
		}

		internal static void OnReceiveSync(ulong _, FastBufferReader reader)
		{
			if (!reader.TryBeginRead(IntSize))
			{
				Plugin.Logger.LogError("Config sync error: Could not begin reading buffer.");
				return;
			}

			reader.ReadValueSafe(out int val, default);
			if (!reader.TryBeginRead(val))
			{
				Plugin.Logger.LogError("Config sync error: Host could not sync.");
				return;
			}

			byte[] data = new byte[val];
			reader.ReadBytesSafe(ref data, val);

			try
			{
				SyncInstance(data);
			}
			catch (Exception e)
			{
				Plugin.Logger.LogError($"Error syncing config instance!\n{e}");
			}
		}
	}
}
