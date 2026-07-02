using RTS.Buildings.Common;
using UnityEngine;

namespace RTS.Units.Common.States
{
    public class AttackState : IUnitState
    {
        private BaseUnitController unitAttacker;
        private Vector3 currentEstPosTargetUnit;
        private Vector3 currentEstPosTargetBuilding;
        private BaseUnitController currentNearestTargetUnit;
        private BaseBuildingController currentNearestTargetBuilding;

        private int maxRetryAttempts = 5;
        private bool hasReturnedToThisState = false;
        private float attackCooldownTimer;

        public void OnEnter(BaseUnitController unit)
        {
            unitAttacker = unit;
            attackCooldownTimer = unit.GetUnitInfo().attackCooldown;

            if (unit.GetCurrentTargetUnit() == null && unit.GetCurrentTargetBuilding() == null)
            {
                unit.ChangeState(new IdleState());
                return;
            }

            if (unit.GetCurrentTargetUnit() != null)
            {
                currentEstPosTargetUnit = unit.GetCurrentTargetUnit().transform.position;
            }
            else
            {
                currentEstPosTargetBuilding = unit.GetCurrentTargetBuilding().transform.position;
            }

            if (hasReturnedToThisState)
            {
                if (unit.GetCurrentTargetUnit() != null)
                {
                    currentNearestTargetUnit = unit.GetMicromanagementUnitController().GetAttackPriorityOpponentUnit();
                    unit.SetCurrentTargetUnit(currentNearestTargetUnit);
                }
                else
                {
                    currentNearestTargetBuilding = unit.GetMicromanagementUnitController().GetAttackPriorityOpponentBuilding();
                    unit.SetCurrentTargetBuilding(currentNearestTargetBuilding);
                }
            }
        }

        public void Tick(BaseUnitController unit)
        {
            if (unit.GetCurrentTargetUnit() == null && unit.GetCurrentTargetBuilding() == null)
            {
                unit.ChangeState(new IdleState());
                return;
            }

            attackCooldownTimer -= Time.deltaTime;

            if (attackCooldownTimer > 0f)
                return;

            attackCooldownTimer = unit.GetUnitInfo().attackCooldown;

            if (currentNearestTargetUnit != null
                && unit.IsDestinationInRange(currentNearestTargetUnit.transform.position, unit.GetUnitInfo().attackRange))
            {
                unit.PerformAttack();
            }
            else if (currentNearestTargetBuilding != null
                && unit.IsDestinationInRange(currentNearestTargetBuilding.transform.position, unit.GetUnitInfo().attackRange))
            {
                unit.PerformAttack();
            }
            else
            {
                if (maxRetryAttempts <= 0)
                {
                    unit.ChangeState(new IdleState());
                    unit.ClearTarget();
                    return;
                }

                maxRetryAttempts--;
                hasReturnedToThisState = false;

                Vector3 moveDestination = Vector3.zero;

                if (currentNearestTargetUnit == null && currentNearestTargetBuilding == null)
                {
                    moveDestination = currentEstPosTargetUnit != Vector3.zero ? currentEstPosTargetUnit
                    : currentEstPosTargetBuilding;
                }else
                {
                    moveDestination = currentNearestTargetUnit ? currentNearestTargetUnit.transform.position
                        : currentNearestTargetBuilding.transform.position;
                }

                unit.CommandMove(moveDestination, ReturnToThisState);
            }
        }

        public void OnExit(BaseUnitController unit)
        {

        }

        #region Helper

        private void ReturnToThisState()
        {
            hasReturnedToThisState = true;

            unitAttacker.ChangeState(this);
        }

        #endregion
    }
}