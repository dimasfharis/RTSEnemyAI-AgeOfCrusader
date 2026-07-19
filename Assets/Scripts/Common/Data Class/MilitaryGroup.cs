using RTS.AI.Behavior;
using RTS.AI.Micromanagement;
using RTS.Buildings.Common;
using RTS.Common.Enums;
using RTS.Core;
using RTS.Managers;
using RTS.Units.Common;
using RTS.Units.Military;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.Common.DataClass
{
    public class MilitaryGroup
    {
        private PlayerInfo playerInfo;
        private MicromanagementAIManager microAIManager;
        private MilitaryUnitManager militaryUnitManager;

        public List<BaseUnitController> units = new List<BaseUnitController>();
        public List<BaseUnitController> enemyUnits = new List<BaseUnitController>();
        public List<BaseBuildingController> enemyBuildings = new List<BaseBuildingController>();

        public AIGoal aiGoal;
        public MilitaryGroupMode militaryGroupMode;

        public bool isTargetAccomplished;
        public List<UnitType> targetUnitTypes = new List<UnitType>();
        public List<BuildingType> targetBuildingTypes = new List<BuildingType>();

        public Vector3 targetPosition;

        public bool isEngaged;
        private Vector3 lastPositionEngaged;
        private BaseUnitController lastUnitEngaged;
        private float engageTimer;
        private float engageDuration = 5f;

        private float retreatTimer;
        private float retreatCheckDuration = 2f;

        // Group Marching
        private int marchingUnitsCount;
        private bool isMarching;
        public bool isAssembled;

        #region Initialization

        public MilitaryGroup(List<BaseUnitController> units, PlayerInfo owner, AIGoal aiGoal = null)
        {
            playerInfo = owner;
            microAIManager = playerInfo.AIManager.GetMicromanagementAIManager();
            militaryUnitManager = playerInfo.MilitaryUnitManager;

            this.aiGoal = aiGoal;
            militaryGroupMode = MilitaryGroupMode.None;
            isTargetAccomplished = false;
            isEngaged = false;
            isMarching = false;
            isAssembled = false;
            marchingUnitsCount = 0;

            RegisterUnits(units);
        }

        #endregion

        #region Lifecycle

        public void OnGroupDisbanded()
        {
            foreach (BaseUnitController unit in units)
            {
                unit.GetMicromanagementUnitController().IntegrateMoveSpeedToMarchingSpeed(false);
            }

            UnregisterUnits(units);

            aiGoal = null;
        }

        #endregion

        #region Loop

        public void Tick()
        {
            UpdateGroupMode();
            UpdateGroupEngagement();

            CheckForRetreat();
        }

        #endregion

        #region Group Mode Management

        private void UpdateGroupMode()
        {
            switch (militaryGroupMode)
            {
                case MilitaryGroupMode.AttackWave:
                    ReinforceAttackWave();
                    break;
                case MilitaryGroupMode.BaseDefense:
                    ReinforceBaseDefense();
                    break;
                case MilitaryGroupMode.EnemyHarassment:
                    ReinforceEnemyHarassment();
                    break;
                case MilitaryGroupMode.EnemySurveying:
                    ReinforceEnemySurveying();
                    break;
                case MilitaryGroupMode.MapSurveying:
                    ReinforceMapSurveying();
                    break;
            }
        }

        #endregion

        #region Unit Registration

        private void RegisterUnits(List<BaseUnitController> units)
        {
            foreach (BaseUnitController unit in units)
            {
                RegisterUnit(unit);
            }
        }

        public void RegisterUnit(BaseUnitController unit)
        {
            if (!units.Contains(unit))
            {
                UnregisterFromAnotherGroup();
                units.Add(unit);
                unit.GetMicromanagementUnitController().militaryGroup = this;
                unit.GetMicromanagementUnitController().OnMarchingStatusChanged += OnUnitMarchingStatusChanged;
                unit.GetMicromanagementUnitController().OnBeingAttacked += OnUnitBeingAttacked;
            }
        }

        public void UnregisterUnits(List<BaseUnitController> units)
        {
            foreach (BaseUnitController unit in units)
            {
                UnregisterUnit(unit);
            }

            this.units.RemoveAll(unit => units.Contains(unit));
        }

        public void UnregisterUnit(BaseUnitController unit)
        {
            if (units.Contains(unit))
            {
                unit.GetMicromanagementUnitController().militaryGroup = null;
                unit.GetMicromanagementUnitController().OnMarchingStatusChanged -= OnUnitMarchingStatusChanged;
                unit.GetMicromanagementUnitController().OnBeingAttacked -= OnUnitBeingAttacked;
            }
        }

        private void UnregisterFromAnotherGroup()
        {
            List<MilitaryGroup> anotherMilitaryGroup = microAIManager.GetAnotherMilitaryGroup(this);

            if (anotherMilitaryGroup != null)
            {
                foreach (BaseUnitController unit in units)
                {
                    foreach (MilitaryGroup group in anotherMilitaryGroup)
                    {
                        if (group.units.Contains(unit))
                        {
                            group.UnregisterUnits(new List<BaseUnitController>() { unit });
                        }
                    }
                }
            }
        }

        #endregion

        #region Group Reinforcement

        #region Attack Wave

        private void ReinforceAttackWave()
        {
            if (!isAssembled)
                return;

            if (!isMarching)
            {
                militaryUnitManager.IssueAttackMoveCommand(units, targetPosition);
                isMarching = true;
            }

            if (isTargetAccomplished || units.Count <= 0)
                OnGroupDisbanded();
        }

        #endregion

        #region Base Defense

        private void ReinforceBaseDefense()
        {

        }

        #endregion

        #region Enemy Harassment

        private void ReinforceEnemyHarassment()
        {

        }

        #endregion

        #region Enemy Surveying

        private void ReinforceEnemySurveying()
        {

        }

        #endregion

        #region Map Surveying

        private void ReinforceMapSurveying()
        {

        }

        #endregion

        #endregion

        #region Group Marching

        private void OnUnitMarchingStatusChanged(bool isMarching)
        {
            if (isMarching)
            {
                marchingUnitsCount++;

                if (marchingUnitsCount >= units.Count && units.Count > 0 && !isAssembled)
                {
                    isAssembled = true;
                    IntegrateMoveSpeedToMarchingSpeed();
                }
            }
            else
            {
                marchingUnitsCount--;
            }
        }

        private void IntegrateMoveSpeedToMarchingSpeed()
        {
            float marchingMoveSpeed = GetMarchingMoveSpeed();

            foreach (BaseUnitController unit in units)
            {
                if (unit is MilitaryUnitController)
                {
                    MilitaryUnitController militaryUnit = unit as MilitaryUnitController;
                    militaryUnit.GetMicromanagementUnitController().IntegrateMoveSpeedToMarchingSpeed(isAssembled);
                }
            }
        }

        #endregion

        #region Group Movement

        public Vector3 GetMeanPositionOfReinforces()
        {
            Vector3 sumPosition = Vector3.zero;
            foreach (BaseUnitController unit in units)
            {
                sumPosition += unit.transform.position;
            }
            Vector3 centerPositionOfReinforces = sumPosition / units.Count;

            return centerPositionOfReinforces;
        }

        #endregion

        #region Group Engagement

        private void UpdateGroupEngagement()
        {
            if (isEngaged)
            {
                engageTimer += Time.deltaTime;
                if (engageTimer >= engageDuration)
                {
                    isEngaged = false;
                    engageTimer = 0f;
                }
            }
        }

        private void SetGroupEngaged(BaseUnitController unitEngaged)
        {
            lastPositionEngaged = unitEngaged.transform.position;
            lastUnitEngaged = unitEngaged;
            isEngaged = true;
            engageTimer = 0f;
        }

        #endregion

        #region Group Awareness

        private void OnUnitBeingAttacked(BaseUnitController unit, BaseUnitController attackerUnit)
        {
            foreach (BaseUnitController unitInGroup in units)
            {
                if (unitInGroup is MilitaryUnitController
                    && unitInGroup.GetCurrentTargetUnit() == null
                    && unitInGroup.GetCurrentTargetBuilding() == null)
                {
                    SetGroupEngaged(unit);
                    unitInGroup.GetMicromanagementUnitController().AttackPriorityOpponent(unitInGroup);
                }
            }
        }

        #endregion

        #region Retreat

        private void CheckForRetreat()
        {
            if (!isEngaged)
                return;

            retreatTimer += Time.deltaTime;

            if (retreatTimer < retreatCheckDuration)
                return;

            retreatTimer = 0f;

            List<PlayerInfo> enemyPlayerInfo = playerInfo.GameManager.GetOpponentPlayerInfo(playerInfo.PlayerNumber);
            List<MilitaryUnitController> enemyUnitsInRange = enemyPlayerInfo[0].MilitaryUnitManager.GetUnitsInRadius(lastPositionEngaged, lastUnitEngaged.GetUnitInfo().lineOfSightRange * 2);

            float militaryPower = CalculateMilitaryPower(units);
            float enemyMilitaryPower = CalculateMilitaryPower(enemyUnitsInRange);

            Debug.Log($"Player {playerInfo.PlayerNumber} Power: {militaryPower}, Player {enemyPlayerInfo[0].PlayerNumber} Power: {enemyMilitaryPower}");

            if (militaryPower < (enemyMilitaryPower * 0.1f))
            {
                militaryUnitManager.IssueRetreatCommand(units);
                isEngaged = false;

                OnGroupDisbanded();
            }
        }

        private float CalculateMilitaryPower(List<MilitaryUnitController> units)
        {
            float totalPower = 0f;
            foreach (MilitaryUnitController unit in units)
            {
                if (unit is MilitaryUnitController)
                {
                    MilitaryUnitController militaryUnit = unit as MilitaryUnitController;
                    float unitPower = militaryUnit.GetUnitInfo().attackPower;
                    float healthFactor = unit.GetUnitInfo().unitHealth / unit.GetUnitInfo().unitMaxHealth;
                    float powerMultiplier = playerInfo.AIManager.GetAIProfile().OurPowerMultiplier;

                    totalPower += unitPower * healthFactor * powerMultiplier;
                }
            }
            return totalPower;
        }

        private float CalculateMilitaryPower(List<BaseUnitController> units)
        {
            float totalPower = 0f;
            foreach (BaseUnitController unit in units)
            {
                if (unit is MilitaryUnitController)
                {
                    MilitaryUnitController militaryUnit = unit as MilitaryUnitController;
                    float unitPower = militaryUnit.GetUnitInfo().attackPower;
                    float healthFactor = unit.GetUnitInfo().unitHealth / unit.GetUnitInfo().unitMaxHealth;
                    float powerMultiplier = playerInfo.AIManager.GetAIProfile().OurPowerMultiplier;

                    totalPower += unitPower * healthFactor * powerMultiplier;
                }
            }
            return totalPower;
        }

        #endregion

        #region Helper

        public float GetMarchingRadius()
        {
            float count = units.Count;

            return count * 1.2f;
        }

        public float GetMarchingMoveSpeed()
        {
            float slowestMoveSpeed = float.MaxValue;

            foreach (BaseUnitController unit in units)
            {
                if (unit is MilitaryUnitController)
                {
                    MilitaryUnitController militaryUnit = unit as MilitaryUnitController;
                    if (militaryUnit.GetUnitInfo().moveSpeed < slowestMoveSpeed)
                    {
                        slowestMoveSpeed = militaryUnit.GetUnitInfo().moveSpeed;
                    }
                }
            }

            return slowestMoveSpeed;
        }

        #endregion
    }
}