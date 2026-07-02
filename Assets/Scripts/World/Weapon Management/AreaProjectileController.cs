using RTS.Buildings.Common;
using RTS.Core;
using RTS.Units.Common;
using RTS.World.WorldManagement;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.World.WeaponManagement
{
    public class AreaProjectileController : MonoBehaviour
    {
        PlayerInfo playerInfo;
        WorldManager worldManager;

        BaseUnitController unit;
        BaseBuildingController building;

        private Vector3 attackStartPos;
        private Vector3 attackDesPos;
        private float moveSpeed = 6f;

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
            float areaOfEffect = building ? building.GetBuildingInfo().attackAreaOfEffectRadius : 1f;
            int mainDamage = building ? building.GetBuildingInfo().attackDamage : unit.GetUnitInfo().attackDamage;
            List<BaseUnitController> unitsInRadius = worldManager.GetOpponentUnitsInRadius(playerInfo, transform.position, areaOfEffect);

            if (unitsInRadius.Count > 0)
            {
                foreach (BaseUnitController unit in unitsInRadius)
                {
                    int damage = CalculateAreaDamage(mainDamage, transform.position, unit.transform.position, areaOfEffect);

                    BaseUnitController unitAttacker = unit != null ? unit : null;

                    unit.ReceiveDamage(damage, unitAttacker);
                }

                return true;
            } else
            {
                return false;
            }
        }

        private bool TryGiveDamageToBuilding()
        {
            float areaOfEffect = building ? building.GetBuildingInfo().attackAreaOfEffectRadius : 1f;
            int mainDamage = building ? building.GetBuildingInfo().attackDamage : unit.GetUnitInfo().attackDamage;
            List<BaseBuildingController> buildingsInRadius = worldManager.GetOpponentBuildingsInRadius(playerInfo, transform.position, areaOfEffect);

            if (buildingsInRadius.Count > 0)
            {
                foreach (BaseBuildingController building in buildingsInRadius)
                {
                    int damage = CalculateAreaDamage(mainDamage, transform.position, building.transform.position, areaOfEffect);

                    building.ReceiveDamage(damage);
                }

                return true;
            } else
            {
                return false;
            }
        }

        private int CalculateAreaDamage(int mainDamage, Vector3 damageSource, Vector3 objectDamaged, float areaOfEffect)
        {
            float distance = Vector3.Distance(damageSource, objectDamaged);
            float multiplier = (areaOfEffect - distance) / areaOfEffect;

            return (int) (multiplier * mainDamage);
        }

        #endregion
    }
}