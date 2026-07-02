using RTS.Common.Enums;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.World.ResourceNodeManagement
{
    public class ResourceNodeDatabase : MonoBehaviour
    {
        [Header("Resource Node Templates")]
        [SerializeField] private List<ResourceNodeSO> resourceNodeTemplates;

        private Dictionary<ResourceType, ResourceNodeSO> resourceNodeDictionary;

        #region Initialization

        private void Awake()
        {
            InitializeDictionary();
        }

        private void InitializeDictionary()
        {
            resourceNodeDictionary = new Dictionary<ResourceType, ResourceNodeSO>();

            foreach (var template in resourceNodeTemplates)
            {
                if (template == null)
                    continue;

                if (resourceNodeDictionary.ContainsKey(template.resourceType))
                {
                    Debug.LogWarning($"Duplicate ResourceType detected: {template.resourceType}");
                    continue;
                }

                resourceNodeDictionary.Add(template.resourceType, template);
            }
        }

        #endregion

        #region Public Methods

        public ResourceNodeSO GetResourceNodeTemplate(ResourceType resourceType)
        {
            if (resourceNodeDictionary == null)
            {
                Debug.LogError("ResourceNodeDatabase not initialized.");
                return null;
            }

            if (!resourceNodeDictionary.TryGetValue(resourceType, out var template))
            {
                Debug.LogError($"ResourceType not found in database: {resourceType}");
                return null;
            }

            return template;
        }

        #endregion
    }
}