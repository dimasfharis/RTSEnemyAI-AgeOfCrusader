using UnityEngine;
using RTS.Core;
using RTS.Buildings.Common.States;
using RTS.Buildings.Data;
using RTS.Common.Enums;
using RTS.Units.Data;
using RTS.Data.StrategicData;
using RTS.Units.Common;
using RTS.World.ResourceNodeManagement;
using System.Collections.Generic;
using RTS.Units.Military;
using RTS.Units.Worker;
using System;
using RTS.Managers;
using RTS.Managers.Map;

namespace RTS.Buildings.Common
{
    public abstract class BaseBuildingController : MonoBehaviour
    {
        public event Action<BaseBuildingController> OnBuildingInstantiated;
        public event Action<float, float> OnBuildProgressChanged;
        public event Action<BaseBuildingController> OnBuildingConstructed;
        public event Action<float, float> OnBuildingHealthChanged;
        public event Action<BaseBuildingController> OnBuildingDestroyed;

        [Header("Core References")]
        protected PlayerInfo playerInfo;
        protected BuildingInfo buildingInfo;
        protected BuildingView buildingView;
        protected UnitDatabase unitDatabase;
        protected MapManager mapManager;
        protected BuildingManager buildingManager;
        protected ResourceNodeManager resourceNodeManager;

        protected IBuildingState currentState;

        [Header("Runtime Data")]
        // Line of Sight
        protected float resourceNodeSightTimer = 0f;
        protected float resourceNodeSightInterval = 7f;
        protected float buildingSightTimer = 0f;
        protected float buildingSightInterval = 15f;
        protected float enemyUnitSightTimer = 0f;
        protected float enemyUnitSightInterval = 3f;
        protected float lastSightUpdateTimer = 0f;
        protected float sightUpdateInterval = 1.5f;

        #region Initialization

        public virtual void Init(PlayerInfo owner, BuildingInfoSO template)
        {
            playerInfo = owner;
            buildingInfo = new BuildingInfo(playerInfo, template);
            buildingView = GetComponentInChildren<BuildingView>();
            buildingView.Init(playerInfo, this);

            unitDatabase = playerInfo.DataManager.unitDatabase;
            mapManager = playerInfo.MapManager;
            buildingManager = playerInfo.BuildingManager;
            resourceNodeManager = playerInfo.GameManager.ResourceNodeManager;

            ChangeState(new UnderConstructionState());

            OnBuildingInstantiated += OnBuildingInstantiatedAction;
            OnBuildingInstantiated?.Invoke(this);
        }

        #endregion

        #region Unity Lifecycle

        protected virtual void Update()
        {
            currentState?.Tick(this);

            UpdateLineOfSight();
        }

        #endregion

        #region State Management

        public void ChangeState(IBuildingState newState)
        {
            if (newState == null)
                return;

            currentState?.OnExit(this);
            currentState = newState;
            currentState.OnEnter(this);
        }

        #endregion

        #region Health & Damage

        public virtual void ReceiveDamage(int damageAmount)
        {
            if (damageAmount <= 0) return;

            buildingInfo.currentHitPoint -= damageAmount;

            OnBuildingHealthChanged?.Invoke(buildingInfo.currentHitPoint, buildingInfo.maxHitPoint);

            if (buildingInfo.currentHitPoint <= 0)
            {
                ChangeState(new DestroyedState());
            }
        }

        #endregion

        #region Construction

        public void AddBuildProgress(float amount)
        {
            if (buildingInfo == null)
                return;

            buildingInfo.buildProgress += amount;

            OnBuildProgressChanged?.Invoke(buildingInfo.buildProgress, buildingInfo.buildTime);

            if (buildingInfo.buildProgress >= buildingInfo.buildTime)
            {
                OnConstructionComplete();
            }
        }

        #endregion

        #region Building Lifecycle

        private void OnBuildingInstantiatedAction(BaseBuildingController controller)
        {
            buildingManager.OnBuildingInstantiated(controller);
        }

        public virtual void OnConstructionComplete()
        {
            buildingInfo.isConstructed = true;

            OnBuildingConstructed?.Invoke(this);

            buildingManager.OnBuildingConstructed(this);

            ChangeState(new ActiveState());
        }

        public virtual void OnBuildingActivated()
        {
            buildingInfo.isConstructed = true;
            buildingInfo.isActive = true;

            playerInfo.BuildingManager.RegisterBuilding(this);
        }

        public virtual void OnBuildingDestroyedAction()
        {
            buildingInfo.isActive = false;
            buildingInfo.isConstructed = false;
            buildingInfo.isDestroyed = true;

            OnBuildingInstantiated -= OnBuildingInstantiatedAction;

            OnBuildingDestroyed?.Invoke(this);

            playerInfo.BuildingManager.UnregisterBuilding(this);

            Destroy(gameObject);
        }

        #endregion

        #region Line of Sight

        private void UpdateLineOfSight()
        {
            ResourceNodeSight();
            BuildingSight();
            EnemyUnitSight();
            MapExploredSight();
        }

        private void ResourceNodeSight()
        {
            resourceNodeSightTimer += Time.deltaTime;
            if (resourceNodeSightTimer >= resourceNodeSightInterval)
            {
                resourceNodeSightTimer = 0f;
                List<ResourceNode> nodesInRadius = resourceNodeManager.GetResourceNodesInRadius(transform.position, buildingInfo.lineOfSightRange);

                mapManager.UpdateResourceNodeMemory(nodesInRadius);
            }
        }

        private void BuildingSight()
        {
            buildingSightTimer += Time.deltaTime;

            if (buildingSightTimer >= buildingSightInterval)
            {
                buildingSightTimer = 0f;

                List<PlayerInfo> opponents = playerInfo.GameManager.GetOpponentPlayerInfo(playerInfo.PlayerNumber);

                foreach (var opponent in opponents)
                {
                    List<BaseBuildingController> buildingsInRadius = opponent.BuildingManager.GetBuildingsInRadius(transform.position, buildingInfo.lineOfSightRange);

                    mapManager.UpdateBuildingMemory(buildingsInRadius, opponent);
                }
            }
        }

        private void EnemyUnitSight()
        {
            enemyUnitSightTimer += Time.deltaTime;

            if (enemyUnitSightTimer >= enemyUnitSightInterval)
            {
                enemyUnitSightTimer = 0f;

                List<PlayerInfo> opponents = playerInfo.GameManager.GetOpponentPlayerInfo(playerInfo.PlayerNumber);

                foreach (var opponent in opponents)
                {
                    List<MilitaryUnitController> militaryUnitsInRadius = opponent.MilitaryUnitManager.GetUnitsInRadius(transform.position, buildingInfo.lineOfSightRange);
                    List<WorkerUnitController> workerUnitsInRadius = opponent.WorkerManager.GetWorkersInRadius(transform.position, buildingInfo.lineOfSightRange);

                    List<BaseUnitController> unitsInRadius = new List<BaseUnitController>();

                    unitsInRadius.AddRange(militaryUnitsInRadius);
                    unitsInRadius.AddRange(workerUnitsInRadius);

                    mapManager.UpdateEnemyUnitMemory(unitsInRadius, opponent);
                }
            }
        }

        private void MapExploredSight()
        {
            lastSightUpdateTimer += Time.deltaTime;

            if (lastSightUpdateTimer >= sightUpdateInterval)
            {
                lastSightUpdateTimer = 0f;

                mapManager.UpdateExploredTiles(transform.position, buildingInfo.lineOfSightRange);
            }
        }

        #endregion

        #region Accessors

        public PlayerInfo GetPlayerInfo()
        {
            return playerInfo;
        }

        public BuildingInfo GetBuildingInfo()
        {
            return buildingInfo;
        }

        public bool IsActive()
        {
            return currentState is ActiveState;
        }

        public bool IsConstructed()
        {
            return buildingInfo != null && buildingInfo.isConstructed;
        }

        public virtual bool CanDeposit(ResourceType resourceType)
        {
            return buildingInfo.CanAcceptResource(resourceType);
        }

        public bool CanAcceptTrainUnit(UnitType unitType)
        {
            return buildingInfo.CanAcceptUnitTrain(unitType);
        }

        #endregion
    }
}