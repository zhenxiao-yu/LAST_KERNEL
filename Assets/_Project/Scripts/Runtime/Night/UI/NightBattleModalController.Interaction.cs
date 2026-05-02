using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    public partial class NightBattleModalController
    {
        // ── Click-to-assign: villager entries ─────────────────────────────────────

        private void OnVillagerClicked(NightFighter fighter)
        {
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
    }
}
