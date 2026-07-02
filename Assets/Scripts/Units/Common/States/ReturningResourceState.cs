using RTS.Buildings.Common;
using RTS.Units.Common;
using RTS.Units.Worker;
using UnityEngine;

namespace RTS.Units.Common.States
{
    public class ReturningResourceState : IUnitState
    {
        WorkerUnitController worker;
        private int maxRetryAttempts = 5;

        public void OnEnter(BaseUnitController unit)
        {
            if (unit is not WorkerUnitController workerUnit)
                return;

            Debug.Log("masuk returning resource state");

            worker = unit as WorkerUnitController;

            BaseBuildingController depositBuilding = worker.GetDepositBuilding();

            if (depositBuilding == null)
            {
                Debug.LogWarning("Worker has no deposit building assigned. Returning to IdleState.");
                unit.ChangeState(new IdleState());
                return;
            }

            Vector3 nearestDepositBuildingTile = unit.GetNearestDepositBuildingTilePos(depositBuilding);

            if (nearestDepositBuildingTile != Vector3.zero
                && worker.IsDestinationInRange(nearestDepositBuildingTile, 1.2f))
            {
                unit.ChangeState(new DepositingState());
            }else
            {
                if (maxRetryAttempts <= 0)
                {
                    Debug.LogWarning("Failed to move to deposit building after multiple attempts");
                    unit.ChangeState(new IdleState());
                    return;
                }

                maxRetryAttempts--;
                worker.CommandMove(nearestDepositBuildingTile, ReturnToThisState);
            }
        }

        public void Tick(BaseUnitController unit)
        {
            
        }

        public void OnExit(BaseUnitController unit)
        {
            
        }

        #region Helper

        private void ReturnToThisState()
        {
            worker.ChangeState(this);
        }

        #endregion
    }
}