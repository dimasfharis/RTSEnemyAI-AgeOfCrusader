using RTS.Buildings.Common;
using RTS.Common.DataClass;
using RTS.Common.Enums;
using RTS.Core;
using RTS.Managers;
using RTS.Units.Common;
using RTS.Units.Common.States;
using RTS.Units.Data;
using RTS.World.WorldManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RTS.AI.Micromanagement
{
    public class MicromanagementUnitController
    {
        public event Action<bool> OnMarchingStatusChanged;
        public event Action<BaseUnitController, BaseUnitController> OnBeingAttacked;

        private PlayerInfo playerInfo;
        private MicromanagementAIManager microAIManager;
        private WorldManager worldManager;
        private MilitaryUnitManager militaryUnitManager;

        private BaseUnitController unitController;
        public MilitaryGroup militaryGroup;

        public bool isMarching;
        private float marchingCheckInterval = 1f;
        private float marchingCheckTime;

        private float attackMoveInterval = 1f;
        private float attackMoveTime;
        public bool isAttackMove;
        public Vector3 attackMoveDestination;

        public Dictionary<BaseUnitController, float> attackerUnits = new Dictionary<BaseUnitController, float>();
        private int beingAttackedInterval = 4;
        private int beingAttackedCount = 1;

        #region Initialization

        public MicromanagementUnitController(PlayerInfo owner, BaseUnitController unitController)
        {
            playerInfo = owner;
            microAIManager = playerInfo.AIManager.GetMicromanagementAIManager();
            worldManager = playerInfo.GameManager.WorldManager;
            militaryUnitManager = playerInfo.MilitaryUnitManager;
            this.unitController = unitController;

            unitController.OnUnitHealthChanged += ResponseForBeingAttacked;
            unitController.OnUnitDead += OnDead;

            isMarching = false;
            isAttackMove = false;
            marchingCheckTime = marchingCheckInterval;
        }

        #endregion

        #region Lifecycle

        private void OnDead(BaseUnitController unit)
        {
            if (militaryGroup != null)
            {
                militaryGroup.UnregisterUnits(new List<BaseUnitController>() { unitController });
            }
        }

        #endregion

        #region Loop

        public void Tick()
        {
            CheckForOpponentAround();
            CheckIsMarching();
            AttackMoveUpdate();

            UnitAttackerUpdate();
        }

        #endregion

        #region Attack Move

        private void AttackMoveUpdate()
        {
            attackMoveTime -= Time.deltaTime;

            if (attackMoveTime > 0f)
                return;

            attackMoveTime = attackMoveInterval;

            if (!isAttackMove)
                return;

            if (unitController.GetCurrentState() is not MoveState
                && unitController.GetCurrentTargetUnit() == null
                && unitController.GetCurrentTargetBuilding() == null)
            {
                militaryUnitManager.IssueMoveCommand(new List<BaseUnitController>() { unitController }, attackMoveDestination, DoAfterAttackMoveReached);
            }

            // check if there enemy units or buildings around, if not, keep moving to target position
            if (unitController.GetCurrentState() is MoveState
                && unitController.GetCurrentTargetUnit() == null
                && unitController.GetCurrentTargetBuilding() == null)
            {
                AttackWhenOpponentAround();
            }
        }

        private void DoAfterAttackMoveReached()
        {
            if (Vector3.Distance(unitController.transform.position, attackMoveDestination) <= militaryGroup?.GetMarchingRadius())
            {
                isAttackMove = false;
            }
        }

        #endregion

        #region Group Marching

        private void CheckIsMarching()
        {
            marchingCheckTime -= Time.deltaTime;

            if (marchingCheckTime > 0f)
                return;

            marchingCheckTime = marchingCheckInterval;

            if (militaryGroup == null)
                return;

            if (Vector3.Distance(unitController.transform.position, militaryGroup.GetMeanPositionOfReinforces()) <= militaryGroup.GetMarchingRadius())
            {
                if (isMarching != true)
                {
                    isMarching = true;
                    OnMarchingStatusChanged?.Invoke(isMarching);
                    IntegrateMoveSpeedToMarchingSpeed(isMarching);
                }
            }
            else
            {
                if (isMarching != false)
                {
                    isMarching = false;
                    OnMarchingStatusChanged?.Invoke(isMarching);
                    IntegrateMoveSpeedToMarchingSpeed(isMarching);
                }
            }
        }

        public void IntegrateMoveSpeedToMarchingSpeed(bool isMarching)
        {
            if (isMarching)
            {
                unitController.GetUnitInfo().moveSpeed = militaryGroup.GetMarchingMoveSpeed();
            }
            else
            {
                UnitDatabase unitDatabase = playerInfo.DataManager.unitDatabase;
                UnitType unitType = unitController.GetUnitInfo().unitType;

                unitController.GetUnitInfo().moveSpeed = unitDatabase.GetUnitTemplate(unitType).moveSpeed;
            }
        }

        #endregion

        #region Attack Priorities

        public BaseUnitController GetAttackPriorityOpponentUnit()
        {
            List<BaseUnitController> opponentUnitInRadius = GetOpponentUnitInRadius();

            if (opponentUnitInRadius.Count <= 0)
                return null;

            // attack enemy unit evenly (not attack the same unit)
            List<BaseUnitController> opponentUnitEvenly = GetLesserAttackedUnits(opponentUnitInRadius);

            // thresholding, just get nearest opponent unit
            // BaseUnitController nearestUnit = GetNearestUnit(opponentUnitEvenly);

            if (opponentUnitEvenly.Count > 0)
            {
                return opponentUnitEvenly[UnityEngine.Random.Range(0, opponentUnitEvenly.Count)];
            }
            else
            {
                return null;
            }
        }

        public BaseBuildingController GetAttackPriorityOpponentBuilding()
        {
            List<BaseBuildingController> opponentBuildingInRadius = GetOpponentBuildingInRadius();

            // thresholding, just get nearest opponent building
            BaseBuildingController nearestBuilding = GetNearestBuilding(opponentBuildingInRadius);

            return nearestBuilding;
        }

        #endregion

        #region Opponent Around Check

        private void CheckForOpponentAround()
        {
            if (unitController.GetCurrentState() is not MoveState moveState)
                return;

            if (unitController.GetCurrentTargetUnit() != null
                && Vector3.Distance(unitController.transform.position, unitController.GetCurrentTargetUnit().transform.position) <= unitController.GetUnitInfo().attackRange)
            {
                if (moveState.doAfterReached != null)
                {
                    unitController.StopMovement();
                }
            } else if (unitController.GetCurrentTargetBuilding() != null
                && Vector3.Distance(unitController.transform.position, unitController.GetCurrentTargetBuilding().transform.position) <= unitController.GetUnitInfo().attackRange)
            {
                if (moveState.doAfterReached != null)
                {
                    unitController.StopMovement();
                }
            }
        }

        private void AttackWhenOpponentAround()
        {
            List<BaseUnitController> opponentUnitInRadius = GetOpponentUnitInRadius();
            if (opponentUnitInRadius.Count > 0)
            {
                BaseUnitController nearestUnit = GetNearestUnit(opponentUnitInRadius);
                if (nearestUnit != null)
                {
                    militaryUnitManager.IssueAttackCommand(new List<BaseUnitController>() { unitController }, nearestUnit);
                    return;
                }
            }
            List<BaseBuildingController> opponentBuildingInRadius = GetOpponentBuildingInRadius();
            if (opponentBuildingInRadius.Count > 0)
            {
                BaseBuildingController nearestBuilding = GetNearestBuilding(opponentBuildingInRadius);
                if (nearestBuilding != null)
                {
                    militaryUnitManager.IssueAttackCommand(new List<BaseUnitController>() { unitController }, nearestBuilding);
                    return;
                }
            }
        }

        #endregion

        #region Attack Response

        private void ResponseForBeingAttacked(float currentHealth, float maxHealth, BaseUnitController attackerUnit)
        {
            if (unitController.GetCurrentState() is AttackState)
                return;

            // Unit attacker update
            BeingAttacked(attackerUnit);

            // Group awareness
            beingAttackedCount--;
            if (beingAttackedCount <= 0)
            {
                beingAttackedCount = beingAttackedInterval;
                OnBeingAttacked?.Invoke(unitController, attackerUnit);

                militaryUnitManager.RespondToBeingAttackedInArea(unitController, 10f);
            }

            // Self-preservation, attack back when being attacked
            List<BaseUnitController> units = new() { unitController };

            BaseUnitController opponentUnit = GetAttackPriorityOpponentUnit();

            if (opponentUnit != null)
            {
                opponentUnit.GetMicromanagementUnitController().BeingAttacked(unitController);
                militaryUnitManager.IssueAttackCommand(units, opponentUnit);
                return;
            }

            BaseBuildingController opponentBuilding = GetAttackPriorityOpponentBuilding();

            if (opponentBuilding != null)
            {
                militaryUnitManager.IssueAttackCommand(units, opponentBuilding);
                return;
            }
        }

        public void BeingAttacked(BaseUnitController attackerUnit)
        {
            if (attackerUnit == null)
                return;
            
            if (!attackerUnits.ContainsKey(attackerUnit))
            {
                attackerUnits.Add(attackerUnit, beingAttackedInterval);
            } else
            {
                attackerUnits[attackerUnit] = beingAttackedInterval;
            }
        }

        private void UnitAttackerUpdate()
        {
            if (attackerUnits.Count <= 0)
                return;

            foreach (BaseUnitController attacker in attackerUnits.Keys.ToList())
            {
                if (attacker == null)
                {
                    attackerUnits.Remove(attacker);
                    return;
                }

                attackerUnits[attacker] -= Time.deltaTime;

                if (attackerUnits[attacker] < 0f)
                    attackerUnits.Remove(attacker);
            }
        }

        #endregion

        #region Helper

        private List<BaseUnitController> GetOpponentUnitInRadius()
        {
            List<BaseUnitController> opponentUnitInRadius = worldManager.GetOpponentUnitsInRadius(
                playerInfo,
                unitController.transform.position,
                unitController.GetUnitInfo().lineOfSightRange);

            return opponentUnitInRadius;
        }

        private List<BaseBuildingController> GetOpponentBuildingInRadius()
        {
            List<BaseBuildingController> opponentBuildingInRadius = worldManager.GetOpponentBuildingsInRadius(
                playerInfo,
                unitController.transform.position,
                unitController.GetUnitInfo().lineOfSightRange);

            return opponentBuildingInRadius;
        }

        private List<BaseUnitController> GetLesserAttackedUnits(List<BaseUnitController> opponentUnits)
        {
            List<BaseUnitController> lesserAttackedUnits = new List<BaseUnitController>();
            int lesserCount = opponentUnits[0].GetMicromanagementUnitController().attackerUnits.Count;

            foreach (var opponentUnit in opponentUnits)
            {
                if (opponentUnit.GetMicromanagementUnitController().attackerUnits.Count < lesserCount)
                {
                    lesserCount = opponentUnit.GetMicromanagementUnitController().attackerUnits.Count;
                    lesserAttackedUnits.Add(opponentUnit);
                }
            }
            if (lesserAttackedUnits.Count > 0)
                return lesserAttackedUnits;
            else
                return opponentUnits;
        }

        private BaseUnitController GetNearestUnit(List<BaseUnitController> units)
        {
            BaseUnitController nearestOpponentUnit = null;
            float nearestDistance = float.MaxValue;

            foreach (var opponentUnit in units)
            {
                float distance = Vector3.Distance(unitController.transform.position, opponentUnit.transform.position);

                if (distance < nearestDistance)
                {
                    nearestOpponentUnit = opponentUnit;
                    nearestDistance = distance;
                }
            }

            return nearestOpponentUnit;
        }

        private BaseBuildingController GetNearestBuilding(List<BaseBuildingController> buildings)
        {
            BaseBuildingController nearestOpponentBuilding = null;
            float nearestDistance = float.MaxValue;

            foreach (var opponentBuilding in buildings)
            {
                float distance = Vector3.Distance(unitController.transform.position, opponentBuilding.transform.position);

                if (distance < nearestDistance)
                {
                    nearestOpponentBuilding = opponentBuilding;
                    nearestDistance = distance;
                }
            }

            return nearestOpponentBuilding;
        }

        #endregion
    }
}