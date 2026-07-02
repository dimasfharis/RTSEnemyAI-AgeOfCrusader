using RTS.SystemManagement.GridSystem;
using RTS.World.WorldManagement;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RTS.AI.Pathfinding.UnitPathfindingController
{
    public class AStarGenerator
    {
        private const int MOVE_STRAIGHT_COST = 10;
        private const int MOVE_DIAGONAL_COST = 14;

        public static AStarGenerator Instance { get; private set; }

        private Tilemap impassibleTilemap;
        private Tilemap buildingTilemap;
        private Tilemap resourceNodeTilemap;

        private Grid<PathNode> grid;
        private List<PathNode> openList;
        private List<PathNode> closedList;

        #region Initialization

        public AStarGenerator(int width, int height, WorldManager worldManager)
        {
            if (Instance != null)
                Debug.LogWarning("AStarGenerator has been instantiated");

            Instance = this;

            impassibleTilemap = worldManager.tileDatabase.GetImpassibleTilemap();
            buildingTilemap = worldManager.tileDatabase.GetBuildingTilemap();
            resourceNodeTilemap = worldManager.tileDatabase.GetResourceNodeTilemap();

            grid = new Grid<PathNode>(
                width,
                height,
                1f,
                Vector3.zero,
                (Grid<PathNode> g, int x, int y) => new PathNode(g, x, y)
                );
        }

        #endregion

        #region Pathfinding

        public List<Vector3> FindPath(Vector3 startWorldPosition, Vector3 endWorldPosition)
        {
            endWorldPosition = GetNearestWalkablePathNodePos(startWorldPosition, endWorldPosition); // If destination is not walkable, find nearest walkable node and set it as new destination

            grid.GetXY(startWorldPosition, out int startX, out int startY);
            grid.GetXY(endWorldPosition, out int endX, out int endY);

            List<PathNode> path = FindPath(startX, startY, endX, endY);
            if (path == null)
            {
                Debug.LogWarning("Path is null");
                return null;
            } else
            {
                List<Vector3> vectorPath = new List<Vector3>();
                foreach (PathNode pathNode in path)
                {
                    vectorPath.Add(new Vector3(pathNode.x, pathNode.y) * grid.GetCellSize() + Vector3.one * grid.GetCellSize() * 0.5f);
                }
                return vectorPath;
            }
        }

        public List<PathNode> FindPath(int startX, int startY, int endX, int endY)
        {
            PathNode startNode = grid.GetGridObject(startX, startY);
            PathNode endNode = grid.GetGridObject(endX, endY);

            openList = new List<PathNode>() { startNode };
            closedList = new List<PathNode>();

            for (int x = 0; x < grid.GetWidth(); x++)
            {
                for (int y = 0; y < grid.GetHeight(); y++)
                {
                    if (IsImpassibleTile(x, y))
                        continue;

                    PathNode pathNode = grid.GetGridObject(x, y);
                    pathNode.gCost = int.MaxValue;
                    pathNode.CalculateFCost();
                    pathNode.cameFromNode = null;
                }
            }

            startNode.gCost = 0;
            startNode.hCost = CalculateDistanceCost(startNode, endNode);
            startNode.CalculateFCost();

            while (openList.Count > 0)
            {
                PathNode currentNode = GetLowestFCostNode(openList);
                if (currentNode == endNode)
                {
                    // Reached final node
                    return CalculatePath(endNode);
                }

                openList.Remove(currentNode);
                closedList.Add(currentNode);

                foreach (PathNode neighbourNode in GetNeighbourList(currentNode))
                {
                    if (closedList.Contains(neighbourNode)) continue;
                    if (!neighbourNode.isWalkable)
                    {
                        closedList.Add(neighbourNode);
                        continue;
                    }

                    int tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNode, neighbourNode);
                    if (tentativeGCost < neighbourNode.gCost)
                    {
                        neighbourNode.cameFromNode = currentNode;
                        neighbourNode.gCost = tentativeGCost;
                        neighbourNode.hCost = CalculateDistanceCost(neighbourNode, endNode);
                        neighbourNode.CalculateFCost();

                        if (!openList.Contains(neighbourNode))
                        {
                            openList.Add(neighbourNode);
                        }
                    }
                }
            }
            return null;
        }

        #endregion

        private Vector3 GetNearestWalkablePathNodePos(Vector3 startPosition, Vector3 endPosition)
        {
            PathNode destinationPathNode = grid.GetGridObject(endPosition);

            if (destinationPathNode.isWalkable)
                return endPosition;

            List<PathNode> neighbourList = GetNeighbourList(destinationPathNode);
            PathNode nearestNode = null;
            bool isFoundNearest = false;
            int maxSearchIterations = 150;

            while (!isFoundNearest && maxSearchIterations > 0)
            {
                PathNode currentNode = GetNearestNode(startPosition, neighbourList);

                if (currentNode.isWalkable)
                {
                    nearestNode = currentNode;
                    isFoundNearest = true;
                } else
                {
                    neighbourList = GetNeighbourList(currentNode);
                    maxSearchIterations--;
                }
            }

            if (nearestNode != null)
            {
                return grid.GetWorldPosition(nearestNode.x, nearestNode.y);
            } else
            {
                Debug.LogWarning("No walkable node found near destination");
                return Vector3.zero;
            }
        }

        private PathNode GetNearestNode(Vector3 startPosition, List<PathNode> neighbourList)
        {
            PathNode nearestNode = neighbourList[0];
            float nearestDistance = Vector3.Distance(startPosition, grid.GetWorldPosition(nearestNode.x, nearestNode.y));

            for (int i = 1; i < neighbourList.Count; i++)
            {
                float distance = Vector3.Distance(startPosition, grid.GetWorldPosition(neighbourList[i].x, neighbourList[i].y));
                if (distance < nearestDistance)
                {
                    nearestNode = neighbourList[i];
                    nearestDistance = distance;
                }
            }

            return nearestNode;
        }

        #region Public API

        public Grid<PathNode> GetGrid()
        {
            return grid;
        }

        #endregion

        #region Helper

        private bool IsImpassibleTile(int x, int y)
        {
            if (
                impassibleTilemap.HasTile(new Vector3Int(x, y))
                || buildingTilemap.HasTile(new Vector3Int(x, y))
                || resourceNodeTilemap.HasTile(new Vector3Int(x, y))
               )
            {
                PathNode pathNode = grid.GetGridObject(x, y);
                pathNode.gCost = int.MaxValue;
                pathNode.CalculateFCost();
                pathNode.cameFromNode = null;
                pathNode.isWalkable = false;

                return true;
            }

            return false;
        }

        private int CalculateDistanceCost(PathNode a, PathNode b)
        {
            int xDistance = Mathf.Abs(a.x - b.x);
            int yDistance = Mathf.Abs(a.y - b.y);
            int remaining = Mathf.Abs(xDistance - yDistance);

            return MOVE_DIAGONAL_COST * Mathf.Min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
        }

        private PathNode GetLowestFCostNode(List<PathNode> pathNodeList)
        {
            PathNode lowestFCostNode = pathNodeList[0];
            for (int i = 1; i < pathNodeList.Count; i++)
            {
                if (pathNodeList[i].fCost < lowestFCostNode.fCost)
                {
                    lowestFCostNode = pathNodeList[i];
                }
            }
            return lowestFCostNode;
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
                if (currentNode.y + 1 < grid.GetHeight())
                    neighbourList.Add(GetNode(currentNode.x - 1, currentNode.y + 1));
            }
            if (currentNode.x + 1 < grid.GetWidth())
            {
                // Right
                neighbourList.Add(GetNode(currentNode.x + 1, currentNode.y));
                // Right Down
                if (currentNode.y - 1 >= 0)
                    neighbourList.Add(GetNode(currentNode.x + 1, currentNode.y - 1));
                // Right Up
                if (currentNode.y + 1 < grid.GetHeight())
                    neighbourList.Add(GetNode(currentNode.x + 1, currentNode.y + 1));
            }
            // Down
            if (currentNode.y - 1 >= 0)
                neighbourList.Add(GetNode(currentNode.x, currentNode.y - 1));
            // Up
            if (currentNode.y + 1 < grid.GetHeight())
                neighbourList.Add(GetNode(currentNode.x, currentNode.y + 1));

            return neighbourList;
        }

        private PathNode GetNode(int x, int y)
        {
            return grid.GetGridObject(x, y);
        }

        private List<PathNode> CalculatePath(PathNode endNode)
        {
            List<PathNode> path = new List<PathNode>();
            path.Add(endNode);

            PathNode currentNode = endNode;
            while (currentNode.cameFromNode != null)
            {
                path.Add(currentNode.cameFromNode);
                currentNode = currentNode.cameFromNode;
            }

            path.Reverse();
            return path;
        }

        #endregion
    }
}