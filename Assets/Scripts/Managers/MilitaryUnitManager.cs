using RTS.AI.Micromanagement;
using RTS.Buildings.Common;
using RTS.Common.Enums;
using RTS.Core;
using RTS.Managers.Research;
using RTS.Units.Common;
using RTS.Units.Military;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.Managers
{
    public class MilitaryUnitManager
    {
        private readonly PlayerInfo playerInfo;
        private MicromanagementAIManager micromanagementAIManager;
        private ResearchManager researchManager;

        private List<MilitaryUnitController> militaryUnits = new List<MilitaryUnitController>();

        #region Initialization

        public MilitaryUnitManager(PlayerInfo owner)
        {
            playerInfo = owner;
        }

        #endregion

        #region Military Unit Registration

        public void RegisterUnit(MilitaryUnitController unit)
        {
            if (!militaryUnits.Contains(unit))
            {
                militaryUnits.Add(unit);
                playerInfo.ResourceManager.AddPopulation(1);
            }
        }

        public void UnregisterUnit(MilitaryUnitController unit)
        {
            if (militaryUnits.Contains(unit))
            {
                militaryUnits.Remove(unit);
            }
        }

        #endregion

        #region Unit Spawning

        public MilitaryUnitController SpawnUnitAtBuilding(BaseBuildingController building, UnitType unitType)
        {
            // Prefab Instantiation
            GameObject prefab = playerInfo.DataManager.unitDatabase.GetUnitPrefab(unitType);
            Vector3 spawnPosition = building.transform.position + building.GetBuildingInfo().unitSpawnPoint;
            GameObject unitObj = UnityEngine.Object.Instantiate(prefab, spawnPosition, Quaternion.identity);

            // Controller & UnitInfo Initialization
            var controller = unitObj.GetComponent<MilitaryUnitController>();
            var unitInfoTemplate = playerInfo.DataManager.unitDatabase.GetUnitTemplate(unitType);
            controller.Init(playerInfo, unitInfoTemplate);

            // Unit Stats Upgrade
            if (researchManager == null)
                researchManager = playerInfo.ResearchManager;
            researchManager.UpgradeStats(controller);

            // Unit Registration
            RegisterUnit(controller);

            return controller;
        }

        #endregion

        #region Commands

        public void IssueAttackMoveCommand(List<BaseUnitController> selectedUnits, Vector3 targetPosition)
        {
            foreach (var unit in selectedUnits)
            {
                unit.IssueAttackMove(targetPosition);
            }
        }

        public void IssueAttackCommand(List<BaseUnitController> selectedUnits, BaseUnitController target)
        {
            foreach (var unit in selectedUnits)
            {
                unit.IssueAttack(target);
            }
        }

        public void IssueAttackCommand(List<BaseUnitController> selectedUnits, BaseBuildingController target)
        {
            foreach (var unit in selectedUnits)
            {
                unit.IssueAttack(target);
            }
        }

        public void IssueMoveCommand(List<BaseUnitController> selectedUnits, Vector3 destination, Action doAfterReached = null)
        {
            playerInfo.AIManager.GetPathfindingAIManager().SetMoveTo(selectedUnits, destination, doAfterReached);
        }

        public void IssueStopCommand(BaseUnitController unit)
        {
            unit.StopMovement();
        }

        public void IssueRetreatCommand(List<BaseUnitController> selectedUnits)
        {
            IssueMoveCommand(selectedUnits, new Vector3(1, 1, 0));
        }

        #endregion

        #region Attacked Response

        public void RespondToBeingAttackedInArea(BaseUnitController attackedUnit, float radius)
        {
            if (micromanagementAIManager == null)
                micromanagementAIManager = playerInfo.AIManager.GetMicromanagementAIManager();

            micromanagementAIManager.ReinforceGroupByArea(attackedUnit.transform.position, radius);
        }

        #endregion

        #region Stats Research Upgrade

        public void LevelUpUnitHP(float amount)
        {
            foreach (var unit in militaryUnits)
            {
                unit.GetUnitInfo().unitMaxHealth *= amount;
                unit.GetUnitInfo().unitHealth = unit.GetUnitInfo().unitMaxHealth;
            }
        }

        public void LevelUpAttackPoint(float amount)
        {
            foreach (var unit in militaryUnits)
            {
                float upgradedValue = unit.GetUnitInfo().attackDamage * amount;
                unit.GetUnitInfo().attackDamage = (int) upgradedValue;
            }
        }

        #endregion

        #region Public API

        public List<MilitaryUnitController> GetAllUnits()
        {
            return militaryUnits;
        }

        public List<MilitaryUnitController> GetAvailableUnits()
        {
            var availableUnits = new List<MilitaryUnitController>();

            foreach (var unit in militaryUnits)
            {
                if (unit.activatedGoal == null)
                {
                    availableUnits.Add(unit);
                }
            }

            return availableUnits;
        }

        public List<MilitaryUnitController> GetIdleUnits()
        {
            var idleUnits = new List<MilitaryUnitController>();

            foreach (var unit in militaryUnits)
            {
                if (unit.IsIdle())
                    idleUnits.Add(unit);
            }

            return idleUnits;
        }

        public List<MilitaryUnitController> GetUnitsInRadius(Vector3 position, float radius)
        {
            var unitsInArea = new List<MilitaryUnitController>();

            foreach (var unit in militaryUnits)
            {
                if (Vector3.Distance(unit.transform.position, position) <= radius)
                {
                    unitsInArea.Add(unit);
                }
            }

            return unitsInArea;
        }

        public BaseUnitController GetUnitAtPosition(Vector3 position)
        {
            BaseUnitController worker = playerInfo.WorkerManager.GetWorkerAtPosition(position);

            if (worker == null)
            {
                foreach (var unit in militaryUnits)
                {
                    if (Vector3.Distance(unit.transform.position, position) < 0.7f)
                    {
                        return unit;
                    }
                }
            }

            return worker;
        }

        public int CalculateTotalPower()
        {
            int totalPower = 0;

            foreach (var unit in militaryUnits)
            {
                totalPower += unit.GetAttackPower();
            }

            return totalPower;
        }

        #endregion
    }
}