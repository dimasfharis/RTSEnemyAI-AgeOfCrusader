using RTS.AI.Pathfinding.UnitPathfindingController;
using RTS.Buildings.Common;
using RTS.Buildings.Data;
using RTS.Common.Enums;
using RTS.Core;
using RTS.Units.Common;
using RTS.World.ResourceNodeManagement;
using RTS.World.WeaponManagement;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RTS.World.WorldManagement
{
    public class WorldManager
    {
        public GameManager gameManager;
        public TileDatabase tileDatabase;
        public ResourceNodeDatabase resourceNodeDatabase;

        public WeaponManager weaponManager;

        #region Initialization

        public WorldManager(GameManager gameManager)
        {
            this.gameManager = gameManager;
            tileDatabase = GameObject.FindAnyObjectByType<TileDatabase>();
            resourceNodeDatabase = GameObject.FindAnyObjectByType<ResourceNodeDatabase>();

            weaponManager = new WeaponManager(this);

            var AStarGenerator = new AStarGenerator(tileDatabase.GetGroundTilemap().size.x, tileDatabase.GetGroundTilemap().size.y, this);
            var FlowfieldGenerator = new FlowfieldGenerator(tileDatabase.GetGroundTilemap().size.x, tileDatabase.GetGroundTilemap().size.y, this);
        }

        #endregion

        #region Opponent Interaction

        public List<BaseUnitController> GetOpponentUnitsInRadius(PlayerInfo playerInfo, Vector3 fromPosition, float radius)
        {
            List<BaseUnitController> unitsInRadius = new List<BaseUnitController>();

            List<PlayerInfo> opponentsPlayerInfo = gameManager.GetOpponentPlayerInfo(playerInfo.PlayerNumber);

            foreach (var player in opponentsPlayerInfo)
            {
                var unitsInArea = player.MilitaryUnitManager.GetUnitsInRadius(fromPosition, radius);

                if (unitsInArea.Count > 0)
                {
                    unitsInRadius.AddRange(unitsInArea);
                }

                var workersInArea = player.WorkerManager.GetWorkersInRadius(fromPosition, radius);

                if (workersInArea.Count > 0)
                {
                    unitsInRadius.AddRange(workersInArea);
                }
            }

            return unitsInRadius;
        }

        public List<BaseBuildingController> GetOpponentBuildingsInRadius(PlayerInfo playerInfo, Vector3 fromPosition, float radius)
        {
            List<BaseBuildingController> buildingsInRadius = new List<BaseBuildingController>();

            List<PlayerInfo> opponentsPlayerInfo = gameManager.GetOpponentPlayerInfo(playerInfo.PlayerNumber);

            foreach (var player in opponentsPlayerInfo)
            {
                var buildingsInArea = player.BuildingManager.GetBuildingsInRadius(fromPosition, radius);

                if (buildingsInArea.Count > 0)
                {
                    buildingsInRadius.AddRange(buildingsInArea);
                }
            }

            return buildingsInRadius;
        }

        #endregion

        #region Queries

        public bool TrySetBuildingTile(Vector3Int position, BuildingType buildingType)
        {
            if (!IsTileImpassible(position, buildingType))
            {
                Debug.LogWarning($"{buildingType} can not placed in impassible tile");
                return false;
            }

            BuildingDatabase buildingDatabase = gameManager.GetDataManager().buildingDatabase;
            int buildingLength = buildingDatabase.GetBuildingTemplate(buildingType).length;
            int buildingWidth = buildingDatabase.GetBuildingTemplate(buildingType).width;

            Tilemap buildingTilemap = tileDatabase.GetBuildingTilemap();
            TileBase buildingTileBase = tileDatabase.GetBuildingTileBase();
            for (int x = 0; x < buildingLength; x++)
            {
                for (int y = 0; y < buildingWidth; y++)
                {
                    buildingTilemap.SetTile(position + new Vector3Int(x, y), buildingTileBase);
                }
            }

            return true;
        }

        public bool TrySetResourceNodeTile(Vector3Int position, ResourceType resourceType)
        {
            if (!IsTileImpassible(position, resourceType))
            {
                Debug.LogWarning($"{resourceType} can not placed in impassible tile");
                return false;
            }

            int resourceNodeLength = resourceNodeDatabase.GetResourceNodeTemplate(resourceType).length;
            int resourceNodeWidth = resourceNodeDatabase.GetResourceNodeTemplate(resourceType).width;

            Tilemap resourceNodeTilemap = tileDatabase.GetResourceNodeTilemap();
            TileBase resourceNodeTileBase = tileDatabase.GetResourceNodeTileBase();
            for (int x = 0; x < resourceNodeLength; x++)
            {
                for (int y = 0; y < resourceNodeWidth; y++)
                {
                    resourceNodeTilemap.SetTile(position + new Vector3Int(x, y), resourceNodeTileBase);
                }
            }

            
            return true;
        }

        public bool IsTileImpassible(Vector3Int position, BuildingType buildingType)
        {
            BuildingDatabase buildingDatabase = gameManager.GetDataManager().buildingDatabase;
            int buildingLength = buildingDatabase.GetBuildingTemplate(buildingType).length;
            int buildingWidth = buildingDatabase.GetBuildingTemplate(buildingType).width;

            Tilemap impassibleTilemap = tileDatabase.GetImpassibleTilemap();
            Tilemap buildingTilemap = tileDatabase.GetBuildingTilemap();
            Tilemap resourceNodeTilemap = tileDatabase.GetResourceNodeTilemap();
            for (int x = 0; x < buildingLength; x++)
            {
                for (int y = 0; y < buildingWidth; y++)
                {
                    if (impassibleTilemap.HasTile(position + new Vector3Int(x, y)))
                    {
                        Debug.LogWarning($"There is impassible Tile in ({x}, {y})");
                        return false;
                    }
                    if (buildingTilemap.HasTile(position + new Vector3Int(x, y)))
                    {
                        Debug.LogWarning($"There is building Tile in ({x}, {y})");
                        return false;
                    }
                    if (resourceNodeTilemap.HasTile(position + new Vector3Int(x, y)))
                    {
                        Debug.LogWarning($"There is resource node Tile in ({x}, {y})");
                        return false;
                    }
                }
            }

            return true;
        }

        public bool IsTileImpassible(Vector3Int position, ResourceType resourceType)
        {
            int resourceNodeLength = resourceNodeDatabase.GetResourceNodeTemplate(resourceType).length;
            int resourceNodeWidth = resourceNodeDatabase.GetResourceNodeTemplate(resourceType).width;

            Tilemap impassibleTilemap = tileDatabase.GetImpassibleTilemap();
            Tilemap buildingTilemap = tileDatabase.GetBuildingTilemap();
            Tilemap resourceNodeTilemap = tileDatabase.GetResourceNodeTilemap();
            for (int x = 0; x < resourceNodeLength; x++)
            {
                for (int y = 0; y < resourceNodeWidth; y++)
                {
                    if (impassibleTilemap.HasTile(position + new Vector3Int(x, y)))
                    {
                        Debug.LogWarning($"There is impassible Tile in ({position.x + x}, {position.y + y})");
                        return false;
                    }
                    if (buildingTilemap.HasTile(position + new Vector3Int(x, y)))
                    {
                        Debug.LogWarning($"There is building Tile in ({position.x + x}, {position.y + y})");
                        return false;
                    }
                    if (resourceNodeTilemap.HasTile(position + new Vector3Int(x, y)))
                    {
                        Debug.LogWarning($"There is resource node Tile in ({position.x + x}, {position.y + y})");
                        return false;
                    }
                }
            }

            return true;
        }

        #endregion
    }
}