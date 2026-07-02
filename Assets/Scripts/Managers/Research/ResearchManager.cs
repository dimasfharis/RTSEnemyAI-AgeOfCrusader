using System.Collections.Generic;
using UnityEngine;
using RTS.Core;
using RTS.Common.Enums;
using RTS.Buildings.Common;
using RTS.Buildings.Common.Interfaces;
using RTS.Research.Data;
using RTS.Units.Common;
using RTS.Units.Worker;
using RTS.Units.Military;
using RTS.Buildings.Defensive;
using RTS.Buildings.Economic;

namespace RTS.Managers.Research
{
    public class ResearchManager
    {
        private PlayerInfo playerInfo;
        private WorkerManager workerManager;
        private MilitaryUnitManager militaryUnitManager;
        private BuildingManager buildingManager;
        private ResearchDatabase researchDatabase;

        private List<ResearchType> totalResearch;
        private List<ResearchType> availableResearchType;
        private List<ResearchType> researchedType;

        #region Initialization

        public ResearchManager(PlayerInfo owner)
        {
            playerInfo = owner;
            workerManager = owner.WorkerManager;
            militaryUnitManager = owner.MilitaryUnitManager;
            buildingManager = owner.BuildingManager;

            totalResearch = new List<ResearchType>();
            availableResearchType = new List<ResearchType>();
            researchedType = new List<ResearchType>();

            InitializeAvailableResearch();
        }

        private void InitializeAvailableResearch()
        {
            foreach (ResearchType type in System.Enum.GetValues(typeof(ResearchType)))
            {
                totalResearch.Add(type);
                availableResearchType.Add(type);
            }
        }

        #endregion

        #region Upgrade Existing Stats

        private void UpgradeExistingStats(ResearchType researchType)
        {
            switch (researchType)
            {
                case ResearchType.LevelUpCarryingCapacity:
                    workerManager.LevelUpCarryingCapacity(researchDatabase.GetUpgradedStat(researchType));
                    break;

                case ResearchType.LevelUpWorkerHP:
                    workerManager.LevelUpWorkerHP(researchDatabase.GetUpgradedStat(researchType));
                    break;

                case ResearchType.LevelUpAllHPUnits:
                    workerManager.LevelUpWorkerHP(researchDatabase.GetUpgradedStat(researchType));
                    militaryUnitManager.LevelUpUnitHP(researchDatabase.GetUpgradedStat(researchType));
                    break;

                case ResearchType.LevelUpAllAttackPoint:
                    workerManager.LevelUpAttackPoint(researchDatabase.GetUpgradedStat(researchType));
                    militaryUnitManager.LevelUpAttackPoint(researchDatabase.GetUpgradedStat(researchType));
                    break;

                case ResearchType.LevelUpTowerDamage:
                    buildingManager.LevelUpTowerDamage(researchDatabase.GetUpgradedStat(researchType));
                    break;

                case ResearchType.LevelUpBaseArmor:
                    buildingManager.LevelUpBaseArmor(researchDatabase.GetUpgradedStat(researchType));
                    break;

                case ResearchType.LevelUpUnitTrainingSpeed:
                    buildingManager.LevelUpUnitTrainingSpeed(researchDatabase.GetUpgradedStat(researchType));
                    break;

                default:
                    break;
            }
        }

        #endregion

        #region Upgrade Stats

        public void UpgradeStats(BaseUnitController unitController)
        {
            if (unitController is WorkerUnitController)
                UpgradeWorkerUnitStats(unitController);

            if (unitController is MilitaryUnitController)
                UpgradeMilitaryUnitStats(unitController);
        }

        public void UpgradeStats(BaseBuildingController buildingController)
        {
            if (buildingController is GuardTowerController
                || buildingController is CannonTowerController)
            {
                UpgradeTowerStats(buildingController);
            }

            if (buildingController is TownCenterController
                || buildingController is GuardTowerController
                || buildingController is CannonTowerController
                || buildingController is WallController)
            {
                UpgradeBaseBuildingArmorStats(buildingController);
            }

            if (buildingController is IUnitTrainer)
            {
                UpgradeUnitTrainerStats(buildingController);
            }
        }

        #region Worker Unit

        private void UpgradeWorkerUnitStats(BaseUnitController unitController)
        {
            if (IsResearched(ResearchType.LevelUpCarryingCapacity))
            {
                float upgradedValue = unitController.GetUnitInfo().carryingCapacity * researchDatabase.GetUpgradedStat(ResearchType.LevelUpCarryingCapacity);
                unitController.GetUnitInfo().carryingCapacity = (int) upgradedValue;
            }

            if (IsResearched(ResearchType.LevelUpWorkerHP))
            {
                float multiplier = researchDatabase.GetUpgradedStat(ResearchType.LevelUpWorkerHP);
                unitController.GetUnitInfo().unitMaxHealth *= multiplier;
                unitController.GetUnitInfo().unitHealth = unitController.GetUnitInfo().unitMaxHealth;
            }

            if (IsResearched(ResearchType.LevelUpAllHPUnits))
            {
                float multiplier = researchDatabase.GetUpgradedStat(ResearchType.LevelUpAllHPUnits);
                unitController.GetUnitInfo().unitMaxHealth *= multiplier;
                unitController.GetUnitInfo().unitHealth = unitController.GetUnitInfo().unitMaxHealth;
            }

            if (IsResearched(ResearchType.LevelUpAllAttackPoint))
            {
                float upgradedValue = unitController.GetUnitInfo().attackDamage * researchDatabase.GetUpgradedStat(ResearchType.LevelUpAllAttackPoint);
                unitController.GetUnitInfo().attackDamage = (int) upgradedValue;
            }
        }

        #endregion

        #region Military Unit

        private void UpgradeMilitaryUnitStats(BaseUnitController unitController)
        {
            if (IsResearched(ResearchType.LevelUpAllHPUnits))
            {
                float multiplier = researchDatabase.GetUpgradedStat(ResearchType.LevelUpAllHPUnits);
                unitController.GetUnitInfo().unitMaxHealth *= multiplier;
                unitController.GetUnitInfo().unitHealth = unitController.GetUnitInfo().unitMaxHealth;
            }

            if (IsResearched(ResearchType.LevelUpAllAttackPoint))
            {
                float upgradedValue = unitController.GetUnitInfo().attackDamage * researchDatabase.GetUpgradedStat(ResearchType.LevelUpAllAttackPoint);
                unitController.GetUnitInfo().attackDamage = (int) upgradedValue;
            }
        }

        #endregion

        #region Tower & Building Armor

        private void UpgradeTowerStats(BaseBuildingController buildingController)
        {
            if (IsResearched(ResearchType.LevelUpTowerDamage))
            {
                float upgradedValue = buildingController.GetBuildingInfo().attackDamage * researchDatabase.GetUpgradedStat(ResearchType.LevelUpTowerDamage);
                buildingController.GetBuildingInfo().attackDamage = (int) upgradedValue;
            }
        }

        private void UpgradeBaseBuildingArmorStats(BaseBuildingController buildingController)
        {
            if (IsResearched(ResearchType.LevelUpBaseArmor))
            {
                float upgradedValue = buildingController.GetBuildingInfo().maxHitPoint * researchDatabase.GetUpgradedStat(ResearchType.LevelUpBaseArmor);
                buildingController.GetBuildingInfo().maxHitPoint = (int) upgradedValue;
                buildingController.GetBuildingInfo().currentHitPoint = buildingController.GetBuildingInfo().maxHitPoint;
            }
        }

        #endregion

        #region Unit Trainer

        private void UpgradeUnitTrainerStats(BaseBuildingController buildingController)
        {
            if (IsResearched(ResearchType.LevelUpUnitTrainingSpeed))
            {
                // placeholder
            }
        }

        #endregion

        #endregion

        #region Public API

        public void AddResearchedType(ResearchType type)
        {
            if (availableResearchType.Contains(type))
            {
                if (researchDatabase == null)
                    researchDatabase = playerInfo.DataManager.researchDatabase;

                availableResearchType.Remove(type);
                researchedType.Add(type);

                UpgradeExistingStats(type);
            } else
            {
                Debug.LogWarning($"Research type {type} is not available for research.");
            }
        }

        public bool IsResearched(ResearchType researchType)
        {
            return researchedType.Contains(researchType);
        }

        public bool TryStartResearch(ResearchType researchType, BaseBuildingController building)
        {
            bool canStartResearch = false;

            if (building == null)
                return false;

            if (building is IStatResearcher researcher)
            {
                canStartResearch = researcher.TryResearch(researchType);
            }else
            {
                Debug.LogWarning("This building cannot research");
            }

            return canStartResearch;
        }

        public List<ResearchType> GetAvailableResearchType()
        {
            return availableResearchType;
        }

        public List<ResearchType> GetResearchedType()
        {
            return researchedType;
        }

        public List<ResearchType> GetTotalResearch()
        {
            return totalResearch;
        }

        #endregion
    }
}