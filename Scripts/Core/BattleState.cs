using System.Collections.Generic;

namespace TacticalHex
{
    public class BattleState
    {
        private readonly int _width;
        private readonly int _height;

        private readonly HexModel[,] _hexes;
        private readonly List<UnitModel> _units = new List<UnitModel>();

        public IEnumerable<UnitModel> Units => _units;

        public BattleState(int width, int height)
        {
            _width = width;
            _height = height;
            _hexes = new HexModel[_width, _height];
        }

        private bool InBounds(int q, int r)
        {
            return q >= 0 && q < _width && r >= 0 && r < _height;
        }

        public void SetHex(int q, int r, HexModel hex)
        {
            if (!InBounds(q, r))
                return;

            _hexes[q, r] = hex;
        }

        public HexModel GetHex(int q, int r)
        {
            if (!InBounds(q, r))
                return null;

            return _hexes[q, r];
        }

        public void AddUnit(UnitModel unit)
        {
            if (unit == null)
                return;

            if (!_units.Contains(unit))
                _units.Add(unit);
        }

        public UnitModel GetUnitAt(HexModel hex)
        {
            if (hex == null)
                return null;

            foreach (var unit in _units)
            {
                if (!unit.IsAlive)
                    continue;

                if (unit.CurrentHex == hex)
                    return unit;
            }

            return null;
        }

        public UnitModel GetUnitAt(int q, int r)
        {
            var hex = GetHex(q, r);
            return GetUnitAt(hex);
        }
    }
}
