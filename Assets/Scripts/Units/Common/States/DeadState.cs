using UnityEngine;
using RTS.Units.Common;

namespace RTS.Units.Common.States
{
    public class DeadState : IUnitState
    {
        private float destroyDelay = 2f;
        private float timer;

        public void OnEnter(BaseUnitController unit)
        {
            timer = destroyDelay;

            unit.PlayDeadAnimation();

            unit.OnDead();
        }

        public void Tick(BaseUnitController unit)
        {
            /*timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                unit.OnDead();
            }*/
        }

        public void OnExit(BaseUnitController unit)
        {

        }
    }
}