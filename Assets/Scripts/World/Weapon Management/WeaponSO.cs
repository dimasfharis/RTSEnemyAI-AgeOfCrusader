using RTS.Common.Enums;
using UnityEngine;

namespace RTS.World.WeaponManagement
{
    [CreateAssetMenu(
        fileName = "WeaponSO_",
        menuName = "RTS/World/Weapon SO"
    )]
    public class WeaponSO : ScriptableObject
    {
        [Header("Prefab")]
        public GameObject weaponPrefab;

        [Header("Identity")]
        public WeaponType weaponType;
    }
}