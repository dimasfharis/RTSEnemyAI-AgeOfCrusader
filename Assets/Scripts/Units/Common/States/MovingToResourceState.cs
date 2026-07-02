using RTS.Units.Common;
using RTS.Units.Common.States;
using RTS.Units.Worker;
using RTS.World.ResourceNodeManagement;
using UnityEngine;

namespace RTS.Unit.Common.States
{
    public class MovingToResourceState : IUnitState
    {
        WorkerUnitController worker;
        private Vector3 currentEstResourceNodePos;
        private ResourceNode currentNearestResourceNode;
        private int maxRetryAttempts = 5;
        private bool hasReturnedToThisState = false;

        public MovingToResourceState(Vector3 estResourceNodePos)
        {
            currentEstResourceNodePos = estResourceNodePos;
        }

        public void OnEnter(BaseUnitController unit)
        {
            if (unit is not WorkerUnitController workerUnit)
                return;

            worker = unit as WorkerUnitController;

            if (currentEstResourceNodePos == Vector3.zero)
            {
                unit.ChangeState(new IdleState());
                return;
            }

            if (hasReturnedToThisState)
            {
                currentNearestResourceNode = worker.GetNearestResourceNodeObject();

                if (currentNearestResourceNode == null)
                {
                    unit.ChangeState(new IdleState());
                    return;
                }

                currentEstResourceNodePos = currentNearestResourceNode.GetPosition() + new Vector3(0.5f, 0.5f);
            }

            if (currentNearestResourceNode != null
                && worker.IsDestinationInRange(currentNearestResourceNode.GetPosition() + new Vector3(0.5f, 0.5f), 1.2f))
            {
                unit.ChangeState(new GatheringState(currentNearestResourceNode));
            }else
            {
                if (maxRetryAttempts <= 0)
                {
                    unit.ChangeState(new IdleState());
                    return;
                }

                maxRetryAttempts--;
                hasReturnedToThisState = false;

                worker.CommandMove(currentEstResourceNodePos, ReturnToThisState);
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
            hasReturnedToThisState = true;

            worker.ChangeState(this);
        }

        #endregion
    }
}