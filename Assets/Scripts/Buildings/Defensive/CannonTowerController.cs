using RTS.Buildings.Common;
using RTS.Buildings.Data;
using RTS.Core;
using RTS.Units.Common;
using RTS.World.WeaponManagement;
using RTS.World.WorldManagement;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.Buildings.Defensive
{
    public class CannonTowerController : BaseBuildingController
    {
        private WorldManager worldManager;
        private WeaponManager weaponManager;

        private float currentTimeToAttack;

        #region Initialization

        public override void Init(PlayerInfo owner, BuildingInfoSO template)
        {
            base.Init(owner, template);

            worldManager = playerInfo.GameManager.WorldManager;
            weaponManager = worldManager.weaponManager;
        }

        #endregion

        #region Update Lifecycle

        protected override void Update()
        {
            base.Update();
            HandleAttackEnemyUnit();
        }

        #endregion

        #region Attack Logic

        private void HandleAttackEnemyUnit()
        {
            currentTimeToAttack += Time.deltaTime;

            if (currentTimeToAttack >= buildingInfo.attackCooldown)
            {
                currentTimeToAttack = 0;

                BaseUnitController nearestOpponentUnit = GetNearestOpponentUnit();

                if (nearestOpponentUnit != null)
                {
                    weaponManager.ShootAreaProjectile(this, transform.position, nearestOpponentUnit.transform.position);
                }
            }
        }

        private BaseUnitController GetNearestOpponentUnit()
        {
            List<BaseUnitController> unitsInRadius = worldManager.GetOpponentUnitsInRadius(playerInfo, transform.position, buildingInfo.attackRange);

            BaseUnitController nearestUnit = null;
            float nearestDistance = float.MaxValue;

            if (unitsInRadius.Count > 0)
            {
                foreach (BaseUnitController unit in unitsInRadius)
                {
                    float distance = Vector3.Distance(transform.position, unit.transform.position);

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestUnit = unit;
                    }
                }

                return nearestUnit;
            }
            else
            {
                return null;
            }
        }

        #endregion
    }
}