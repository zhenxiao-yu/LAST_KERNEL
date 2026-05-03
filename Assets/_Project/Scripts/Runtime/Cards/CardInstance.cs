using System.Collections;
using System.Collections.Generic;
using System.Text;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

namespace Markyu.LastKernel
{
    [RequireComponent(typeof(MeshRenderer), typeof(BoxCollider))]
    public class CardInstance : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Fields & Properties
        [BoxGroup("Text References")]
        [Required, SerializeField, Tooltip("Displays the card's name.")]
        private TextMeshPro titleText;

        [BoxGroup("Text References")]
        [SerializeField, Tooltip("Displays sell price.")]
        private TextMeshPro priceText;

        [BoxGroup("Text References")]
        [SerializeField, Tooltip("Displays nutrition value.")]
        private TextMeshPro nutritionText;

        [BoxGroup("Text References")]
        [SerializeField, Tooltip("Displays current health.")]
        private TextMeshPro healthText;

        public CardDefinition Definition { get; protected set; }
        public CardSettings Settings { get; private set; }
        public CardStack Stack { get; set; }
        public Vector2 Size { get; private set; }

        public CombatStats Stats { get; private set; }
        public int UsesLeft { get; private set; }
        public int CurrentHealth { get; private set; }
        public int CurrentNutrition { get; private set; }

        public CardDefinition BaseDefinition => EquipperComponent?.OriginalDefinition ?? Definition;
        public CardStack OriginalCraftingStack { get; set; }

        public CardCombatant Combatant { get; private set; }
        public CardEquipper EquipperComponent { get; private set; }
        public CardEquipment EquipmentComponent { get; private set; }
        public CardFeelPresenter FeelPresenter { get; private set; }
        public CardView View { get; private set; }

        public bool IsBeingDragged { get; set; }

        // Optional additive VFX overlay (All In 1 Sprite Shader). Assign in prefab inspector.
        // Null-safe — card works fully without it.
        [SerializeField] private CardFeedbackController _feedbackController;

        private Camera _mainCam;
        private MeshRenderer _renderer;
        private BoxCollider _col;

        private Tween _moveTween;
        private Tween _combatTween;
        private Tween _hurtTween;

        private Highlight _highlight;
        private MaterialPropertyBlock _artPropBlock;

        private bool _isHovered;

        private VillagerLockToggle _lockToggle;

        private Vector3 _dampedTargetPos;
        private bool _isFollowingDamped;
        #endregion

        #region Lifecycle & Initialization
        private void OnEnable()
        {
            GameLocalization.Initialize();
            GameLocalization.LanguageChanged += HandleLanguageChanged;
            RefreshLocalizedPresentation();
        }

        public void Initialize(CardDefinition definition, CardSettings settings = null, CardStack stackToIgnore = null)
        {
            if (definition == null)
            {
                Debug.LogError("CardInstance: Cannot initialize with a null card definition.", this);
                enabled = false;
                return;
            }

            if (settings == null)
            {
                Debug.LogError($"CardInstance: Cannot initialize '{definition.DisplayName}' without CardSettings.", this);
                enabled = false;
                return;
            }

            _mainCam = Camera.main;
            _renderer = GetComponent<MeshRenderer>();
            _col = GetComponent<BoxCollider>();

            Combatant = GetComponent<CardCombatant>();
            EquipperComponent = GetComponent<CardEquipper>();
            EquipmentComponent = GetComponent<CardEquipment>();
            FeelPresenter = CardFeelPresenter.EnsureOn(gameObject);
            CardLongPressHandler.EnsureOn(gameObject);
            View = GetComponent<CardView>();
            _lockToggle = GetComponent<VillagerLockToggle>();

            gameObject.name = $"{(definition is PackDefinition ? "Pack" : "Card")}_{definition.DisplayName}";

            Definition = definition;
            Settings = settings;
            Size = new Vector2(_col.size.x, _col.size.z) + settings.Margin;

            Stats = definition.CreateCombatStats();

            UsesLeft = (definition is PackDefinition packDefinition)
                ? packDefinition.Slots.Count
                : definition.Uses;

            CurrentHealth = Stats.MaxHealth.Value;
            CurrentNutrition = definition.Nutrition;

            RefreshLocalizedPresentation();

            UpdateStatDisplays();

            ApplyArtTexture(Definition.ArtTexture);

            Stack = new CardStack(this, transform.position);
            CardManager.Instance?.RegisterStack(Stack);
            TryAttachToNearbyStack(settings.SpawnAttachRadius, stackToIgnore);
            CardManager.Instance?.ResolveOverlaps();

            // The presenter is initialized after gameplay state and art are ready so shader feedback
            // reads the correct material defaults and overlay texture.
            if (settings.FeelProfile != null)
            {
                FeelPresenter?.Initialize(settings.FeelProfile);
            }
            else
            {
                Debug.LogWarning($"CardInstance: '{Definition.DisplayName}' has no CardFeelProfile assigned in CardSettings.", this);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            _feedbackController?.SetHover(true);
            InfoPanel.Instance?.RegisterCardHover(GetInfo(), GetCardInfo());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            _feedbackController?.SetHover(false);
            InfoPanel.Instance?.UnregisterHover();
        }

        private void OnDisable()
        {
            GameLocalization.LanguageChanged -= HandleLanguageChanged;
            _isHovered = false;
            KillTweens();

            InfoPanel.Instance?.UnregisterHover();
        }

        private void Update()
        {
            if (_isHovered && Stack != null && Stack.IsCrafting)
            {
                InfoPanel.Instance?.RegisterHover(GetInfo());
            }

            if (_isFollowingDamped)
            {
                transform.position = ExponentialMove(
                    transform.position,
                    _dampedTargetPos,
                    Settings.SwaySharpness,
                    Time.unscaledDeltaTime
                );

                if (Vector3.SqrMagnitude(transform.position - _dampedTargetPos) < 0.0001f)
                {
                    transform.position = _dampedTargetPos;
                    _isFollowingDamped = false;
                }
            }
        }
        #endregion

        #region Information & Visuals

        public void ShowInspectInfo() => InfoPanel.Instance?.RegisterCardHover(GetInfo(), GetCardInfo());

        public void HideInspectInfo() => InfoPanel.Instance?.UnregisterHover();

        private CardInfoData? GetCardInfo()
        {
            if (Stack == null || Stack.IsCrafting || Stack.Cards.Count > 1 || Stack.TopCard == null)
                return null;

            var def   = Stack.TopCard.Definition;
            var stats = Stack.TopCard.Stats;
            return new CardInfoData(
                category:       def.Category,
                combat:         def.CombatType,
                currentHP:      CurrentHealth,
                maxHP:          (int)stats.MaxHealth.Value,
                formattedStats: def.CombatType != CombatType.None ? stats.GetFormattedStats() : null,
                sellPrice:      def.SellPrice,
                nutrition:      CurrentNutrition,
                usesLeft:       UsesLeft,
                loreText:       CardDossierFormatter.BuildLore(def),
                visibleStatsText: CardDossierFormatter.BuildVisibleStats(def, stats, CurrentHealth),
                hiddenStatsText:  CardDossierFormatter.BuildHiddenStats(def, stats),
                economyText:      CardDossierFormatter.BuildEconomy(def, CurrentNutrition, UsesLeft)
            );
        }

        private (string, string) GetInfo()
        {
            (string header, string body) info = ("", "");

            if (Stack == null) return info;

            if (Stack.IsCrafting)
            {
                var task = CraftingManager.Instance?.GetCraftingTask(Stack);
                if (task == null || task.Recipe == null)
                {
                    return info;
                }

                info.header = task.Recipe.DisplayName;
                info.body = GameLocalization.Format("card.timeLeft", task.Recipe.CraftingDuration - task.Progress);
            }
            else if (Stack.Cards.Count > 1)
            {
                info.header = GameLocalization.Get("card.stackHeader");

                var counts = new Dictionary<CardDefinition, int>();
                foreach (var c in Stack.Cards)
                {
                    counts.TryGetValue(c.Definition, out int n);
                    counts[c.Definition] = n + 1;
                }

                var sb = new StringBuilder();
                bool first = true;
                foreach (var kvp in counts)
                {
                    if (!first) sb.Append(", ");
                    sb.Append(kvp.Key.DisplayName).Append(" x").Append(kvp.Value);
                    first = false;
                }
                sb.Append('.');
                info.body = sb.ToString();
            }
            else if (Stack.TopCard != null)
            {
                info.header = Stack.TopCard.Definition.DisplayName;
                info.body = CardDossierFormatter.BuildLore(Stack.TopCard.Definition);
            }

            return info;
        }

        public void UpdateStatDisplays()
        {
            if (Definition == null)
            {
                return;
            }

            if (priceText != null) priceText.text = Definition.SellPrice.ToString();
            if (nutritionText != null) nutritionText.text = CurrentNutrition.ToString();
            if (healthText != null) healthText.text = CurrentHealth.ToString();

            View?.SetStats(
                Definition.SellPrice.ToString(),
                CurrentNutrition.ToString(),
                CurrentHealth.ToString());
        }

        public void UpdatePriceText(string text)
        {
            if (priceText != null)
            {
                priceText.text = text;
            }
        }

        public void SetHighlighted(bool value)
        {
            if (Settings == null || Settings.OutlineMaterial == null)
            {
                return;
            }

            if (_highlight == null)
            {
                var mesh = GetComponent<MeshFilter>().mesh;
                _highlight = new Highlight(transform, mesh, Settings.OutlineMaterial);
                GetComponent<CardRenderOrderController>()?.Refresh();
            }
            else _highlight.SetActive(value);

            View?.SetHighlighted(value);
        }

        public void PlayPuffParticle()
        {
            if (Settings?.PuffParticle != null)
            {
                Instantiate(Settings.PuffParticle, transform.position, Quaternion.identity);
            }
        }
        #endregion

        #region World Interactions
        public IEnumerator Consume(CardInstance character, int amountNeeded, System.Action<int> onConsumed)
        {
            CardStack oldStack = null;

            if (Stack != null && Stack.Cards.Count > 1)
            {
                if (Stack.IsCrafting)
                {
                    CraftingManager.Instance.StopCraftingTask(Stack);
                }

                oldStack = Stack;
                Stack.RemoveCard(this);
                Stack = new CardStack(this, transform.position);
                CardManager.Instance.RegisterStack(Stack);
            }

            yield return transform.DOMoveY(Settings.DragHeight, 0.1f)
                .SetUpdate(true)
                .SetLink(gameObject)
                .WaitForCompletion();

            Vector3 target = new Vector3(
                character.transform.position.x,
                Settings.DragHeight,
                character.transform.position.z
            );

            AudioManager.Instance?.PlaySFX(AudioId.CardSwipe);

            yield return transform.DOMove(target, 0.2f)
                .SetUpdate(true)
                .SetLink(gameObject)
                .WaitForCompletion();

            AudioManager.Instance?.PlaySFX(AudioId.Eat);

            // If this card came from a stack, reapply the stack’s layout so remaining cards shift correctly.
            if (oldStack != null) oldStack.SetTargetPosition(oldStack.TargetPosition);

            yield return new WaitForSecondsRealtime(0.25f);

            int amountToEat = Mathf.Min(CurrentNutrition, amountNeeded);
            CurrentNutrition -= amountToEat;
            UpdateStatDisplays();

            onConsumed?.Invoke(amountToEat);

            if (CurrentNutrition <= 0)
            {
                Kill();
            }
            else if (Stack != null)
            {
                Stack.SetTargetPosition(transform.position);
                yield return new WaitForSecondsRealtime(0.25f);
                CardManager.Instance.ResolveOverlaps();
            }
        }

        public CardStack TryAttachToNearbyStack(float radius, CardStack stackToIgnore = null)
        {
            if (stackToIgnore == CardStack.RefuseAll)
                return null;

            var bestCandidateStack = FindBestMergeCandidate(radius, stackToIgnore);
            if (bestCandidateStack == null)
                return null;

            var droppedStack = Stack;

            // Returning to the stack we were pulled from: restore the paused crafting task.
            if (bestCandidateStack == OriginalCraftingStack)
            {
                CraftingManager.Instance.ResumeCraftingTask(OriginalCraftingStack);
            }
            // Merging a crafting stack elsewhere: cancel its task before the cards move.
            else if (droppedStack.IsCrafting && CraftingManager.Instance != null)
            {
                CraftingManager.Instance.StopCraftingTask(droppedStack);
            }

            bestCandidateStack.MergeWith(droppedStack);

            if (droppedStack.Cards.Count == 0)
            {
                CardManager.Instance?.UnregisterStack(droppedStack);
                bestCandidateStack.SetTargetPosition(bestCandidateStack.TargetPosition);
            }
            else
            {
                // Cards remain (e.g., ChestLogic kept some after a partial deposit).
                // Keep the source stack alive and let CardManager resolve the overlap.
                CardManager.Instance?.ResolveOverlaps();
            }

            // If the target was already crafting, adding a card may satisfy or break the
            // current recipe — re-validate so an invalid composition is stopped immediately.
            // On idle stacks, check from scratch for a new recipe.
            if (bestCandidateStack.IsCrafting)
                CraftingManager.Instance?.ValidateAndResumeTask(bestCandidateStack);
            else
                CraftingManager.Instance?.CheckForRecipe(bestCandidateStack);

            return bestCandidateStack;
        }

        // Returns the nearest compatible stack within radius, or null.
        // Applies stacking rules and crafting safety checks; deduplicates by stack.
        private CardStack FindBestMergeCandidate(float radius, CardStack stackToIgnore)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, radius);
            CardStack best = null;
            float bestSqrDist = float.MaxValue;
            var checkedStacks = new HashSet<CardStack>();

            foreach (var hit in hits)
            {
                var otherCard = hit.GetComponent<CardInstance>();
                if (otherCard == null) continue;

                var candidate = otherCard.Stack;

                if (candidate == null || candidate == Stack || candidate == stackToIgnore)
                    continue;

                // A crafting stack only accepts cards that are valid ingredients for its active recipe.
                // Exception: allow re-joining the stack we were originally pulled from.
                if (candidate.IsCrafting && candidate != OriginalCraftingStack)
                {
                    if (CraftingManager.Instance == null ||
                        !CraftingManager.Instance.CanJoinActiveCraft(candidate, Definition))
                        continue;
                }

                if (!CanStack(Definition, candidate.BottomCard.Definition))
                    continue;

                if (!checkedStacks.Add(candidate)) continue;

                // Measure distance to the closest individual card in the candidate stack,
                // not the stack anchor, so tall stacks don't lose to nearby single cards.
                float sqrDist = float.MaxValue;
                foreach (var card in candidate.Cards)
                {
                    float d = (card.transform.position - transform.position).sqrMagnitude;
                    if (d < sqrDist) sqrDist = d;
                }

                if (sqrDist < bestSqrDist)
                {
                    bestSqrDist = sqrDist;
                    best = candidate;
                }
            }

            return best;
        }
        #endregion

        #region State Management
        public void Use() => UsesLeft--;

        public void Heal(int healAmount)
        {
            CurrentHealth = Stats != null
                ? Mathf.Min(Stats.MaxHealth.Value, CurrentHealth + Mathf.Max(0, healAmount))
                : CurrentHealth + Mathf.Max(0, healAmount);
            UpdateStatDisplays();
            _feedbackController?.PlayHealingPulse();
        }

        public void TakeDamage(int damage)
        {
            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
            UpdateStatDisplays();

            _hurtTween?.Kill();
            _hurtTween = null;

            bool handledByFeel = FeelPresenter != null && FeelPresenter.OnDamageTaken();
            _feedbackController?.PlayDamageFlash();
            if (handledByFeel)
            {
                return;
            }

            // Fix C-4: the original fallback used _renderer.material (creates an instance,
            // conflicts with MaterialPropertyBlock) and DOPunchRotation on the root transform
            // (collider stays axis-aligned while mesh tilts, causing overlap desync).
            // CardFeelPresenter.EnsureOn() in Initialize() makes this branch dead code for
            // any card that went through Initialize(). Log and bail rather than run broken effects.
            Debug.LogWarning($"CardInstance: TakeDamage visual fallback on '{name}' — FeelPresenter is missing. Skipping damage effects.", this);
            _hurtTween = null;
        }

        public void Kill()
        {
            CardManager.Instance?.NotifyCardKilled(this);

            KillTweens();

            EquipperComponent?.UnequipAll();

            if (Combatant != null && Combatant.IsInCombat && Combatant.CurrentCombatTask != null)
            {
                Combatant.CurrentCombatTask.RemoveCombatant(this);
            }

            if (Definition != null && Definition.Category is CardCategory.Mob or CardCategory.Character)
            {
                CardDefinition loot = Definition.GetRandomLoot();
                if (loot != null)
                {
                    CardManager.Instance?.CreateCardInstance(loot, transform.position);
                }
            }

            PlayPuffParticle();

            if (Stack != null) Stack.DestroyCard(this);
            else GameObject.Destroy(gameObject);
        }

        public void SetVisible(bool value)
        {
            if (_renderer != null) _renderer.enabled = value;
            if (_col != null) _col.enabled = value;

            if (titleText != null) titleText.enabled = value;
            if (priceText != null) priceText.enabled = value;
            if (nutritionText != null) nutritionText.enabled = value;
            if (healthText != null) healthText.enabled = value;
        }

        public void SetDefinition(CardDefinition newDefinition)
        {
            if (newDefinition == null)
            {
                Debug.LogWarning("CardInstance: Attempted to set a null card definition.", this);
                return;
            }

            Definition = newDefinition;
            Stats = Definition.CreateCombatStats();
            RefreshLocalizedPresentation();
            ApplyArtTexture(Definition.ArtTexture);

            FeelPresenter?.RefreshMaterialState();
        }

        public void RestoreSavedStats(CardData cardData)
        {
            UsesLeft = cardData.UsesLeft;
            CurrentHealth = cardData.CurrentHealth;
            CurrentNutrition = cardData.CurrentNutrition;

            UpdateStatDisplays();

            if (gameObject.TryGetComponent<ChestLogic>(out var chest))
            {
                chest.RestoreCoins(cardData.StoredCoins);
            }

            if (cardData.IsAILocked && gameObject.TryGetComponent<VillagerLockToggle>(out var lockToggle))
            {
                lockToggle.RestoreLocked(true);
            }
        }
        #endregion

        #region Localization & Presentation
        private bool CanStack(CardDefinition bottom, CardDefinition top)
        {
            return CardManager.Instance != null && CardManager.Instance.CanStack(bottom, top);
        }

        private void HandleLanguageChanged(GameLanguage _)
        {
            RefreshLocalizedPresentation();
        }

        public void RefreshDisplay() => RefreshLocalizedPresentation();

        private void RefreshLocalizedPresentation()
        {
            if (Definition == null)
            {
                return;
            }

            string title = _lockToggle != null && _lockToggle.IsLocked
                ? $"[#] {Definition.DisplayName}"
                : Definition.DisplayName;

            if (titleText != null)
            {
                titleText.text = title;
            }

            View?.SetTitle(title);

            if (_isHovered)
            {
                InfoPanel.Instance?.RegisterHover(GetInfo());
            }
        }

        private void ApplyArtTexture(Texture texture)
        {
            if (_renderer != null && texture != null)
            {
                // Fix C-2: use MaterialPropertyBlock to avoid creating a material instance,
                // which would conflict with CardFeelPresenter's sharedMaterial baseline.
                if (_artPropBlock == null) _artPropBlock = new MaterialPropertyBlock();
                _renderer.GetPropertyBlock(_artPropBlock);
                _artPropBlock.SetTexture("_OverlayTex", texture);
                _renderer.SetPropertyBlock(_artPropBlock);
            }

            View?.SetArt(texture);
        }
        #endregion

        #region Movement & Animation
        public void SetTargetAnimated(Vector3 target, bool forceGround = false)
        {
            _isFollowingDamped = false;

            if (forceGround) target.y = 0f;
            KillMoveTween();
            _moveTween = transform.DOMove(target, Settings.MoveDuration)
                .SetEase(Settings.MoveEase)
                .SetUpdate(true)
                .SetLink(gameObject);

            if (Time.timeScale == 0f)
            {
                _moveTween.OnUpdate(() => Physics.SyncTransforms());
            }
        }

        public void SetTargetAnimated(Vector3 target, float duration, Ease ease, bool forceGround = false)
        {
            _isFollowingDamped = false;

            if (forceGround) target.y = 0f;
            KillMoveTween();
            _moveTween = transform.DOMove(target, duration)
                .SetEase(ease)
                .SetUpdate(true)
                .SetLink(gameObject);

            if (Time.timeScale == 0f)
            {
                _moveTween.OnUpdate(() => Physics.SyncTransforms());
            }
        }

        public void SetTargetInstant(Vector3 target, bool forceGround = false)
        {
            _isFollowingDamped = false;

            if (forceGround) target.y = 0f;
            KillMoveTween();
            transform.position = target;

            if (Time.timeScale == 0f) Physics.SyncTransforms();
        }

        public void SetTargetDamped(Vector3 target)
        {
            KillMoveTween();
            _dampedTargetPos = target;
            _isFollowingDamped = true;
        }

        private Vector3 ExponentialMove(Vector3 current, Vector3 target, float sharpness, float deltaTime)
        {
            float t = 1f - Mathf.Exp(-sharpness * deltaTime);
            return Vector3.LerpUnclamped(current, target, t);
        }

        public bool IsCombatant(CardInstance c) =>
            c.Definition.Category == CardCategory.Character ||
            c.Definition.Category == CardCategory.Mob;

        public Tween StartCombatTween(Tween tween)
        {
            KillMotionTweens();

            _combatTween = tween;
            return _combatTween;
        }

        public void KillMotionTweens()
        {
            KillMoveTween();
            _combatTween?.Kill();
            _combatTween = null;
            _isFollowingDamped = false;
        }

        private void KillMoveTween()
        {
            _moveTween?.Kill();
            _moveTween = null;
        }

        public void KillTweens()
        {
            KillMotionTweens();
            _hurtTween?.Kill();
            _hurtTween = null;
            FeelPresenter?.KillFeelTweens();
        }
        #endregion
    }
}

