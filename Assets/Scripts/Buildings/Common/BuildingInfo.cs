using System.Collections.Generic;
using RTS.Common.Enums;
using RTS.Common.Structs;
using RTS.Buildings.Data;
using UnityEngine;
using RTS.Core;

namespace RTS.Buildings.Common
{
    [System.Serializable]
    public class BuildingInfo
    {
        [Header("Identity")]
        public PlayerInfo playerInfo;
        public BuildingType buildingType;
        public BuildingCategory buildingCategory;

        [Header("Dimension")]
        public int length;
        public int width;

        [Header("Unit Spawn Point")]
        public Vector3 unitSpawnPoint;

        [Header("Cost")]
        public List<ResourceAmount> buildingCostList;

        [Header("Construction")]
        public bool isConstructed;
        public float buildProgress;
        public float buildTime;

        [Header("Health")]
        public bool isActive;
        public bool isDestroyed;
        public int maxHitPoint;
        public int currentHitPoint;

        [Header("Line of Sight")]
        public float lineOfSightRange;

        [Header("Defense")]
        public bool canAttack;
        public int attackDamage;
        public float attackAreaOfEffectRadius;
        public float attackRange;
        public float attackCooldown;
        public int defensePower;

        [Header("Economy")]
        public bool isResourceDeposit;
        public List<ResourceType> acceptedResources;

        [Header("Population")]
        public int populationProvided;

        [Header("Production")]
        public bool canTrainUnit;
        public List<UnitType> trainableUnits;

        #region Initialization

        public BuildingInfo(PlayerInfo owner, BuildingInfoSO template)
        {
            playerInfo = owner;

            InitFromTemplate(template);
        }

        private void InitFromTemplate(BuildingInfoSO so)
        {
            // Identity
            buildingType = so.buildingType;
            buildingCategory = so.buildingCategory;

            // Dimension
            length = so.length;
            width = so.width;

            // Unit Spawn Point
            unitSpawnPoint = Vector3.down;

            // Cost
            buildingCostList = new List<ResourceAmount>(so.buildingCostList);

            // Construction
            isConstructed = false;
            buildTime = so.buildTime;

            // Health
            isActive = false;
            isDestroyed = false;
            maxHitPoint = so.maxHitPoint;
            currentHitPoint = maxHitPoint;

            // Line of Sight
            lineOfSightRange = so.lineOfSightRange;

            // Defense
            canAttack = so.canAttack;
            attackDamage = so.attackDamage;
            attackAreaOfEffectRadius = so.attackAreaOfEffectRadius;
            attackRange = so.attackRange;
            attackCooldown = so.attackCooldown;
            defensePower = so.defensePower;

            // Economy
            isResourceDeposit = so.isResourceDeposit;
            acceptedResources = new List<ResourceType>(so.acceptedResources);

            // Population
            populationProvided = so.populationProvided;

            // Production
            canTrainUnit = so.canTrainUnit;
            trainableUnits = new List<UnitType>(so.trainableUnits);
        }

        #endregion

        #region Public API

        public bool IsAlive()
        {
            return currentHitPoint > 0;
        }

        public float GetBuildProgressNormalized()
        {
            if (buildTime <= 0f) return 1f;
            return Mathf.Clamp01(buildProgress / buildTime);
        }

        public bool CanAcceptResource(ResourceType resourceType)
        {
            return isResourceDeposit && acceptedResources.Contains(resourceType);
        }

        public bool CanAcceptUnitTrain(UnitType unitType)
        {
            foreach (UnitType unit in trainableUnits)
            {
                if (unit == unitType) return true;
            }

            return false;
        }

        #endregion
    }
}

