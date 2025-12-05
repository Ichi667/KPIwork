namespace TacticalHex
{
    public enum Faction
    {
        Player,
        Enemy
    }

    public class HeroModel
    {
        public string Name { get; }
        public Faction Faction { get; }

        public int Attack { get; }
        public int Defense { get; }

        public HeroConfig Config { get; }

        public HeroModel(HeroConfig config)
        {
            Config = config;

            if (config != null)
            {
                Name = string.IsNullOrEmpty(config.HeroName) ? "Герой" : config.HeroName;
                Faction = config.Faction;
                Attack = config.Attack;
                Defense = config.Defense;
            }
            else
            {
                Name = "Герой";
                Faction = Faction.Player;
                Attack = 0;
                Defense = 0;
            }
        }

        public HeroModel(string name, Faction faction, int attack, int defense)
        {
            Name = name;
            Faction = faction;
            Attack = attack;
            Defense = defense;
            Config = null;
        }
    }
}
