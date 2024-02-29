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
	public class Config : SyncedConfig<Config>
	{
		[DataMember] public SyncedEntry<int> MaximumItems { get; private set; }

		public Config(ConfigFile cfg) : base(PluginInfo.PLUGIN_GUID)
		{
			ConfigManager.Register(this);

			MaximumItems = cfg.BindSyncedEntry(
				"General",
				"MaximumItemsToSell",
				0,
				"Maximum amount of items that can be sold at once (Infinite = 0, Vanilla Lethal Company = 12)"
			);
		}
	}
}
