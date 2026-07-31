using RTS.AI.Behavior;
using RTS.Buildings.Common;
using RTS.Common.Enums;
using RTS.Core;
using RTS.Managers;
using RTS.Managers.Map;
using RTS.Units.Worker;
using RTS.World.ResourceNodeManagement;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.AI.GoalManagement
{
    public class BuildGoalExecutor
    {
        private PlayerInfo playerInfo;
        private BuildingManager buildingManager;
        private WorkerManager workerManager;
        private MapManager mapManager;

        private ResourceNodeManager resourceNodeManager;

        private const float BASE_BUILD_RADIUS = 15f;
        private const float executionTime = 8f;
        private float currentTime;

        private Vector3 basePosition;

        #region Initialization

        public BuildGoalExecutor(PlayerInfo owner)
        {
            playerInfo = owner;
            buildingManager = owner.BuildingManager;
            workerManager = owner.WorkerManager;
            mapManager = owner.MapManager;

            resourceNodeManager = owner.ResourceNodeManager;

            // dont search here
            //basePosition = buildingManager.GetBuilding(BuildingType.TownCenter).transform.position;
        }

        #endregion

        #region Tick

        public void Tick()
        {
            // Check for building not yet built
        }

        #endregion

        #region Execution

        public void Execute(AIGoal goal)
        {
            if (goal.IsCompleted || goal.GoalType != AIGoalType.BuildStructure)
                return;

            switch (goal.BuildingType)
            {
                case BuildingType.House:
                    ExecuteHouseBuilding(goal);
                    break;

                case BuildingType.Barracks:
                case BuildingType.SiegeWorkshop:
                    ExecuteMilitaryBuilding(goal);
                    break;

                case BuildingType.CannonTower:
                case BuildingType.GuardTower:
                    ExecuteDefensiveBuilding(goal);
                    break;
            }
        }

        private bool TryExecuteBuild(AIGoal goal, Vector3 buildingPosition)
        {
            // Get Build Position
            if (buildingPosition == Vector3.zero)
                return false;

            // Get Worker Available
            var idleWorkers = workerManager.GetIdleWorkers();
            if (idleWorkers == null || idleWorkers.Count == 0)
                return false;

            WorkerUnitController worker = idleWorkers[0];

            // Assign Worker to Build
            bool success = workerManager.TryAssignWorkerToBuild(
                new List<WorkerUnitController> { worker },
                goal.BuildingType,
                buildingPosition);

            if (success)
            {
                goal.AddProgress(1);
                return true;
            }

            return false;
        }

        #endregion

        #region Building Placement

            #region House Building

            private void ExecuteHouseBuilding(AIGoal aiGoal)
            {
                Vector3 baseRef = buildingManager.GetBuilding(BuildingType.TownCenter).transform.position;
                float scanRadius = playerInfo.DataManager.buildingDatabase.GetBuildingTemplate(aiGoal.BuildingType).lineOfSightRange * 3;

                List<Vector3> enemyDirections = playerInfo.DataManager.GetPlayerStrategicData().GetEnemyAttackDirectionWithinRadius(baseRef, scanRadius);

                Vector3 safeDirection = Vector3.zero;

                if (enemyDirections != null && enemyDirections.Count > 0)
                {
                    Vector3 avgEnemyDir = Vector3.zero;
                    foreach (Vector3 direction in enemyDirections)
                    {
                        avgEnemyDir += direction;
                    }
                    avgEnemyDir.Normalize();

                    safeDirection = -avgEnemyDir;
                }
                
                Vector3 buildPosition = mapManager.FindBuildablePositionNear(aiGoal.BuildingType, baseRef, scanRadius, safeDirection);

                TryExecuteBuild(aiGoal, buildPosition);
            }

            #endregion

            #region Military Building

            private void ExecuteMilitaryBuilding(AIGoal aiGoal)
            {
                Vector3 baseRef = buildingManager.GetBuilding(BuildingType.TownCenter).transform.position;
                float scanRadius = playerInfo.DataManager.buildingDatabase.GetBuildingTemplate(aiGoal.BuildingType).lineOfSightRange * 3;

                Vector3 buildPosition = mapManager.FindBuildablePositionNear(aiGoal.BuildingType, baseRef, scanRadius);

                TryExecuteBuild(aiGoal, buildPosition);
            }

            #endregion

            #region Defensive Building

            private void ExecuteDefensiveBuilding(AIGoal aiGoal)
            {
                Vector3 baseRef = buildingManager.GetBuilding(BuildingType.TownCenter).transform.position;
                float scanRadius = playerInfo.DataManager.buildingDatabase.GetBuildingTemplate(aiGoal.BuildingType).lineOfSightRange * 3;

                List<Vector3> enemyDirections = playerInfo.DataManager.GetPlayerStrategicData().GetEnemyAttackDirectionWithinRadius(baseRef, scanRadius);

                Vector3 defenseDirection = Vector3.zero;

                if (enemyDirections != null && enemyDirections.Count > 0)
                {
                    Vector3 avgEnemyDir = Vector3.zero;
                    foreach (Vector3 direction in enemyDirections)
                    {
                        avgEnemyDir += direction;
                    }
                    avgEnemyDir.Normalize();
                    defenseDirection = avgEnemyDir;
                }

                BaseBuildingController outerBuildingInDirection = mapManager.GetOuterBuildingInDirection(baseRef, defenseDirection);

                Vector3 buildPosition = mapManager.FindBuildablePositionNear(aiGoal.BuildingType, outerBuildingInDirection.transform.position, scanRadius, defenseDirection);

                TryExecuteBuild(aiGoal, buildPosition);
            }

            #endregion

        #endregion
    }
}