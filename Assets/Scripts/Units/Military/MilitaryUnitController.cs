using UnityEngine;
using RTS.Units.Common;
using RTS.Core;
using RTS.Common.Enums;
using RTS.Units.Data;
using RTS.World.WeaponManagement;

namespace RTS.Units.Military
{
    public class MilitaryUnitController : BaseUnitController
    {
        private WeaponManager weaponManager;

        #region Initialization

        public override void Init(PlayerInfo owner, UnitInfoSO template)
        {
            base.Init(owner, template);
            weaponManager = playerInfo.GameManager.WorldManager.weaponManager;
        }

        #endregion

        #region Unit Combat

        public override void PerformAttack()
        {
            if (GetCurrentTargetUnit() == null && GetCurrentTargetBuilding() == null)
                return;

            if (GetCurrentTargetUnit() != null)
            {
                PerformAttackByUnitType();

                if (GetCurrentTargetUnit().GetUnitInfo().unitHealth <= 0f || GetCurrentTargetUnit() == null)
                    ClearTarget();
            }else
            {
                PerformAttackByUnitType();

                if (GetCurrentTargetBuilding().GetBuildingInfo().currentHitPoint <= 0f || GetCurrentTargetBuilding() == null)
                    ClearTarget();
            }
        }

        private void PerformAttackByUnitType()
        {
            switch (unitInfo.unitType)
            {
                case UnitType.Militia:
                case UnitType.Swordsman:
                    if (GetCurrentTargetUnit() != null)
                    {
                        GetCurrentTargetUnit().ReceiveDamage(unitInfo.attackDamage, this);
                    } else
                    {
                        GetCurrentTargetBuilding().ReceiveDamage(unitInfo.attackDamage);
                    }
                    break;

                case UnitType.Archer:
                case UnitType.Crossbowman:
                case UnitType.Scorpion:
                    if (GetCurrentTargetUnit() != null)
                    {
                        weaponManager.ShootSingleProjectile(this, transform.position, GetCurrentTargetUnit().transform.position);
                    }
                    else
                    {
                        weaponManager.ShootSingleProjectile(this, transform.position, GetCurrentTargetBuilding().transform.position);
                    }
                    break;

                case UnitType.Mangonel:
                    if (GetCurrentTargetUnit() != null)
                    {
                        weaponManager.ShootAreaProjectile(this, transform.position, GetCurrentTargetUnit().transform.position);
                    }
                    else
                    {
                        weaponManager.ShootAreaProjectile(this, transform.position, GetCurrentTargetBuilding().transform.position);
                    }
                    break;
            }
        }

        #endregion

        #region Health and Damage

        public override void OnDead()
        {
            playerInfo.MilitaryUnitManager.UnregisterUnit(this);

            base.OnDead();
        }

        #endregion

        #region Accessors

        public int GetAttackPower()
        {
            return unitInfo.attackPower;
        }

        #endregion
    }
}