using RTS.Buildings.Common;

namespace RTS.Buildings.Common.States
{
    public class ActiveState : IBuildingState
    {
        public void OnEnter(BaseBuildingController building)
        {
            building.OnBuildingActivated();
        }

        public void Tick(BaseBuildingController building)
        {
            
        }

        public void OnExit(BaseBuildingController building)
        {
            
        }
    }
}