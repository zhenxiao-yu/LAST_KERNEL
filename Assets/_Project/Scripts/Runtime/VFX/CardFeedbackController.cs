using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    // Additive VFX overlay for cards using All In 1 Sprite Shader.
    // Works via child SpriteRenderers — does NOT touch Card.shadergraph or CardFeelPresenter.
    // Add to a card prefab alongside (not replacing) CardFeelPresenter.
    [AddComponentMenu("Last Kernel/VFX/Card Feedback Controller")]
    public class CardFeedbackController : MonoBehaviour
    {
        enum PersistentState { None, Hover, Selected, RarePulse }

        // ── Overlay Renderers ─────────────────────────────────────────────────
        // If left null, child GameObjects are auto-created in Awake.
        [BoxGroup("Renderers")]
        [Tooltip("Persistent states: hover outline, selected outline, rare pulse.")]
        [SerializeField] private SpriteRenderer _persistentRenderer;

        [BoxGroup("Renderers")]
        [Tooltip("Transient flashes: damage, critical, healing.")]
        [SerializeField] private SpriteRenderer _flashRenderer;

        // ── Materials (shared — do NOT instantiate per-card) ─────────────────
        // Shared materials are safe here because SpriteRenderer.color (used for alpha fade)
        // is a per-component property independent of the material. The shader effects
        // (outline, glow) are driven by shader Time and material properties set in the editor.
        [BoxGroup("Materials/Outline")]
        [SerializeField] public Material hoverOutlineMaterial;

        [BoxGroup("Materials/Outline")]
        [SerializeField] public Material selectedOutlineMaterial;

        [BoxGroup("Materials/Flash")]
        [SerializeField] public Material damageFlashMaterial;

        [BoxGroup("Materials/Flash")]
        [SerializeField] public Material criticalFlashMaterial;

        [BoxGroup("Materials/Flash")]
        [SerializeField] public Material healingPulseMaterial;

        [BoxGroup("Materials/Pulse")]
        [SerializeField] public Material rarePulseMaterial;

        // ── Timing ────────────────────────────────────────────────────────────
        [BoxGroup("Timing")]
        [Range(0.02f, 0.3f)]
        [SerializeField] public float hoverFadeDuration = 0.08f;

        [BoxGroup("Timing")]
        [Range(0.05f, 0.5f)]
        [SerializeField] public float flashDuration = 0.12f;

        // ── Options ───────────────────────────────────────────────────────────
        [BoxGroup("Options")]
        [SerializeField] public bool enableRarePulse;

        [BoxGroup("Options")]
        [Tooltip("Sorting order added on top of the card's MeshRenderer. Adjust if overlay clips incorrectly.")]
        [SerializeField] private int _sortingOrderOffset = 50;

        // ── Runtime ───────────────────────────────────────────────────────────
        private PersistentState _persistentState;
        private Tween _persistentFade;
        private Tween _flashDelay;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            EnsureRenderers();
            _persistentRenderer.enabled = false;
            _flashRenderer.enabled = false;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void SetHover(bool active)
        {
            if (active)
            {
                if (_persistentState == PersistentState.Selected) return;
                ShowPersistent(hoverOutlineMaterial, PersistentState.Hover);
            }
            else
            {
                if (_persistentState != PersistentState.Hover) return;
                HidePersistent(restoreRarePulse: enableRarePulse);
            }
        }

        public void SetSelected(bool active)
        {
            if (active)
                ShowPersistent(selectedOutlineMaterial, PersistentState.Selected);
            else
                HidePersistent(restoreRarePulse: enableRarePulse);
        }

        public void PlayDamageFlash() => PlayFlash(damageFlashMaterial, flashDuration);

        public void PlayCriticalFlash() => PlayFlash(criticalFlashMaterial, flashDuration);

        // Healing pulse is longer than damage flash — subtle, not alarming.
        public void PlayHealingPulse() => PlayFlash(healingPulseMaterial, flashDuration * 2f);

        public void SetRarePulse(bool active)
        {
            enableRarePulse = active;
            if (active && _persistentState == PersistentState.None)
                ShowPersistent(rarePulseMaterial, PersistentState.RarePulse);
            else if (!active && _persistentState == PersistentState.RarePulse)
                HidePersistent();
        }

        public void ClearAll()
        {
            _persistentFade?.Kill();
            _flashDelay?.Kill();
            if (_persistentRenderer != null) _persistentRenderer.enabled = false;
            if (_flashRenderer != null) _flashRenderer.enabled = false;
            _persistentState = PersistentState.None;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void ShowPersistent(Material mat, PersistentState state)
        {
            if (mat == null) return;
            _persistentFade?.Kill();
            _persistentRenderer.sharedMaterial = mat;
            _persistentState = state;
            SetAlpha(_persistentRenderer, 0f);
            _persistentRenderer.enabled = true;
            // DOFade on SpriteRenderer requires DOTween sprite module; use DOTween.To instead.
            _persistentFade = DOTween
                .To(() => _persistentRenderer.color.a, a => SetAlpha(_persistentRenderer, a), 1f, hoverFadeDuration)
                .SetLink(gameObject);
        }

        private void HidePersistent(bool restoreRarePulse = false)
        {
            _persistentFade?.Kill();
            _persistentFade = DOTween
                .To(() => _persistentRenderer.color.a, a => SetAlpha(_persistentRenderer, a), 0f, hoverFadeDuration)
                .OnComplete(() =>
                {
                    if (_persistentRenderer != null) _persistentRenderer.enabled = false;
                    _persistentState = PersistentState.None;
                    if (restoreRarePulse && rarePulseMaterial != null)
                        ShowPersistent(rarePulseMaterial, PersistentState.RarePulse);
                })
                .SetLink(gameObject);
        }

        private void PlayFlash(Material mat, float duration)
        {
            if (mat == null) return;
            _flashDelay?.Kill();
            _flashRenderer.sharedMaterial = mat;
            SetAlpha(_flashRenderer, 1f);
            _flashRenderer.enabled = true;
            _flashDelay = DOVirtual
                .DelayedCall(duration, () => { if (_flashRenderer != null) _flashRenderer.enabled = false; })
                .SetLink(gameObject);
        }

        private static void SetAlpha(SpriteRenderer sr, float a)
        {
            var c = sr.color;
            c.a = a;
            sr.color = c;
        }

        private void EnsureRenderers()
        {
            if (_persistentRenderer == null)
                _persistentRenderer = GetOrCreateChildRenderer("FX_Persistent", _sortingOrderOffset);
            if (_flashRenderer == null)
                _flashRenderer = GetOrCreateChildRenderer("FX_Flash", _sortingOrderOffset + 10);
        }

        private SpriteRenderer GetOrCreateChildRenderer(string childName, int sortingOffset)
        {
            var t = transform.Find(childName);
            if (t == null)
            {
                t = new GameObject(childName).transform;
                t.SetParent(transform, false);
                t.localPosition = Vector3.zero;
                t.localScale = Vector3.one;
            }

            var sr = t.GetComponent<SpriteRenderer>() ?? t.gameObject.AddComponent<SpriteRenderer>();

            // Mirror sorting from card's MeshRenderer so overlay stays above it at all states.
            var parentMesh = GetComponentInChildren<MeshRenderer>();
            if (parentMesh != null)
            {
                sr.sortingLayerName = parentMesh.sortingLayerName;
                sr.sortingOrder = parentMesh.sortingOrder + sortingOffset;
            }

            return sr;
        }
    }
}
