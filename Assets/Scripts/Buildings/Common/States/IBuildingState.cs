using RTS.Buildings.Common;

namespace RTS.Buildings.Common.States
{
    public interface IBuildingState
    {
        void OnEnter(BaseBuildingController building);
        void Tick(BaseBuildingController building);
        void OnExit(BaseBuildingController building);
    }
}