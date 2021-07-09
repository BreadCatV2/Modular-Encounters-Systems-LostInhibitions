﻿using ModularEncountersSystems.Core;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace ModularEncountersSystems.Entities {
	public static class EntityWatcher {

		
		public static bool NewPlayerConnected = false;

		public static Action UnloadEntities;

		public static void RegisterWatcher() {

			var entityList = new HashSet<IMyEntity>();
			MyAPIGateway.Entities.GetEntities(entityList);

			foreach (var entity in entityList) {

				NewEntityDetected(entity);

			}

			MyVisualScriptLogicProvider.PlayerConnected += PlayerManager.PlayerConnectEvent;
			PlayerManager.RefreshAllPlayers(true);

			UnloadEntities += GridManager.UnloadEntities;
			UnloadEntities += PlayerManager.UnloadEntities;

			MyAPIGateway.Entities.OnEntityAdd += NewEntityDetected;

			

			MES_SessionCore.UnloadActions += UnregisterWatcher;

		}

		public static void NewEntityDetected(IMyEntity entity) {

			var cubeGrid = entity as IMyCubeGrid;

			if (cubeGrid != null) {

				lock (GridManager.Grids) {

					var gridEntity = new GridEntity(entity);
					UnloadEntities += gridEntity.Unload;
					GridManager.Grids.Add(gridEntity);

				}
				
				return;
			
			}

			var planet = entity as MyPlanet;

			if (planet != null) {

				var planetEntity = new PlanetEntity(entity);
				UnloadEntities += planetEntity.Unload;
				PlanetManager.Planets.Add(planetEntity);
				return;

			}

			var safezone = entity as MySafeZone;

			if (safezone != null) {

				var safezoneEntity = new SafeZoneEntity(entity);
				UnloadEntities += safezoneEntity.Unload;
				SafeZoneManager.SafeZones.Add(safezoneEntity);
				return;

			}

		}

		public static void UnregisterWatcher() {

			MyAPIGateway.Entities.OnEntityAdd -= NewEntityDetected;

			UnloadEntities?.Invoke();

			UnloadEntities = null;
			GridManager.Grids.Clear();
			PlayerManager.Players.Clear();
			PlanetManager.Planets.Clear();
			SafeZoneManager.SafeZones.Clear();
			

		}

	}

}