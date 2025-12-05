using UnityEngine;

namespace TacticalHex
{
    [CreateAssetMenu(fileName = "UnitConfig", menuName = "TacticalHex/Unit Config")]
    public class UnitConfig : ScriptableObject
    {
        [Header("основне")]
        public string UnitName;
        public Sprite Icon;

        [Header("хар-кі")]
        public int Attack = 5;
        public int Defense = 5;
        public int MinDamage = 1;
        public int MaxDamage = 3;
        public int HealthPerUnit = 10;
        public int Initiative = 10;
        public int Speed = 4;

        [Header("Атака")]
        public bool IsRanged;
        public int MinRange = 1;
        public int MaxRange = 6;

        [Header("мили атака стрелка")]
        [Tooltip("множник ближньго боя.")]
        public float MeleeDamageMultiplier = 0.5f;

        [Header("стек (не юзать, це базове)")]
        public int DefaultStackSize = 10;

        [Header("атака по можливості")]
        [Tooltip("чи може юніт атаковати ворога при відході.")]
        public bool HasOpportunityAttack = true;

        [Tooltip("множник урона атакі по можливості.")]
        [Range(0f, 3f)]
        public float OpportunityAttackMultiplier = 0.5f;
    }
}
