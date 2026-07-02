using System.Collections.Generic;
using UnityEngine;
using RTS.Common.Enums;
using RTS.Core;
using RTS.World.WorldManagement;

namespace RTS.World.ResourceNodeManagement
{
    public class ResourceNodeManager
    {
        public GameManager gameManager;
        public WorldManager worldManager;

        public static ResourceNodeManager Instance { get; private set; }

        private readonly List<ResourceNode> resourceNodes;

        #region Initialization

        public ResourceNodeManager(GameManager gameManager)
        {
            if (Instance != null)
            {
                Debug.LogWarning("Instance already instantiated");
                return;
            }

            Instance = this;
            this.gameManager = gameManager;
            this.worldManager = gameManager.WorldManager;

            resourceNodes = new List<ResourceNode>();

            GetAllResourceNodesAtStartup();
        }

        #endregion

        #region Registration and Depletion

        public void RegisterNode(ResourceNode node)
        {
            if (!resourceNodes.Contains(node))
            {
                resourceNodes.Add(node);
                worldManager.TrySetResourceNodeTile(
                    new Vector3Int((int)node.GetPosition().x, (int)node.GetPosition().y),
                    node.GetResourceType());
            }
        }

        public void UnregisterNode(ResourceNode node)
        {
            if (resourceNodes.Contains(node))
            {
                resourceNodes.Remove(node);
            }
        }

        public void NotifyNodeDepleted(ResourceNode node)
        {
            UnregisterNode(node);
            GameObject.Destroy(node.gameObject);

            // MapManager.MakeTerrainWalkable();

        }

        #endregion

        #region Public API

        public List<ResourceNode> GetResourceNodesInRadius(Vector3 fromPosition, float radius)
        {
            List<ResourceNode> nodesInRadius = new List<ResourceNode>();

            foreach (var node in resourceNodes)
            {
                if (Vector3.Distance(fromPosition, node.GetPosition()) < radius)
                {
                    nodesInRadius.Add(node);
                }
            }

            return nodesInRadius;
        }

        public ResourceNode GetResourceNodeAtPosition(Vector3 position)
        {
            foreach (var node in resourceNodes)
            {
                if (Vector3.Distance(position, node.GetPosition()) < 0.1f)
                {
                    return node;
                }
            }

            return null;
        }

        public ResourceNode GetNearestResourceNodeInRadius(Vector3 fromPosition, float radius, ResourceType resourceType)
        {
            List<ResourceNode> nodesInRadius = GetResourceNodesInRadius(fromPosition, radius);
            ResourceNode nearestNode = null;

            foreach (var node in nodesInRadius)
            {
                if (node.GetResourceType() != resourceType)
                    continue;

                if (nearestNode == null)
                {
                    nearestNode = node;
                }
                else
                {
                    if (Vector3.Distance(fromPosition, node.GetPosition()) < Vector3.Distance(fromPosition, nearestNode.GetPosition()))
                        nearestNode = node;
                }
            }

            return nearestNode;
        }

        public ResourceNode GetNearestResourceNode(ResourceType resourceType, Vector3 fromPosition)
        {
            ResourceNode nearestResourceNode = null;

            foreach (var node in resourceNodes)
            {
                if (node.GetResourceType() != resourceType)
                    continue;

                if (nearestResourceNode == null)
                {
                    nearestResourceNode = node;
                } else
                {
                    if (Vector3.Distance(fromPosition, node.GetPosition()) < Vector3.Distance(fromPosition, nearestResourceNode.GetPosition()))
                        nearestResourceNode = node;
                }
            }

            return nearestResourceNode;
        }

        public int GetTotalActiveNodes()
        {
            return resourceNodes.Count;
        }

        #endregion

        #region Helper

        private void GetAllResourceNodesAtStartup()
        {
            ResourceNode[] nodes = GameManager.FindObjectsOfType<ResourceNode>();
            
            foreach (var node in nodes)
            {
                node.Init(this);
                RegisterNode(node);
            }
        }

        #endregion
    }
}

