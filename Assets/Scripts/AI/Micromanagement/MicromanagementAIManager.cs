using RTS.Common.DataClass;
using RTS.Core;
using RTS.Managers;
using RTS.Units.Common;
using UnityEngine;
using System.Collections.Generic;
using RTS.Units.Military;
using System.Linq;
using RTS.Common.Enums;

namespace RTS.AI.Micromanagement
{
    public class MicromanagementAIManager
    {
        private PlayerInfo playerInfo;
        private MilitaryUnitManager militaryUnitManager;
        private List<MilitaryGroup> militaryGroups = new List<MilitaryGroup>();

        #region Initialization

        public MicromanagementAIManager(PlayerInfo owner)
        {
            playerInfo = owner;
            militaryUnitManager = playerInfo.MilitaryUnitManager;
        }

        #endregion

        #region Loop

        public void Tick()
        {
            UpdateMilitaryGroups();
        }

        private void UpdateMilitaryGroups()
        {
            if (militaryGroups.Count > 0)
            {
                foreach (MilitaryGroup militaryGroup in militaryGroups)
                {
                    militaryGroup.Tick();
                }
            }
        }

        #endregion

        #region Group Reinforcement

        public void ReinforceAttackWave(Vector3 fromPosition, float radius, Vector3 targetPosition)
        {
            MilitaryGroup militaryGroup = ReinforceGroupByArea(fromPosition, radius);
            militaryGroup.militaryGroupMode = MilitaryGroupMode.AttackWave;
            militaryGroup.targetPosition = targetPosition;

            AssemblyGroup(militaryGroup);
        }

        private MilitaryGroup ReinforceGroupByArea(Vector3 fromPosition, float radius)
        {
            List<MilitaryUnitController> unitsInRadius = militaryUnitManager.GetUnitsInRadius(fromPosition, radius);

            MilitaryGroup newGroup = new MilitaryGroup(unitsInRadius.Cast<BaseUnitController>().ToList(), playerInfo);
            militaryGroups.Add(newGroup);
            return newGroup;
        }

        #endregion

        #region Group Movement

        public void AssemblyGroup(MilitaryGroup militaryGroup)
        {
            Vector3 assemblyPosition = militaryGroup.GetMeanPositionOfReinforces();

            militaryUnitManager.IssueMoveCommand(militaryGroup.units, assemblyPosition);
        }

        #endregion

        #region Public API

        public List<MilitaryGroup> GetAnotherMilitaryGroup(MilitaryGroup militaryGroup)
        {
            return militaryGroups.Where(group => group != militaryGroup).ToList();
        }

        #endregion
    }
}