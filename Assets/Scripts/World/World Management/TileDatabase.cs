using RTS.Buildings.Common;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RTS.World.WorldManagement
{
    public class TileDatabase : MonoBehaviour
    {
        [Header("Tile Templates")]
        [SerializeField] private TileBase buildingTile;
        [SerializeField] private TileBase resourceNodeTile;

        [SerializeField] private Tilemap groundTilemap;
        [SerializeField] private Tilemap impassibleTilemap;
        [SerializeField] private Tilemap buildingTilemap;
        [SerializeField] private Tilemap resourceNodeTilemap;

        #region Scan Tilemap in Radius

        public List<Vector2Int> GetGroundTilemapInRadius(Vector3 worldPosition, float radius)
        {
            List<Vector2Int> groundTilePositions = new List<Vector2Int>();

            Vector3Int centerCell = groundTilemap.WorldToCell(worldPosition);
            int cellRadius = Mathf.CeilToInt(radius / groundTilemap.cellSize.x);

            for (int x = -cellRadius; x <= cellRadius; x++)
            {
                for (int y = -cellRadius; y <= cellRadius; y++)
                {
                    Vector3Int currentCell = new Vector3Int(centerCell.x + x, centerCell.y + y, centerCell.z);
                    if (groundTilemap.HasTile(currentCell))
                    {
                        Vector3 tileWorldPos = groundTilemap.CellToWorld(currentCell) + groundTilemap.cellSize / 2;
                        if (Vector3.Distance(tileWorldPos, worldPosition) <= radius)
                        {
                            groundTilePositions.Add(new Vector2Int(currentCell.x, currentCell.y));
                        }
                    }
                }
            }

            return groundTilePositions;
        }

        public List<Vector2> GetImpassibleTilemapInRadius(Vector3 worldPosition, float radius)
        {
            List<Vector2> impassibleTilePositions = new List<Vector2>();

            Vector3Int centerCell = impassibleTilemap.WorldToCell(worldPosition);
            int cellRadius = Mathf.CeilToInt(radius / impassibleTilemap.cellSize.x);

            for (int x = -cellRadius; x <= cellRadius; x++)
            {
                for (int y = -cellRadius; y <= cellRadius; y++)
                {
                    Vector3Int currentCell = new Vector3Int(centerCell.x + x, centerCell.y + y, centerCell.z);
                    if (impassibleTilemap.HasTile(currentCell) || buildingTilemap.HasTile(currentCell) || resourceNodeTilemap.HasTile(currentCell))
                    {
                        Vector3 tileWorldPos = impassibleTilemap.CellToWorld(currentCell) + impassibleTilemap.cellSize / 2;
                        if (Vector3.Distance(tileWorldPos, worldPosition) <= radius)
                        {
                            impassibleTilePositions.Add(new Vector2(currentCell.x, currentCell.y));
                        }
                    }
                }
            }

            return impassibleTilePositions;
        }

        #endregion

        #region Public API

        public List<Vector3> GetBuildingTileWorldPos(BaseBuildingController building)
        {
            Vector3 buildingPosition = building.transform.position;
            Vector3Int cellPosition = buildingTilemap.WorldToCell(buildingPosition);

            List<Vector3> buildingTilePositions = new List<Vector3>();

            for (int x = 0; x < building.GetBuildingInfo().length; x++)
            {
                for (int y = 0; y < building.GetBuildingInfo().width; y++)
                {
                    Vector3Int currentCell = new Vector3Int(cellPosition.x + x, cellPosition.y + y, cellPosition.z);
                    if (buildingTilemap.HasTile(currentCell))
                    {
                        Vector3 tileWorldPos = buildingTilemap.CellToWorld(currentCell) + buildingTilemap.cellSize / 2;
                        buildingTilePositions.Add(tileWorldPos);
                    }
                }
            }

            return buildingTilePositions;
        }

        public TileBase GetBuildingTileBase()
        {
            if (buildingTile == null)
            {
                Debug.LogWarning("Building TileBase not found");
                return null;
            }
            return buildingTile;
        }

        public TileBase GetResourceNodeTileBase()
        {
            if (resourceNodeTile == null)
            {
                Debug.LogWarning("ResourceNode TileBase not found");
                return null;
            }
            return resourceNodeTile;
        }

        public Tilemap GetGroundTilemap()
        {
            if (groundTilemap == null)
            {
                Debug.LogWarning("Ground Tilemap not found");
                return null;
            }
            return groundTilemap;
        }

        public Tilemap GetImpassibleTilemap()
        {
            if (impassibleTilemap == null)
            {
                Debug.LogWarning("Impassible Tilemap not found");
                return null;
            }
            return impassibleTilemap;
        }

        public Tilemap GetBuildingTilemap()
        {
            if (buildingTilemap == null)
            {
                Debug.LogWarning("Building Tilemap not found");
                return null;
            }
            return buildingTilemap;
        }

        public Tilemap GetResourceNodeTilemap()
        {
            if (resourceNodeTilemap == null)
            {
                Debug.LogWarning("ResourceNode Tilemap not found");
                return null;
            }
            return resourceNodeTilemap;
        }

        #endregion
    }
}