using RTS.Common.Enums;
using UnityEngine;

namespace RTS.World.ResourceNodeManagement
{
    public class ResourceNode : MonoBehaviour
    {
        public int length { get; private set; }
        public int width { get; private set; }

        [SerializeField] private ResourceType resourceType;

        private int maxAmount;
        private int currentAmount;
        private ResourceNodeManager resourceNodeManager;

        #region Initialization

        public void Init(ResourceNodeManager owner)
        {
            resourceNodeManager = owner;
            currentAmount = maxAmount;

            ResourceNodeInfoInit();
        }

        private void ResourceNodeInfoInit()
        {
            ResourceNodeSO template = resourceNodeManager.worldManager.resourceNodeDatabase.GetResourceNodeTemplate(resourceType);

            maxAmount = template.maxAmount;
            currentAmount = maxAmount;
            length = template.length;
            width = template.width;
        }

        #endregion

        #region Public API

        public ResourceType GetResourceType()
        {
            return resourceType;
        }

        public Vector3 GetPosition()
        {
            return transform.position;
        }

        public bool IsDepleted()
        {
            return currentAmount <= 0;
        }

        public int Take(int amount)
        {
            if (IsDepleted())
                return 0;

            int harvestedAmount = Mathf.Min(amount, currentAmount);
            currentAmount -= harvestedAmount;

            if (IsDepleted())
            {
                resourceNodeManager.NotifyNodeDepleted(this);
            }

            return harvestedAmount;
        }

        #endregion
    }
}