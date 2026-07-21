using RTS.Core;
using RTS.Common.Enums;
using RTS.AI.Decision;
using RTS.AI.Behavior;
using RTS.AI.Resources;
using RTS.AI.Micromanagement;
using RTS.AI.Pathfinding;
using System.Diagnostics;

namespace RTS.AI
{
    public class AIManager
    {
        private PlayerInfo playerInfo;
        private AIProfileSO AIProfileSO;

        private DecisionMakingAIManager decisionAI;
        private ResourceManagementAIManager resourceAI;
        private EnemyBehaviorAIManager enemyAI;
        private MicromanagementAIManager microAI;
        private PathfindingAIManager pathAI;

        private bool isInitialized;

        #region Initialization

        public AIManager(PlayerInfo owner, AIProfileType aiProfileType)
        {
            if (isInitialized)
                return;

            playerInfo = owner;
            AIProfileSO = playerInfo.DataManager.aiProfileDatabase.GetAIProfileTemplate(aiProfileType);

            decisionAI = new DecisionMakingAIManager(playerInfo, AIProfileSO);
            resourceAI = new ResourceManagementAIManager(playerInfo);
            enemyAI = new EnemyBehaviorAIManager(playerInfo, this);
            microAI = new MicromanagementAIManager(playerInfo);
            pathAI = new PathfindingAIManager(playerInfo);

            isInitialized = true;
        }

        #endregion

        #region Update Loop

        public void Tick()
        {
            if (!isInitialized) return;
            if (!playerInfo.IsBot) return;
            if (playerInfo.IsDefeat) return;

            decisionAI.Tick();
            resourceAI.Tick();
            enemyAI.Tick();
            microAI.Tick();
        }

        #endregion

        #region Public API

        public AIProfileSO GetAIProfile()
        {
            return AIProfileSO;
        }

        public DecisionMakingAIManager GetDecisionMakingAIManager()
        {
            return decisionAI;
        }

        public ResourceManagementAIManager GetResourceManagementAIManager()
        {
            return resourceAI;
        }

        public EnemyBehaviorAIManager GetEnemyBehaviorAIManager()
        {
            return enemyAI;
        }

        public MicromanagementAIManager GetMicromanagementAIManager()
        {
            return microAI;
        }

        public PathfindingAIManager GetPathfindingAIManager()
        {
            return pathAI;
        }

        #endregion
    }
}