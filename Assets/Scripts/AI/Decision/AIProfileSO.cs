using RTS.Common.Enums;
using UnityEngine;

[CreateAssetMenu(menuName = "RTS/AI/AI Profile")]
public class AIProfileSO : ScriptableObject
{
    [Header("Identity")]
    public AIProfileType aiProfileType;

    [Header("Power Sensitivity")]
    public float OurPowerMultiplier = 1f;
    public float EnemyPowerMultiplier = 1f;

    [Header("Economy Sensitivity")]
    public float OurResourceMultiplier = 1f;
    public float IncomeMultiplier = 1f;

    [Header("Threat Sensitivity")]
    public float EnemyThreatMultiplier = 1f;
    public float BaseDamageMultiplier = 1f;

    [Header("Unit Training Preferences")]
    public float WorkerMultiplier = 1f;
    public float MilitaryMultiplier = 1f;

    [Header("Building Construction Preferences")]
    public float EconomyBuildingMultiplier = 1f;
    public float MilitaryBuildingMultiplier = 1f;

    [Header("Base Defense Preferences")]
    public float DefenseMultiplier = 1f;

    [Header("Research Preferences")]
    public float ResearchMultiplier = 1f;

    [Header("Military Action Preferences")]
    public float AttackMultiplier = 1f;
    public float PatrolMultiplier = 1f;
    public float HarassMultiplier = 1f;
}