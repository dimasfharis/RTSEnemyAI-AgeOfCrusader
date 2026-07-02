using RTS.Buildings.Common;
using RTS.Common.Enums;
using RTS.Units.Common;
using RTS.World.WorldManagement;
using UnityEngine;

namespace RTS.World.WeaponManagement
{
    public class WeaponManager
    {
        private WorldManager worldManager;
        private WeaponDatabase weaponDatabase;

        #region Initialization

        public WeaponManager(WorldManager worldManager)
        {
            this.worldManager = worldManager;

            weaponDatabase = GameObject.FindAnyObjectByType<WeaponDatabase>();
        }

        #endregion

        #region Single Projectile Shoot

        public void ShootSingleProjectile(BaseUnitController unit, Vector3 attackStartPos, Vector3 attackDesPos)
        {
            // Prefab Instantiation
            GameObject prefab = weaponDatabase.GetWeaponTemplate(WeaponType.SingleProjectile).weaponPrefab;
            GameObject singleProjectileGO = GameObject.Instantiate(prefab, attackStartPos, Quaternion.identity);

            // Controller Initialization
            var controller = singleProjectileGO.GetComponent<SingleProjectileController>();
            controller.Init(unit, attackStartPos, attackDesPos);
        }

        public void ShootSingleProjectile(BaseBuildingController building, Vector3 attackStartPos, Vector3 attackDesPos)
        {
            // Prefab Instantiation
            GameObject prefab = weaponDatabase.GetWeaponTemplate(WeaponType.SingleProjectile).weaponPrefab;
            GameObject singleProjectileGO = GameObject.Instantiate(prefab, attackStartPos, Quaternion.identity);

            // Controller Initialization
            var controller = singleProjectileGO.GetComponent<SingleProjectileController>();
            controller.Init(building, attackStartPos, attackDesPos);
        }

        #endregion

        #region Area Projectile Shoot

        public void ShootAreaProjectile(BaseUnitController unit, Vector3 attackStartPos, Vector3 attackDesPos)
        {
            // Prefab Instantiation
            GameObject prefab = weaponDatabase.GetWeaponTemplate(WeaponType.AreaProjectile).weaponPrefab;
            GameObject areaProjectileGO = GameObject.Instantiate(prefab, attackStartPos, Quaternion.identity);

            // Controller Initialization
            var controller = areaProjectileGO.GetComponent<AreaProjectileController>();
            controller.Init(unit, attackStartPos, attackDesPos);
        }

        public void ShootAreaProjectile(BaseBuildingController building, Vector3 attackStartPos, Vector3 attackDesPos)
        {
            // Prefab Instantiation
            GameObject prefab = weaponDatabase.GetWeaponTemplate(WeaponType.AreaProjectile).weaponPrefab;
            GameObject areaProjectileGO = GameObject.Instantiate(prefab, attackStartPos, Quaternion.identity);

            // Controller Initialization
            var controller = areaProjectileGO.GetComponent<AreaProjectileController>();
            controller.Init(building, attackStartPos, attackDesPos);
        }

        #endregion
    }
}