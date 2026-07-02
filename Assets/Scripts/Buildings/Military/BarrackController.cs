using RTS.Buildings.Common;
using RTS.Common.Enums;
using RTS.Buildings.Common.Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.Buildings.Military
{
    public class BarrackController : BaseBuildingController, IUnitTrainer
    {
        public event Action<float, float> OnTrainingProgressChanged;
        public event Action<UnitType> OnUnitTrained;

        private Queue<UnitType> trainingQueue = new Queue<UnitType>();

        private float currentTrainingProgress;

        #region Update Lifecycle

        protected override void Update()
        {
            base.Update();

            HandleUnitTraining();
        }

        #endregion

        #region Unit Training

        public bool TryTrainUnit(UnitType unitType)
        {
            if (!CanTrainUnit(unitType))
                return false;

            var cost = playerInfo.DataManager.unitDatabase.GetUnitCost(unitType);
            bool canTrain = playerInfo.ResourceManager.SpendResource(cost);

            if (canTrain)
            {
                trainingQueue.Enqueue(unitType);
                return true;
            }

            return false;
        }

        public bool CanTrainUnit(UnitType unitType)
        {
            if (playerInfo.ResourceManager.IsPopulationFull())
                return false;

            var cost = playerInfo.DataManager.unitDatabase.GetUnitCost(unitType);

            if (!playerInfo.ResourceManager.CanAfford(cost))
                return false;

            return true;
        }

        #endregion

        #region Unit Training Lifecycle

        private void HandleUnitTraining()
        {
            if (trainingQueue.Count <= 0)
                return;

            currentTrainingProgress += Time.deltaTime;

            UnitType currentType = trainingQueue.Peek();
            float trainingTime = playerInfo.DataManager.unitDatabase.GetUnitTrainingTime(currentType);

            OnTrainingProgressChanged.Invoke(currentTrainingProgress, trainingTime);

            if (currentTrainingProgress >= trainingTime)
            {
                currentTrainingProgress = 0f;

                trainingQueue.Dequeue();
                SpawnUnit(currentType);

                OnUnitTrained.Invoke(currentType);
            }
        }

        private void SpawnUnit(UnitType unitType)
        {
            playerInfo.MilitaryUnitManager.SpawnUnitAtBuilding(this, unitType);
        }

        #endregion
    }
}