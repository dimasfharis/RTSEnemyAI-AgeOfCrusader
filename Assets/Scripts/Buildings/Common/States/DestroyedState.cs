using UnityEngine;
using RTS.Buildings.Common;

namespace RTS.Buildings.Common.States
{
    public class DestroyedState : IBuildingState
    {
        public void OnEnter(BaseBuildingController building)
        {
            building.OnBuildingDestroyedAction();
        }

        public void Tick(BaseBuildingController building)
        {
            
        }

        public void OnExit(BaseBuildingController building)
        {
            
        }
    }
}