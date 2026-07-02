using RTS.Units.Worker;
using UnityEngine;

namespace RTS.Units.Common.States
{
    public class DepositingState : IUnitState
    {
        public void OnEnter(BaseUnitController unit)
        {
            if (unit is not WorkerUnitController worker)
                return;

            worker.DepositResource();
            Debug.Log("Terdeposit");

            worker.SetWorkerToGather(worker.GetCurrentResourceNode().GetResourceType());
        }

        public void Tick(BaseUnitController unit)
        {
            
        }

        public void OnExit(BaseUnitController unit)
        {
            
        }
    }
}