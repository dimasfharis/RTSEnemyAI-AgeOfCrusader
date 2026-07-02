using RTS.Core;
using RTS.Data;
using RTS.Common.Enums;
using UnityEngine.UIElements;

namespace RTS.AI.Resources
{
    public class ResourceManagementAIManager
    {
        private PlayerInfo playerInfo;
        private DataManager dataManager;

        #region Initialization

        public ResourceManagementAIManager(PlayerInfo owner)
        {
            playerInfo = owner;
            dataManager = owner.DataManager;
        }

        #endregion

        #region Loop

        public void Tick()
        {
            
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

                case AIStrategyMode.Attack:
                    FocusMilitaryResource();
                    break;

                case AIStrategyMode.Defend:
                    FocusDefensiveStability();
                    break;

                case AIStrategyMode.Recovery:
                    FocusFastRecovery();
                    break;
            }
        }

        #endregion

        #region Worker Production

        private void EnsureWorkerProduction()
        {
            int currentWorkers = playerInfo.WorkerManager.GetWorkerCount();

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

        private void FocusMilitaryResource()
        {
            
        }

        private void FocusDefensiveStability()
        {
            
        }

        private void FocusFastRecovery()
        {
            
        }

        #endregion
    }
}