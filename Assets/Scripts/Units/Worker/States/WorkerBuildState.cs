using UnityEngine;
using RTS.Units.Common.States;
using RTS.Common.Enums;
using RTS.Units.Common;
using RTS.Buildings.Common;

namespace RTS.Units.Worker.States
{
    public class WorkerBuildState : IUnitState
    {
        private WorkerUnitController workerUnit;
        private int maxRetryAttempts = 5;

        public void OnEnter(BaseUnitController unit)
        {
            if (unit is not WorkerUnitController worker)
                return;

            if (!worker.HasBuildingTemplate() || worker.GetCurrentBuildingPosition() == Vector3.zero)
            {
                Debug.LogWarning("Worker has no building template or position assigned. Returning to IdleState.");
                worker.ChangeState(new IdleState());
                return;
            }

            Vector3 nearestBuildingTilePos = worker.GetNearestBuildingTilePos(worker.GetCurrentBuildingPosition(), worker.GetCurrentBuildingTemplate());

            if (nearestBuildingTilePos != Vector3.zero
                && !worker.IsDestinationInRange(nearestBuildingTilePos, 1.2f))
            {
                if (maxRetryAttempts <= 0)
                {
                    Debug.LogWarning("Failed to move to building site after multiple attempts");
                    worker.ChangeState(new IdleState());
                    return;
                }

                workerUnit = worker;

                maxRetryAttempts--;
                worker.CommandMove(nearestBuildingTilePos, ReturnToThisState);
            }
        }

        public void Tick(BaseUnitController unit)
        {
            if (unit is not WorkerUnitController worker)
                return;

            if (!worker.HasBuildingTemplate() || worker.GetCurrentBuildingPosition() == Vector3.zero)
            {
                Debug.LogWarning("Worker has no building template or position assigned. Returning to IdleState.");
                worker.ChangeState(new IdleState());
                return;
            }

            Vector3 nearestBuildingTilePos = worker.GetNearestBuildingTilePos(worker.GetCurrentBuildingPosition(), worker.GetCurrentBuildingTemplate());

            if (nearestBuildingTilePos != Vector3.zero
                && !worker.IsDestinationInRange(nearestBuildingTilePos, 1.2f))
            {
                unit.transform.position = Vector3.MoveTowards(unit.transform.position, nearestBuildingTilePos, worker.GetUnitInfo().moveSpeed * Time.deltaTime);
                return;
            } else if (nearestBuildingTilePos == Vector3.zero)
            {
                Debug.LogWarning("No valid building tile found near the target position. Returning to IdleState.");
                worker.ChangeState(new IdleState());
                return;
            }

            BaseBuildingController buildingController = worker.GetBuildingInstantiatedByTilePos(nearestBuildingTilePos);

            if (buildingController == null)
            {
                BaseBuildingController constructedBuilding = worker.GetBuildingByTilePos(nearestBuildingTilePos);

                if (constructedBuilding != null)
                {
                    buildingController = constructedBuilding;
                } else
                {
                    worker.BuildCommand();
                    return; // important to wait for the building to be instantiated before trying to add progress
                }
            } else if (buildingController != null)
            {
                worker.curBaseBuildingController = buildingController;
                worker.isBuildingInstantiated = true;
            }

            if (buildingController.IsConstructed())
            {
                worker.ClearBuildTarget();
                worker.ChangeState(new IdleState());
                return;
            }

            buildingController.AddBuildProgress(worker.GetUnitInfo().buildSpeed * Time.deltaTime);
        }

        public void OnExit(BaseUnitController unit)
        {
            
        }

        #region Helper

        private void ReturnToThisState()
        {
            workerUnit.ChangeState(this);
        }

        #endregion
    }
}