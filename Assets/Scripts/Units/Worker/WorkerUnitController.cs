using UnityEngine;
using RTS.Units.Common;
using RTS.Units.Common.States;
using RTS.Buildings.Common;
using RTS.Units.Worker.States;
using RTS.Buildings.Data;
using RTS.World.ResourceNodeManagement;
using RTS.Unit.Common.States;
using RTS.Common.Enums;
using RTS.Data.StrategicData;

namespace RTS.Units.Worker
{
    public class WorkerUnitController : BaseUnitController
    {
        [Header("Runtime Data")]
        // Resource Gathering
        private ResourceType currentResourceType;
        private ResourceNode currentResourceNode;
        private BaseBuildingController currentDepositBuilding;

        // Building Construction
        private BuildingInfoSO curBuildingTemplate;
        private Vector3 curBuildingPosition;
        public BaseBuildingController curBaseBuildingController;
        public bool isBuildingInstantiated;

        #region Health and Damage

        public override void OnDead()
        {
            playerInfo.WorkerManager.UnregisterWorker(this);

            base.OnDead();
        }

        #endregion

        #region Resource Gathering

        public void SetWorkerToGather(ResourceType resourceType)
        {
            currentResourceType = resourceType;

            Vector3 currentEstResourceNodePos = GetNearestResourceNode(resourceType);

            if (currentEstResourceNodePos != Vector3.zero)
            {
                ChangeState(new MovingToResourceState(currentEstResourceNodePos));
            }
        }

        public void SetCurrentResourceNode(ResourceNode resourceNode)
        {
            currentResourceNode = resourceNode;
        }

        public int GatherResource()
        {
            if (currentResourceNode == null)
                return 0;

            int gatheredAmount = currentResourceNode.Take(1);
            unitInfo.carryingAmount += gatheredAmount;

            Debug.Log($"total carry: {unitInfo.carryingAmount}");

            return gatheredAmount;
        }

        public void DepositResource()
        {
            playerInfo.ResourceManager.AddResource(currentResourceNode.GetResourceType(), unitInfo.carryingAmount);

            unitInfo.carryingAmount = 0;
        }

        public bool IsCarryFull()
        {
            if (unitInfo.carryingAmount >= unitInfo.carryingCapacity)
                return true;

            return false;
        }

        private Vector3 GetNearestResourceNode(ResourceType resourceType)
        {
            Vector3 nearestNode = mapManager.GetNearestResourceNodeFromPosition(
                GetNearestDepositBuildingPosition(resourceType), resourceType);

            if (nearestNode == null)
            {
                Debug.LogWarning("No resource node found for type: " + resourceType);
                return Vector3.zero;
            }

            return nearestNode;
        }

        public ResourceNode GetNearestResourceNodeObject()
        {
            ResourceNode nearestNode = resourceNodeManager.GetNearestResourceNodeInRadius(
                transform.position, unitInfo.lineOfSightRange, currentResourceType);

            if (nearestNode == null)
            {
                Debug.LogWarning("No resource node found for type: " + currentResourceType);
                return null;
            }

            return nearestNode;
        }

        private Vector3 GetNearestDepositBuildingPosition(ResourceType resourceType)
        {
            BaseBuildingController nearestDeposit = playerInfo.BuildingManager.GetNearestDepositBuilding(transform.position, resourceType);

            if (nearestDeposit == null)
            {
                Debug.LogWarning("No deposit building found for resource type: " + resourceType);
                return Vector3.zero;
            }

            currentDepositBuilding = nearestDeposit;

            return nearestDeposit.transform.position;
        }

        #endregion

        #region Building Construction

        public void AssignWorkerToBuild(BuildingInfoSO template, Vector3 buildingPosition)
        {
            curBuildingTemplate = template;
            curBuildingPosition = new Vector3(Mathf.FloorToInt(buildingPosition.x), Mathf.FloorToInt(buildingPosition.y), 0);
            isBuildingInstantiated = false;

            ChangeState(new WorkerBuildState());
        }

        public void BuildCommand()
        {
            if (curBuildingTemplate == null)
                return;

            GameObject buildingObj = Instantiate(
                curBuildingTemplate.buildingPrefab,
                curBuildingPosition,
                Quaternion.identity
                );

            BaseBuildingController buildingController = buildingObj.GetComponent<BaseBuildingController>();

            if (buildingController == null)
            {
                Debug.LogError("Building prefab has no BaseBuildingController");
                return;
            }

            buildingController.Init(playerInfo, curBuildingTemplate);

            curBaseBuildingController = buildingController;
            isBuildingInstantiated = true;
        }

        public void ClearBuildTarget()
        {
            curBuildingTemplate = null;
            curBuildingPosition = Vector3.zero;
            curBaseBuildingController = null;
            isBuildingInstantiated = false;
        }

        public BaseBuildingController GetBuildingInstantiatedByTilePos(Vector3 tilePos)
        {
            return buildingManager.GetBuildingInstantiatedByTilePos(tilePos);
        }

        #endregion

        #region Accessors

        public ResourceNode GetCurrentResourceNode()
        {
            return currentResourceNode;
        }

        public BaseBuildingController GetCurrentBuildingController()
        {
            return curBaseBuildingController;
        }

        public BaseBuildingController GetDepositBuilding()
        {
            return currentDepositBuilding;
        }

        public bool HasBuildingTemplate()
        {
            return curBuildingTemplate != null;
        }

        public BuildingInfoSO GetCurrentBuildingTemplate()
        {
            return curBuildingTemplate;
        }

        public Vector3 GetCurrentBuildingPosition()
        {
            return curBuildingPosition;
        }

        public bool IsWorkerIdle()
        {
            return currentState is IdleState;
        }

        #endregion
    }
}