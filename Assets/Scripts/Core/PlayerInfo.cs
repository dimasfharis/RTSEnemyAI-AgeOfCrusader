using RTS.Managers;
using RTS.Managers.Research;
using RTS.AI;
using RTS.Data;
using RTS.Managers.Map;
using RTS.World.ResourceNodeManagement;
using RTS.Common.Enums;
using RTS.Managers.Log;

namespace RTS.Core
{
    public class PlayerInfo
    {
        // ===== Identity =====
        public int PlayerNumber { get; private set; }
        public string PlayerName { get; private set; }
        public bool IsBot { get; private set; }
        public bool IsDefeat { get; private set; }

        // ===== Managers =====
        public ResourceManager ResourceManager { get; private set; }
        public WorkerManager WorkerManager { get; private set; }
        public BuildingManager BuildingManager { get; private set; }
        public MilitaryUnitManager MilitaryUnitManager { get; private set; }
        public AIManager AIManager { get; private set; }
        public DataManager DataManager { get; private set; }
        public ResearchManager ResearchManager { get; private set; }
        public MapManager MapManager { get; private set; }
        public PlayerLogManager PlayerLogManager { get; private set; }

        // ===== Game Managers =====
        public GameManager GameManager { get; private set; }
        public ResourceNodeManager ResourceNodeManager { get; private set; }

        #region Initialization

        public PlayerInfo(
            int playerNumber,
            string playerName,
            bool isBot,
            AIProfileType aiProfileType,
            GameManager gameManager)
        {
            PlayerNumber = playerNumber;
            PlayerName = playerName;
            IsBot = isBot;
            IsDefeat = false;

            GameManager = gameManager;
            ResourceNodeManager = gameManager.ResourceNodeManager;

            ResourceManager = new ResourceManager(this);
            WorkerManager = new WorkerManager(this);
            BuildingManager = new BuildingManager(this);
            MilitaryUnitManager = new MilitaryUnitManager(this);
            ResearchManager = new ResearchManager(this);
            MapManager = new MapManager(this);
            DataManager = new DataManager(this);
            AIManager = new AIManager(this, aiProfileType);
            PlayerLogManager = new PlayerLogManager(this);
        }

        #endregion

        #region Update Lifecycle

        public void Tick()
        {
            ResourceManager.Tick();
            WorkerManager.Tick();
            AIManager.Tick();
            PlayerLogManager.Tick();
        }

        #endregion

        #region Public API

        public void MarkDefeat()
        {
            if (IsDefeat) return;
            IsDefeat = true;
        }

        #endregion
    }
}

