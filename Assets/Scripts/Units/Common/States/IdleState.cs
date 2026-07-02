

using System.Diagnostics;

namespace RTS.Units.Common.States
{
    public class IdleState : IUnitState
    {
        public void OnEnter(BaseUnitController unit)
        {
            unit.PlayIdleAnimation();
            UnityEngine.Debug.Log("idle ya");
        }

        public void Tick(BaseUnitController unit)
        {
            // waiting orders
        }

        public void OnExit(BaseUnitController unit)
        {
            // no special exit logic
        }
    }
}

