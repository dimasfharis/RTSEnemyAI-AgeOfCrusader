using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using RTS.SystemManagement.GridSystem;
using RTS.World.WorldManagement;

namespace RTS.AI.Pathfinding.UnitPathfindingController
{
    public class FlowfieldGenerator
    {
        public static FlowfieldGenerator Instance { get; private set; }

        private Tilemap impassibleTilemap;
        private Tilemap buildingTilemap;
        private Tilemap resourceNodeTilemap;

        private int width;
        private int height;

        public Grid<PathNode> curFlowfieldGrid;
        public PathNode destinationPathNode;

        #region Initialization

        public FlowfieldGenerator(int _width, int _height, WorldManager worldManager)
        {
            if (Instance != null)
                Debug.LogWarning("FlowfieldGenerator has been instantiated");

            Instance = this;

            impassibleTilemap = worldManager.tileDatabase.GetImpassibleTilemap();
            buildingTilemap = worldManager.tileDatabase.GetBuildingTilemap();
            resourceNodeTilemap = worldManager.tileDatabase.GetResourceNodeTilemap();

            width = _width;
            height = _height;
        }

        #endregion

        #region Flow Field Generation

        public Grid<PathNode> GenerateFlowField(Vector3 destination)
        {
            ResetCurrentFlowfield();
            ResetBestCostValue();
            CreateCostField();

            destination = GetNearestWalkablePathNodePos(destination);

            CreateIntegrationField(destination);
            CreateFlowfield();

            return curFlowfieldGrid;
        }

        #endregion

        #region Flow Field Processing

        private void ResetCurrentFlowfield()
        {
            curFlowfieldGrid = new Grid<PathNode>(
                width,
                height,
                1f,
                Vector3.zero,
                (Grid<PathNode> g, int x, int y) => new PathNode(g, x, y));
        }

        private void ResetBestCostValue()
        {
            for (int x = 0; x < curFlowfieldGrid.GetWidth(); x++)
            {
                for (int y = 0; y < curFlowfieldGrid.GetHeight(); y++)
                {
                    PathNode curNode = curFlowfieldGrid.GetGridObject(x, y);
                    curNode.bestCost = ushort.MaxValue;
                }
            }
        }

        private void CreateCostField()
        {
            for (int x = 0; x < curFlowfieldGrid.GetWidth(); x++)
            {
                for (int y = 0; y < curFlowfieldGrid.GetHeight(); y++)
                {
                    Vector3 pos = curFlowfieldGrid.GetWorldPosition(x, y);
                    Vector3Int position = Vector3Int.FloorToInt(pos);

                    if (impassibleTilemap.HasTile(position)
                        || buildingTilemap.HasTile(position)
                        || resourceNodeTilemap.HasTile(position)
                       )
                    {
                        curFlowfieldGrid.GetGridObject(x, y).isWalkable = false;
                        curFlowfieldGrid.GetGridObject(x, y).IncreaseCost(255);
                        continue;
                    }
                }
            }
        }

        private void CreateIntegrationField(Vector3 destination)
        {
            destinationPathNode = curFlowfieldGrid.GetGridObject(destination);

            destinationPathNode.cost = 0;
            destinationPathNode.bestCost = 0;

            Queue<PathNode> nodesToCheck = new Queue<PathNode>();

            nodesToCheck.Enqueue(destinationPathNode);

            while (nodesToCheck.Count > 0)
            {
                PathNode curNode = nodesToCheck.Dequeue();
                List<PathNode> curNeighbours = GetNeighbourNodes(curNode.gridIndex, GridDirection.CardinalDirections);
                foreach (PathNode curNeighbour in curNeighbours)
                {
                    if (curNeighbour.cost == byte.MaxValue)
                    {
                        continue;
                    }
                    if (curNeighbour.cost + curNode.bestCost < curNeighbour.bestCost)
                    {
                        curNeighbour.bestCost = (ushort)(curNeighbour.cost + curNode.bestCost);
                        nodesToCheck.Enqueue(curNeighbour);
                    }
                }
            }
        }

        private void CreateFlowfield()
        {
            for (int x = 0; x < curFlowfieldGrid.GetWidth(); x++)
            {
                for (int y = 0; y < curFlowfieldGrid.GetHeight(); y++)
                {
                    PathNode curNode = curFlowfieldGrid.GetGridObject(x, y);
                    List<PathNode> curNeighbours = GetNeighbourNodes(curNode.gridIndex, GridDirection.AllDirections);

                    int bestCost = curNode.bestCost;

                    foreach (PathNode curNeighbour in curNeighbours)
                    {
                        if (curNeighbour.bestCost < bestCost)
                        {
                            bestCost = curNeighbour.bestCost;
                            curNode.bestDirection = GridDirection.GetDirectionFromV2I(curNeighbour.gridIndex - curNode.gridIndex);
                        }
                    }
                }
            }
        }

        #endregion

        #region Helpers

        private List<PathNode> GetNeighbourNodes(Vector2Int nodeIndex, List<GridDirection> directions)
        {
            List<PathNode> neighbourNodes = new List<PathNode>();

            foreach (Vector2Int curDirection in directions)
            {
                PathNode newNeighbour = GetCellAtRelativePos(nodeIndex, curDirection);
                if (newNeighbour != null)
                {
                    neighbourNodes.Add(newNeighbour);
                }
            }
            return neighbourNodes;
        }

        private PathNode GetCellAtRelativePos(Vector2Int originPos, Vector2Int relativePos)
        {
            Vector2Int finalPos = originPos + relativePos;

            if (finalPos.x < 0
                || finalPos.x >= curFlowfieldGrid.GetWidth()
                || finalPos.y < 0
                || finalPos.y >= curFlowfieldGrid.GetHeight())
            {
                return null;
            }
            else
            {
                return curFlowfieldGrid.GetGridObject(finalPos.x, finalPos.y);
            }
        }

        private Vector3 GetNearestWalkablePathNodePos(Vector3 destination)
        {
            PathNode destinationPathNode = curFlowfieldGrid.GetGridObject(destination);

            if (destinationPathNode.isWalkable)
                return destination;

            List<PathNode> neighbourList = GetNeighbourList(destinationPathNode);
            List<PathNode> closedList = new List<PathNode>
            {
                destinationPathNode
            };

            PathNode nearestNode = null;
            bool isFoundNearest = false;
            int maxSearchIterations = 200;

            while (!isFoundNearest && maxSearchIterations > 0)
            {
                PathNode currentNode = neighbourList[0];

                if (currentNode.isWalkable)
                {
                    nearestNode = currentNode;
                    isFoundNearest = true;
                }
                else
                {
                    List<PathNode> newNeighbourList = GetNeighbourList(currentNode);

                    foreach (var newNeighbour in newNeighbourList)
                    {
                        if (!closedList.Contains(newNeighbour) && !neighbourList.Contains(newNeighbour))
                        {
                            neighbourList.Add(newNeighbour);
                        }
                    }

                    neighbourList.Remove(currentNode);
                    closedList.Add(currentNode);

                    maxSearchIterations--;
                }
            }

            if (nearestNode != null)
            {
                return curFlowfieldGrid.GetWorldPosition(nearestNode.x, nearestNode.y);
            }
            else
            {
                Debug.LogWarning("No walkable node found near destination");
                return Vector3.zero;
            }
        }

        private List<PathNode> GetNeighbourList(PathNode currentNode)
        {
            List<PathNode> neighbourList = new List<PathNode>();

            if (currentNode.x - 1 >= 0)
            {
                // Left
                neighbourList.Add(GetNode(currentNode.x - 1, currentNode.y));
                // Left Down
                if (currentNode.y - 1 >= 0)
                    neighbourList.Add(GetNode(currentNode.x - 1, currentNode.y - 1));
                // Left Up
                if (currentNode.y + 1 < curFlowfieldGrid.GetHeight())
                    neighbourList.Add(GetNode(currentNode.x - 1, currentNode.y + 1));
            }
            if (currentNode.x + 1 < curFlowfieldGrid.GetWidth())
            {
                // Right
                neighbourList.Add(GetNode(currentNode.x + 1, currentNode.y));
                // Right Down
                if (currentNode.y - 1 >= 0)
                    neighbourList.Add(GetNode(currentNode.x + 1, currentNode.y - 1));
                // Right Up
                if (currentNode.y + 1 < curFlowfieldGrid.GetHeight())
                    neighbourList.Add(GetNode(currentNode.x + 1, currentNode.y + 1));
            }
            // Down
            if (currentNode.y - 1 >= 0)
                neighbourList.Add(GetNode(currentNode.x, currentNode.y - 1));
            // Up
            if (currentNode.y + 1 < curFlowfieldGrid.GetHeight())
                neighbourList.Add(GetNode(currentNode.x, currentNode.y + 1));

            return neighbourList;
        }

        private PathNode GetNode(int x, int y)
        {
            return curFlowfieldGrid.GetGridObject(x, y);
        }

        #endregion
    }
}