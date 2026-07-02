using System;
using System.Collections.Generic;
using RTS.AI.Behavior;
using RTS.Common.Enums;
using RTS.Common.Structs;
using RTS.Core;
using UnityEngine;

namespace RTS.Managers
{
    public class ResourceManager
    {
        public static event Action<Dictionary<ResourceType, int>, PlayerInfo> OnResourceChanged;
        public static event Action<int, int, PlayerInfo> OnPopulationChanged;

        private readonly PlayerInfo playerInfo;

        private readonly Dictionary<ResourceType, int> resources =
            new Dictionary<ResourceType, int>();
        private readonly Dictionary<ResourceType, float> resourceIncomeRate =
            new Dictionary<ResourceType, float>();

        private float incomeUpdate = 0f;
        private float incomeUpdateInterval = 2f; // income rate is amount gathered per minute

        // ===== Population =====
        private int currentPopulation;
        private int populationCapacity;

        #region Initialization

        public ResourceManager(PlayerInfo playerInfo)
        {
            this.playerInfo = playerInfo;

            // init default values
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                resources[type] = 0;
            }

            currentPopulation = 0;
            populationCapacity = 0;
        }

        #endregion

        #region Update Loop

        public void Tick()
        {
            UpdateIncomeRate();
        }

        #endregion

        #region Income Calculation

        public void UpdateIncomeRate()
        {
            incomeUpdate += Time.deltaTime;

            if (incomeUpdate > incomeUpdateInterval)
            {
                incomeUpdate = 0f;

                resourceIncomeRate[ResourceType.Food] = GetAmount(ResourceType.Food) / incomeUpdateInterval;
                resourceIncomeRate[ResourceType.Wood] = GetAmount(ResourceType.Wood) / incomeUpdateInterval;
                resourceIncomeRate[ResourceType.Gold] = GetAmount(ResourceType.Gold) / incomeUpdateInterval;
                resourceIncomeRate[ResourceType.Stone] = GetAmount(ResourceType.Stone) / incomeUpdateInterval;
            }
        }

        #endregion

        #region Public API

        public int GetAmount(ResourceType type)
        {
            return resources[type];
        }

        public float GetIncomeRate(ResourceType type)
        {
            return resourceIncomeRate[type];
        }

        public List<ResourceAmount> GetProductionResourceAmount(Dictionary<UnitType, int> unitComposition)
        {
            if (unitComposition == null)
                return null;

            List<ResourceAmount> resourceAmounts = new List<ResourceAmount>();

            foreach (var unit in unitComposition)
            {
                var unitType = unit.Key;
                var unitTypeQty = unit.Value;

                List<ResourceAmount> costPerUnit = playerInfo.DataManager.unitDatabase.GetUnitCost(unitType);
                foreach (ResourceAmount c in costPerUnit)
                {
                    ResourceAmount resourceAmount = new ResourceAmount
                    {
                        resourceType = c.resourceType,
                        amount = c.amount * unitTypeQty
                    };

                    if (resourceAmounts.Exists(r => r.resourceType == resourceAmount.resourceType))
                    {
                        int newAmount = resourceAmount.amount + resourceAmounts.Find(r => r.resourceType == resourceAmount.resourceType).amount;
                        resourceAmounts.RemoveAll(r => r.resourceType == resourceAmount.resourceType);

                        resourceAmounts.Add(new ResourceAmount
                        {
                            resourceType = resourceAmount.resourceType,
                            amount = newAmount
                        });
                    }
                    else
                    {
                        resourceAmounts.Add(resourceAmount);
                    }
                }
            }

            return resourceAmounts;
        }

        public bool CanAfford(List<ResourceAmount> cost)
        {
            foreach (var resource in cost)
            {
                if (resources[resource.resourceType] < resource.amount)
                    return false;
            }
            return true;
        }

        public bool SpendResource(List<ResourceAmount> cost)
        {
            if (!CanAfford(cost))
                return false;

            foreach (var resource in cost)
            {
                resources[resource.resourceType] -= resource.amount;
            }

            // Debug & Monitoring Purpose
            UpdateResourceChanged();

            return true;
        }

        public void AddResource(ResourceType type, int amount)
        {
            if (amount <= 0) return;

            resources[type] += amount;

            // Debug & Monitoring Purpose
            UpdateResourceChanged();
        }

        #endregion

        #region Population

        public bool IsPopulationFull()
        {
            return currentPopulation >= populationCapacity;
        }

        public void AddPopulation(int amount)
        {
            currentPopulation += amount;

            // Debug & Monitoring Purpose
            UpdatePopulationChanged();
        }

        public void RemovePopulation(int amount)
        {
            currentPopulation -= amount;
            if (currentPopulation < 0)
                currentPopulation = 0;

            // Debug & Monitoring Purpose
            UpdatePopulationChanged();
        }

        public void AddPopulationCapacity(int amount)
        {
            populationCapacity += amount;

            // Debug & Monitoring Purpose
            UpdatePopulationChanged();
        }

        public void RemovePopulationCapacity(int amount)
        {
            populationCapacity -= amount;
            if (populationCapacity < 0)
                populationCapacity = 0;

            // Debug & Monitoring Purpose
            UpdatePopulationChanged();
        }

        public int GetCurrentPopulation()
        {
            return currentPopulation;
        }

        public int GetPopulationCapacity()
        {
            return populationCapacity;
        }

        #endregion

        #region Debug & Monitoring Purpose

        private void UpdateResourceChanged()
        {
            OnResourceChanged.Invoke(resources, playerInfo);
        }

        private void UpdatePopulationChanged()
        {
            OnPopulationChanged.Invoke(currentPopulation, populationCapacity, playerInfo);
        }

        #endregion
    }
}

