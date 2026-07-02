using UnityEngine;
using System.Collections.Generic;
using RTS.Buildings.Common;
using RTS.Common.Enums;
using RTS.Buildings.Common.Interfaces;
using System;

namespace RTS.Buildings.Economic
{
    public class TownCenterController : BaseBuildingController, IUnitTrainer
    {
        public event Action<float, float> OnTrainingProgressChanged;
        public event Action<UnitType> OnUnitTrained;

        private Queue<UnitType> trainingQueue = new Queue<UnitType>();

        private float currentTrainingProgress = 0f;

        #region Update Lifecycle

        protected override void Update()
        {
            base.Update();

            HandleWorkerTraining();
        }

        #endregion

        #region Population

        public override void OnBuildingActivated()
        {
            if (playerInfo != null)
            {
                playerInfo.ResourceManager.AddPopulationCapacity(buildingInfo.populationProvided);
            }

            base.OnBuildingActivated();
        }

        public override void OnBuildingDestroyedAction()
        {
            if (playerInfo != null)
            {
                playerInfo.ResourceManager.RemovePopulationCapacity(buildingInfo.populationProvided);
            }

            base.OnBuildingDestroyedAction();
        }

        #endregion

        #region Unit Training

        public bool TryTrainUnit(UnitType unitType = UnitType.Worker)
        {
            if (!CanTrainWorker())
                return false;

            var cost = playerInfo.DataManager.unitDatabase.GetUnitCost(UnitType.Worker);
            bool canTrain = playerInfo.ResourceManager.SpendResource(cost);

            if (canTrain)
            {
                trainingQueue.Enqueue(unitType);
                return true;
            }

            return false;
        }

        public bool CanTrainWorker()
        {
            if (playerInfo.ResourceManager.IsPopulationFull())
                return false;

            var cost = playerInfo.DataManager.unitDatabase.GetUnitCost(UnitType.Worker);

            if (!playerInfo.ResourceManager.CanAfford(cost))
                return false;

            return true;
        }

        #endregion

        #region Unit Training Lifecycle

        private void HandleWorkerTraining()
        {
            if (trainingQueue.Count <= 0)
                return;

            currentTrainingProgress += Time.deltaTime;

            UnitType currentUnit = trainingQueue.Peek();
            float trainingTime = playerInfo.DataManager.unitDatabase.GetUnitTrainingTime(currentUnit);

            OnTrainingProgressChanged.Invoke(currentTrainingProgress, trainingTime);

            if (currentTrainingProgress >= trainingTime)
            {
                currentTrainingProgress = 0f;

                trainingQueue.Dequeue();
                SpawnWorker();

                OnUnitTrained.Invoke(currentUnit);
            }
        }

        private void SpawnWorker()
        {
            playerInfo.WorkerManager.SpawnUnitAtBuilding(this, UnitType.Worker);
        }

        #endregion
    }
}