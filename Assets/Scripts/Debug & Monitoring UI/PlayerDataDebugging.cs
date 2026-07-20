using RTS.AI.Behavior;
using RTS.AI.Decision;
using RTS.AI.GoalManagement;
using RTS.Common.Enums;
using RTS.Core;
using RTS.Managers;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace RTS.Monitoring
{
    public class PlayerDataDebugging : MonoBehaviour
    {
        [Header("Player Info")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private int playerNumber;

        [Header("Resource")]
        [SerializeField] private TextMeshProUGUI woodAmountText;
        [SerializeField] private TextMeshProUGUI goldAmountText;
        [SerializeField] private TextMeshProUGUI stoneAmountText;
        [SerializeField] private TextMeshProUGUI foodAmountText;
        [SerializeField] private TextMeshProUGUI populationText;

        [Header("AI Strategy Mode")]
        [SerializeField] private TextMeshProUGUI aiStrategyChosed;
        [SerializeField] private TextMeshProUGUI[] aiStrategy = new TextMeshProUGUI[4];
        [SerializeField] private TextMeshProUGUI[] aiStrategyScore = new TextMeshProUGUI[4];

        [Header("Generated Goals")]
        private static int goalActiveCount = 4;
        private static int goalPoolCount = 6;
        [SerializeField] private TextMeshProUGUI[] goalActiveText = new TextMeshProUGUI[goalActiveCount];
        [SerializeField] private TextMeshProUGUI[] payloadActiveText = new TextMeshProUGUI[goalActiveCount];
        [SerializeField] private TextMeshProUGUI[] progressActiveText = new TextMeshProUGUI[goalActiveCount];
        [SerializeField] private TextMeshProUGUI[] scoreActiveText = new TextMeshProUGUI[goalActiveCount];

        [SerializeField] private TextMeshProUGUI[] goalPoolText = new TextMeshProUGUI[goalPoolCount];
        [SerializeField] private TextMeshProUGUI[] scorePoolText = new TextMeshProUGUI[goalPoolCount];

        #region Initialization

        private void Awake()
        {
            GameManager.OnPlayerCreated += UpdatePlayerNameUI;
            ResourceManager.OnResourceChanged += UpdateResourceChangedUI;
            ResourceManager.OnPopulationChanged += UpdatePopulationChangedUI;
            DecisionMakingAIManager.OnStrategyModeUpdated += UpdateStrategyModeChangedUI;
            GoalCoordinator.OnGoalsGeneratedChanged += UpdateGeneratedGoalsChangedUI;
        }

        #endregion

        #region Update Player Name UI

        private void UpdatePlayerNameUI(PlayerInfo playerInfo)
        {
            if (playerInfo.PlayerNumber == playerNumber)
            {
                playerNameText.text = playerInfo.PlayerName;
            }
        }

        #endregion

        #region Update Resource & Population UI

        private void UpdateResourceChangedUI(Dictionary<ResourceType, int> resources, PlayerInfo playerInfo)
        {
            if (playerInfo.PlayerNumber == playerNumber)
            {
                woodAmountText.text = resources[ResourceType.Wood].ToString();
                goldAmountText.text = resources[ResourceType.Gold].ToString();
                stoneAmountText.text = resources[ResourceType.Stone].ToString();
                foodAmountText.text = resources[ResourceType.Food].ToString();
            }
        }

        private void UpdatePopulationChangedUI(int curPop, int popCap, PlayerInfo playerInfo)
        {
            if (playerInfo.PlayerNumber == playerNumber)
            {
                populationText.text = $"{curPop}/{popCap}";
            }
        }

        #endregion

        #region Update Strategy Mode UI

        private void UpdateStrategyModeChangedUI(Dictionary<AIStrategyMode, float> strategyModesCalculated, PlayerInfo playerInfo)
        {
            if (playerInfo.PlayerNumber == playerNumber)
            {
                Dictionary<AIStrategyMode, float> sortedStrategyModes = SortStrategyModeScore(strategyModesCalculated);

                int count = 0;
                foreach (var mode in sortedStrategyModes)
                {
                    aiStrategy[count].text = mode.Key.ToString();
                    aiStrategyScore[count].text = mode.Value.ToString("0.00");

                    count++;
                }

                aiStrategyChosed.text = new string($"{aiStrategy[0].text} Mode");
            }
        }

        private Dictionary<AIStrategyMode, float> SortStrategyModeScore(Dictionary<AIStrategyMode, float> strategyModeDict)
        {
            var sorted = strategyModeDict.OrderByDescending(x => x.Value)
                .ToDictionary(x => x.Key, x => x.Value);

            return sorted;
        }

        #endregion

        #region Update Generated Goals

        private void UpdateGeneratedGoalsChangedUI(List<AIGoal> goalPool, List<AIGoal> activeGoals, PlayerInfo playerInfo)
        {
            if (playerInfo.PlayerNumber == playerNumber)
            {
                for (int i = 0; i < goalActiveCount; i++)
                {
                    string goalActive = "";
                    string payloadActive = ": ";
                    string progressActive = "";
                    string scoreActive = "";

                    if (i < activeGoals.Count)
                    {
                        goalActive = activeGoals[i].GoalType.ToString();

                        if (activeGoals[i].UnitTrainingRequirements != null && activeGoals[i].UnitTrainingRequirements.Count > 0)
                        {
                            string unitRequirements = "";

                            foreach (var requirement in activeGoals[i].UnitTrainingRequirements)
                            {
                                unitRequirements += $"{requirement.Key} ";
                            }

                            payloadActive = $": {unitRequirements}";
                        }
                        else if (activeGoals[i].BuildingType != BuildingType.None)
                            payloadActive = $": {activeGoals[i].BuildingType}";
                        else if (activeGoals[i].ResearchType != ResearchType.None)
                            payloadActive = $": {activeGoals[i].ResearchType}";

                        progressActive = $"({activeGoals[i].currentProgress}/{activeGoals[i].targetAmount})";
                        scoreActive = activeGoals[i].Score.ToString("0.00");
                    }

                    goalActiveText[i].text = goalActive;
                    payloadActiveText[i].text = payloadActive;
                    progressActiveText[i].text = progressActive;
                    scoreActiveText[i].text = scoreActive;
                }

                for (int i = 0; i < goalPoolCount; i++)
                {
                    string goalTypePool = "";
                    string scorePool = "";

                    if (i < goalPool.Count)
                    {
                        goalTypePool = goalPool[i].GoalType.ToString();
                        scorePool = goalPool[i].Score.ToString("0.00");
                    }

                    goalPoolText[i].text = goalTypePool;
                    scorePoolText[i].text = scorePool;
                }
            }
        }

        #endregion

        #region Map Exploration Log

        public void MapExplorationPrintLog(PlayerInfo playerInfo)
        {
            if (playerInfo.PlayerNumber == playerNumber)
            {
                playerInfo.PlayerLogManager.PrintMapExplorationLog();
            }
        }

        #endregion

        #region Public API

        public int GetPlayerNumber()
        {
            return playerNumber;
        }

        #endregion
    }
}