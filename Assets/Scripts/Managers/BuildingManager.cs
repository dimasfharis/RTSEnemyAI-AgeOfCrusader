using RTS.Buildings.Common;
using RTS.Buildings.Common.Interfaces;
using RTS.Buildings.Data;
using RTS.Buildings.Defensive;
using RTS.Buildings.Economic;
using RTS.Common.Enums;
using RTS.Core;
using RTS.Managers.Research;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.Managers
{
    public class BuildingManager
    {
        private readonly PlayerInfo playerInfo;
        private readonly ResourceManager resourceManager;
        private ResearchManager researchManager;

        private readonly List<BaseBuildingController> buildings = new List<BaseBuildingController>();
        private readonly List<BaseBuildingController> instantiatedBuildings = new List<BaseBuildingController>();

        #region Initialization

        public BuildingManager(PlayerInfo playerInfo)
        {
            this.playerInfo = playerInfo;
            this.resourceManager = playerInfo.ResourceManager;
        }

        #endregion

        #region Register / Unregister

        public void RegisterBuilding(BaseBuildingController building)
        {
            if (buildings.Contains(building))
                return;

            buildings.Add(building);
        }

        public void UnregisterBuilding(BaseBuildingController building)
        {
            if (!buildings.Contains(building))
                return;

            buildings.Remove(building);

            // Remove building tile
            playerInfo.GameManager.WorldManager.RemoveBuildingTile(building.GetBuildingInfo().buildingType, building.transform.position);
        }

        #endregion

        #region Public API

        public List<BaseBuildingController> GetAllBuildings()
        {
            return buildings;
        }

        public List<BaseBuildingController> GetBuildingsInRadius(Vector3 position, float radius)
        {
            List<BaseBuildingController> buildingInRadius = new List<BaseBuildingController>();

            foreach (var building in buildings)
            {
                if (Vector3.Distance(position, building.transform.position) <= radius)
                {
                    buildingInRadius.Add(building);
                }
            }

            return buildingInRadius;
        }

        public BaseBuildingController GetBuildingAtPosition(Vector3 position)
        {
            foreach (var building in buildings)
            {
                if (Vector3.Distance(position, building.transform.position) < 0.1f)
                {
                    return building;
                }
            }

            return null;
        }

        public BaseBuildingController GetNearestBuilding(Vector3 fromPosition, BuildingCategory category)
        {
            BaseBuildingController nearest = null;
            float minDist = float.MaxValue;

            foreach (var building in buildings)
            {
                if (building.GetBuildingInfo().buildingCategory != category)
                    continue;

                float dist = Vector3.Distance(fromPosition, building.transform.position);

                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = building;
                }
            }

            return nearest;
        }

        public bool HasBuilding(BuildingType type)
        {
            foreach (var building in buildings)
            {
                if (building.GetBuildingInfo().buildingType == type)
                    return true;
            }
            return false;
        }

        public BaseBuildingController GetBuilding(BuildingType type)
        {
            foreach (var building in buildings)
            {
                if (building.GetBuildingInfo().buildingType == type)
                    return building;
            }

            Debug.LogWarning("Building not found");
            return null;
        }

        public BuildingCategory GetBuildingCategory(BuildingType type)
        {
            BuildingCategory category = playerInfo.DataManager.buildingDatabase.GetBuildingCategory(type);

            return category;
        }

        public int CountBuilding(BuildingType type)
        {
            int count = 0;
            foreach (var building in buildings)
            {
                if (building.GetBuildingInfo().buildingType == type)
                    count++;
            }
            return count;
        }

        public int GetBuildingCountByCategory(BuildingCategory category)
        {
            int count = 0;

            foreach (var b in buildings)
            {
                if (b.GetBuildingInfo().buildingCategory == category)
                {
                    count++;
                }
            }

            return count;
        }

        public BaseBuildingController GetBuildingByTilePos(Vector3 tilePos)
        {
            Vector3 tilePosFloored = new Vector3(Mathf.Floor(tilePos.x), Mathf.Floor(tilePos.y), Mathf.Floor(tilePos.z));

            foreach (var building in buildings)
            {
                Vector3 buildingTilePos = new Vector3(Mathf.Floor(building.transform.position.x), Mathf.Floor(building.transform.position.y), Mathf.Floor(building.transform.position.z));
                int buildingLength = building.GetBuildingInfo().length;
                int buildingWidth = building.GetBuildingInfo().width;

                if (tilePosFloored.x >= buildingTilePos.x
                    && tilePosFloored.x <= buildingTilePos.x + buildingLength
                    && tilePosFloored.y >= buildingTilePos.y
                    && tilePosFloored.y <= buildingTilePos.y + buildingWidth)
                {
                    return building;
                }
            }

            return null;
        }

        #endregion

        #region Building Construction

        public bool CanAfford(BuildingType type)
        {
            var cost = playerInfo.DataManager.buildingDatabase.GetBuildingCost(type);

            bool canAfford = resourceManager.CanAfford(cost);

            return canAfford;
        }

        private bool TrySetBuildingTile(BuildingType buildingType, Vector3 position)
        {
            Vector3Int position3Int = new Vector3Int((int)position.x, (int)position.y);
            if (playerInfo.GameManager.WorldManager.TrySetBuildingTile(buildingType, position3Int))
                return true;

            return false;
        }

        public void OnBuildingInstantiated(BaseBuildingController buildingController)
        {
            if (instantiatedBuildings.Contains(buildingController))
                return;

            instantiatedBuildings.Add(buildingController);

            // Set building tile
            TrySetBuildingTile(buildingController.GetBuildingInfo().buildingType, buildingController.transform.position);
        }

        public void OnBuildingConstructed(BaseBuildingController buildingController)
        {
            if (instantiatedBuildings.Contains(buildingController))
                instantiatedBuildings.Remove(buildingController);
        }

        public BaseBuildingController GetBuildingInstantiatedByTilePos(Vector3 tilePos)
        {
            Vector3 tilePosFloored = new Vector3(Mathf.Floor(tilePos.x), Mathf.Floor(tilePos.y), Mathf.Floor(tilePos.z));

            foreach (var building in instantiatedBuildings)
            {
                Vector3 buildingTilePos = new Vector3(Mathf.Floor(building.transform.position.x), Mathf.Floor(building.transform.position.y), Mathf.Floor(building.transform.position.z));
                int buildingLength = building.GetBuildingInfo().length;
                int buildingWidth = building.GetBuildingInfo().width;

                if (tilePosFloored.x >= buildingTilePos.x
                    && tilePosFloored.x <= buildingTilePos.x + buildingLength
                    && tilePosFloored.y >= buildingTilePos.y
                    && tilePosFloored.y <= buildingTilePos.y + buildingWidth)
                {
                    return building;
                }
            }

            return null;
        }

        #endregion

        #region Building Construction (for initial building)

        public bool TryPlaceBuilding(BuildingType type, Vector3 position)
        {
            BuildingInfoSO buildingTemplate = playerInfo.DataManager.buildingDatabase.GetBuildingTemplate(type);
            GameObject buildingGO = buildingTemplate.buildingPrefab;

            if (!TrySetBuildingTile(type, position))
            {
                Debug.LogWarning("Building placement tilemap is not clear");
                return false;
            }

            GameObject buildingObj = GameObject.Instantiate(buildingGO, position, Quaternion.identity);

            BaseBuildingController buildingController = buildingObj.GetComponent<BaseBuildingController>();

            if (buildingController == null)
            {
                Debug.LogError("Building prefab has no BaseBuildingController");
                return false;
            }

            buildingController.Init(playerInfo, buildingTemplate);

            if (researchManager == null)
                researchManager = playerInfo.ResearchManager;
            researchManager.UpgradeStats(buildingController);

            return true;
        }

        #endregion

        #region Required Unit Production Building

        public List<BaseBuildingController> GetRequiredProductionBuildings(Dictionary<UnitType, int> unitComposition)
        {
            List<BaseBuildingController> productionBuildings = new List<BaseBuildingController>();

            foreach (var unit in unitComposition)
            {
                if (HasRequiredProductionBuilding(unit.Key))
                {
                    productionBuildings.Add(GetRequiredProductionBuilding(unit.Key));
                }
            }

            return productionBuildings;
        }

        public BaseBuildingController GetRequiredProductionBuilding(UnitType unitType)
        {
            BuildingType buildingType = playerInfo.DataManager.unitDatabase.GetRequiredBuilding(unitType);

            if (HasBuilding(buildingType))
                return GetBuilding(buildingType);

            return null;
        }

        public bool HasRequiredProductionBuilding(Dictionary<UnitType, int> unitComposition)
        {
            foreach (var unit in unitComposition)
            {
                if (!HasRequiredProductionBuilding(unit.Key))
                    return false;
            }

            return true;
        }

        public bool HasRequiredProductionBuilding(UnitType unitType)
        {
            BuildingType buildingType = playerInfo.DataManager.unitDatabase.GetRequiredBuilding(unitType);

            if (HasBuilding(buildingType))
                return true;

            return false;
        }

        #endregion

        #region Unit Training

        public bool TryTrainUnit(BaseBuildingController building, UnitType unitType, int amount)
        {
            bool canTrain = false;

            if (building == null)
                return false;

            if (building is IUnitTrainer trainer && building.CanAcceptTrainUnit(unitType))
            {
                for (int i = 0; i < amount; i++)
                {
                    canTrain = trainer.TryTrainUnit(unitType);

                    if (!canTrain)
                    {
                        Debug.LogWarning("This building stop training at middle of progress!");
                        return false;
                    }
                }
            }else
            {
                Debug.LogWarning("This building cannot train units!");
            }

            return canTrain;
        }

        #endregion

        #region Resource Gathering

        public BaseBuildingController GetNearestDepositBuilding(Vector3 fromPosition, ResourceType resourceType)
        {
            BaseBuildingController nearest = null;
            float minDist = float.MaxValue;

            foreach (var building in buildings)
            {
                if (!building.GetBuildingInfo().CanAcceptResource(resourceType))
                    continue;

                float dist = Vector3.Distance(fromPosition, building.transform.position);

                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = building;
                }
            }
            return nearest;
        }

        #endregion

        #region Stats Research Upgrade

        public void LevelUpTowerDamage(float amount)
        {
            foreach (var building in buildings)
            {
                if (building is CannonTowerController || building is GuardTowerController)
                {
                    float upgradedValue = building.GetBuildingInfo().attackDamage * amount;
                    building.GetBuildingInfo().attackDamage = (int) upgradedValue;
                }
            }
        }

        public void LevelUpBaseArmor(float amount)
        {
            foreach (var building in buildings)
            {
                if (building is CannonTowerController
                    || building is GuardTowerController
                    || building is WallController
                    || building is TownCenterController)
                {
                    float upgradedValue = building.GetBuildingInfo().maxHitPoint * amount;
                    building.GetBuildingInfo().maxHitPoint = (int) upgradedValue;
                    building.GetBuildingInfo().currentHitPoint = building.GetBuildingInfo().maxHitPoint;
                }
            }
        }

        public void LevelUpUnitTrainingSpeed(float amount)
        {
            foreach (var building in buildings)
            {
                if (building is IUnitTrainer)
                {
                    // placeholder
                }
            }
        }

        #endregion
    }
}