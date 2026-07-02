using RTS.Common.Enums;
using System;

namespace RTS.Common.Structs
{
    [Serializable]
    public struct ResourceAmount
    {
        public ResourceType resourceType;
        public int amount;
    }
}