using RTS.Common.Enums;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.World.ResourceNodeManagement
{
    [CreateAssetMenu(
        fileName = "ResourceNodeSO_",
        menuName = "RTS/World/Resource Node SO"
    )]
    public class ResourceNodeSO : ScriptableObject
    {
        [Header("Identity")]
        public ResourceType resourceType;

        [Header("Resource Amount")]
        public int maxAmount;

        [Header("Dimension")]
        public int length;
        public int width;
    }
}

