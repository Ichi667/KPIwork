using UnityEngine;

namespace TacticalHex
{
    public class AIController : MonoBehaviour
    {
        private BattleController _battle;

        private void Awake()
        {
            _battle = GetComponent<BattleController>();
            if (_battle == null)
            {
                Debug.LogError("AIController: test.");
            }
        }

        public void TakeTurn(UnitModel unit)
        {
            if (_battle == null)
                return;

            _battle.EnemyTakeTurn(unit);
        }
    }
}
