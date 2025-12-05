using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;


namespace TacticalHex
{
    public class BattleController : MonoBehaviour
    {
        [Header("Grid settings")]
        [SerializeField] private int _width = 15;
        [SerializeField] private int _height = 11;

        [Header("Pause menu")]
        [SerializeField] private GameObject _pauseMenuRoot;

        
        [Header("Grid references")]
        [SerializeField] private HexView _hexPrefab;
        [SerializeField] private Transform _hexGridRoot;

        [Header("Opportunity attack")]
        [SerializeField] private float _opportunityAttackDelay = 0.5f;

        [Header("Unit references")]
        [SerializeField] private UnitView _unitPrefab;
        [SerializeField] private Transform _unitsRoot;

        [Header("Heroes")]
        [SerializeField] private HeroConfig _defaultPlayerHeroConfig;
        [SerializeField] private HeroConfig _defaultEnemyHeroConfig;

        [Header("UI")]
        [SerializeField] private UnitInfoPanel _unitInfoPanel;

        [Header("AI")]
        [SerializeField] private AIController _aiController;

        [Header("Scenes")]
        [SerializeField] private string _menuSceneName = "MainMenu";
        public Vector2 HexWorldSize { get; private set; }

        public BattleState State { get; private set; }
        public UnitInfoPanel UnitInfoPanel => _unitInfoPanel;
        [Header("Turn delay")]
        [SerializeField] private float _turnDelaySeconds = 1f;

        private bool _inputLocked;
        private Coroutine _turnDelayCoroutine; 
        private bool _isPaused;
        private HeroModel _playerHero;
        private HeroModel _enemyHero;

        private readonly Dictionary<UnitModel, UnitView> _unitViews =
            new Dictionary<UnitModel, UnitView>();

        private readonly List<HexView> _hexViews = new List<HexView>();

        private List<UnitModel> _turnOrder;
        private int _currentTurnIndex = -1;
        private UnitModel _currentUnit;

        private bool _battleEnded;
        private Faction? _winnerFaction;

        private void Awake()
        {
            State = new BattleState(_width, _height);

            CreateHeroes();
            GenerateGrid();
            SpawnInitialUnits();
            InitializeTurnOrder();

            if (_aiController == null)
                _aiController = GetComponent<AIController>();

            StartNextTurnImmediate();
        }

        private void Update()
        {
            if (_battleEnded)
                return;

            if (Keyboard.current != null && 
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
                return;
            }

        if (_isPaused)
            return;

            if (_inputLocked)
                return;

            if (Mouse.current == null)
                return;

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                HandleRightClick();
                return;
            }

            if (!Mouse.current.leftButton.wasPressedThisFrame)
                return;

            if (Camera.main == null)
            {
                return;
            }

            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, 0f)
            );

            Vector2 origin = new Vector2(worldPos.x, worldPos.y);
            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.zero);

            if (hits == null || hits.Length == 0)
                return;

            UnitView unitView = null;
            HexView hexView = null;

            foreach (var hit in hits)
            {
                if (hit.collider == null)
                    continue;

                if (unitView == null)
                    unitView = hit.collider.GetComponent<UnitView>();

                if (hexView == null)
                    hexView = hit.collider.GetComponent<HexView>();
            }

            if (unitView != null)
            {
                OnUnitClicked(unitView);
                return;
            }

            if (hexView != null)
            {
                OnHexClicked(hexView);
                return;
            }
        }

        private void HandleRightClick()
        {
            if (Camera.main == null)
            {
                return;
            }

            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, 0f)
            );

            Vector2 origin = new Vector2(worldPos.x, worldPos.y);
            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.zero);

            if (hits == null || hits.Length == 0)
            {
                _unitInfoPanel?.Clear();
                return;
            }

            UnitView unitView = null;

            foreach (var hit in hits)
            {
                if (hit.collider == null)
                    continue;

                if (unitView == null)
                    unitView = hit.collider.GetComponent<UnitView>();
            }

            if (unitView != null && unitView.Model != null)
            {
                _unitInfoPanel?.ShowUnit(unitView.Model);
            }
            else
            {
                _unitInfoPanel?.Clear();
            }
        }

        private void CreateHeroes()
        {
            HeroConfig playerConfig = GameSession.PlayerHeroConfig ?? _defaultPlayerHeroConfig;
            HeroConfig enemyConfig  = GameSession.EnemyHeroConfig  ?? _defaultEnemyHeroConfig;

            if (playerConfig != null)
                _playerHero = new HeroModel(playerConfig);
            else
                _playerHero = new HeroModel("Герой ігрока", Faction.Player, 2, 1);

            if (enemyConfig != null)
                _enemyHero = new HeroModel(enemyConfig);
            else
                _enemyHero = new HeroModel("Герой ШІ", Faction.Enemy, 2, 1);
        }

        #region Grid

        private void GenerateGrid()
        {
            if (_hexPrefab == null || _hexGridRoot == null)
            {
                return;
            }

            var spriteRenderer = _hexPrefab.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                return;
            }

            float hexWidth  = spriteRenderer.bounds.size.x;
            float hexHeight = spriteRenderer.bounds.size.y;

            float xOffset = hexWidth * 0.75f;
            float yOffset = hexHeight;

            float gridWidthWorld  = (_width  - 1) * xOffset;
            float gridHeightWorld = (_height - 1) * yOffset;

            float xOrigin = -gridWidthWorld  / 2f;
            float yOrigin = -gridHeightWorld / 2f;

            HexWorldSize = new Vector2(hexWidth, hexHeight);

            _hexViews.Clear();

            for (int q = 0; q < _width; q++)
            {
                for (int r = 0; r < _height; r++)
                {
                    float xPos = xOrigin + q * xOffset;
                    float yPos = yOrigin + r * yOffset;

                    if (q % 2 == 1)
                        yPos += yOffset / 2f;

                    var model = new HexModel(q, r);
                    model.WorldPosition = new Vector3(xPos, yPos, 0f);

                    var view = Instantiate(
                        _hexPrefab,
                        model.WorldPosition,
                        Quaternion.identity,
                        _hexGridRoot
                    );

                    view.Controller = this;
                    view.Init(model);
                    State.SetHex(q, r, model);
                    _hexViews.Add(view);
                }
            }
        }

        public Vector2 GetHexWorldSize()
        {
            if (_hexPrefab == null)
                return Vector2.zero;

            var sr = _hexPrefab.GetComponent<SpriteRenderer>();
            if (sr == null)
                return Vector2.zero;

            return sr.bounds.size;
        }


        #endregion

        #region Units spawn

        private void SpawnInitialUnits()
        {
            if (_unitPrefab == null || _unitsRoot == null)
            {
                return;
            }

            SpawnHeroArmy(_playerHero);
            SpawnHeroArmy(_enemyHero);
        }

        private void SpawnHeroArmy(HeroModel hero)
        {
            if (hero == null || hero.Config == null || hero.Config.Army == null)
                return;

            var army = hero.Config.Army;
            int unitsPerSide = army.Length;

            int startRow = (_height - unitsPerSide) / 2;
            int column = hero.Faction == Faction.Player ? 0 : _width - 1;

            for (int i = 0; i < unitsPerSide; i++)
            {
                var slot = army[i];
                if (slot == null || slot.Unit == null || slot.Count <= 0)
                    continue;

                int r = startRow + i;
                string instanceName = hero.Faction == Faction.Player
                    ? $"P_{i + 1}"
                    : $"E_{i + 1}";

                SpawnSingleUnit(slot.Unit, instanceName, hero.Faction, hero, column, r, slot.Count);
            }
        }

        private void SpawnSingleUnit(
            UnitConfig config,
            string instanceName,
            Faction faction,
            HeroModel hero,
            int q,
            int r,
            int stackSize)
        {
            var hex = State.GetHex(q, r);
            if (hex == null || config == null)
                return;

            int count = stackSize > 0 ? stackSize : config.DefaultStackSize;
            if (count < 1)
                count = 1;

            var model = new UnitModel(config, instanceName, faction, hero, count);
            model.SetHex(hex);
            State.AddUnit(model);

            var view = Instantiate(
                _unitPrefab,
                hex.WorldPosition,
                Quaternion.identity,
                _unitsRoot
            );

        view.Controller = this;
        view.Init(model);  

            _unitViews[model] = view;
        }

        #endregion

        #region Turn order

        private void InitializeTurnOrder()
        {
            _turnOrder = new List<UnitModel>(State.Units);
            _turnOrder.Sort((a, b) => b.Initiative.CompareTo(a.Initiative));
            _currentTurnIndex = -1;
        }

        private void ClearAllHexHighlights()
        {
            foreach (var hexView in _hexViews)
                hexView?.ResetHighlight();
        }


        private void HighlightAvailableMoves(UnitModel unit)
        {
            ClearAllHexHighlights();

            if (unit == null || unit.CurrentHex == null)
                return;

            foreach (var hexView in _hexViews)
            {
                if (hexView == null || hexView.Model == null)
                    continue;

                var hex = hexView.Model;

                var unitThere = State.GetUnitAt(hex);
                if (unitThere != null && unitThere != unit)
                    continue;

                int dist = HexModel.Distance(unit.CurrentHex, hex);
                if (dist <= 0 || dist > unit.Speed)
                    continue;


                var path = FindPath(unit, hex);
                if (path != null)
                    hexView.SetHighlight(HexHighlightType.Move);
            }
        }

                private void StartNextTurn()
        {
            if (_battleEnded)
                return;


            if (_turnDelayCoroutine != null)
                return;

            _turnDelayCoroutine = StartCoroutine(StartNextTurnDelayed());
        }

        private System.Collections.IEnumerator StartNextTurnDelayed()
        {
            _inputLocked = true;                             
            yield return new WaitForSeconds(_turnDelaySeconds);
            _inputLocked = false;
            _turnDelayCoroutine = null;

            StartNextTurnImmediate();    
        }

        private void StartNextTurnImmediate()
        {
            CheckBattleEnd();
            if (_battleEnded)
                return;

            foreach (var view in _unitViews.Values)
                view.SetActive(false);

            if (_turnOrder == null || _turnOrder.Count == 0)
                return;

            int safety = 0;
            do
            {
                _currentTurnIndex++;
                if (_currentTurnIndex >= _turnOrder.Count)
                    _currentTurnIndex = 0;

                _currentUnit = _turnOrder[_currentTurnIndex];
            }
            while ((_currentUnit == null || !_currentUnit.IsAlive) &&
                safety++ < _turnOrder.Count);

            if (_currentUnit == null || !_currentUnit.IsAlive)
            {
                Debug.Log("нема доступних бнітів.");
                _unitInfoPanel?.Clear();
                ClearAllHexHighlights();
                return;
            }

            if (_unitViews.TryGetValue(_currentUnit, out var currentView))
                currentView.SetActive(true);

            if (_currentUnit.Faction == Faction.Player)
            {
                Debug.Log($"Хід ігрока: [{_currentUnit.Name}] x{_currentUnit.UnitCount}");
                HighlightAvailableMoves(_currentUnit);
            }
            else
            {
                Debug.Log($"Хід юнита ШІ: [{_currentUnit.Name}] x{_currentUnit.UnitCount}");
                if (_aiController != null)
                    _aiController.TakeTurn(_currentUnit);
                else
                    ExecuteSimpleEnemyTurn(_currentUnit);
            }
        }


        public void OnEndTurnButton()
        {
            if (_battleEnded)
                return;

            StartNextTurn();
        }

        #endregion

        #region Click handlers

        public void OnUnitClicked(UnitView view)
        {
            if (view == null || view.Model == null)
                return;

            var clickedUnit = view.Model;

            if (_battleEnded)
                return;

            if (_currentUnit == null || !_currentUnit.IsAlive)
                return;

            if (_currentUnit == clickedUnit)
            {
                if (_currentUnit.Faction == Faction.Player)
                    HighlightAvailableMoves(_currentUnit);
                return;
            }

            if (_currentUnit.Faction != Faction.Player)
                return;

            if (clickedUnit.Faction == _currentUnit.Faction)
                return;

            TryAttack(_currentUnit, clickedUnit);
        }

        public void OnHexClicked(HexView hexView)
        {
            if (hexView == null || hexView.Model == null)
                return;

            if (_battleEnded)
                return;

            if (_currentUnit == null || !_currentUnit.IsAlive)
                return;

            if (_currentUnit.Faction != Faction.Player)
                return;

            var unitThere = State.GetUnitAt(hexView.Model);
            if (unitThere != null)
                return;

            TryMove(_currentUnit, hexView.Model);
        }

        #endregion

        #region Movement / pathfinding / opportunity attack

        private void TryMove(UnitModel unit, HexModel targetHex)
        {
            PerformMove(unit, targetHex, endTurn: true);
        }


        /// тут я юзав BFS 
        private List<HexModel> FindPath(UnitModel unit, HexModel target)
        {
            if (unit == null || unit.CurrentHex == null || target == null)
                return null;

            var start = unit.CurrentHex;
            if (start == target)
                return new List<HexModel> { start };

            var frontier = new Queue<HexModel>();
            var cameFrom = new Dictionary<HexModel, HexModel>();

            frontier.Enqueue(start);
            cameFrom[start] = null;

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();

                if (current == target)
                    break;

                foreach (var neighbor in GetNeighbors(current))
                {
                    if (cameFrom.ContainsKey(neighbor))
                        continue;

                    var unitThere = State.GetUnitAt(neighbor);
                    if (unitThere != null && unitThere != unit)
                        continue;

                    int distFromStart = HexModel.Distance(start, neighbor);
                    if (distFromStart > unit.Speed)
                        continue;

                    frontier.Enqueue(neighbor);
                    cameFrom[neighbor] = current;
                }
            }

            if (!cameFrom.ContainsKey(target))
                return null;

            var path = new List<HexModel>();
            var cur = target;
            while (cur != null)
            {
                path.Add(cur);
                cur = cameFrom[cur];
            }
            path.Reverse();
            return path;
        }

        private IEnumerable<HexModel> GetNeighbors(HexModel hex)
        {
            foreach (var hv in _hexViews)
            {
                var h = hv.Model;
                if (h == null || h == hex)
                    continue;

                if (HexModel.Distance(hex, h) == 1)
                    yield return h;
            }
        }

        private bool PerformMove(UnitModel unit, HexModel targetHex, bool endTurn)
        {
            if (unit == null || targetHex == null)
                return false;

            if (unit.CurrentHex == null)
                return false;

            var path = FindPath(unit, targetHex);
            if (path == null || path.Count == 0)
            {
                Debug.Log("Путя нема.");
                return false;
            }

            ResolveOpportunityAttacksOnMoveStart(unit);

            if (!unit.IsAlive)
            {
                if (_unitViews.TryGetValue(unit, out var deadView))
                    Destroy(deadView.gameObject);

                CheckBattleEnd();

                if (!_battleEnded && endTurn)
                    StartNextTurn();

                return false;
            }

            StartCoroutine(MoveAfterOpportunity(unit, targetHex, path, endTurn));

            return true;
        }

        private IEnumerator MoveAfterOpportunity(
            UnitModel unit,
            HexModel targetHex,
            List<HexModel> path,
            bool endTurn)
        {
            yield return new WaitForSeconds(_opportunityAttackDelay);

            if (!unit.IsAlive || _battleEnded)
                yield break;

            unit.SetHex(targetHex);

            if (_unitViews.TryGetValue(unit, out var view))
                view.AnimateMoveAlongPath(path);

            if (endTurn)
            {
                StartNextTurn();
            }
            else if (unit.Faction == Faction.Player)
            {
                HighlightAvailableMoves(unit);
            }
        }

        private void ResolveOpportunityAttacksOnMoveStart(UnitModel mover)
        {
            if (mover == null || mover.CurrentHex == null)
                return;

            var attackers = new List<UnitModel>();

            foreach (var other in State.Units)
            {
                if (!other.IsAlive)
                    continue;

                if (!other.OpportunityAttackEnabled)
                    continue;

                if (other.Faction == mover.Faction)
                    continue;

                if (other.CurrentHex == null)
                    continue;

                int dist = HexModel.Distance(other.CurrentHex, mover.CurrentHex);
                if (dist == 1)
                    attackers.Add(other);
            }

            foreach (var attacker in attackers)
            {
                ApplyOpportunityAttack(attacker, mover);
                if (!mover.IsAlive)
                    break;
            }
        }

        private void ApplyOpportunityAttack(UnitModel attacker, UnitModel mover)
        {
            if (attacker == null || mover == null)
                return;
            if (!attacker.IsAlive || !mover.IsAlive)
                return;

            int baseDamagePerUnit = Random.Range(attacker.MinDamage, attacker.MaxDamage + 1);
            int count = attacker.UnitCount;

            float damage = baseDamagePerUnit * count;

            int att = attacker.EffectiveAttack;
            int def = mover.EffectiveDefense;
            float mod = 1f + 0.05f * (att - def);
            mod = Mathf.Clamp(mod, 0.3f, 3f);

            damage *= mod;
            damage *= attacker.OpportunityAttackMultiplier;

            int finalDamage = Mathf.Max(1, Mathf.RoundToInt(damage));

            mover.TakeDamage(finalDamage);

            Debug.Log(
                $"[{attacker.Name}] ударив на {finalDamage} урона по [{mover.Name}] (атака по можливості)."
            );

            if (_unitViews.TryGetValue(attacker, out var attackerView))
            {
                Vector3 targetPos = mover.CurrentHex != null
                    ? mover.CurrentHex.WorldPosition
                    : attackerView.transform.position;

                attackerView.PlayMeleeAttack(targetPos);
            }

            if (_unitViews.TryGetValue(mover, out var moverView))
            {
                if (mover.IsAlive)
                    moverView.Refresh();
                else
                    Destroy(moverView.gameObject);
            }
        }

        #endregion

        #region Attack

        private void TryAttack(UnitModel attacker, UnitModel defender)
        {
            if (TryApplyAttack(attacker, defender))
                StartNextTurn();
        }

        private bool TryApplyAttack(UnitModel attacker, UnitModel defender)
        {
            if (attacker == null || defender == null)
                return false;

            if (!attacker.IsAlive || !defender.IsAlive)
                return false;

            bool isMelee;
            if (!attacker.CanAttack(defender, out isMelee))
            {
                Debug.Log("ціль далеко.");
                return false;
            }

            int baseDamagePerUnit = Random.Range(attacker.MinDamage, attacker.MaxDamage + 1);
            int count = attacker.UnitCount;

            float damage = baseDamagePerUnit * count;

            int att = attacker.EffectiveAttack;
            int def = defender.EffectiveDefense;
            float mod = 1f + 0.05f * (att - def);
            mod = Mathf.Clamp(mod, 0.3f, 3f);

            damage *= mod;

            if (attacker.IsRanged && HasAdjacentEnemy(attacker))
            {
                damage *= attacker.MeleeDamageMultiplier;
            }

            int finalDamage = Mathf.Max(1, Mathf.RoundToInt(damage));

            defender.TakeDamage(finalDamage);

            Debug.Log(
                $"[{attacker.Name}] x{attacker.UnitCount} вдарили [{defender.Name}] x{defender.UnitCount} на {finalDamage} дамага."
            );

            if (_unitViews.TryGetValue(attacker, out var attackerView))
            {
                Vector3 targetPos = defender.CurrentHex != null
                    ? defender.CurrentHex.WorldPosition
                    : attackerView.transform.position;

                if (isMelee)
                    attackerView.PlayMeleeAttack(targetPos);
                else if (attacker.IsRanged)
                    attackerView.PlayRangedAttack(targetPos);
            }

            if (_unitViews.TryGetValue(defender, out var targetView))
            {
                if (defender.IsAlive)
                    targetView.Refresh();
                else
                    Destroy(targetView.gameObject);
            }

            if (!defender.IsAlive)
            {
                CheckBattleEnd();
            }

            return true;
        }

        private bool HasAdjacentEnemy(UnitModel unit)
        {
            if (unit == null || unit.CurrentHex == null)
                return false;

            foreach (var other in State.Units)
            {
                if (!other.IsAlive)
                    continue;

                if (other.Faction == unit.Faction)
                    continue;

                if (other.CurrentHex == null)
                    continue;

                int dist = HexModel.Distance(unit.CurrentHex, other.CurrentHex);
                if (dist == 1)
                    return true;
            }

            return false;
        }

        #endregion

        #region Enemy AI

        public void EnemyTakeTurn(UnitModel unit)
        {
            if (_battleEnded)
                return;

            if (unit == null || !unit.IsAlive || unit.CurrentHex == null)
            {
                StartNextTurn();
                return;
            }

            List<UnitModel> enemies = new List<UnitModel>();
            foreach (var candidate in State.Units)
            {
                if (candidate == null || !candidate.IsAlive)
                    continue;
                if (candidate.Faction == unit.Faction)
                    continue;
                if (candidate.CurrentHex == null)
                    continue;

                enemies.Add(candidate);
            }

            if (enemies.Count == 0)
            {
                StartNextTurn();
                return;
            }

            UnitModel bestAttackTarget = null;
            float bestAttackScore = float.NegativeInfinity;

            foreach (var target in enemies)
            {
                bool isMelee;
                if (!unit.CanAttack(target, out isMelee))
                    continue;

                float score = EvaluateAttackScore(unit, target, isMelee, unit.CurrentHex);
                if (score > bestAttackScore)
                {
                    bestAttackScore = score;
                    bestAttackTarget = target;
                }
            }

            if (bestAttackTarget != null)
            {
                TryApplyAttack(unit, bestAttackTarget);
                StartNextTurn();
                return;
            }

            HexModel bestMoveHex = null;
            float bestMoveScore = float.NegativeInfinity;

            foreach (var hexView in _hexViews)
            {
                if (hexView == null || hexView.Model == null)
                    continue;

                var hex = hexView.Model;

                var unitThere = State.GetUnitAt(hex);
                if (unitThere != null && unitThere != unit)
                    continue;

                int distFromUnit = HexModel.Distance(unit.CurrentHex, hex);
                if (distFromUnit == 0 || distFromUnit > unit.Speed)
                    continue;

                float score = EvaluateMovePosition(unit, hex, enemies);

                score -= distFromUnit * 0.2f;

                if (score > bestMoveScore)
                {
                    bestMoveScore = score;
                    bestMoveHex = hex;
                }
            }

            if (bestMoveHex != null)
            {
                PerformMove(unit, bestMoveHex, endTurn: true);
                return;
            }

            UnitModel fallbackTarget = ChooseNearestEnemy(unit, enemies);
            if (fallbackTarget != null)
            {
                var moveHex = FindBestMoveTowards(unit, fallbackTarget);
                if (moveHex != null)
                {
                    PerformMove(unit, moveHex, endTurn: true);
                }
                else
                {
                    StartNextTurn();
                }
            }
            else
            {
                StartNextTurn();
            }
        }

        private UnitModel ChooseNearestEnemy(UnitModel unit, List<UnitModel> enemies)
        {
            UnitModel best = null;
            int bestDist = int.MaxValue;

            if (unit == null || unit.CurrentHex == null)
                return null;

            foreach (var candidate in enemies)
            {
                if (candidate == null || !candidate.IsAlive || candidate.CurrentHex == null)
                    continue;

                int dist = HexModel.Distance(unit.CurrentHex, candidate.CurrentHex);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = candidate;
                }
            }

            return best;
        }

        private float EvaluateBaseTargetPriority(UnitModel unit, UnitModel target, HexModel fromHex = null)
        {
            if (unit == null || target == null)
                return 0f;
            if (!target.IsAlive || target.CurrentHex == null)
                return 0f;

            float score = 0f;

            score += target.Attack * target.UnitCount * 0.1f;

            if (target.IsRanged)
            {
                score += unit.IsRanged ? 25f : 15f;
            }

            float hpRatio = (float)target.CurrentTotalHealth / Mathf.Max(1, target.MaxTotalHealth);

            score += (1f - hpRatio) * 15f;

            HexModel refHex = fromHex ?? unit.CurrentHex;
            if (refHex != null && target.CurrentHex != null)
            {
                int dist = HexModel.Distance(refHex, target.CurrentHex);
                score -= dist;
            }

            return score;
        }

        private float EvaluateAttackScore(UnitModel unit, UnitModel target, bool isMeleeAttack, HexModel fromHex)
        {
            float score = EvaluateBaseTargetPriority(unit, target, fromHex);

            float avgPerUnit = 0.5f * (unit.MinDamage + unit.MaxDamage);
            float approxDamage = avgPerUnit * unit.UnitCount;

            if (approxDamage >= target.CurrentTotalHealth)
            {
                score += 50f;
            }
            else
            {
                float dmgRatio = approxDamage / Mathf.Max(1, target.CurrentTotalHealth);
                score += dmgRatio * 20f;
            }

            if (isMeleeAttack && target.IsRanged && !unit.IsRanged)
            {
                score += 40f;
            }

            if (unit.IsRanged && !isMeleeAttack)
            {
                score += 5f;
            }

            return score;
        }

        private float EvaluateMovePosition(UnitModel unit, HexModel hex, List<UnitModel> enemies)
        {
            if (unit == null || hex == null)
                return 0f;

            float bestTargetScoreFromHere = float.NegativeInfinity;

            foreach (var target in enemies)
            {
                if (target == null || !target.IsAlive || target.CurrentHex == null)
                    continue;

                float score = EvaluateBaseTargetPriority(unit, target, hex);

                int dist = HexModel.Distance(hex, target.CurrentHex);

                if (!unit.IsRanged && target.IsRanged && dist == 1)
                    score += 35f;

                if (unit.IsRanged &&
                    dist >= unit.MinRange &&
                    dist <= unit.MaxRange)
                {
                    score += 5f;
                }

                if (score > bestTargetScoreFromHere)
                    bestTargetScoreFromHere = score;
            }

            float danger = 0f;
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive || enemy.CurrentHex == null)
                    continue;

                int distToEnemy = HexModel.Distance(hex, enemy.CurrentHex);

                if (!enemy.IsRanged && distToEnemy == 1)
                    danger += enemy.Attack * enemy.UnitCount * 0.1f;

                if (enemy.IsRanged &&
                    distToEnemy >= enemy.MinRange &&
                    distToEnemy <= enemy.MaxRange)
                {
                    danger += enemy.Attack * enemy.UnitCount * 0.05f;
                }
            }

            float finalScore = bestTargetScoreFromHere - danger;
            return finalScore;
        }

        private void ExecuteSimpleEnemyTurn(UnitModel unit)
        {
            EnemyTakeTurn(unit);
        }

        #endregion


        private float EvaluateBaseTargetPriority(UnitModel unit, UnitModel target)
        {
            if (unit == null || target == null)
                return 0f;
            if (!target.IsAlive || target.CurrentHex == null)
                return 0f;

            float score = 0f;

            score += target.Attack * target.UnitCount * 0.1f;

            if (target.IsRanged)
            {
                score += unit.IsRanged ? 25f : 15f;
            }

            float hpRatio = (float)target.CurrentTotalHealth / Mathf.Max(1, target.MaxTotalHealth);
            score += (1f - hpRatio) * 10f;

            if (unit.CurrentHex != null && target.CurrentHex != null)
            {
                int dist = HexModel.Distance(unit.CurrentHex, target.CurrentHex);
                score -= dist;
            }

            return score;
        }

        private float EvaluateAttackScore(UnitModel unit, UnitModel target, bool isMeleeAttack)
        {
            float score = EvaluateBaseTargetPriority(unit, target);

            float avgPerUnit = 0.5f * (unit.MinDamage + unit.MaxDamage);
            float approxDamage = avgPerUnit * unit.UnitCount;

            if (approxDamage >= target.CurrentTotalHealth)
            {
                score += 50f;
            }
            else
            {
                float dmgRatio = approxDamage / Mathf.Max(1, target.CurrentTotalHealth);
                score += dmgRatio * 20f;
            }

            if (isMeleeAttack && target.IsRanged && !unit.IsRanged)
            {
                score += 40f;
            }

            if (unit.IsRanged && !isMeleeAttack)
            {
                score += 5f;
            }

            return score;
        }


        private HexModel FindBestMoveTowards(UnitModel unit, UnitModel target)
        {
            if (unit.CurrentHex == null || target.CurrentHex == null)
                return null;

            var from = unit.CurrentHex;
            var to   = target.CurrentHex;

            int currentDist = HexModel.Distance(from, to);
            HexModel bestHex = null;
            int bestDistToTarget = currentDist;

            foreach (var view in _hexViews)
            {
                var hex = view.Model;
                if (hex == null)
                    continue;

                if (hex == from)
                    continue;

                var unitThere = State.GetUnitAt(hex);
                if (unitThere != null && unitThere != unit)
                    continue;

                int distFromUnit = HexModel.Distance(from, hex);
                if (distFromUnit == 0 || distFromUnit > unit.Speed)
                    continue;

                int distToTarget = HexModel.Distance(hex, to);
                if (distToTarget < bestDistToTarget)
                {
                    bestDistToTarget = distToTarget;
                    bestHex = hex;
                }
            }

            return bestHex;
        }

        private void TogglePause()
        {
            if (_isPaused)
                ResumeGame();
            else
                PauseGame();
        }

        public void PauseGame()
        {
            if (_isPaused)
                return;

            _isPaused = true;
            _inputLocked = true;
            Time.timeScale = 0f;

            if (_pauseMenuRoot != null)
                _pauseMenuRoot.SetActive(true);
        }

        public void ResumeGame()
        {
            if (!_isPaused)
                return;

            _isPaused = false;
            _inputLocked = false;
            Time.timeScale = 1f;

            if (_pauseMenuRoot != null)
                _pauseMenuRoot.SetActive(false);
        }

        public void ExitToMenuFromPause()
        {
            Time.timeScale = 1f;
            _isPaused = false;
            _inputLocked = false;

            if (!string.IsNullOrEmpty(_menuSceneName))
                SceneManager.LoadScene(_menuSceneName);
        }


        #region Battle end

        private void CheckBattleEnd()
        {
            if (_battleEnded)
                return;

            bool anyPlayer = false;
            bool anyEnemy  = false;

            foreach (var unit in State.Units)
            {
                if (!unit.IsAlive)
                    continue;

                if (unit.Faction == Faction.Player)
                    anyPlayer = true;
                else if (unit.Faction == Faction.Enemy)
                    anyEnemy = true;
            }

            if (!anyPlayer || !anyEnemy)
            {
                _battleEnded = true;

                if (anyPlayer && !anyEnemy)
                    _winnerFaction = Faction.Player;
                else if (anyEnemy && !anyPlayer)
                    _winnerFaction = Faction.Enemy;
                else
                    _winnerFaction = null;

                ClearAllHexHighlights();
                _currentUnit = null;
                _unitInfoPanel?.Clear();

                string msg;
                if (_winnerFaction.HasValue)
                    msg = $"Переможець: {_winnerFaction.Value}";
                else
                    msg = "Нічия.";

                Debug.Log(msg);

                if (!string.IsNullOrEmpty(_menuSceneName))
                    SceneManager.LoadScene(_menuSceneName);
            }
        }

        #endregion
    }
}
