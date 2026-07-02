using RTS.Common.Enums;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.World.WeaponManagement
{
    public class WeaponDatabase : MonoBehaviour
    {
        [Header("Weapon Templates")]
        [SerializeField] private List<WeaponSO> weaponTemplates;

        private Dictionary<WeaponType, WeaponSO> weaponDictionary;

        #region Initialization

        private void Awake()
        {
            InitializeDictionary();
        }

        private void InitializeDictionary()
        {
            weaponDictionary = new Dictionary<WeaponType, WeaponSO>();

            foreach (var template in weaponTemplates)
            {
                if (template == null)
                    continue;

                if (weaponDictionary.ContainsKey(template.weaponType))
                {
                    Debug.LogWarning($"Duplicat WeaponType detected: {template.weaponType}");
                    continue;
                }

                weaponDictionary.Add(template.weaponType, template);
            }
        }

        #endregion

        #region Public Methods

        public WeaponSO GetWeaponTemplate(WeaponType weaponType)
        {
            if (weaponDictionary == null)
            {
                Debug.LogError("WeaponDatabase not initialized.");
                return null;
            }

            if (!weaponDictionary.TryGetValue(weaponType, out var template))
            {
                Debug.LogError($"WeaponType not found in database: {weaponType}");
                return null;
            }

            return template;
        }

        #endregion
    }
}