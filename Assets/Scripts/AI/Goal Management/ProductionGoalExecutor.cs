using RTS.AI.Behavior;
using RTS.Buildings.Common;
using RTS.Buildings.Common.Interfaces;
using RTS.Common.Enums;
using RTS.Core;
using RTS.Data;
using RTS.Managers;
using System.Collections.Generic;

namespace RTS.AI.GoalManagement
{
    public class ProductionGoalExecutor
    {
        private PlayerInfo playerInfo;
        private ResourceManager resourceManager;
        private DataManager dataManager;
        private BuildingManager buildingManager;

        #region Initialization

        public ProductionGoalExecutor(PlayerInfo owner)
        {
            playerInfo = owner;
            resourceManager = owner.ResourceManager;
            dataManager = owner.DataManager;
            buildingManager = owner.BuildingManager;
        }

        #endregion

        #region Tick

        public void Tick()
        {

        }

        #endregion

        #region Execution

        public void Execute(AIGoal goal)
        {
            if (goal.GoalType != AIGoalType.TrainUnit)
                return;

            if (goal.IsCompleted)
                return;

            if (!CanExecute(goal))
            {
                FulfillRequirements(goal);
                return;
            }

            TryExecuteTrain(goal);
        }

        private bool CanExecute(AIGoal goal)
        {
            // Check if the player has enough resources to train the unit
            if (!resourceManager.CanAfford(resourceManager.GetProductionResourceAmount(goal.UnitTrainingRequirements)))
                return false;

            // Check if the player has the required building to train the unit
            if (!buildingManager.HasRequiredProductionBuilding(goal.UnitTrainingRequirements))
                return false;

            return true;
        }

        private void FulfillRequirements(AIGoal goal)
        {
            if (!buildingManager.HasRequiredProductionBuilding(goal.UnitTrainingRequirements))
            {
                // inform EnemyBehaviorAIManager to prioritize building needed
                // set building needed isDependedByOther true
                // for this train worker goal phase, just leave it this way
                // do for later military unit train goal
            }

            // fulfill population capacity if not yet fulfilled
        }

        private bool TryExecuteTrain(AIGoal goal)
        {
            // Get Building to train
            Dictionary<UnitType, int> unitComposition = goal.UnitTrainingRequirements;

            // Unit Production Check
            foreach (var unit in unitComposition)
            {
                BaseBuildingController productionBuilding = buildingManager.GetRequiredProductionBuilding(unit.Key);

                bool success = buildingManager.TryTrainUnit(productionBuilding, unit.Key, unit.Value);

                if (success)
                {
                    goal.AddProgress(unit.Value);

                    if (goal.currentProgress >= goal.targetAmount)
                    {
                        goal.MarkCompleted();
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion
    }
}