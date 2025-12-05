using UnityEngine;
using UnityEngine.SceneManagement;

namespace TacticalHex
{
    public class HeroSelectionMenu : MonoBehaviour
    {
        [Header("герої")]
        [SerializeField] private HeroConfig[] _availableHeroes;

        [Header("імя сцени")]
        [SerializeField] private string _battleSceneName = "BattleScene";

        private HeroConfig _selectedPlayerHero;
        private HeroConfig _selectedEnemyHero;

        public void SelectPlayerHero(int index)
        {
            var hero = GetHeroByIndex(index);
            if (hero == null) return;

            _selectedPlayerHero = hero;
            Debug.Log($"[Menu] для ігрока {index}: assetName={hero.name}, heroName={hero.HeroName}");
        }

        public void SelectEnemyHero(int index)
        {
            var hero = GetHeroByIndex(index);
            if (hero == null) return;

            _selectedEnemyHero = hero;
            Debug.Log($"[Menu] для ШІ {index}: assetName={hero.name}, heroName={hero.HeroName}");
        }

        public void SelectPlayerHeroByAssetName(string assetName)
        {
            var hero = GetHeroByAssetName(assetName);
            if (hero == null)
            {
                return;
            }

            _selectedPlayerHero = hero;
            Debug.Log($"[Menu] для ігрока assetName={hero.name}, heroName={hero.HeroName}");
        }

        public void SelectEnemyHeroByAssetName(string assetName)
        {
            var hero = GetHeroByAssetName(assetName);
            if (hero == null)
            {
                return;
            }

            _selectedEnemyHero = hero;
            Debug.Log($"[Menu] для ворога assetName={hero.name}, heroName={hero.HeroName}");
        }

        public void StartBattle()
        {
            if ((_selectedPlayerHero == null || _selectedEnemyHero == null) &&
                (_availableHeroes == null || _availableHeroes.Length == 0))
            {
                return;
            }

            if (_selectedPlayerHero == null)
            {
                _selectedPlayerHero = _availableHeroes[0];
            }

            if (_selectedEnemyHero == null)
            {
                _selectedEnemyHero = _availableHeroes.Length > 1 ? _availableHeroes[1] : _availableHeroes[0];
            }

            GameSession.PlayerHeroConfig = _selectedPlayerHero;
            GameSession.EnemyHeroConfig  = _selectedEnemyHero;

            if (string.IsNullOrWhiteSpace(_battleSceneName))
            {
                return;
            }

            SceneManager.LoadScene(_battleSceneName);
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        private HeroConfig GetHeroByIndex(int index)
        {
            if (_availableHeroes == null || index < 0 || index >= _availableHeroes.Length)
            {
                return null;
            }

            var hero = _availableHeroes[index];

            return hero;
        }

        private HeroConfig GetHeroByAssetName(string assetName)
        {
            if (_availableHeroes == null)
                return null;

            foreach (var hero in _availableHeroes)
            {
                if (hero == null) continue;
                if (hero.name == assetName)
                    return hero;
            }

            return null;
        }
    }
}
