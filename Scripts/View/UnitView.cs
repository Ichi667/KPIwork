using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TacticalHex
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class UnitView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private TextMeshPro _countText;

        [Header("Movement animation")]
        [SerializeField] private float _moveDurationPerHex = 0.15f;

        [Header("Melee attack animation")]
        [SerializeField] private float _meleeLungeDistance = 0.25f;
        [SerializeField] private float _meleeLungeDuration = 0.12f;

        [Header("Ranged attack (не робить, колись пофікшу)")]
        [SerializeField] private ProjectileView _projectilePrefab;

        public UnitModel Model { get; private set; }
        public BattleController Controller { get; set; }

        private Color _baseColor;
        private Vector3 _baseScale;
        private Coroutine _animCoroutine;

        private void Awake()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Init(UnitModel model)
        {
            Model = model;

            if (Model.CurrentHex != null)
                transform.position = Model.CurrentHex.WorldPosition;

            name = $"Unit_{Model.InstanceName}_{Model.Faction}";

            if (Model.Config != null && Model.Config.Icon != null)
                _spriteRenderer.sprite = Model.Config.Icon;

            FitToHex();
            _baseScale = transform.localScale;

            UpdateBaseColor();
            UpdateCountText();
            SetActive(false);
        }

        private void UpdateBaseColor()
        {
            if (_spriteRenderer == null || Model == null)
                return;

            _baseColor = Color.white;
            _spriteRenderer.color = _baseColor;
        }


        public void SetActive(bool isActive)
        {
            if (_spriteRenderer == null)
                return;

            _spriteRenderer.color = isActive ? Color.yellow : _baseColor;
        }

        public void Refresh()
        {
            UpdateCountText();
        }

        private void UpdateCountText()
        {
            if (_countText == null || Model == null)
                return;

            if (Model.UnitCount > 1)
            {
                _countText.gameObject.SetActive(true);
                _countText.text = Model.UnitCount.ToString();
            }
            else
            {
                _countText.gameObject.SetActive(false);
            }
        }

        public void AnimateMoveAlongPath(IReadOnlyList<HexModel> path)
        {
            if (!isActiveAndEnabled)
                return;

            if (path == null || path.Count <= 1)
                return;

            if (_animCoroutine != null)
                StopCoroutine(_animCoroutine);

            _animCoroutine = StartCoroutine(MoveAlongPathCoroutine(path));
        }

        private IEnumerator MoveAlongPathCoroutine(IReadOnlyList<HexModel> path)
        {
            for (int i = 1; i < path.Count; i++)
            {
                var from = path[i - 1];
                var to = path[i];

                Vector3 startPos = from.WorldPosition;
                Vector3 endPos = to.WorldPosition;

                float distance = Vector3.Distance(startPos, endPos);
                float duration = Mathf.Max(0.05f, _moveDurationPerHex * distance);

                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    transform.position = Vector3.Lerp(startPos, endPos, t);
                    yield return null;
                }

                transform.position = endPos;
            }

            _animCoroutine = null;
        }

        private void FitToHex()
        {
            if (Controller == null)
                return;
            if (_spriteRenderer == null || _spriteRenderer.sprite == null)
                return;

            Vector2 hexSize = Controller.GetHexWorldSize();
            if (hexSize == Vector2.zero)
                return;

            Vector2 spriteSize = _spriteRenderer.sprite.bounds.size;

            float scaleX = hexSize.x / spriteSize.x;
            float scaleY = hexSize.y / spriteSize.y;

            float scale = Mathf.Min(scaleX, scaleY) * 0.7f;

            transform.localScale = Vector3.one * scale;
        }

        public void PlayMeleeAttack(Vector3 targetWorldPos)
        {
            if (!isActiveAndEnabled)
                return;

            if (_animCoroutine != null)
                StopCoroutine(_animCoroutine);

            _animCoroutine = StartCoroutine(MeleeAttackCoroutine(targetWorldPos));
        }

        private IEnumerator MeleeAttackCoroutine(Vector3 targetWorldPos)
        {
            Vector3 origin = transform.position;
            Vector3 dir = targetWorldPos - origin;
            dir.z = 0f;

            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector3.right;
            else
                dir.Normalize();

            Vector3 attackPos = origin + dir * _meleeLungeDistance;

            float halfDuration = Mathf.Max(0.02f, _meleeLungeDuration * 0.5f);

            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                transform.position = Vector3.Lerp(origin, attackPos, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                transform.position = Vector3.Lerp(attackPos, origin, t);
                yield return null;
            }

            transform.position = origin;
            _animCoroutine = null;
        }

        public void PlayRangedAttack(Vector3 targetWorldPos)
        {
            if (!isActiveAndEnabled)
                return;

            if (_projectilePrefab == null)
                return;

            var projectile = Instantiate(
                _projectilePrefab,
                transform.position,
                Quaternion.identity
            );

            projectile.Launch(transform.position, targetWorldPos);
        }

        private void OnMouseDown()
        {
            Debug.Log($"[UnitView] Click {Model?.Name} {Model?.Faction}");
        }
    }
}
