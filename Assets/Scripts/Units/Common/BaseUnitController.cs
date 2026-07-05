using RTS.AI.Behavior;
using RTS.AI.Micromanagement;
using RTS.AI.Pathfinding;
using RTS.Buildings.Common;
using RTS.Buildings.Data;
using RTS.Core;
using RTS.Managers;
using RTS.Managers.Map;
using RTS.Units.Common.States;
using RTS.Units.Data;
using RTS.Units.Military;
using RTS.Units.Worker;
using RTS.World.ResourceNodeManagement;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.Units.Common
{
    public abstract class BaseUnitController : MonoBehaviour
    {
        public event Action<BaseUnitController> OnUnitMoveReached;
        public event Action<float, float, BaseUnitController> OnUnitHealthChanged;
        public event Action<BaseUnitController> OnUnitDead;

        [Header("Core References")]
        protected PlayerInfo playerInfo;
        protected UnitInfo unitInfo;
        protected UnitView unitView;
        protected MicromanagementUnitController microUnitController;
        protected IUnitState currentState;
        protected PathfindingAIManager pathfindingAIManager;
        protected BuildingManager buildingManager;
        protected MilitaryUnitManager militaryUnitManager;
        protected ResourceNodeManager resourceNodeManager;
        protected MapManager mapManager;

        // RUNTIME DATA
        [Space]
        [Header("Activated Goal")]
        public AIGoal activatedGoal = null;

        [Header("Unit Movement and Collision Avoidance")]
        public SingleUnitMovement singleUnitMovement;
        public GroupUnitMovement groupUnitMovement;

        public Vector2 velocity;
        public Vector2 acceleration;
        public float maxForce = 4f;
        protected Vector3 currentDestination;

        protected float timeOnSamePosition = 0f;
        protected Vector3 lastPosition;

        [Header("Unit Combat")]
        private BaseUnitController currentTargetUnit;
        private BaseBuildingController currentTargetBuilding;

        [Header("Line of Sight")]
        protected float resourceNodeSightTimer = 0f;
        protected float resourceNodeSightInterval = 1.5f;
        protected float buildingSightTimer = 0f;
        protected float buildingSightInterval = 2.5f;
        protected float enemyUnitSightTimer = 0f;
        protected float enemyUnitSightInterval = 1f;
        protected float lastSightUpdateTimer = 0f;
        protected float sightUpdateInterval = 0.8f;

        #region Initialization

        public virtual void Init(PlayerInfo owner, UnitInfoSO template)
        {
            playerInfo = owner;

            unitInfo = new UnitInfo(template, playerInfo);
            unitView = GetComponentInChildren<UnitView>();
            unitView?.Init(playerInfo, this);
            microUnitController = new MicromanagementUnitController(playerInfo, this);

            pathfindingAIManager = playerInfo.AIManager.GetPathfindingAIManager();
            buildingManager = playerInfo.BuildingManager;
            militaryUnitManager = playerInfo.MilitaryUnitManager;
            resourceNodeManager = playerInfo.GameManager.ResourceNodeManager;
            mapManager = playerInfo.MapManager;

            ChangeState(new IdleState());
        }

        #endregion

        #region Unity Lifecycle

        protected virtual void Update()
        {
            currentState?.Tick(this);

            HandleMovement();
            HandleUnitCollisionAvoidance();

            UpdateLineOfSight();

            microUnitController.Tick();
        }

        #endregion

        #region State Management

        public void ChangeState(IUnitState newState)
        {
            if (newState == null)
                return;

            currentState?.OnExit(this);
            currentState = newState;
            currentState.OnEnter(this);
        }

        #endregion

        #region Common Unit Actions

        public virtual void PlayIdleAnimation()
        {
            // Implement idle animation logic
        }

        #endregion

        #region Unit Combat

        public void IssueAttackMove(Vector3 destination)
        {
            microUnitController.isAttackMove = true;
            microUnitController.attackMoveDestination = destination;
        }

        public virtual void IssueAttack(BaseUnitController target)
        {
            if (target == null)
                return;

            currentTargetUnit = target;
            ChangeState(new AttackState());
        }

        public virtual void IssueAttack(BaseBuildingController target)
        {
            if (target == null)
                return;

            currentTargetBuilding = target;
            ChangeState(new AttackState());
        }

        public virtual void PerformAttack()
        {
            if (currentTargetUnit == null && currentTargetBuilding == null)
                return;

            if (currentTargetUnit != null)
            {
                currentTargetUnit.ReceiveDamage(unitInfo.attackDamage, this);

                if (currentTargetUnit.unitInfo.unitHealth <= 0f || currentTargetUnit == null)
                    ClearTarget();
            }else
            {
                currentTargetBuilding.ReceiveDamage(unitInfo.attackDamage);

                if (currentTargetBuilding.GetBuildingInfo().currentHitPoint <= 0f || currentTargetBuilding == null)
                    ClearTarget();
            }
        }

        public void ClearTarget()
        {
            currentTargetUnit = null;
            currentTargetBuilding = null;
        }

        #endregion

        #region Unit Movement

        public void CommandMove(Vector3 destination, Action doAfterReached = null)
        {
            List<BaseUnitController> units = new List<BaseUnitController>
            {
                this
            };

            militaryUnitManager.IssueMoveCommand(units, destination, doAfterReached);
        }

        public void SetDestinationMove(Vector3 destination)
        {
            currentDestination = destination;
        }

        public void StopMovement()
        {
            if (currentState is MoveState move && move.doAfterReached != null)
            {
                OnUnitMoveReached?.Invoke(this);
                move.OnExit(this);
            }else if (currentState is MoveState)
            {
                OnUnitMoveReached?.Invoke(this);
                ChangeState(new IdleState());
            }
        }

        private void HandleMovement()
        {
            if (currentState is not MoveState) return;

            pathfindingAIManager.HandleMovement(this);

            if (Vector3.Distance(transform.position, currentDestination) < 0.05f
                || IsOnSamePositionForAWhile())
            {
                StopMovement();
            }
        }

        private bool IsOnSamePositionForAWhile(float thresholdTime = 3f)
        {
            timeOnSamePosition += Time.deltaTime;

            if (timeOnSamePosition >= thresholdTime)
            {
                timeOnSamePosition = 0f;

                if (Vector3.Distance(lastPosition, transform.position) < 0.2f)
                {
                    Debug.Log("unit in same position for a while");
                    return true;
                }

                lastPosition = transform.position;
            }

            return false;
        }

        #endregion

        #region Unit Collision Avoidance

        private void HandleUnitCollisionAvoidance()
        {
            pathfindingAIManager.HandleUnitCollisionAvoidance(this);

            UpdatePhysics();
        }

        private void UpdatePhysics()
        {
            float moveSpeed = GetUnitInfo().moveSpeed;

            velocity += acceleration * Time.deltaTime;
            velocity = Vector2.ClampMagnitude(velocity, moveSpeed);

            transform.position += (Vector3)(velocity * Time.deltaTime);

            velocity = Vector2.Lerp(velocity, Vector2.zero, Time.deltaTime * 2f);

            acceleration = Vector2.zero;
        }

        public void ApplyForce(Vector2 force)
        {
            acceleration += force;
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

                List<ResourceNode> nodesInRadius = resourceNodeManager.GetResourceNodesInRadius(transform.position, unitInfo.lineOfSightRange);
            
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
                    List<BaseBuildingController> buildingsInRadius = opponent.BuildingManager.GetBuildingsInRadius(transform.position, unitInfo.lineOfSightRange);

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
                    List<MilitaryUnitController> militaryUnitsInRadius = opponent.MilitaryUnitManager.GetUnitsInRadius(transform.position, unitInfo.lineOfSightRange);
                    List<WorkerUnitController> workerUnitsInRadius = opponent.WorkerManager.GetWorkersInRadius(transform.position, unitInfo.lineOfSightRange);

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

                List<Vector2Int> newlyExploredTiles = mapManager.UpdateExploredTiles(transform.position, unitInfo.lineOfSightRange);

                // If the unit is assigned to a scout goal,
                // update the progress of the goal based on the number of newly explored tiles
                if (newlyExploredTiles.Count > 0 && activatedGoal != null)
                {
                    if (activatedGoal.GoalType == AIGoalType.AssignScout)
                    {
                        activatedGoal.AddProgress(newlyExploredTiles.Count);
                    }
                }
            }
        }

        #endregion

        #region Health and Damage

        public virtual void ReceiveDamage(int damageAmount, BaseUnitController attackerUnit = null)
        {
            if (damageAmount <= 0) return;

            unitInfo.unitHealth -= damageAmount;

            OnUnitHealthChanged?.Invoke(unitInfo.unitHealth, unitInfo.unitMaxHealth, attackerUnit);

            if (unitInfo.unitHealth <= 0)
            {
                ChangeState(new DeadState());
            }
        }

        public virtual void PlayDeadAnimation()
        {
            // Override in specific unit if needed
        }

        public virtual void OnDead()
        {
            OnUnitDead?.Invoke(this);

            Destroy(gameObject);
        }

        #endregion

        #region Goal Management

        public void UnassignGoal()
        {
            if (activatedGoal != null)
            {
                activatedGoal = null;
            }
        }

        #endregion

        #region Public API

        public List<BaseUnitController> GetNeighbourUnitNearby(float radius)
        {
            var unitsInArea = new List<BaseUnitController>();

            List<MilitaryUnitController> militaryUnits = playerInfo.MilitaryUnitManager.GetAllUnits();
            List<WorkerUnitController> workerUnits = playerInfo.WorkerManager.GetAllUnits();

            foreach (var unit in militaryUnits)
            {
                if (Vector3.Distance(this.transform.position, unit.transform.position) <= radius)
                {
                    unitsInArea.Add(unit);
                }
            }
            foreach (var unit in workerUnits)
            {
                if (Vector3.Distance(this.transform.position, unit.transform.position) <= radius)
                {
                    unitsInArea.Add(unit);
                }
            }

            return unitsInArea;
        }

        public Vector3 GetNearestDepositBuildingTilePos(BaseBuildingController building)
        {
            Vector3 nearestTilePos = Vector3.zero;
            float nearestDistance = float.MaxValue;

            List<Vector3> buildingTilePos = playerInfo.GameManager.WorldManager.tileDatabase.GetBuildingTileWorldPos(building);

            foreach (var tilePos in buildingTilePos)
            {
                float distance = Vector3.Distance(transform.position, tilePos);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTilePos = tilePos;
                }
            }

            return nearestTilePos;
        }

        public Vector3 GetNearestBuildingTilePos(Vector3 buildingPosition, BuildingInfoSO buildingTemplate)
        {
            Vector3 nearestTilePos = Vector3.zero;
            float nearestDistance = float.MaxValue;

            for (int x = 0; x < buildingTemplate.length; x++)
            {
                for (int y = 0; y < buildingTemplate.width; y++)
                {
                    Vector3 tilePos = new Vector3(buildingPosition.x + x + 0.5f, buildingPosition.y + y + 0.5f);
                    float distance = Vector3.Distance(transform.position, tilePos);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestTilePos = tilePos;
                    }
                }
            }

            return nearestTilePos;
        }

        public BaseBuildingController GetBuildingByTilePos(Vector3 tilePos)
        {
            return buildingManager.GetBuildingByTilePos(tilePos);
        }

        #endregion

        #region Public Setter

        public void SetCurrentTargetUnit(BaseUnitController unit)
        {
            currentTargetUnit = unit;
        }

        public void SetCurrentTargetBuilding(BaseBuildingController building)
        {
            currentTargetBuilding = building;
        }

        #endregion

        #region Accessors

        public PlayerInfo GetPlayerInfo()
        {
            return playerInfo;
        }

        public IUnitState GetCurrentState()
        {
            return currentState;
        }

        public UnitInfo GetUnitInfo()
        {
            return unitInfo;
        }

        public MicromanagementUnitController GetMicromanagementUnitController()
        {
            return microUnitController;
        }

        public bool IsDestinationInRange(Vector3 destination, float range)
        {
            return Vector3.Distance(transform.position, destination) <= range;
        }

        public BaseUnitController GetCurrentTargetUnit()
        {
            return currentTargetUnit;
        }

        public BaseBuildingController GetCurrentTargetBuilding()
        {
            return currentTargetBuilding;
        }

        public bool IsIdle()
        {
            return currentState is IdleState;
        }

        #endregion
    }
}