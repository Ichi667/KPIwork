using UnityEngine;

namespace TacticalHex
{
    public enum HexHighlightType
    {
        None,
        Move,
        Attack
    }

    [RequireComponent(typeof(SpriteRenderer))]
    public class HexView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Base color")]
        [SerializeField] private Color _baseColor = Color.white;

        [Header("Highlight colors")]
        [SerializeField] private Color _moveHighlightColor = new Color(0f, 1f, 0f, 0.5f);
        [SerializeField] private Color _attackHighlightColor = new Color(1f, 0f, 0f, 0.5f);

        public HexModel Model { get; private set; }
        public BattleController Controller { get; set; }

        public HexHighlightType CurrentHighlight { get; private set; } = HexHighlightType.None;

        private void Awake()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_spriteRenderer != null && _baseColor == default)
                _baseColor = _spriteRenderer.color;

            if (_spriteRenderer != null)
                _spriteRenderer.color = _baseColor;
        }

        public void Init(HexModel model)
        {
            Model = model;

            if (Model != null)
                transform.position = Model.WorldPosition;

            if (_spriteRenderer != null)
                _spriteRenderer.color = _baseColor;

            CurrentHighlight = HexHighlightType.None;
        }

        public void SetHighlight(HexHighlightType type)
        {
            CurrentHighlight = type;

            if (_spriteRenderer == null)
                return;

            switch (type)
            {
                case HexHighlightType.None:
                    _spriteRenderer.color = _baseColor;
                    break;

                case HexHighlightType.Move:
                    _spriteRenderer.color = _moveHighlightColor;
                    break;

                case HexHighlightType.Attack:
                    _spriteRenderer.color = _attackHighlightColor;
                    break;
            }
        }

        public void ResetHighlight()
        {
            SetHighlight(HexHighlightType.None);
        }

        private void OnMouseDown()
        {
            Debug.Log($"[HexView] OnMouseDown{Model?.Q},{Model?.R}");
        }
    }
}
