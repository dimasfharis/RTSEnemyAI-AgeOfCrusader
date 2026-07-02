using UnityEngine;
using RTS.Buildings.Common;

namespace RTS.Buildings.Common.States
{
    public class UnderConstructionState : IBuildingState
    {
        public void OnEnter(BaseBuildingController building)
        {
            var info = building.GetBuildingInfo();
            info.isConstructed = false;
        }

        public void Tick(BaseBuildingController building)
        {
            
        }

        public void OnExit(BaseBuildingController building)
        {
            
        }
    }
}