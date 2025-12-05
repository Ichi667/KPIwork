using TMPro;
using UnityEngine;

namespace TacticalHex
{
    public class UnitInfoPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;

        private void Start()
        {
            Clear();
        }

        public void ShowUnit(UnitModel unit)
        {
            if (_text == null)
                return;

            if (unit == null)
            {
                Clear();
                return;
            }

            string attackType = unit.IsRanged ? "Дальній бій" : "Ближній бій";

            _text.text =
                $"{unit.Name}\n" +
                $"\n" +
                $"Кількість: {unit.UnitCount}\n" +
                $"HP: {unit.CurrentHealthPerUnit}/{unit.MaxHealthPerUnit}\n" +
                $"{attackType}\n" +
                $"Атака: {unit.EffectiveAttack}\n" +
                $"Захист: {unit.EffectiveDefense}\n" +
                $"Шкода: {unit.MinDamage}-{unit.MaxDamage}\n" +
                $"Ініціатива: {unit.Initiative}\n" +
                $"Швидкість: {unit.Speed}";
        }

        public void Clear()
        {
            if (_text != null)
                _text.text = "";
        }
    }
}
