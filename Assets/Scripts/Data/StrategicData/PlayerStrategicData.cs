using RTS.AI.Behavior;
using RTS.Buildings.Data;
using RTS.Common.Enums;
using RTS.Core;
using RTS.Managers.Map;
using RTS.Units.Data;
using System.Collections.Generic;
using UnityEngine;


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
            float radius = playerInfo.DataManager.buildingDatabase.GetBuildingTemplate(BuildingType.TownCenter).lineOfSightRange * 4;
            List<Vector3> directions = GetEnemyAttackDirectionWithinRadius(townhallPosition, radius);

            Vector3 calculatedDirection = Vector3.zero;

            foreach (var direction in directions)
            {
                calculatedDirection += (Vector3)direction;
            }

            return calculatedDirection /= directions.Count;
        }

        public List<Vector3> GetEnemyAttackDirectionWithinRadius(Vector3 towardPosition, float radius)
        {
            List<Vector3> directions = new List<Vector3>();
            var knownEnemyUnits = mapManager.GetKnownEnemyUnits();

            // if not seen the enemy yet, get enemy base direction
            if (knownEnemyUnits == null || knownEnemyUnits.Count <= 0)
            {
                List<PlayerInfo> enemyPlayerInfo = playerInfo.GameManager.GetOpponentPlayerInfo(playerInfo.PlayerNumber);

                if (enemyPlayerInfo != null && enemyPlayerInfo.Count > 0)
                {
                    Vector3 enemyBasePosition = enemyPlayerInfo[0].BuildingManager.GetBuilding(BuildingType.TownCenter).transform.position;
                    Vector3 dirToBase = (enemyBasePosition - towardPosition).normalized;
                    directions.Add(dirToBase);
                }
                
                return directions;
            }

            // Take directions towards towardPosition for each enemy unit in radius
            foreach (var enemyUnit in knownEnemyUnits)
            {
                float distance = Vector3.Distance(towardPosition, enemyUnit.Key);
                if (distance <= radius && distance > 0.01f)
                {
                    Vector3 dirToEnemy = (enemyUnit.Key - towardPosition).normalized;
                    directions.Add(dirToEnemy);
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