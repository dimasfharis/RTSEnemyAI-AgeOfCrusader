using RTS.Buildings.Common;
using RTS.Core;
using RTS.Units.Common;
using RTS.World.WorldManagement;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.World.WeaponManagement
{
    public class SingleProjectileController : MonoBehaviour
    {
        PlayerInfo playerInfo;
        WorldManager worldManager;

        BaseUnitController unit;
        BaseBuildingController building;

        private Vector3 attackStartPos;
        private Vector3 attackDesPos;
        private float moveSpeed = 10f;

        #region Initialization

        public void Init(BaseUnitController unit, Vector3 attackStartPos, Vector3 attackDesPos)
        {
            this.unit = unit;
            this.playerInfo = unit.GetPlayerInfo();
            worldManager = playerInfo.GameManager.WorldManager;

            this.attackStartPos = attackStartPos;
            this.attackDesPos = attackDesPos;
        }

        public void Init(BaseBuildingController building, Vector3 attackStartPos, Vector3 attackDesPos)
        {
            this.building = building;
            this.playerInfo = building.GetPlayerInfo();
            worldManager = playerInfo.GameManager.WorldManager;

            this.attackStartPos = attackStartPos;
            this.attackDesPos = attackDesPos;
        }

        #endregion

        #region Projectile Update

        private void Update()
        {
            MoveProjectile();
        }

        #endregion

        #region Projectile Movement

        private void MoveProjectile()
        {
            Vector3 projectilePos = transform.position;
            projectilePos.z = 0;

            attackDesPos.z = 0;

            transform.position = Vector3.MoveTowards(projectilePos, attackDesPos, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, attackDesPos) < 0.05f)
            {
                OnDestinationReached();
            }
        }

        private void OnDestinationReached()
        {
            bool isSuccessful = TryGiveDamageToUnit();

            if (!isSuccessful)
                TryGiveDamageToBuilding();

            Destroy(gameObject);
        }

        #endregion

        #region Damage Dealing

        private bool TryGiveDamageToUnit()
        {
            List<BaseUnitController> unitsInRadius = worldManager.GetOpponentUnitsInRadius(playerInfo, transform.position, 0.6f);

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

                int attackDamage;

                if (unit == null && building == null)
                {
                    return false;
                } else if (unit != null)
                {
                    attackDamage = unit.GetUnitInfo().attackDamage;
                }
                else
                {
                    attackDamage = building.GetBuildingInfo().attackDamage;
                }

                nearestUnit.ReceiveDamage(attackDamage, unit ? unit : null);
                return true;
            } else
            {
                return false;
            }
        }

        private bool TryGiveDamageToBuilding()
        {
            List<BaseBuildingController> buildingsInRadius = worldManager.GetOpponentBuildingsInRadius(playerInfo, transform.position, 0.6f);
            
            BaseBuildingController nearestBuilding = null;
            float nearestDistance = float.MaxValue;
            
            if (buildingsInRadius.Count > 0)
            {
                foreach (BaseBuildingController building in buildingsInRadius)
                {
                    float distance = Vector3.Distance(transform.position, building.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestBuilding = building;
                    }
                }

                int attackDamage;

                if (unit == null && building == null)
                {
                    return false;
                }
                else if (unit != null)
                {
                    attackDamage = unit.GetUnitInfo().attackDamage;
                }
                else
                {
                    attackDamage = building.GetBuildingInfo().attackDamage;
                }

                nearestBuilding.ReceiveDamage(attackDamage);
                return true;
            } else
            {
                return false;
            }
        }

        #endregion
    }
}