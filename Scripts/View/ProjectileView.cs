using UnityEngine;
using System.Collections;

namespace TacticalHex
{

    public class ProjectileView : MonoBehaviour
    {
        [SerializeField] private float _speed = 15f;
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private float _lifeAfterHit = 0.05f;

        public void Launch(Vector3 start, Vector3 end)
        {
            StartCoroutine(LaunchRoutine(start, end));
        }

        private IEnumerator LaunchRoutine(Vector3 start, Vector3 end)
        {
            transform.position = start;

            float distance = Vector3.Distance(start, end);
            if (distance < 0.001f)
            {
                Destroy(gameObject);
                yield break;
            }

            float duration = distance / Mathf.Max(0.01f, _speed);
            float elapsed = 0f;

            if (_lineRenderer != null)
            {
                _lineRenderer.positionCount = 2;
                _lineRenderer.SetPosition(0, start);
                _lineRenderer.SetPosition(1, start);
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector3 pos = Vector3.Lerp(start, end, t);
                transform.position = pos;

                if (_lineRenderer != null)
                {
                    _lineRenderer.SetPosition(0, start);
                    _lineRenderer.SetPosition(1, pos);
                }

                yield return null;
            }

            transform.position = end;

            if (_lineRenderer != null)
            {
                _lineRenderer.SetPosition(0, start);
                _lineRenderer.SetPosition(1, end);
            }

            if (_lifeAfterHit > 0f)
                yield return new WaitForSeconds(_lifeAfterHit);

            Destroy(gameObject);
        }
    }
}
