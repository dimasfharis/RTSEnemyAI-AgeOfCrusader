using RTS.AI.Behavior;
using RTS.Buildings.Common;
using RTS.Common.Enums;
using RTS.Core;
using RTS.Data;
using RTS.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RTS.AI.GoalManagement
{
    public class ProductionGoalExecutor
    {
        private PlayerInfo playerInfo;
        private ResourceManager resourceManager;
        private DataManager dataManager;
        private BuildingManager buildingManager;
        private GoalCoordinator goalCoordinator;
        private EnemyBehaviorAIManager enemyBehaviorAIManager;

        #region Initialization

        public ProductionGoalExecutor(PlayerInfo owner, GoalCoordinator goalCoordinator)
        {
            playerInfo = owner;
            resourceManager = owner.ResourceManager;
            dataManager = owner.DataManager;
            buildingManager = owner.BuildingManager;
            this.goalCoordinator = goalCoordinator;
            enemyBehaviorAIManager = goalCoordinator.GetEnemyBehaviorAIManager();
        }

        #endregion

        #region Tick

        public void Tick()
        {
            // check for unexecuted goal, try to execute again
        }

        #endregion

        #region Execution

        public void Execute(AIGoal goal)
        {
            if (goal.IsCompleted || goal.GoalType != AIGoalType.TrainUnit)
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

            // Check if the population is exceeding to train unit
            int totalUnitNeeds = goal.UnitTrainingRequirements.Values.Sum();
            if (resourceManager.IsPopulationExceedingToTrain(totalUnitNeeds))
                return false;

            return true;
        }

        private void FulfillRequirements(AIGoal goal)
        {
            // fulfill required building production
            if (!buildingManager.HasRequiredProductionBuilding(goal.UnitTrainingRequirements))
            {
                // set building needed isDependedByOther
                List<BuildingType> buildingProductionTypes = buildingManager.GetRequiredProductionBuildingTypes(goal.UnitTrainingRequirements);
                enemyBehaviorAIManager.SetBuildGoalDependency(buildingProductionTypes, goal);

                Debug.Log($"Player {playerInfo.PlayerNumber} Request military building to {goal.GoalType}");
            }

            // fulfill population capacity if not yet fulfilled
            int totalUnitNeeds = goal.UnitTrainingRequirements.Values.Sum();
            if (resourceManager.IsPopulationExceedingToTrain(totalUnitNeeds))
            {
                enemyBehaviorAIManager.SetBuildGoalDependency(BuildingType.House, goal);

                Debug.Log($"Player {playerInfo.PlayerNumber} Request house building to {goal.GoalType}");
            }
        }

        private bool TryExecuteTrain(AIGoal goal)
        {
            // Get unit composition to train
            Dictionary<UnitType, int> unitComposition = goal.UnitTrainingRequirements;

            // Unset goal dependency
            enemyBehaviorAIManager.UnsetBuildGoalDependency(goal);

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