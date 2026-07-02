using RTS.Monitoring;
using RTS.Buildings.Common;
using RTS.Buildings.Economic;
using RTS.Buildings.Military;
using RTS.Common.Enums;
using RTS.Data;
using RTS.Managers;
using RTS.Managers.Research;
using RTS.Units.Common;
using RTS.Units.Worker;
using RTS.World.ResourceNodeManagement;
using RTS.World.WorldManagement;
using System;
using System.Collections.Generic;
using UnityEngine;
using RTS.AI.GoalManagement;
using RTS.AI.Behavior;

namespace RTS.Core
{
    public class GameManager : MonoBehaviour
    {
        public ResourceNodeManager ResourceNodeManager { get; private set; }
        public WorldManager WorldManager { get; private set; }

        public static event Action<PlayerInfo> OnPlayerCreated;

        private Dictionary<int, PlayerInfo> Players;
        private int numberOfPlayers = 0;

        #region Developer Purpose Only

        #region Do Scout

        [SerializeField] private DoScout doScout;

        [System.Serializable]
        public struct DoScout
        {
            public int playerNumber;
            public Vector3 scoutPosition;
        }

        [ContextMenu("Do Scout")]
        public void TestDoScout()
        {
            PlayerInfo playerInfo = Players[doScout.playerNumber];
            MilitaryGoalExecutor militaryGoalExecutor = playerInfo.AIManager.GetEnemyBehaviorAIManager().GetGoalCoordinator().GetMilitaryGoalExecutor();

            AIGoal goal = new AIGoal(AIGoalType.AssignScout, 0f);
            goal.SetTargetPosition(doScout.scoutPosition);

            militaryGoalExecutor.Execute(goal);
        }

        #endregion

        #region Reinforce Wave Attack

        [SerializeField] private ReinforceAttackWave reinforceAttackWave;

        [System.Serializable]
        public struct ReinforceAttackWave
        {
            public int playerNumber;
            public Vector3 fromPosition;
            public float radius;
            public Vector3 targetPosition;
        }

        [ContextMenu("Reinforce Attack Wave")]
        public void TestReinforceWaveAttack()
        {
            PlayerInfo playerInfo = Players[reinforceAttackWave.playerNumber];

            playerInfo.AIManager.GetMicromanagementAIManager().ReinforceAttackWave(
                reinforceAttackWave.fromPosition,
                reinforceAttackWave.radius,
                reinforceAttackWave.targetPosition);
        }    

        #endregion

        #region Generate Units

        [SerializeField] private GenerateUnits generateUnits;

        [System.Serializable]
        public struct GenerateUnits
        {
            public int playerNumber;
            public BaseBuildingController buildingToSpawn;
            public UnitType unitTypeToSpawn;
            public int numberOfUnitToSpawn;
        }

        [ContextMenu("Generate Units")]
        public void TestGenerateUnits()
        {
            PlayerInfo playerInfo = Players[generateUnits.playerNumber];

            for (int i = 0; i < generateUnits.numberOfUnitToSpawn; i++)
            {
                playerInfo.MilitaryUnitManager.SpawnUnitAtBuilding(generateUnits.buildingToSpawn, generateUnits.unitTypeToSpawn);
            }
        }

        #endregion

        #region Unit Attack

        [SerializeField] private AttackUnit attackUnit;

        [System.Serializable]
        public struct AttackUnit
        {
            public BaseUnitController unitAttacker;
            public BaseUnitController opponentUnit;
        }

        [ContextMenu("Attack Unit")]
        public void TestAttackUnit()
        {
            PlayerInfo player = Players[1];
            MilitaryUnitManager militaryUnitManager = player.MilitaryUnitManager;

            List<BaseUnitController> units = new List<BaseUnitController>();
            units.Add(attackUnit.unitAttacker);

            militaryUnitManager.IssueAttackCommand(units, attackUnit.opponentUnit);
        }

        #endregion

        #region Add Resources

        [SerializeField] private AddResources addResources;

        [System.Serializable]
        public struct AddResources
        {
            public int addWood;
            public int addGold;
            public int addStone;
            public int addFood;
        }

        [ContextMenu("Add Resources")]
        public void TestAddResources()
        {
            PlayerInfo player = Players[1];
            ResourceManager resourceManager = player.ResourceManager;

            resourceManager.AddResource(ResourceType.Wood, addResources.addWood);
            resourceManager.AddResource(ResourceType.Gold, addResources.addGold);
            resourceManager.AddResource(ResourceType.Stone, addResources.addStone);
            resourceManager.AddResource(ResourceType.Food, addResources.addFood);
        }

        #endregion

        #region Check Unit / Building Stats

        [SerializeField] private CheckStats checkStats;

        [System.Serializable]
        public struct CheckStats
        {
            public BaseBuildingController checkBuildingStats;
            public BaseUnitController checkUnitStats;
        }

        [ContextMenu("Check Stats")]
        public void TestCheckStats()
        {
            if (checkStats.checkBuildingStats != null)
            {
                BaseBuildingController building = checkStats.checkBuildingStats;
                BuildingInfo info = building.GetBuildingInfo();
                Debug.Log($"Building {info.buildingType} \n" +
                    $"Attack Damage : {info.attackDamage} \n" +
                    $"Current HP : {info.currentHitPoint} \n" +
                    $"Max HP : {info.maxHitPoint}"
                    );
            }

            if (checkStats.checkUnitStats != null)
            {
                BaseUnitController unit = checkStats.checkUnitStats;
                UnitInfo info = unit.GetUnitInfo();
                Debug.Log($"Unit {info.unitType} \n" +
                    $"Carrying Capacity : {info.carryingCapacity} \n" +
                    $"Current HP : {info.unitHealth} \n" +
                    $"Max HP : {info.unitMaxHealth} \n" +
                    $"Attack Damage : {info.attackDamage} \n"
                    );
            }
        }

        #endregion

        #region Research Lab

        [SerializeField] private ResearchStats researchStats;

        [System.Serializable]
        public struct ResearchStats
        {
            public ResearchLabController researchLab;
            public ResearchType researchType;
        }

        [ContextMenu("Research Stats")]
        public void TestResearch()
        {
            PlayerInfo player = Players[1];
            ResearchManager researchManager = player.ResearchManager;

            researchManager.TryStartResearch(researchStats.researchType, researchStats.researchLab);
        }

        #endregion

        #region Unit Training

        [SerializeField] private UnitTraining unitTraining;

        [System.Serializable]
        public struct UnitTraining
        {
            public BaseBuildingController buildingTrainer;
            public UnitType unitTrainType;
        }

        [ContextMenu("Unit Training")]
        public void TestUnitTraining()
        {
            PlayerInfo player = Players[1];
            BuildingManager buildingManager = player.BuildingManager;

            buildingManager.TryTrainUnit(unitTraining.unitTrainType, unitTraining.buildingTrainer);
        }

        #endregion

        #region Building Construction

        [SerializeField] private BuildingConstruction buildingConstruction;

        [System.Serializable]
        public struct BuildingConstruction
        {
            public BuildingType buildingTypeToTest;
            public Vector3 buildingPositionToTest;
            public List<WorkerUnitController> workerToBuildTest;
        }

        [ContextMenu("Building Construction Test")]
        public void TestBuildingConstruction()
        {
            PlayerInfo player = Players[1];
            WorkerManager workerManager = player.WorkerManager;

            workerManager.TryAssignWorkerToBuild(
                buildingConstruction.workerToBuildTest,
                buildingConstruction.buildingTypeToTest,
                buildingConstruction.buildingPositionToTest);
        }

        #endregion

        #region Resource Gathering

        [SerializeField] private ResourceGathering resourceGathering;

        [System.Serializable]
        public struct ResourceGathering
        {
            public List<WorkerUnitController> workers;
            public ResourceType resourceType;
        }

        [ContextMenu("Resource Gathering")]
        public void TestWorkerResourceGathering()
        {
            PlayerInfo player = Players[1];
            WorkerManager workerManager = player.WorkerManager;

            workerManager.AssignWorkerToGather(
                resourceGathering.workers,
                resourceGathering.resourceType);
        }

        #endregion

        #region Unit Pathfinding

        [SerializeField] private UnitPathfinding unitPathfinding;

        [System.Serializable]
        public struct UnitPathfinding
        {
            public List<BaseUnitController> units;
            public Vector3 destination;
        }

        [ContextMenu("Unit Pathfinding")]
        public void TestPathfinding()
        {
            PlayerInfo player = Players[1];
            MilitaryUnitManager militaryUnitManager = player.MilitaryUnitManager;

            militaryUnitManager.IssueMoveCommand(
                unitPathfinding.units,
                unitPathfinding.destination);
        }

        #endregion

        #region Map Exploration Print Log

        [SerializeField] private MapExplorationPrintLog mapExplorationPrintLog;

        [System.Serializable]
        public struct MapExplorationPrintLog
        {
            public int playerNumber;
        }

        [ContextMenu("Map Exploration Print Log")]
        public void TestMapExplorationPrintLog()
        {
            PlayerInfo player = Players[mapExplorationPrintLog.playerNumber];

            PlayerDataDebugging[] playerDataDebugging = GameObject.FindObjectsOfType<PlayerDataDebugging>();

            foreach (PlayerDataDebugging debugging in playerDataDebugging)
            {
                if (debugging.GetPlayerNumber() == mapExplorationPrintLog.playerNumber)
                {
                    debugging.MapExplorationPrintLog(player);
                }
            }
        }

        #endregion

        /*#region Tilemap

        [SerializeField] private Tilemap groundTilemap;
        [SerializeField] private Tilemap realGround;
        [SerializeField] private TileBase water;

        [ContextMenu("Get Tile Base")]
        public void GetTileBase()
        {
            TileBase tileku = groundTilemap.GetTile(new Vector3Int (1, 0, 0));
            TileBase tileGround = realGround.GetTile(new Vector3Int(0, 1, 0));
            if (tileku != null)
            {
                Debug.Log(tileku.name);
            }
            else
            {
                Debug.Log($"No {tileku} tilebase found");
            }
            if (tileGround != null)
            {
                Debug.Log(tileGround.name);
            }
            else
            {
                Debug.Log($"No {tileGround} tilebase found");
            }
            realGround.SetTile(new Vector3Int(6, 6, 0), water);
        }

        #endregion*/

        #endregion

        #region Initialization

        private void Awake()
        {
            Players = new Dictionary<int, PlayerInfo>();

            WorldManager = new WorldManager(this);
            ResourceNodeManager = new ResourceNodeManager(this);
        }

        private void Start()
        {
            DoGameStarter();
        }

        #endregion

        #region Game Update Lifecycle

        private void Update()
        {
            foreach (var player in Players)
            {
                player.Value.Tick();
            }
        }

        #endregion

        #region Game Starter

        private void DoGameStarter()
        {
            GeneratePlayerDummy();

            // Player Initiation
            foreach (PlayerInitialization dummy in dummyPlayers)
            {
                // Player Creation
                PlayerInfo player = CreateNewPlayer(dummy.playerName, dummy.isBot, dummy.aiProfileType);

                // Town Center Initiation
                if (!player.BuildingManager.TryPlaceBuilding(BuildingType.TownCenter, dummy.townCenterPosition))
                {
                    Debug.LogWarning($"Building {BuildingType.TownCenter} can not placed");
                } else
                {
                    FinishTownCenterBuildProgress(player);
                }

                // Worker Initiation
                InitiateWorker(player, dummy.numbOfWorkerInitiate);

                // Player Resource Initiation
                InitiateResource(player);
            }
        }

        private void FinishTownCenterBuildProgress(PlayerInfo playerInfo)
        {
            TownCenterController controller = GameObject.FindObjectOfType<TownCenterController>();
            if (controller == null)
            {
                Debug.LogWarning("Town center controller is missing");
            }

            float maxBuildProgress = playerInfo.DataManager.buildingDatabase.GetBuildingTemplate(BuildingType.TownCenter).buildTime;
            controller.AddBuildProgress(maxBuildProgress);
        }

        private void InitiateWorker(PlayerInfo playerInfo, int count)
        {
            for (int i = 0; i < count; i++)
            {
                BaseBuildingController townCenterController = playerInfo.BuildingManager.GetBuilding(BuildingType.TownCenter);

                playerInfo.WorkerManager.SpawnUnitAtBuilding(townCenterController, UnitType.Worker);
            }
        }

        private void InitiateResource(PlayerInfo playerInfo)
        {
            ResourceManager resourceManager = playerInfo.ResourceManager;

            resourceManager.AddResource(ResourceType.Wood, 140);
            resourceManager.AddResource(ResourceType.Gold, 115);
            resourceManager.AddResource(ResourceType.Stone, 120);
            resourceManager.AddResource(ResourceType.Food, 125);
        }

        #endregion

        #region Player Creation

        private PlayerInfo CreateNewPlayer(string playerName, bool isBot, AIProfileType aiProfileType)
        {
            numberOfPlayers++;

            PlayerInfo newPlayer = new PlayerInfo(
                numberOfPlayers,
                playerName,
                isBot,
                aiProfileType,
                this);

            Players.Add(numberOfPlayers, newPlayer);

            // Debug Purpose
            OnPlayerCreated.Invoke(newPlayer);

            return newPlayer;
        }

        #endregion

        #region Data Dummy

        [SerializeField] private List<PlayerInitialization> dummyPlayers = new List<PlayerInitialization>();

        private class PlayerInitialization
        {
            public string playerName;
            public bool isBot;
            public AIProfileType aiProfileType;
            public Vector2 townCenterPosition;
            public int numbOfWorkerInitiate = 12;
        }

        private void GeneratePlayerDummy()
        {
            PlayerInitialization player1 = new PlayerInitialization();
            player1.playerName = "Alexander The Great";
            player1.isBot = true;
            player1.aiProfileType = AIProfileType.Aggressive;
            player1.townCenterPosition = new Vector2(8, 7);

            PlayerInitialization player2 = new PlayerInitialization();
            player2.playerName = "King Darius III";
            player2.isBot = true;
            player2.aiProfileType = AIProfileType.Defensive;
            player2.townCenterPosition = new Vector2(63, 67);

            dummyPlayers.Add(player1);
            dummyPlayers.Add(player2);
        }

        #endregion

        #region Public API

        public DataManager GetDataManager()
        {
            if (Players.TryGetValue(1, out PlayerInfo playerInfo))
            {
                return playerInfo.DataManager;
            } else
            {
                Debug.LogWarning("DataManager not found");
                return null;
            }
        }

        public Vector3 GetStarterBaseLocation(int playerId)
        {
            PlayerInfo player = Players[playerId];

            BaseBuildingController building = player.BuildingManager.GetBuilding(BuildingType.TownCenter);
            Vector3 buildingPosition = building.transform.position;

            return buildingPosition;
        }

        public List<PlayerInfo> GetOpponentPlayerInfo(int selfPlayerNumber)
        {
            List<PlayerInfo> opponents = new List<PlayerInfo>();

            foreach (var player in Players)
            {
                if (player.Key != selfPlayerNumber)
                {
                    opponents.Add(player.Value);
                }
            }

            return opponents;
        }

        #endregion
    }
}