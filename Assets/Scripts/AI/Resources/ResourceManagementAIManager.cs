using RTS.Core;
using RTS.Data;
using RTS.Common.Enums;
using RTS.AI.Behavior;
using System.Collections.Generic;
using RTS.AI.GoalManagement;
using UnityEngine;
using RTS.Common.Structs;
using RTS.Managers;

namespace RTS.AI.Resources
{
    public class ResourceManagementAIManager
    {
        private PlayerInfo playerInfo;
        private ResourceManager resourceManager;
        private DataManager dataManager;
        private GoalCoordinator goalCoordinator;

        private List<AIGoal> activeGoals;
        private float activeGoalsUpdateTimer;
        private const float activeGoalsUpdateInterval = 12f;

        private List<ResourceAmount> resourceNeeds;
        private Dictionary<ResourceType, float> currentResourceNeedsRatio;

        #region Initialization

        public ResourceManagementAIManager(PlayerInfo owner)
        {
            playerInfo = owner;
            resourceManager = playerInfo.ResourceManager;
            dataManager = owner.DataManager;

            activeGoals = new List<AIGoal>();
        }

        #endregion

        #region Loop

        public void Tick()
        {
            // Getting goalcoordinator if not yet
            if (goalCoordinator == null)
                goalCoordinator = playerInfo.AIManager.GetEnemyBehaviorAIManager().GetGoalCoordinator();

            UpdateResourceNeedsRatio();
        }

        #endregion

        #region Goal Priority Resource Needs

        private void UpdateResourceNeedsRatio()
        {
            // Update the list of active goals
            UpdateActiveGoals();

            // Extract resource needs from active goals
            resourceNeeds = ExtractResourceNeedsFromGoals();

            // Calculate the ratio of resource needs
            currentResourceNeedsRatio = CalculateResourceNeedsRatio(resourceNeeds);
        }

        private void UpdateActiveGoals()
        {
            activeGoalsUpdateTimer += Time.deltaTime;

            if (activeGoalsUpdateTimer >= activeGoalsUpdateInterval)
            {
                activeGoalsUpdateTimer = 0f;
                activeGoals = goalCoordinator.GetActiveGoals();
            }
        }

        private List<ResourceAmount> ExtractResourceNeedsFromGoals()
        {
            List<ResourceAmount> resourceNeeds = new List<ResourceAmount>();

            foreach (AIGoal goal in activeGoals)
            {
                List<ResourceAmount> needs = goal.GetResourceNeeds();

                resourceNeeds = resourceManager.AddResourceAmount(resourceNeeds, needs);
            }

            return resourceNeeds;
        }

        private Dictionary<ResourceType, float> CalculateResourceNeedsRatio(List<ResourceAmount> resourceNeeds)
        {
            Dictionary<ResourceType, float> needsRatio = new Dictionary<ResourceType, float>();
            float totalNeeds = 0f;

            foreach (ResourceAmount need in resourceNeeds)
            {
                totalNeeds += need.amount;
            }

            foreach (ResourceAmount need in resourceNeeds)
            {
                float ratio = (float)need.amount / totalNeeds;

                needsRatio[need.resourceType] = ratio;
            }

            return needsRatio;
        }

        #endregion

        #region Worker Distribution

        private void AdjustWorkerDistribution(AIStrategyMode mode)
        {
            switch (mode)
            {
                case AIStrategyMode.Economic:
                    FocusBalancedEconomy();
                    break;
            }
        }

        #endregion

        #region Worker Production

        private void EnsureWorkerProduction()
        {
            int currentWorkers = playerInfo.WorkerManager.GetAllUnits().Count;

            if (currentWorkers < 10) // Example threshold
            {
                
            }    
        }

        #endregion

        #region Mode Behaviors

        private void FocusBalancedEconomy()
        {
            var idleWorkers = playerInfo.WorkerManager.GetIdleWorkers();

            foreach (var worker in idleWorkers)
            {
                var targetResource = ChooseBalancedResource();
                var node = playerInfo.ResourceNodeManager.GetNearestResourceNode(targetResource, worker.transform.position);

                // playerInfo.WorkerManager.AssignWorkerToGather(worker, node);
            }
        }

        private ResourceType ChooseBalancedResource()
        {
            int food = playerInfo.ResourceManager.GetAmount(ResourceType.Food);
            int wood = playerInfo.ResourceManager.GetAmount(ResourceType.Wood);
            int gold = playerInfo.ResourceManager.GetAmount(ResourceType.Gold);
            int stone = playerInfo.ResourceManager.GetAmount(ResourceType.Stone);

            if (food < wood && food < gold)
                return ResourceType.Food;

            if (wood < gold)
                return ResourceType.Wood;

            if (stone < gold)
                return ResourceType.Stone;

            return ResourceType.Gold;
        }

        #endregion

        #region Public API

        public Dictionary<ResourceType, float> GetCurrentResourceNeedsRatio()
        {
            return currentResourceNeedsRatio;
        }

        public int GetAllResourceNeedsAmount()
        {
            int amount = 0;

            foreach (var resourceAmount in resourceNeeds)
            {
                amount += resourceAmount.amount;
            }

            return amount;
        }

        #endregion
    }
}