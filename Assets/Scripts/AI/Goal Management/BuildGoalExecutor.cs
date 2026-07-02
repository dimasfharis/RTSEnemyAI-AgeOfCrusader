using RTS.AI.Behavior;
using RTS.Common.Enums;
using RTS.Core;
using RTS.Managers;
using RTS.Managers.Map;
using RTS.Units.Worker;
using RTS.World.ResourceNodeManagement;
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

        }

        #endregion

        #region Execution

        public void Execute(AIGoal goal)
        {
            currentTime += Time.deltaTime;

            if (currentTime >= executionTime)
            {
                currentTime = 0f;

                //Debug.Log("executing build goal...");
            }

            /*if (goal.GoalType != AIGoalType.BuildStructure)
                return;

            if (goal.IsCompleted)
                return;

            TryExecuteBuild(goal);*/
        }

        private bool TryExecuteBuild(AIGoal goal)
        {
            // Get Building Type to be build
            BuildingType buildingType = goal.BuildingType;

            // Has Enough Resource Checking
            if (!buildingManager.CanAfford(buildingType))
                return false;

            // Get Worker Available
            WorkerUnitController worker = workerManager.GetIdleWorkers()[0];
            if (worker == null)
                return false;

            // Get Build Position
            Vector3 buildPosition = FindBuildPosition(buildingType);
            if (buildPosition == Vector3.zero)
                return false;

            // Building Placement
            // without worker
            bool success = buildingManager.TryPlaceBuilding(
                buildingType,
                buildPosition);

            if (success)
            {
                goal.AddProgress(1);

                if (goal.currentProgress >= goal.targetAmount)
                {
                    goal.MarkCompleted();
                }
            }

            return success;
        }

        #endregion

        #region Building Placement

        private Vector3 FindBuildPosition(BuildingType buildingType)
        {
            BuildingCategory category = buildingManager.GetBuildingCategory(buildingType);

            switch (category)
            {
                case BuildingCategory.Economic:
                    return FindEconomyPlacement(buildingType);

                case BuildingCategory.Military:
                    return FindMilitaryPlacement();

                case BuildingCategory.Defensive:
                    return FindDefensePlacement();
            }

            return Vector3.zero;
        }

        private Vector3 FindEconomyPlacement(BuildingType buildingType)
        {
            ResourceType resourceType = playerInfo.DataManager.buildingDatabase.GetBuildingAcceptedResources(buildingType)[0];
            ResourceNode node = resourceNodeManager.GetNearestResourceNode(resourceType, basePosition);

            if (node == null)
                return Vector3.zero;

            Vector3 nearestNode = node.GetPosition();

            return mapManager.FindBuildablePositionNear(nearestNode, 8f);
        }

        private Vector3 FindMilitaryPlacement()
        {
            return mapManager.FindBuildablePositionNear(basePosition, BASE_BUILD_RADIUS);
        }

        private Vector3 FindDefensePlacement()
        {
            Vector3 frontier = mapManager.GetFrontierPosition();

            return mapManager.FindBuildablePositionNear(frontier, 6f);
        }

        #endregion
    }
}