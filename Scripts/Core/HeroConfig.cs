using UnityEngine;

namespace TacticalHex
{
    [CreateAssetMenu(fileName = "HeroConfig", menuName = "TacticalHex/Hero Config")]
    public class HeroConfig : ScriptableObject
    {
        [Header("основне")]
        public int Id;
        public string HeroName;
        public Faction Faction;

        [Header("хар-кі")]
        public int Attack = 0;
        public int Defense = 0;

        [System.Serializable]
        public class ArmySlot
        {
            public UnitConfig Unit;
            [Min(0)]
            public int Count = 0;
        }

        [Header("стартова армія")]
        public ArmySlot[] Army;
    }
}
