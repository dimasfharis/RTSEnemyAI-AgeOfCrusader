using RTS.AI.Behavior;
using RTS.Buildings.Common;
using RTS.Common.Enums;
using RTS.Core;
using RTS.Data;
using RTS.Managers;
using RTS.Buildings.Common.Interfaces;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.AI.GoalManagement
{
    public class ProductionGoalExecutor
    {
        private PlayerInfo playerInfo;
        private ResourceManager resourceManager;
        private DataManager dataManager;
        private BuildingManager buildingManager;

        private const int MAX_PARALLEL_TRAIN = 3;

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

            /*int activeTraining = 0;

            if (activeTraining >= MAX_PARALLEL_TRAIN)
                return;

            bool started = TryExecuteTrain(goal);

            if (started)
                activeTraining++;*/
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

        }

        private bool TryExecuteTrain(AIGoal goal)
        {
            // Get Unit Type to be trained
            UnitType unitType = goal.UnitType;

            // Unit Production Check
            List<BaseBuildingController> allBuildings = buildingManager.GetAllBuildings();
            foreach (BaseBuildingController building in allBuildings)
            {
                if (building is IUnitTrainer unitTrainer)
                {
                    bool success = buildingManager.TryTrainUnit(unitType, building);

                    if (success)
                    {
                        goal.AddProgress(1);

                        if (goal.currentProgress >= goal.targetAmount)
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