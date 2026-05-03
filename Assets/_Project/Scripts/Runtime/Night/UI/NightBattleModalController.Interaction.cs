using UnityEngine;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    public partial class NightBattleModalController
    {
        private const float DragStartThreshold = 8f;

        private NightFighter  _dragFighter;
        private int           _dragSourceSlot = -1;
        private VisualElement _dragSourceElement;
        private VisualElement _dragHoverSlot;
        private Vector2       _dragStartPosition;
        private bool          _dragActive;
        private bool          _suppressClickAfterDrag;
        private int           _dragPointerId = -1;

        // ── Click-to-assign: villager entries ─────────────────────────────────────

        private void OnVillagerClicked(NightFighter fighter)
        {
            if (_suppressClickAfterDrag)
            {
                _suppressClickAfterDrag = false;
                return;
            }

            if (_phase != Phase.Prep) return;
            if (_team.Contains(fighter)) return;

            if (_interaction == PrepInteraction.AwaitingTarget)
                CancelCurrentSelection();

            if (_interaction == PrepInteraction.AwaitingSlot && _pendingFighter?.Id == fighter.Id)
            {
                CancelCurrentSelection();
                return;
            }

            if (_pendingVillagerEl != null) _pendingVillagerEl.RemoveFromClassList("nbm-villager-entry--selected");
            foreach (var sv in _slotViews) sv.SetHighlighted(false);

            _pendingFighter    = fighter;
            _interaction       = PrepInteraction.AwaitingSlot;
            _pendingVillagerEl = _villagerEls.TryGetValue(fighter.Id, out var el) ? el : null;
            _pendingVillagerEl?.AddToClassList("nbm-villager-entry--selected");

            for (int i = 0; i < NightTeam.MaxSlots; i++)
                if (_team.IsSlotEmpty(i)) _slotViews[i].SetHighlighted(true);
        }

        // ── Click-to-assign: player prep slots ───────────────────────────────────

        private void OnSlotClicked(int slotIndex)
        {
            if (_suppressClickAfterDrag)
            {
                _suppressClickAfterDrag = false;
                return;
            }

            if (_phase != Phase.Prep) return;

            switch (_interaction)
            {
                case PrepInteraction.AwaitingSlot:
                    AssignFighterToSlot(slotIndex);
                    break;

                case PrepInteraction.AwaitingTarget:
                    ApplyShopItemToSlot(slotIndex);
                    break;

                case PrepInteraction.Idle:
                    if (!_team.IsSlotEmpty(slotIndex))
                        UnassignFromSlot(slotIndex);
                    break;
            }
        }

        private void AssignFighterToSlot(int slotIndex)
        {
            if (_pendingFighter == null) return;

            if (!_team.IsSlotEmpty(slotIndex))
            {
                var evicted = _team.Clear(slotIndex);
                if (evicted != null) MarkVillagerAvailable(evicted);
            }

            _team.Assign(slotIndex, _pendingFighter);
            _slotViews[slotIndex].Assign(_pendingFighter);
            MarkVillagerAssigned(_pendingFighter);

            CancelCurrentSelection();
            UpdatePlayerCountLabel();
        }

        private void UnassignFromSlot(int slotIndex)
        {
            var fighter = _team.Clear(slotIndex);
            if (fighter == null) return;

            _slotViews[slotIndex].Clear();
            MarkVillagerAvailable(fighter);
            UpdatePlayerCountLabel();
        }

        // ── Click-to-assign: shop items ───────────────────────────────────────────

        private void OnShopItemClicked(NightShopItemDefinition def)
        {
            if (_phase != Phase.Prep) return;

            int gold = NightBattleManager.Instance?.PlayerGold ?? 0;
            if (gold < def.goldCost)
            {
                AddLog(GameLocalization.Format("night.modal.log.noGold", def.goldCost, gold), "nbm-log-entry--system");
                return;
            }

            if (!def.requiresTarget)
            {
                if (!def.CanApply(null, _team))
                {
                    AddLog(GameLocalization.Get("night.modal.log.noSlot"), "nbm-log-entry--system");
                    return;
                }
                if (NightBattleManager.Instance?.TrySpendGold(def.goldCost) == true)
                {
                    def.Apply(null, _team);
                    MarkShopItemPurchased(def);
                    RefreshTeamSlots();
                    AddLog(GameLocalization.Format("night.modal.log.purchased", def.DisplayName), "nbm-log-entry--system");
                }
                return;
            }

            if (_interaction == PrepInteraction.AwaitingTarget && _pendingItem == def)
            {
                CancelCurrentSelection();
                return;
            }

            CancelCurrentSelection();

            _pendingItem = def;
            _interaction = PrepInteraction.AwaitingTarget;

            for (int i = 0; i < NightTeam.MaxSlots; i++)
                _slotViews[i].SetHighlighted(!_team.IsSlotEmpty(i));

            foreach (var sv in _shopViews)
                sv.SetSelected(sv.Definition == def);

            AddLog(GameLocalization.Format("night.modal.log.itemSelected", def.DisplayName), "nbm-log-entry--system");
        }

        private void ApplyShopItemToSlot(int slotIndex)
        {
            if (_pendingItem == null || _team.IsSlotEmpty(slotIndex)) return;

            var fighter = _team.GetSlot(slotIndex);
            if (!_pendingItem.CanApply(fighter, _team))
            {
                CancelCurrentSelection();
                return;
            }

            if (NightBattleManager.Instance?.TrySpendGold(_pendingItem.goldCost) == true)
            {
                _pendingItem.Apply(fighter, _team);
                MarkShopItemPurchased(_pendingItem);
                _slotViews[slotIndex].RefreshDisplay(fighter);
                AddLog(GameLocalization.Format("night.modal.log.itemApplied", _pendingItem.DisplayName, fighter.DisplayName), "nbm-log-entry--system");
            }

            CancelCurrentSelection();
        }

        // ── Slot / fighter state helpers ──────────────────────────────────────────

        private void MarkVillagerAssigned(NightFighter f)
        {
            if (!_villagerEls.TryGetValue(f.Id, out var el)) return;
            el.AddToClassList("nbm-villager-entry--assigned");

            var badge = el.Q<Label>("nbm-villager-badge");
            if (badge != null)
            {
                int slot = _team.SlotOf(f);
                badge.text = slot == 0
                    ? GameLocalization.Get("night.modal.slot.front")
                    : GameLocalization.Format("night.modal.slot.n", slot + 1);
                badge.RemoveFromClassList("lk-hidden");
            }
        }

        private void MarkVillagerAvailable(NightFighter f)
        {
            if (!_villagerEls.TryGetValue(f.Id, out var el)) return;
            el.RemoveFromClassList("nbm-villager-entry--assigned");

            var badge = el.Q<Label>("nbm-villager-badge");
            badge?.AddToClassList("lk-hidden");
        }

        private void MarkShopItemPurchased(NightShopItemDefinition def)
        {
            foreach (var sv in _shopViews)
                if (sv.Definition == def) { sv.MarkPurchased(); break; }
        }

        private void RefreshTeamSlots()
        {
            for (int i = 0; i < NightTeam.MaxSlots; i++)
            {
                var f = _team.GetSlot(i);
                if (f == null) continue;
                if (_slotViews[i].AssignedFighter?.Id != f.Id)
                    _slotViews[i].Assign(f);
                else
                    _slotViews[i].RefreshDisplay(f);
            }
            UpdatePlayerCountLabel();
        }

        private void CancelCurrentSelection()
        {
            _pendingFighter    = null;
            _pendingItem       = null;
            _interaction       = PrepInteraction.Idle;

            if (_pendingVillagerEl != null)
            {
                _pendingVillagerEl.RemoveFromClassList("nbm-villager-entry--selected");
                _pendingVillagerEl = null;
            }

            foreach (var sv in _slotViews) sv.SetHighlighted(false);
            foreach (var sv in _shopViews) sv.SetSelected(false);
        }

        // ── Drag-to-assign / drag-to-reorder ─────────────────────────────────────

        private void BeginDragCandidate(
            NightFighter fighter,
            int sourceSlot,
            VisualElement source,
            PointerDownEvent evt)
        {
            if (_phase != Phase.Prep || fighter == null || source == null || evt.button != 0)
                return;

            if (sourceSlot < 0 && _team.Contains(fighter))
                return;

            EndDrag(clearHighlights: true);

            _dragFighter       = fighter;
            _dragSourceSlot    = sourceSlot;
            _dragSourceElement = source;
            _dragStartPosition = ToPanelPoint(evt.position);
            _dragPointerId     = evt.pointerId;
            _dragActive        = false;

            source.CapturePointer(evt.pointerId);
            source.RegisterCallback<PointerMoveEvent>(OnDragPointerMove);
            source.RegisterCallback<PointerUpEvent>(OnDragPointerUp);
            source.RegisterCallback<PointerCancelEvent>(OnDragPointerCancel);
        }

        private void OnDragPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != _dragPointerId || _dragFighter == null)
                return;

            if (!_dragActive)
            {
                Vector2 delta = ToPanelPoint(evt.position) - _dragStartPosition;
                if (delta.sqrMagnitude < DragStartThreshold * DragStartThreshold)
                    return;

                StartDrag();
            }

            UpdateDragHover(ToPanelPoint(evt.position));
            evt.StopPropagation();
        }

        private void OnDragPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != _dragPointerId)
                return;

            bool wasActive = _dragActive;
            Vector2 pointerPosition = ToPanelPoint(evt.position);
            int targetSlot = wasActive ? ResolveSlotAt(pointerPosition) : -1;
            bool droppedOnVillagerList = _villagersList != null && _villagersList.worldBound.Contains(pointerPosition);

            if (wasActive)
            {
                _suppressClickAfterDrag = true;

                if (targetSlot >= 0)
                    CommitDragToSlot(targetSlot);
                else if (_dragSourceSlot >= 0 && droppedOnVillagerList)
                    UnassignFromSlot(_dragSourceSlot);

                _root?.schedule.Execute(() => _suppressClickAfterDrag = false).StartingIn(50);
                evt.StopPropagation();
            }

            EndDrag(clearHighlights: true);
        }

        private void OnDragPointerCancel(PointerCancelEvent evt)
        {
            if (evt.pointerId == _dragPointerId)
                EndDrag(clearHighlights: true);
        }

        private void StartDrag()
        {
            _dragActive = true;
            CancelCurrentSelection();
            _dragSourceElement?.AddToClassList("nbm-dragging");

            for (int i = 0; i < NightTeam.MaxSlots; i++)
                _slotViews[i].SetHighlighted(_dragSourceSlot >= 0 || _team.IsSlotEmpty(i));

            if (_dragSourceSlot >= 0)
                _slotViews[_dragSourceSlot].Root.AddToClassList("nb-prep-slot--drag-source");
        }

        private void UpdateDragHover(Vector2 position)
        {
            if (_dragHoverSlot != null)
            {
                _dragHoverSlot.RemoveFromClassList("nb-prep-slot--drag-over");
                _dragHoverSlot = null;
            }

            int slot = ResolveSlotAt(position);
            if (slot < 0)
                return;

            _dragHoverSlot = _slotViews[slot].Root;
            _dragHoverSlot.AddToClassList("nb-prep-slot--drag-over");
        }

        private int ResolveSlotAt(Vector2 position)
        {
            for (int i = 0; i < NightTeam.MaxSlots; i++)
            {
                VisualElement root = _slotViews[i]?.Root;
                if (root != null && root.worldBound.Contains(position))
                    return i;
            }

            return -1;
        }

        private static Vector2 ToPanelPoint(Vector3 position) =>
            new(position.x, position.y);

        private void CommitDragToSlot(int targetSlot)
        {
            if (_dragFighter == null || targetSlot < 0 || targetSlot >= NightTeam.MaxSlots)
                return;

            if (_dragSourceSlot < 0)
            {
                _pendingFighter = _dragFighter;
                _interaction = PrepInteraction.AwaitingSlot;
                AssignFighterToSlot(targetSlot);
                return;
            }

            if (_dragSourceSlot == targetSlot)
                return;

            var moving = _team.Clear(_dragSourceSlot);
            if (moving == null)
                return;

            var displaced = _team.Clear(targetSlot);
            _slotViews[_dragSourceSlot].Clear();
            _slotViews[targetSlot].Clear();

            _team.Assign(targetSlot, moving);
            _slotViews[targetSlot].Assign(moving);
            MarkVillagerAssigned(moving);

            if (displaced != null)
            {
                _team.Assign(_dragSourceSlot, displaced);
                _slotViews[_dragSourceSlot].Assign(displaced);
                MarkVillagerAssigned(displaced);
            }

            UpdatePlayerCountLabel();
            AddLog(GameLocalization.Format("night.modal.log.slotMoved", moving.DisplayName, targetSlot + 1), "nbm-log-entry--system");
        }

        private void EndDrag(bool clearHighlights)
        {
            if (_dragSourceElement != null)
            {
                if (_dragPointerId >= 0 && _dragSourceElement.HasPointerCapture(_dragPointerId))
                    _dragSourceElement.ReleasePointer(_dragPointerId);

                _dragSourceElement.UnregisterCallback<PointerMoveEvent>(OnDragPointerMove);
                _dragSourceElement.UnregisterCallback<PointerUpEvent>(OnDragPointerUp);
                _dragSourceElement.UnregisterCallback<PointerCancelEvent>(OnDragPointerCancel);
                _dragSourceElement.RemoveFromClassList("nbm-dragging");
            }

            if (_dragHoverSlot != null)
                _dragHoverSlot.RemoveFromClassList("nb-prep-slot--drag-over");

            if (clearHighlights)
            {
                foreach (var sv in _slotViews)
                {
                    if (sv == null) continue;
                    sv.SetHighlighted(false);
                    sv.Root.RemoveFromClassList("nb-prep-slot--drag-source");
                    sv.Root.RemoveFromClassList("nb-prep-slot--drag-over");
                }
            }

            _dragFighter       = null;
            _dragSourceSlot    = -1;
            _dragSourceElement = null;
            _dragHoverSlot     = null;
            _dragActive        = false;
            _dragPointerId     = -1;
        }
    }
}
