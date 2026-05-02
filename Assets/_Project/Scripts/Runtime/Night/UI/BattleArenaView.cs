using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Markyu.LastKernel
{
    /// <summary>
    /// SAP-style auto-battle arena. Renders as a UGUI canvas overlay during the night
    /// battle phase. Autonomous — subscribes to NightBattleManager static events directly,
    /// no serialized references needed except the GameObject itself in the scene.
    ///
    /// Unit layout (front units closest to the center divider):
    ///   [D4][D3][D2][D1] ⚔ [E1][E2][E3][E4]
    ///
    /// Animation pipeline per tick:
    ///   Events from CombatLane are queued; the drain coroutine plays them one at a time so
    ///   each attack lunges, hits, and resolves visually before the next begins.
    /// </summary>
    public class BattleArenaView : MonoBehaviour
    {
        // ── Layout constants ──────────────────────────────────────────────────
        private const float PanelW       = 920f;
        private const float PanelH       = 190f;
        private const float SlotSpacing  = 92f;
        private const float CenterGap    = 60f;  // gap either side of center divider
        private const float AnimDelay    = 0.08f; // seconds between queued events

        // ── Runtime state ─────────────────────────────────────────────────────
        private Canvas      _canvas;
        private GameObject  _panel;
        private CombatLane  _lane;

        private readonly List<BattleUnitView> _defViews = new();
        private readonly List<BattleUnitView> _eneViews = new();

        private readonly Queue<(CombatUnit atk, CombatUnit tgt, int dmg, bool crit)> _events = new();
        private bool _drainingQueue;
        private bool _fastForward;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            NightBattleManager.OnBattleStarted    += HandleBattleStarted;
            NightBattleManager.OnBattleComplete   += HandleBattleComplete;
            NightBattleManager.OnFastResolveEnabled += HandleFastResolve;
        }

        private void OnDestroy()
        {
            NightBattleManager.OnBattleStarted    -= HandleBattleStarted;
            NightBattleManager.OnBattleComplete   -= HandleBattleComplete;
            NightBattleManager.OnFastResolveEnabled -= HandleFastResolve;
            UnbindLane();
        }

        // ── NightBattleManager handlers ───────────────────────────────────────

        private void HandleBattleStarted(CombatLane lane, NightWaveDefinition wave)
        {
            _fastForward = false;
            _lane = lane;
            _events.Clear();

            try
            {
                BuildArena(lane);
            }
            catch (Exception e)
            {
                Debug.LogError($"[BattleArenaView] BuildArena threw: {e.Message}");
            }

            BindLane(lane);
        }

        private void HandleBattleComplete(NightCombatResult result)
        {
            if (result == null) return;
            StartCoroutine(FinishAndFade(result.PlayerWon));
        }

        private void HandleFastResolve()
        {
            _fastForward = true;
            foreach (var v in _defViews) v.SetFastForward(true);
            foreach (var v in _eneViews) v.SetFastForward(true);
        }

        // ── CombatLane event binding ──────────────────────────────────────────

        private void BindLane(CombatLane lane)
        {
            lane.OnAttackResolved += OnAttackResolved;
            lane.OnUnitDied       += OnUnitDied;
        }

        private void UnbindLane()
        {
            if (_lane == null) return;
            _lane.OnAttackResolved -= OnAttackResolved;
            _lane.OnUnitDied       -= OnUnitDied;
            _lane = null;
        }

        private void OnAttackResolved(CombatUnit atk, CombatUnit tgt, int dmg, bool crit)
        {
            _events.Enqueue((atk, tgt, dmg, crit));
            if (!_drainingQueue) StartCoroutine(DrainQueue());
        }

        private void OnUnitDied(CombatUnit unit)
        {
            // Death is handled inside DrainQueue after the hit animation resolves.
            // No extra enqueue needed — the HP drain to 0 triggers the visual.
        }

        // ── Animation queue ───────────────────────────────────────────────────

        private IEnumerator DrainQueue()
        {
            _drainingQueue = true;
            while (_events.Count > 0)
            {
                var (atk, tgt, dmg, crit) = _events.Dequeue();
                yield return PlayAttackEvent(atk, tgt, dmg, crit);

                if (_fastForward)
                {
                    while (_events.Count > 0)
                    {
                        var evt = _events.Dequeue();
                        FindView(evt.tgt)?.AnimateHP();
                    }
                    UpdateFrontHighlights();
                    break;
                }

                yield return new WaitForSeconds(AnimDelay);
            }
            _drainingQueue = false;
        }

        private IEnumerator PlayAttackEvent(CombatUnit atk, CombatUnit tgt, int dmg, bool crit)
        {
            var atkView = FindView(atk);
            var tgtView = FindView(tgt);

            if (atkView == null) yield break;

            // 1. Attacker lunges
            var lunge = atkView.AnimateAttack();
            float lungeTime = _fastForward ? 0.01f : 0.12f;
            yield return new WaitForSeconds(lungeTime);

            if (dmg > 0)
            {
                // 2. Hit flash + shake on target
                tgtView?.AnimateHit(crit);

                // 3. Floating damage number
                if (tgtView != null)
                    SpawnDamageNumber(tgtView, dmg, crit);

                // 4. HP bar drain
                tgtView?.AnimateHP();

                yield return new WaitForSeconds(_fastForward ? 0.01f : 0.10f);

                // 5. Death pop if target died
                if (tgtView != null && !tgt.IsAlive)
                {
                    var deathSeq = tgtView.AnimateDeath();
                    float deathTime = _fastForward ? 0.01f : 0.28f;
                    yield return new WaitForSeconds(deathTime);
                    UpdateFrontHighlights();
                }
            }
            else
            {
                // Miss — brief "MISS" text
                if (tgtView != null) SpawnMissText(tgtView);
                yield return new WaitForSeconds(_fastForward ? 0.01f : 0.08f);
            }

            // Wait for lunge to finish
            if (lunge != null && lunge.IsActive())
                yield return lunge.WaitForCompletion();
        }

        // ── Arena construction ────────────────────────────────────────────────

        private void BuildArena(CombatLane lane)
        {
            DestroyArena();

            // Canvas
            var canvasGO = new GameObject("BattleArenaCanvas");
            canvasGO.transform.SetParent(transform, false);
            _canvas              = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;
            var scaler           = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode   = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight  = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // Panel
            _panel = new GameObject("ArenaPanel", typeof(RectTransform));
            _panel.transform.SetParent(canvasGO.transform, false);
            var panelRT = _panel.GetComponent<RectTransform>();
            panelRT.anchorMin  = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax  = new Vector2(0.5f, 0.5f);
            panelRT.pivot      = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta  = new Vector2(PanelW, PanelH);
            panelRT.anchoredPosition = new Vector2(0f, 160f);

            var panelBg  = _panel.AddComponent<Image>();
            panelBg.color = new Color(0.03f, 0.04f, 0.07f, 0.94f);

            // VS divider
            var vs = new GameObject("VS", typeof(RectTransform));
            vs.transform.SetParent(_panel.transform, false);
            var vsRT  = vs.GetComponent<RectTransform>();
            vsRT.anchorMin = new Vector2(0.5f, 0.12f);
            vsRT.anchorMax = new Vector2(0.5f, 0.88f);
            vsRT.sizeDelta = new Vector2(2f, 0f);
            var vsImg  = vs.AddComponent<Image>();
            vsImg.color = new Color(0.8f, 0.2f, 0.2f, 0.6f);

            // Header label
            MakeHeaderLabel(_panel.transform, "⚔  NIGHT COMBAT  ⚔", new Vector2(0f, 0.80f), new Vector2(1f, 1.0f));

            // Spawn unit views
            float defStartX = -(CenterGap);
            float eneStartX = CenterGap;

            for (int i = 0; i < lane.Defenders.Count; i++)
            {
                float x = defStartX - i * SlotSpacing;
                var v = SpawnUnitView(panelRT, lane.Defenders[i], new Vector2(x, 0f));
                _defViews.Add(v);
            }

            for (int i = 0; i < lane.Enemies.Count; i++)
            {
                float x = eneStartX + i * SlotSpacing;
                var v = SpawnUnitView(panelRT, lane.Enemies[i], new Vector2(x, 0f));
                _eneViews.Add(v);
            }

            UpdateFrontHighlights();

            // Entrance animation: cards drop in from above
            float entranceTime = 0.35f;
            foreach (var v in _defViews)
            {
                var home = v.Root.anchoredPosition;
                v.Root.anchoredPosition = home + new Vector2(0f, 120f);
                DOTween.To(() => v.Root.anchoredPosition, x => v.Root.anchoredPosition = x, home, entranceTime)
                       .SetEase(Ease.OutBack).SetLink(v.Root.gameObject);
            }
            for (int i = 0; i < _eneViews.Count; i++)
            {
                var v    = _eneViews[i];
                var home = v.Root.anchoredPosition;
                v.Root.anchoredPosition = home + new Vector2(0f, 120f);
                DOTween.To(() => v.Root.anchoredPosition, x => v.Root.anchoredPosition = x, home, entranceTime)
                       .SetEase(Ease.OutBack)
                       .SetDelay(i * 0.05f)
                       .SetLink(v.Root.gameObject);
            }
        }

        private BattleUnitView SpawnUnitView(RectTransform parent, CombatUnit unit, Vector2 pos)
        {
            var v = BattleUnitView.Create(parent, unit);
            v.Root.anchoredPosition = pos;
            v.Root.pivot = new Vector2(0.5f, 0.5f);
            return v;
        }

        private void DestroyArena()
        {
            if (_canvas != null) Destroy(_canvas.gameObject);
            _canvas = null;
            _panel  = null;
            _defViews.Clear();
            _eneViews.Clear();
        }

        // ── Floating numbers ──────────────────────────────────────────────────

        private void SpawnDamageNumber(BattleUnitView target, int dmg, bool isCrit)
        {
            if (_panel == null) return;

            var go = new GameObject("DmgNum", typeof(RectTransform));
            go.transform.SetParent(_panel.transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80f, 36f);
            rt.anchoredPosition = target.Root.anchoredPosition + new Vector2(0f, 55f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            var cg  = go.AddComponent<CanvasGroup>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text       = isCrit ? $"✦{dmg}!" : dmg.ToString();
            tmp.fontSize   = isCrit ? 22f : 17f;
            tmp.fontStyle  = isCrit ? FontStyles.Bold : FontStyles.Normal;
            tmp.color      = isCrit ? new Color(1f, 0.85f, 0f) : Color.white;
            tmp.alignment  = TextAlignmentOptions.Center;

            float dur = _fastForward ? 0.08f : 0.55f;

            var targetDmgPos = new Vector2(rt.anchoredPosition.x, rt.anchoredPosition.y + (_fastForward ? 20f : 70f));
            DOTween.To(() => rt.anchoredPosition, x => rt.anchoredPosition = x, targetDmgPos, dur)
                   .SetEase(Ease.OutQuad).SetLink(go);
            DOTween.Sequence().SetLink(go)
                .AppendInterval(dur * 0.4f)
                .Append(DOTween.To(() => cg.alpha, x => cg.alpha = x, 0f, dur * 0.6f))
                .OnComplete(() => { if (go != null) Destroy(go); });

            if (isCrit) rt.DOScale(Vector3.one * 1.35f, dur * 0.25f)
                           .OnComplete(() => rt.DOScale(Vector3.one, dur * 0.25f).SetLink(go))
                           .SetLink(go);
        }

        private void SpawnMissText(BattleUnitView target)
        {
            if (_panel == null) return;

            var go = new GameObject("MissTxt", typeof(RectTransform));
            go.transform.SetParent(_panel.transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(60f, 28f);
            rt.anchoredPosition = target.Root.anchoredPosition + new Vector2(0f, 45f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            var cg  = go.AddComponent<CanvasGroup>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = "MISS";
            tmp.fontSize  = 12f;
            tmp.color     = new Color(0.7f, 0.7f, 0.75f, 0.9f);
            tmp.alignment = TextAlignmentOptions.Center;

            float dur = _fastForward ? 0.05f : 0.40f;
            var targetMissPos = new Vector2(rt.anchoredPosition.x, rt.anchoredPosition.y + 40f);
            DOTween.To(() => rt.anchoredPosition, x => rt.anchoredPosition = x, targetMissPos, dur)
                   .SetEase(Ease.OutQuad).SetLink(go);
            DOTween.To(() => cg.alpha, x => cg.alpha = x, 0f, dur * 0.7f)
                   .SetDelay(dur * 0.3f).SetLink(go)
                   .OnComplete(() => { if (go != null) Destroy(go); });
        }

        // ── Finish / fade-out ─────────────────────────────────────────────────

        private IEnumerator FinishAndFade(bool playerWon)
        {
            // Wait for in-flight animations to drain, with a safety timeout.
            float drainTimeout = 8f;
            float drainElapsed = 0f;
            while (_drainingQueue && drainElapsed < drainTimeout)
            {
                drainElapsed += Time.deltaTime;
                yield return null;
            }
            if (_drainingQueue)
            {
                _drainingQueue = false;
                _events.Clear();
            }
            yield return new WaitForSeconds(_fastForward ? 0.05f : 0.55f);

            // Flash the panel border green (win) or red (loss).
            if (_panel != null)
            {
                var img = _panel.GetComponent<Image>();
                if (img != null)
                {
                    Color flash = playerWon
                        ? new Color(0.05f, 0.30f, 0.10f, 0.97f)
                        : new Color(0.30f, 0.04f, 0.04f, 0.97f);
                    DOTween.To(() => img.color, x => img.color = x, flash, _fastForward ? 0.05f : 0.25f).SetLink(_panel);
                }
            }

            yield return new WaitForSeconds(_fastForward ? 0.05f : 0.60f);

            // Fade whole canvas out.
            if (_canvas != null)
            {
                var cg = _canvas.gameObject.GetComponent<CanvasGroup>()
                      ?? _canvas.gameObject.AddComponent<CanvasGroup>();
                yield return DOTween.To(() => cg.alpha, x => cg.alpha = x, 0f, _fastForward ? 0.05f : 0.35f)
                                    .SetLink(_canvas.gameObject)
                                    .WaitForCompletion();
            }

            UnbindLane();
            DestroyArena();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private BattleUnitView FindView(CombatUnit unit)
        {
            foreach (var v in _defViews) if (v.Unit == unit) return v;
            foreach (var v in _eneViews) if (v.Unit == unit) return v;
            return null;
        }

        private void UpdateFrontHighlights()
        {
            bool defFrontFound = false;
            foreach (var v in _defViews)
            {
                bool isFront = !defFrontFound && v.Unit.IsAlive;
                v.SetFrontHighlight(isFront);
                if (isFront) defFrontFound = true;
            }

            bool eneFrontFound = false;
            foreach (var v in _eneViews)
            {
                bool isFront = !eneFrontFound && v.Unit.IsAlive;
                v.SetFrontHighlight(isFront);
                if (isFront) eneFrontFound = true;
            }
        }

        private static void MakeHeaderLabel(Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject("Header", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var tmp        = go.AddComponent<TextMeshProUGUI>();
            tmp.text       = text;
            tmp.fontSize   = 14f;
            tmp.fontStyle  = FontStyles.Bold;
            tmp.color      = new Color(0.85f, 0.65f, 0.15f, 1f);
            tmp.alignment  = TextAlignmentOptions.Center;
        }
    }
}
