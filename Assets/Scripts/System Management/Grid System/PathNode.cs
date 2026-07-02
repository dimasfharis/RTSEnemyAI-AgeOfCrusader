using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.SystemManagement.GridSystem
{
    public class PathNode
    {
        private Grid<PathNode> grid;
        public int x;
        public int y;
        public Vector2Int gridIndex;

        public int gCost;
        public int hCost;
        public int fCost;

        public byte cost;
        public ushort bestCost;
        public GridDirection bestDirection;

        public bool isWalkable;
        public PathNode cameFromNode;

        public PathNode(Grid<PathNode> grid, int x, int y, bool isWalkable = true)
        {
            this.grid = grid;
            this.x = x;
            this.y = y;
            this.isWalkable = isWalkable;

            this.gridIndex = new Vector2Int(x, y);
            cost = 1;
            bestCost = ushort.MaxValue;
            bestDirection = GridDirection.None;
        }

        public void CalculateFCost()
        {
            fCost = gCost + hCost;
        }

        public void IncreaseCost(int amount)
        {
            if (cost == byte.MaxValue)
                return;
            if (amount + cost >= 255)
                cost = byte.MaxValue;
            else
                cost += (byte)amount;
        }
    }
}