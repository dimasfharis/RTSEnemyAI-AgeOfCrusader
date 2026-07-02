using System;
using UnityEngine;

namespace RTS.Units.Common.States
{
    public class MoveState : IUnitState
    {
        public Action doAfterReached;
        private Vector3 destination;
        private bool isAfterReachedExecuted = false;

        public MoveState(Vector3 targetPos, Action doAfterReached = null)
        {
            destination = targetPos;

            this.doAfterReached = doAfterReached;
        }

        public void OnEnter(BaseUnitController unit)
        {
            unit.SetDestinationMove(destination);

            Debug.Log("unit masuk move state");
        }

        public void Tick(BaseUnitController unit)
        {
            // movement handled by unit controller
        }

        public void OnExit(BaseUnitController unit)
        {
            if (!isAfterReachedExecuted)
            {
                isAfterReachedExecuted = true;
                Debug.Log(unit.name + " has reached destination in MoveState OnExit, will invoke doAfterReached");
                doAfterReached?.Invoke();
            }
        }
    }
}

