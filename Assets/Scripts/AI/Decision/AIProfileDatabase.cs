using RTS.Buildings.Data;
using RTS.Common.Enums;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.AI.Decision
{
    public class AIProfileDatabase : MonoBehaviour
    {
        [Header("AI Profile Templates")]
        [SerializeField] private List<AIProfileSO> aiProfileTemplates;

        private Dictionary<AIProfileType, AIProfileSO> aiProfileDictionary;

        #region Initialization

        private void Awake()
        {
            InitializeDictionary();
        }

        private void InitializeDictionary()
        {
            aiProfileDictionary = new Dictionary<AIProfileType, AIProfileSO>();

            foreach (var template in aiProfileTemplates)
            {
                if (template == null)
                    continue;

                if (aiProfileDictionary.ContainsKey(template.aiProfileType))
                {
                    Debug.LogWarning($"Duplicate AIProfileType detected: {template.aiProfileType}");
                    continue;
                }

                aiProfileDictionary.Add(template.aiProfileType, template);
            }
        }

        #endregion

        #region Public Methods

        public AIProfileSO GetAIProfileTemplate(AIProfileType aiProfileType)
        {
            if (aiProfileDictionary == null)
            {
                Debug.LogError("AIProfileDatabase not initialized.");
                return null;
            }

            if (!aiProfileDictionary.TryGetValue(aiProfileType, out var template))
            {
                Debug.LogError($"AIProfileType not found in database: {aiProfileType}");
                return null;
            }

            return template;
        }

        #endregion
    }
}