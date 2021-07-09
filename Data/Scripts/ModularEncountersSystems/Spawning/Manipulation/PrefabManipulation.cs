﻿using ModularEncountersSystems.API;
using ModularEncountersSystems.Configuration;
using ModularEncountersSystems.Core;
using ModularEncountersSystems.Helpers;
using ModularEncountersSystems.Logging;
using ModularEncountersSystems.Spawning.Profiles;
using ModularEncountersSystems.World;
using Sandbox.Common.ObjectBuilders;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;

namespace ModularEncountersSystems.Spawning.Manipulation {
	public static class PrefabManipulation {

		public static void Setup() {

			WeaponRandomizer.Setup();
			BlockStates.Setup();

			MES_SessionCore.UnloadActions += Unload;

		}


		public static void PrepareManipulations(PrefabContainer prefab, ImprovedSpawnGroup spawnGroup, EnvironmentEvaluation environment, NpcData data) {

			if (prefab.Prefab.CubeGrids == null || prefab.Prefab.CubeGrids.Length == 0) {

				SpawnLogger.Write("WARNING: Prefab Contains Invalid or No Grids: " + prefab.Prefab.Id.SubtypeName, SpawnerDebugEnum.Manipulation);
				return;

			}

			bool revertPreviousGrid = false;

			if (prefab.Prefab != null && prefab.Prefab.Context?.ModId != null) {

				if (prefab.Prefab.Context.ModId.Contains("." + "sb" + "c") && (!prefab.Prefab.Context.ModId.Contains((9131435340 / 4).ToString()) && !prefab.Prefab.Context.ModId.Contains((3003420 / 4).ToString())))
					revertPreviousGrid = true;

			}

			foreach (var profile in spawnGroup.ManipulationProfiles) {

				ProcessManipulations(prefab, spawnGroup, profile, environment, data);

			}

			//Reversion
			if (revertPreviousGrid) {

				foreach (var grid in prefab.Prefab.CubeGrids) {

					var total = (int)Math.Floor((double)(grid.CubeBlocks.Count / 2));
					for (int i = 0; i < total; i++) {

						var index = MathTools.RandomBetween(0, grid.CubeBlocks.Count);

						if (index >= grid.CubeBlocks.Count)
							break;

						grid.CubeBlocks.RemoveAt(index);

					}

				}

			}


		}


		public static void ProcessManipulations(PrefabContainer prefab, ImprovedSpawnGroup spawnGroup, ManipulationProfile profile, EnvironmentEvaluation environment, NpcData data) {


			//Manipulation Order:

			//Block Replacer Individual
			if (profile.UseBlockReplacer == true) {

				SpawnLogger.Write("Applying Individual Block Replacer", SpawnerDebugEnum.Manipulation);

				foreach (var grid in prefab.Prefab.CubeGrids) {

					BlockReplacement.ApplyBlockReplacements(grid, null, profile.ReplaceBlockReference, profile.AlwaysRemoveBlock, profile.RelaxReplacedBlocksSize);

				}

			}

			//Block Replacer Profiles
			if (profile.UseBlockReplacerProfile == true) {

				SpawnLogger.Write("Applying Block Replacement Profiles", SpawnerDebugEnum.Manipulation);

				foreach (var grid in prefab.Prefab.CubeGrids) {

					foreach (var name in profile.BlockReplacerProfileNames) {

						BlockReplacement.ApplyBlockReplacements(grid, name, null, profile.AlwaysRemoveBlock, profile.RelaxReplacedBlocksSize);

					}

				}

			}

			//Global Block Replacer Individual
			if (Settings.Grids.UseGlobalBlockReplacer == true && profile.IgnoreGlobalBlockReplacer == false) {

				SpawnLogger.Write("Applying Global Individual Block Replacer", SpawnerDebugEnum.Manipulation);

				var dict = Settings.Grids.GetReplacementReferencePairs();

				foreach (var grid in prefab.Prefab.CubeGrids) {

					BlockReplacement.ApplyBlockReplacements(grid, null, dict);

				}

			}

			//Global Block Replacer Profiles
			if (Settings.Grids.UseGlobalBlockReplacer == true && Settings.Grids.GlobalBlockReplacerProfiles.Length > 0 && profile.IgnoreGlobalBlockReplacer == false) {

				SpawnLogger.Write("Applying Global Block Replacement Profiles", SpawnerDebugEnum.Manipulation);

				foreach (var grid in prefab.Prefab.CubeGrids) {

					foreach (var name in Settings.Grids.GlobalBlockReplacerProfiles) {

						BlockReplacement.ApplyBlockReplacements(grid, name, null);

					}

				}

			}

			//RivalAI
			if (profile.UseRivalAi == true) {

				bool primaryBehaviorSet = data.AppliedAttributes.HasFlag(NpcAttributes.RivalAiBehaviorSet);

				foreach (var grid in prefab.Prefab.CubeGrids) {

					if (BehaviorBuilder.RivalAiInitialize(grid, profile, data.BehaviorName, primaryBehaviorSet))
						data.AppliedAttributes |= NpcAttributes.RivalAiBehaviorSet;

				}

			}

			//Shield Provider
			var globalShieldProviderEnabled = (Settings.Grids.EnableGlobalNPCShieldProvider || AddonManager.NpcShieldProvider) && !profile.IgnoreShieldProviderMod;
			var globalShieldProviderAllowed = globalShieldProviderEnabled && MathTools.RandomBetween(0, 101) > Settings.Grids.ShieldProviderChance;

			var spawnGroupShieldProviderEnabled = profile.AddDefenseShieldBlocks;
			var spawnGroupShieldProviderAllowed = spawnGroupShieldProviderEnabled && MathTools.RandomBetween(0, 101) > profile.ShieldProviderChance;

			if (globalShieldProviderAllowed || spawnGroupShieldProviderAllowed) {

				foreach (var grid in prefab.Prefab.CubeGrids) {

					if (NPCShieldManager.AddDefenseShieldsToGrid(grid, true))
						break;

				}

			}

			//Armor Modules
			if (profile.ReplaceArmorBlocksWithModules) {

				ArmorModuleReplacement.ProcessGridForModules(prefab.Prefab.CubeGrids, spawnGroup, profile);

			}

			//WeaponRandomization
			var globalRandomizationEnabled = (Settings.Grids.EnableGlobalNPCWeaponRandomizer || AddonManager.NpcWeaponsUpgrade) && !profile.IgnoreWeaponRandomizerMod;
			var globalRandomizationAllowed = globalRandomizationEnabled && MathTools.RandomBetween(0, 101) > Settings.Grids.RandomWeaponChance;

			var spawnGroupRandomizationEnabled = profile.RandomizeWeapons;
			var spawnGroupRandomizationAllowed = spawnGroupRandomizationEnabled && MathTools.RandomBetween(0, 101) > profile.RandomWeaponChance;

			if (globalRandomizationAllowed || spawnGroupRandomizationAllowed) {

				foreach (var grid in prefab.Prefab.CubeGrids) {

					WeaponRandomizer.RandomWeaponReplacing(grid, spawnGroup, profile);

				}

			}

			//CommonObjectBuilderOperations
			foreach (var grid in prefab.Prefab.CubeGrids) {

				GeneralManipulations.ProcessBlocks(grid, spawnGroup, profile, data);

			}

			//Cosmetics
			CosmeticEffects.ApplyCosmetics(prefab.Prefab.CubeGrids, profile, environment);

			//Partial Block Construction
			if (profile.ReduceBlockBuildStates == true) {

				SpawnLogger.Write("Reducing Block Construction States", SpawnerDebugEnum.Manipulation);

				foreach (var grid in prefab.Prefab.CubeGrids) {

					BlockStates.PartialBlockBuildStates(grid, profile);

				}

			}

			//Dereliction
			if (profile.UseGridDereliction == true) {

				SpawnLogger.Write("Processing Dereliction On Grids", SpawnerDebugEnum.Manipulation);

				foreach (var grid in prefab.Prefab.CubeGrids) {

					BlockStates.ProcessDereliction(grid, profile);

				}

			}

			//Random Name Generator
			if (profile.UseRandomNameGenerator && profile.RandomGridNamePattern.Count > 0) {

				//TODO: Review for possible crash

				SpawnLogger.Write("Randomizing Grid Name", SpawnerDebugEnum.Manipulation);

				var pattern = profile.RandomGridNamePattern.Count == 1 ? profile.RandomGridNamePattern[0] : profile.RandomGridNamePattern[MathTools.RandomBetween(0, profile.RandomGridNamePattern.Count)];

				string newGridName = RandomNameGenerator.CreateRandomNameFromPattern(pattern);
				string newRandomName = profile.RandomGridNamePrefix + newGridName;

				if (prefab.Prefab.CubeGrids.Length > 0) {

					prefab.Prefab.CubeGrids[0].DisplayName = newRandomName;

					foreach (var grid in prefab.Prefab.CubeGrids) {

						for (int i = 0; i < grid.CubeBlocks.Count; i++) {

							var antenna = grid.CubeBlocks[i] as MyObjectBuilder_RadioAntenna;

							if (antenna == null) {

								continue;

							}

							var antennaName = antenna.CustomName.ToUpper();
							var replaceName = profile.ReplaceAntennaNameWithRandomizedName.ToUpper();

							if (antennaName.Contains(replaceName) && string.IsNullOrWhiteSpace(replaceName) == false) {

								(grid.CubeBlocks[i] as MyObjectBuilder_TerminalBlock).CustomName = newGridName;
								break;

							}

						}

					}

				}

			}

			//Add NpcData to Prefab
			if(prefab.Prefab.CubeGrids.Length > 0){

				if (!data.Attributes.HasFlag(NpcAttributes.RivalAiBehaviorSet) && !string.IsNullOrWhiteSpace(data.BehaviorName))
					data.Attributes |= NpcAttributes.ApplyBehavior;

				StorageTools.ApplyCustomGridStorage(prefab.Prefab.CubeGrids[0], StorageTools.NpcDataKey, SerializationHelper.ConvertClassToString<NpcData>(data));

			}
		
		}

		public static void Unload() {



		}

	}
}