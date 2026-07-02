using RTS.Units.Common;
using RTS.Units.Worker;
using RTS.World;
using RTS.World.ResourceNodeManagement;
using System.Security.Cryptography;
using UnityEngine;

namespace RTS.Units.Common.States
{
    public class GatheringState : IUnitState
    {
        private ResourceNode currentResourceNode;
        private float gatherTimer;
        private const float gatherInterval = 1f;

        public GatheringState(ResourceNode resourceNode)
        {
            if (resourceNode == null)
            {
                Debug.LogWarning("Resource node is null. Cannot enter GatheringState.");
                return;
            }

            currentResourceNode = resourceNode;
        }

        public void OnEnter(BaseUnitController unit)
        {
            if (unit is not WorkerUnitController worker)
                return;

            worker.SetCurrentResourceNode(currentResourceNode);
            gatherTimer = gatherInterval;
        }

        public void Tick(BaseUnitController unit)
        {
            if (unit is not WorkerUnitController worker)
                return;

            if (worker.GetCurrentResourceNode() == null)
            {
                unit.ChangeState(new IdleState());
                return;
            }

            gatherTimer -= Time.deltaTime;

            if (gatherTimer <= 0)
            {
                gatherTimer = gatherInterval;

                int gatheredAmount = worker.GatherResource();

                if (gatheredAmount <= 0 || worker.IsCarryFull())
                {
                    unit.ChangeState(new ReturningResourceState());
                }
            }
        }

        public void OnExit(BaseUnitController unit)
        {
            // No special cleanup needed on exit
        }
    }
}