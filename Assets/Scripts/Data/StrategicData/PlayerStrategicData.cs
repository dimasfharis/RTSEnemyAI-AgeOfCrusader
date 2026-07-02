using RTS.Common.Enums;
using RTS.Core;
using RTS.Units.Data;
using RTS.Buildings.Data;
using System.Collections.Generic;
using UnityEngine;
using RTS.Managers.Map;


namespace RTS.Data.StrategicData
{
    public class PlayerStrategicData
    {
        // References
        private PlayerInfo playerInfo;
        private DataManager dataManager;
        private MapManager mapManager;
        private UnitDatabase unitDatabase;
        private BuildingDatabase buildingDatabase;

        #region Initialization

        public PlayerStrategicData(PlayerInfo owner, DataManager dataManager)
        {
            playerInfo = owner;
            mapManager = playerInfo.MapManager;

            this.dataManager = dataManager;

            unitDatabase = dataManager.unitDatabase;
            buildingDatabase = dataManager.buildingDatabase;
        }

        #endregion

        #region Public API

        public int GetEstimatedEnemyMilitaryPower()
        {
            int totalPower = 0;
            var knownEnemyUnits = mapManager.GetKnownEnemyUnits();

            foreach (var unit in knownEnemyUnits)
            {
                int thisUnitPower = unitDatabase.GetUnitAttackPower(unit.Value.UnitType);

                totalPower += thisUnitPower;
            }

            return totalPower;
        }

        public Vector3 GetBaseDefensePoint()
        {
            Vector3 townhallPosition = playerInfo.BuildingManager.GetBuilding(BuildingType.TownCenter).transform.position;
            List<Vector3> directions = GetEnemyAttackDirection(townhallPosition);

            Vector3 calculatedDirection = Vector3.zero;

            foreach (var direction in directions)
            {
                calculatedDirection += (Vector3)direction;
            }

            return calculatedDirection /= directions.Count;
        }

        public List<Vector3> GetEnemyAttackDirection(Vector3 pos)
        {
            List<Vector3> directions = new List<Vector3>();
            var knownEnemyUnits = mapManager.GetKnownEnemyUnits();

            foreach (var enemyUnit in knownEnemyUnits)
            {
                if (Vector3.Distance(pos, enemyUnit.Key) <= 20f)
                {
                    directions.Add(enemyUnit.Key);
                }
            }

            return directions;
        }

        public int GetExposedEnemyWorkerCount()
        {
            int exposedEnemyWorkers = 0;
            var knownEnemyUnits = mapManager.GetKnownEnemyUnits();
            Vector3 knownEnemyBaseLocation = mapManager.GetKnownEnemyBaseLocations();

            foreach (var enemyUnit in knownEnemyUnits)
            {
                if (enemyUnit.Value.UnitType == UnitType.Worker)
                {
                    if (Vector3.Distance(enemyUnit.Key, knownEnemyBaseLocation) > 20f)
                    {
                        exposedEnemyWorkers++;
                    }
                }
            }

            return exposedEnemyWorkers;
        }

        public Vector3 GetEnemyExposedEcoPosition()
        {
            Vector3 ecoPosition = Vector3.zero;
            var knownEnemyUnits = mapManager.GetKnownEnemyUnits();
            Vector3 knownEnemyBaseLocation = mapManager.GetKnownEnemyBaseLocations();

            foreach (var enemyUnit in knownEnemyUnits)
            {
                if (enemyUnit.Value.UnitType == UnitType.Worker)
                {
                    if (Vector3.Distance(enemyUnit.Key, knownEnemyBaseLocation) > 20f)
                        ecoPosition = enemyUnit.Key;
                }
            }

            return ecoPosition;
        }

        public Vector3 GetPatrolPoint()
        {
            return Vector3.zero; // placeholder, need to be replaced
        }

        public int GetEstimatedEnemyWorkerCount()
        {
            int totalWorkers = 0;
            var knownEnemyUnits = mapManager.GetKnownEnemyUnits();

            foreach (var unit in knownEnemyUnits)
            {
                if (unit.Value.UnitType == UnitType.Worker)
                {
                    totalWorkers++;
                }
            }

            return totalWorkers;
        }

        public int GetEnemyUnitsInRadius(Vector3 position, float radius)
        {
            int totalCount = 0;
            var knownEnemyUnits = mapManager.GetKnownEnemyUnits();

            foreach (var unit in knownEnemyUnits)
            {
                if (Vector3.Distance(position, unit.Key) <= radius)
                {
                    totalCount++;
                }
            }

            return totalCount;
        }

        #endregion
    }
}