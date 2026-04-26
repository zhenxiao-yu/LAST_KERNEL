// CardInstance — Runtime representation of a single card on the board.
//
// Owns the card's identity (Definition, Stats), mutable runtime state
// (CurrentHealth, CurrentNutrition, UsesLeft), and visual presentation.
// Acts as the integration point for optional capability components:
//   CardCombatant    — combat attack/defense logic
//   CardEquipper     — equipping items onto this card (class change)
//   CardEquipment    — this card IS an item that can be equipped onto another
//   CardFeelPresenter — hover/pickup/damage visual feedback (shaders, tweens)
//
// Regions:
//   Fields & Properties      — serialized text refs, runtime state, component refs
//   Lifecycle & Initialization — Initialize, pointer hover, OnEnable/Disable, Update
//   Information & Visuals    — hover tooltip, stat text, highlight, art texture
//   World Interactions       — Consume (feeding animation), TryAttachToNearbyStack
//   State Management         — Heal, TakeDamage, Kill, SetDefinition, RestoreSavedStats
//   Localization & Presentation — language change handler, localized text refresh
//   Movement & Animation     — tween-based movement, damped drag trailing, combat tweens
//
// Future refactor candidates:
//   • TryAttachToNearbyStack — merge + validation logic; natural home is CardController
//     once CardController owns the full drag-drop lifecycle end-to-end.
//   • SetTargetAnimated/Instant/Damped + tween fields — natural CardMovement component;
//     requires CardStack to cache the component instead of calling through CardInstance.
//
// Key dependencies:
//   CardManager      — stack registration, overlap resolution, stats notification
//   CraftingManager  — crafting task queries in TryAttachToNearbyStack
//   CardFeelPresenter — damage and hover visual feedback delegation
//   GameLocalization — display name and description refresh on language change

using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        [Header("Identification")]
        [SerializeField, Tooltip("The TextMeshPro component used to display the card's name.")]
        private TextMeshPro titleText;

        [Header("Stat Displays")]
        [SerializeField, Tooltip("The TextMeshPro component for displaying the card's sell price.")]
        private TextMeshPro priceText;

        [SerializeField, Tooltip("The TextMeshPro component for displaying the card's nutrition value.")]
        private TextMeshPro nutritionText;

        [SerializeField, Tooltip("The TextMeshPro component for displaying the card's current health.")]
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

        private Camera _mainCam;
        private MeshRenderer _renderer;
        private BoxCollider _col;

        private Tween _moveTween;
        private Tween _combatTween;
        private Tween _hurtTween;

        private Highlight _highlight;

        private bool _isHovered;

        private Vector3 _dampVelocity;
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

        /// <summary>
        /// Fully initializes the card: setting definitions, generating stats, applying visuals,
        /// creating and registering its stack, and attempting to merge with any nearby compatible stack.
        /// </summary>
        /// <param name="definition">The data definition for this card.</param>
        /// <param name="settings">Runtime movement and behavior settings.</param>
        /// <param name="stackToIgnore">A specific stack to ignore when searching for nearby merge candidates.</param>
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
            View = GetComponent<CardView>();

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
            InfoPanel.Instance?.RegisterHover(GetInfo());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
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

                var grouped = Stack.Cards
                    .GroupBy(c => c.Definition)
                    .Select(g => new { g.Key.DisplayName, Count = g.Count() })
                    .ToList();

                for (int i = 0; i < grouped.Count; i++)
                {
                    var item = grouped[i];
                    info.body += $"{item.DisplayName} x{item.Count}";
                    info.body += i < grouped.Count - 1 ? ", " : ".";
                }
            }
            else if (Stack.TopCard != null)
            {
                info.header = Stack.TopCard.Definition.DisplayName;
                info.body = Stack.TopCard.Definition.Description;

                if (Stack.TopCard.Definition.Category is CardCategory.Character)
                {
                    info.body += "\n" + GameLocalization.Format("card.health", CurrentHealth, Stack.TopCard.Stats.MaxHealth.Value);

                    if (Stack.TopCard.Definition.CombatType != CombatType.None)
                    {
                        info.body += $"\n{Stack.TopCard.Definition.CombatType.ToString()}";
                        info.body += $"\n{Stack.TopCard.Stats.GetFormattedStats()}";
                    }
                }
            }

            return info;
        }

        /// <summary>
        /// Updates the visible TextMeshPro displays (price, nutrition, and health)
        /// to reflect the card's current stat values.
        /// </summary>
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

        /// <summary>
        /// Allows external components to safely update the price text.
        /// </summary>
        public void UpdatePriceText(string text)
        {
            if (priceText != null)
            {
                priceText.text = text;
            }
        }

        /// <summary>
        /// Controls the visual highlighting state of the card.
        /// Creates the necessary <see cref="Highlight"/> component if it does not already exist.
        /// </summary>
        /// <param name="value">If true, the card is highlighted; otherwise, the highlight is hidden.</param>
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
            }
            else _highlight.SetActive(value);

            View?.SetHighlighted(value);
        }

        /// <summary>
        /// Instantiates the <see cref="PuffParticle"/> visual effect at the card's position.
        /// This effect typically plays when the card performs an action or is destroyed.
        /// </summary>
        public void PlayPuffParticle()
        {
            if (Settings?.PuffParticle != null)
            {
                Instantiate(Settings.PuffParticle, transform.position, Quaternion.identity);
            }
        }
        #endregion

        #region World Interactions
        /// <summary>
        /// Executes the process of consuming this card as food for a character.
        /// </summary>
        /// <param name="character">The <see cref="CardInstance"/> performing the consumption.</param>
        /// <param name="amountNeeded">The total amount of nutrition the character requires.</param>
        /// <param name="onConsumed">Action invoked with the actual amount of nutrition consumed.</param>
        /// <returns>An IEnumerator for use in a coroutine, handling animations and delays.</returns>
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
                .WaitForCompletion();

            Vector3 target = new Vector3(
                character.transform.position.x,
                Settings.DragHeight,
                character.transform.position.z
            );

            AudioManager.Instance?.PlaySFX(AudioId.CardSwipe);

            yield return transform.DOMove(target, 0.2f)
                .SetUpdate(true)
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

        /// <summary>
        /// Searches for a nearby compatible <see cref="CardStack"/> and merges into it.
        /// Validates stacking rules, crafting ingredient safety, and board placement.
        /// </summary>
        /// <param name="radius">Sphere radius to search for candidate stacks.</param>
        /// <param name="stackToIgnore">Stack to exclude from consideration (e.g. the stack just split from).</param>
        /// <returns>The stack the card merged into, or null if no compatible candidate was found.</returns>
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

            // Never reset an active workstation timer — only check for a new recipe on idle stacks.
            if (!bestCandidateStack.IsCrafting)
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
        /// <summary>
        /// Decrements the <see cref="UsesLeft"/> counter for the card.
        /// </summary>
        public void Use() => UsesLeft--;

        /// <summary>
        /// Increases the card's <see cref="CurrentHealth"/> by the specified amount and updates the stat displays.
        /// </summary>
        /// <param name="healAmount">The amount of health to restore.</param>
        public void Heal(int healAmount)
        {
            CurrentHealth = Stats != null
                ? Mathf.Min(Stats.MaxHealth.Value, CurrentHealth + Mathf.Max(0, healAmount))
                : CurrentHealth + Mathf.Max(0, healAmount);
            UpdateStatDisplays();
        }

        /// <summary>
        /// Reduces the card's <see cref="CurrentHealth"/> by the specified damage amount,
        /// ensuring health does not drop below zero, and triggers visual effects (flash and shake).
        /// </summary>
        /// <param name="damage">The amount of damage to inflict.</param>
        public void TakeDamage(int damage)
        {
            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
            UpdateStatDisplays();

            _hurtTween?.Kill();
            _hurtTween = null;

            bool handledByFeel = FeelPresenter != null && FeelPresenter.OnDamageTaken();
            if (handledByFeel)
            {
                return;
            }

            var sequence = DOTween.Sequence();

            if (_renderer != null && _renderer.material.HasProperty("_FlashAmount"))
            {
                sequence.Join(_renderer.material
                    .DOFloat(1f, "_FlashAmount", 0.1f)
                    .SetDelay(0.05f)
                    .SetLoops(2, LoopType.Yoyo));
            }

            sequence.Join(transform
                .DOPunchRotation(new Vector3(0, 15, 0), 0.25f, vibrato: 25));

            _hurtTween = sequence.SetUpdate(true);
        }

        /// <summary>
        /// Destroys the card instance, notifying the <see cref="CardManager"/>, cleaning up combat status and equipment, 
        /// spawning loot if applicable, playing the puff particle effect, and finally destroying the card/stack.
        /// </summary>
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

        /// <summary>
        /// Toggles the visibility and collision state of the card and all its display components.
        /// </summary>
        /// <param name="value">If true<, the card is visible and collidable; otherwise, it is hidden.</param>
        public void SetVisible(bool value)
        {
            if (_renderer != null) _renderer.enabled = value;
            if (_col != null) _col.enabled = value;

            if (titleText != null) titleText.enabled = value;
            if (priceText != null) priceText.enabled = value;
            if (nutritionText != null) nutritionText.enabled = value;
            if (healthText != null) healthText.enabled = value;
        }

        /// <summary>
        /// Force-sets the card's definition, updates its combat stats, name, and art texture.
        /// Used primarily by <see cref="CardEquipper"/> when a card's class or type changes.
        /// </summary>
        /// <param name="newDefinition">The new definition to assign to the card.</param>
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

        /// <summary>
        /// Overwrites the card's current dynamic stats (UsesLeft, CurrentHealth, CurrentNutrition) 
        /// with values loaded from saved data and updates the displays.
        /// </summary>
        /// <param name="cardData">The data object containing the saved stat values.</param>
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

        private void RefreshLocalizedPresentation()
        {
            if (Definition == null)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text = Definition.DisplayName;
            }

            View?.SetTitle(Definition.DisplayName);

            if (_isHovered)
            {
                InfoPanel.Instance?.RegisterHover(GetInfo());
            }
        }

        private void ApplyArtTexture(Texture texture)
        {
            if (_renderer != null && texture != null)
            {
                _renderer.material.SetTexture("_OverlayTex", texture);
            }

            View?.SetArt(texture);
        }
        #endregion

        #region Movement & Animation
        /// <summary>
        /// Moves the card to a specified target position using a DOTween animation,
        /// overriding any existing movement tweens.
        /// </summary>
        /// <param name="target">The world position to move the card to.</param>
        /// <param name="forceGround">If true, forces the Y position of the target to 0f.</param>
        public void SetTargetAnimated(Vector3 target, bool forceGround = false)
        {
            _isFollowingDamped = false;

            if (forceGround) target.y = 0f;
            KillMoveTween();
            _moveTween = transform.DOMove(target, Settings.MoveDuration)
                .SetEase(Settings.MoveEase)
                .SetUpdate(true);

            if (Time.timeScale == 0f)
            {
                _moveTween.OnUpdate(() => Physics.SyncTransforms());
            }
        }

        /// <summary>
        /// Immediately sets the card's position to the specified target, cancelling any active move tweens.
        /// </summary>
        /// <param name="target">The world position to place the card at.</param>
        /// <param name="forceGround">If true, forces the Y position of the target to 0f.</param>
        public void SetTargetInstant(Vector3 target, bool forceGround = false)
        {
            _isFollowingDamped = false;

            if (forceGround) target.y = 0f;
            KillMoveTween();
            transform.position = target;

            if (Time.timeScale == 0f) Physics.SyncTransforms();
        }

        /// <summary>
        /// Sets the target for the card to move towards using SmoothDamp.
        /// Used specifically for trailing cards during a drag.
        /// </summary>
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

        /// <summary>
        /// Checks if the given card is categorized as a Character or a Mob,
        /// indicating it is a combat-capable entity.
        /// </summary>
        /// <param name="c">The <see cref="CardInstance"/> to check.</param>
        /// <returns>True if the card is a Character or Mob; otherwise, false.</returns>
        public bool IsCombatant(CardInstance c) =>
            c.Definition.Category == CardCategory.Character ||
            c.Definition.Category == CardCategory.Mob;

        /// <summary>
        /// Sets a new combat-related DOTween animation, first clearing any existing move or combat tweens.
        /// </summary>
        /// <param name="tween">The new DOTween animation to execute for combat visuals.</param>
        /// <returns>The combat Tween that was started.</returns>
        public Tween StartCombatTween(Tween tween)
        {
            KillMotionTweens();

            _combatTween = tween;
            return _combatTween;
        }

        /// <summary>
        /// Stops card movement/combat tweens without touching the presentation tweens.
        /// Stack layout uses this frequently; killing feel here would cancel hover/spawn feedback.
        /// </summary>
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

        /// <summary>
        /// Safely stops and cleans up all active DOTween animations on this card, including feel tweens.
        /// </summary>
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

