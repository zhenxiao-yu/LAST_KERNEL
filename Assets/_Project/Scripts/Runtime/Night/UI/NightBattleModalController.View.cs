using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    public partial class NightBattleModalController
    {
        // ── UI construction helpers ───────────────────────────────────────────────

        private void BuildEnemyPreview(NightWaveDefinition wave)
        {
            if (wave == null || _enemyRow == null) return;
            var enemies = NightBattleManager.BuildEnemyListForCurrentDay(wave);

            for (int i = 0; i < enemies.Count; i++)
            {
                var def   = enemies[i];
                bool front = i == 0;

                var card = new VisualElement();
                card.AddToClassList("nbm-enemy-card");
                if (front) card.AddToClassList("nbm-enemy-card--front");

                var name = new Label(def.DisplayName.ToUpper());
                name.AddToClassList("nbm-enemy-card__name");

                var stats = new Label(GameLocalization.Format("night.modal.stats.enemy", def.Attack, def.MaxHP));
                stats.AddToClassList("nbm-enemy-card__stats");

                card.Add(name);
                card.Add(stats);

                if (front)
                {
                    var badge = new Label(GameLocalization.Get("night.modal.slot.front"));
                    badge.AddToClassList("nbm-enemy-card__front-badge");
                    card.Add(badge);
                }

                _enemyRow.Add(card);
            }

            for (int i = enemies.Count; i < NightTeam.MaxSlots; i++)
            {
                var ph = new VisualElement();
                ph.AddToClassList("nbm-enemy-card");
                ph.style.opacity = 0.12f;
                _enemyRow.Add(ph);
            }

            if (_enemyCountLabel != null)
                _enemyCountLabel.text = GameLocalization.Format("night.modal.units", enemies.Count);
        }

        private void BuildPlayerSlots()
        {
            if (_playerRow == null) return;
            for (int i = 0; i < NightTeam.MaxSlots; i++)
            {
                var sv = new NightPrepSlotView(i);
                sv.OnClicked += OnSlotClicked;
                sv.Root.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (sv.AssignedFighter != null)
                        BeginDragCandidate(sv.AssignedFighter, sv.SlotIndex, sv.Root, evt);
                });
                _slotViews[i] = sv;
                _playerRow.Add(sv.Root);
            }
        }

        private VisualElement BuildVillagerEntry(NightFighter fighter)
        {
            float hpFraction = fighter.BaseMaxHealth > 0
                ? (float)fighter.BaseHealth / fighter.BaseMaxHealth
                : 1f;
            bool isWounded  = hpFraction < 1f;
            bool isCritical = hpFraction < 0.35f;

            var row = new VisualElement();
            row.AddToClassList("nbm-villager-entry");
            if (isWounded) row.AddToClassList("nbm-villager-entry--wounded");

            // Card art icon
            var icon = new VisualElement();
            icon.AddToClassList("nbm-villager-entry__icon");
            var tex = fighter.SourceCard?.Definition?.ArtTexture;
            if (tex != null)
                icon.style.backgroundImage = new StyleBackground(Background.FromTexture2D(tex));
            else
                icon.style.opacity = 0f;
            row.Add(icon);

            // Info column: name + HP bar
            var info = new VisualElement();
            info.AddToClassList("nbm-villager-entry__info");

            var nameLabel = new Label(fighter.DisplayName);
            nameLabel.AddToClassList("nbm-villager-entry__name");
            info.Add(nameLabel);

            var hpBar = new VisualElement();
            hpBar.AddToClassList("nbm-villager-entry__hp-bar");
            var hpFill = new VisualElement();
            hpFill.AddToClassList("nbm-villager-entry__hp-fill");
            if (isCritical)      hpFill.AddToClassList("nbm-villager-entry__hp-fill--critical");
            else if (isWounded)  hpFill.AddToClassList("nbm-villager-entry__hp-fill--wounded");
            hpFill.style.width = Length.Percent(hpFraction * 100f);
            hpBar.Add(hpFill);
            info.Add(hpBar);

            row.Add(info);

            var statsLabel = new Label(GameLocalization.Format("night.modal.stats.fighter", fighter.FinalAttack, fighter.FinalMaxHealth));
            statsLabel.AddToClassList("nbm-villager-entry__stats");
            row.Add(statsLabel);

            var badge = new Label(GameLocalization.Get("night.modal.slot.front"));
            badge.name = "nbm-villager-badge";
            badge.AddToClassList("nbm-villager-entry__badge");
            badge.AddToClassList("lk-hidden");
            row.Add(badge);

            row.RegisterCallback<ClickEvent>(_ => OnVillagerClicked(fighter));
            row.RegisterCallback<PointerDownEvent>(evt => BeginDragCandidate(fighter, -1, row, evt));

            return row;
        }

        private void BuildRewardChoices(IReadOnlyList<CardDefinition> choices)
        {
            _rewardChoiceEls.Clear();
            _rewardOptions?.Clear();

            bool hasChoices = choices != null && choices.Count > 0;
            _rewardTitle?.EnableInClassList("lk-hidden", !hasChoices);
            _rewardOptions?.EnableInClassList("lk-hidden", !hasChoices);

            if (!hasChoices)
                return;

            if (_rewardTitle != null)
                _rewardTitle.text = GameLocalization.Get("night.modal.reward.title");

            foreach (CardDefinition reward in choices)
            {
                if (reward == null)
                    continue;

                VisualElement card = BuildRewardCard(reward);
                _rewardChoiceEls[reward] = card;
                _rewardOptions?.Add(card);
            }
        }

        private VisualElement BuildRewardCard(CardDefinition reward)
        {
            var card = new VisualElement();
            card.AddToClassList("nbm-reward-card");

            var art = new VisualElement();
            art.AddToClassList("nbm-reward-card__art");
            if (reward.ArtTexture != null)
                art.style.backgroundImage = new StyleBackground(Background.FromTexture2D(reward.ArtTexture));
            card.Add(art);

            var name = new Label(reward.DisplayName);
            name.AddToClassList("nbm-reward-card__name");

            var category = new Label(GameLocalization.Format(
                "night.modal.reward.category",
                CardDossierFormatter.CategoryLabel(reward.Category).ToUpperInvariant()));
            category.AddToClassList("nbm-reward-card__category");

            var statPod = new Label(CardDossierFormatter.BuildRewardPod(reward));
            statPod.AddToClassList("nbm-reward-card__stat-pod");

            string lore = CardDossierFormatter.BuildLore(reward);
            string description = string.IsNullOrWhiteSpace(lore)
                ? GameLocalization.Get("night.modal.reward.noDescription")
                : lore;
            var desc = new Label(description);
            desc.AddToClassList("nbm-reward-card__desc");

            string hiddenStats = CardDossierFormatter.BuildHiddenStats(reward);
            var hidden = new Label(hiddenStats);
            hidden.AddToClassList("nbm-reward-card__hidden");
            hidden.EnableInClassList("lk-hidden", string.IsNullOrWhiteSpace(hiddenStats));

            card.Add(name);
            card.Add(category);
            card.Add(statPod);
            card.Add(desc);
            card.Add(hidden);
            card.RegisterCallback<ClickEvent>(_ => OnRewardChoiceClicked(reward));

            return card;
        }

        // ── Battle log ────────────────────────────────────────────────────────────

        private void AddLog(string message, string extraClass = null)
        {
            if (_battleLog == null) return;

            var label = new Label($"> {message}");
            label.AddToClassList("nbm-log-entry");
            if (!string.IsNullOrEmpty(extraClass)) label.AddToClassList(extraClass);

            _battleLog.Add(label);
            _logLineCount++;

            if (_logLineCount > maxLogLines && _battleLog.contentContainer.childCount > 0)
            {
                _battleLog.contentContainer.RemoveAt(0);
                _logLineCount--;
            }

            _battleLog.schedule.Execute(() =>
                _battleLog.verticalScroller.value = _battleLog.verticalScroller.highValue
            ).StartingIn(0);
        }

        // ── Localization ──────────────────────────────────────────────────────────

        private void BindStaticText()
        {
            if (_btnStart  != null) _btnStart.text  = GameLocalization.Get("night.modal.btn.start");
            if (_btnFast   != null) _btnFast.text   = GameLocalization.Get("night.modal.btn.fast");
            if (_btnCancel != null) _btnCancel.text = GameLocalization.Get("night.modal.btn.autoDeploy");
            if (_btnReturn != null) _btnReturn.text = GameLocalization.Get("night.modal.btn.return");

            if (_lblEnemySection    != null) _lblEnemySection.text    = GameLocalization.Get("night.modal.section.enemy");
            if (_lblColonySection   != null) _lblColonySection.text   = GameLocalization.Get("night.modal.section.colony");
            if (_lblDefendersHeader != null) _lblDefendersHeader.text = GameLocalization.Get("night.modal.section.defenders");
            if (_lblLogHeader       != null) _lblLogHeader.text       = GameLocalization.Get("night.modal.section.log");
            if (_lblShopHeader      != null) _lblShopHeader.text      = GameLocalization.Get("night.modal.section.shop");

            if (_assignHint != null) _assignHint.text = GameLocalization.Get("night.modal.hint.assign");
            if (_shopHint   != null) _shopHint.text   = GameLocalization.Get("night.modal.hint.shop");
        }

        // ── Misc UI helpers ───────────────────────────────────────────────────────

        private void SetVisible(bool visible)
        {
            if (_root == null) return;
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (visible)
                _root.schedule.Execute(() => _root.Focus()).StartingIn(0);
        }

        private void SetStatus(string text)
        {
            if (_statusLabel != null) _statusLabel.text = text.ToUpper();
        }

        private void UpdatePlayerCountLabel()
        {
            if (_playerCountLabel != null)
                _playerCountLabel.text = GameLocalization.Format("night.modal.slotsStatus", _team.FilledSlotCount, NightTeam.MaxSlots);
        }

        private void LockVillagerList(bool locked)
        {
            foreach (var kv in _villagerEls)
                kv.Value.SetEnabled(!locked);
        }

        private static string ComputeThreatLabel(NightWaveDefinition wave)
        {
            int totalAtk = 0;
            if (wave != null)
                foreach (var e in NightBattleManager.BuildEnemyListForCurrentDay(wave)) totalAtk += e.Attack;

            string tierKey = totalAtk <= 3  ? "night.modal.threat.low"
                           : totalAtk <= 8  ? "night.modal.threat.moderate"
                           : totalAtk <= 15 ? "night.modal.threat.high"
                           : "night.modal.threat.critical";
            return GameLocalization.Format("night.modal.threat", GameLocalization.Get(tierKey));
        }

        // ── Debug hotkeys ─────────────────────────────────────────────────────────
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void HandleDebugKeys()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;

            // Shift+N — trigger a test night immediately from any game state
            if (kb.nKey.wasPressedThisFrame && kb.shiftKey.isPressed)
            {
                Debug.Log("[DEBUG] Shift+N — Trigger test night");
                NightBattleManager.Instance?.DebugTriggerNight();
                return;
            }

            if (kb.bKey.wasPressedThisFrame && _phase == Phase.Prep)
            {
                Debug.Log("[DEBUG] B — force Start Battle");
                OnStartBattleClicked();
            }
            if (kb.vKey.wasPressedThisFrame && _phase == Phase.Battle)
            {
                Debug.Log("[DEBUG] V — Force Victory");
                _activeLane?.ForceEnd();
            }
            if (kb.lKey.wasPressedThisFrame && _phase == Phase.Battle && _activeLane != null)
            {
                Debug.Log("[DEBUG] L — Force Defeat");
                foreach (var u in _activeLane.Defenders) u.TakeDamage(u.MaxHP * 99);
                _activeLane.Tick(0f);
            }
        }
#endif
    }
}
