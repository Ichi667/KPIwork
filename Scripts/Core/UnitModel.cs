using UnityEngine;

namespace TacticalHex
{
    public class UnitModel
    {
        public UnitConfig Config { get; }
        public string InstanceName { get; }
        public string Name => Config != null ? Config.UnitName : InstanceName;

        public Faction Faction { get; }
        public HeroModel Hero { get; }

        public int Attack => Config != null ? Config.Attack : 0;
        public int Defense => Config != null ? Config.Defense : 0;
        public int MinDamage => Config != null ? Config.MinDamage : 1;
        public int MaxDamage => Config != null ? Config.MaxDamage : 3;
        public int HealthPerUnit => Config != null ? Config.HealthPerUnit : 10;
        public int Initiative => Config != null ? Config.Initiative : 10;
        public int Speed => Config != null ? Config.Speed : 4;

        public bool IsRanged => Config != null && Config.IsRanged;
        public int MinRange => Config != null ? Config.MinRange : 1;
        public int MaxRange => Config != null ? Config.MaxRange : 6;

        public float MeleeDamageMultiplier =>
            Config != null ? Config.MeleeDamageMultiplier : 1f;

        public bool OpportunityAttackEnabled { get; private set; }
        public float OpportunityAttackMultiplier { get; private set; }

        public int InitialUnitCount { get; }
        public int UnitCount { get; private set; }

        public int CurrentHealth { get; private set; }
        public int CurrentTotalHealth => CurrentHealth;
        public int MaxHealthPerUnit => HealthPerUnit;
        public int MaxTotalHealth => InitialUnitCount * HealthPerUnit;

        public int CurrentHealthPerUnit
        {
            get
            {
                if (UnitCount <= 0)
                    return 0;

                if (MaxHealthPerUnit <= 0)
                    return 0;

                int remainder = CurrentHealth % MaxHealthPerUnit;

                if (remainder == 0)
                    return MaxHealthPerUnit;


                return remainder;
            }
        }

        public bool IsAlive => UnitCount > 0;

        public HexModel CurrentHex { get; private set; }

        public int EffectiveAttack => Attack + (Hero?.Attack ?? 0);
        public int EffectiveDefense => Defense + (Hero?.Defense ?? 0);

        public UnitModel(UnitConfig config,
                         string instanceName,
                         Faction faction,
                         HeroModel hero,
                         int stackSize)
        {
            Config = config;
            InstanceName = instanceName;
            Faction = faction;
            Hero = hero;

            UnitCount = Mathf.Max(1, stackSize);
            InitialUnitCount = UnitCount;

            CurrentHealth = UnitCount * HealthPerUnit;

            if (config != null)
            {
                OpportunityAttackEnabled = config.HasOpportunityAttack;
                OpportunityAttackMultiplier = config.OpportunityAttackMultiplier;
            }
            else
            {
                OpportunityAttackEnabled = false;
                OpportunityAttackMultiplier = 0.5f;
            }
        }

        public void SetHex(HexModel hex)
        {
            CurrentHex = hex;
        }

        public void TakeDamage(int damage)
        {
            if (damage <= 0 || !IsAlive)
                return;

            CurrentHealth -= damage;

            if (CurrentHealth <= 0)
            {
                UnitCount = 0;
                CurrentHealth = 0;
                return;
            }

            int fullUnits = CurrentHealth / HealthPerUnit;
            if (CurrentHealth % HealthPerUnit != 0)
                fullUnits++;

            UnitCount = Mathf.Max(1, fullUnits);
        }

        public bool CanAttack(UnitModel target, out bool isMelee)
        {
            isMelee = false;

            if (target == null || !target.IsAlive)
                return false;
            if (CurrentHex == null || target.CurrentHex == null)
                return false;

            int dist = HexModel.Distance(CurrentHex, target.CurrentHex);


            if (dist == 1)
            {
                isMelee = true;
                return true;
            }

            if (IsRanged)
            {
                if (dist >= MinRange && dist <= MaxRange)
                {
                    isMelee = false;
                    return true;
                }
            }

            return false;
        }
    }
}
