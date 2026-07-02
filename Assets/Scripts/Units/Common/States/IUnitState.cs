

namespace RTS.Units.Common.States
{
    public interface IUnitState
    {
        void OnEnter(BaseUnitController unit);
        void Tick(BaseUnitController unit);
        void OnExit(BaseUnitController unit);
    }
}

