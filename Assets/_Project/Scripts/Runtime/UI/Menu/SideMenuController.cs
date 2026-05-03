using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    [RequireComponent(typeof(UIDocument))]
    public class SideMenuController : MonoBehaviour
    {
        // ── Constants ────────────────────────────────────────────────────

        private const float SLIDE_SECONDS = 0.2f;
        private const float PANEL_WIDTH_FALLBACK = 380f;

        private float _panelWidth = PANEL_WIDTH_FALLBACK;

        private const string SYMBOL_EXPANDED  = "▼"; // ▼
        private const string SYMBOL_COLLAPSED = "►"; // ►

        // ── UI elements ──────────────────────────────────────────────────

        private UIDocument   _doc;
        private VisualElement _root;
        private VisualElement _panel;
        private Button        _btnToggle;
        private Button        _btnTabQuests;
        private Button        _btnTabRecipes;
        private VisualElement _questsTab;
        private VisualElement _recipesTab;
        private VisualElement _questsList;
        private VisualElement _recipesList;
        private Label         _lblQuestProgress;
        private VisualElement _fillQuestProgress;
        private Label         _lblQuestsEmpty;
        private Label         _lblRecipesEmpty;

        public static SideMenuController Instance { get; private set; }

        // ── Slide state ──────────────────────────────────────────────────

        private bool    _isOpen;
        private float   _slideX = PANEL_WIDTH_FALLBACK;
        private Tweener _slideTween;

        // ── Quest tracking ───────────────────────────────────────────────

        private readonly Dictionary<QuestGroup, VisualElement>    _groupHeaders  = new();
        private readonly Dictionary<QuestGroup, VisualElement>    _groupBodies   = new();
        private readonly Dictionary<QuestInstance, VisualElement> _questItems    = new();
        private readonly Dictionary<Quest, QuestGroup>            _questToGroup  = new();
        private readonly Dictionary<QuestGroup, bool>             _groupExpanded = new();

        // ── Recipe tracking ──────────────────────────────────────────────

        private readonly Dictionary<RecipeCategory, VisualElement> _categoryHeaders  = new();
        private readonly Dictionary<RecipeCategory, VisualElement> _categoryBodies   = new();
        private readonly Dictionary<string, VisualElement>         _recipeItems      = new();
        private readonly Dictionary<string, RecipeCategory>        _recipeCategories = new();
        private readonly Dictionary<RecipeCategory, bool>          _categoryExpanded = new();

        // ────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _doc  = GetComponent<UIDocument>();
            _root = _doc.rootVisualElement;

            _panel           = _root.Q("side-menu-panel");
            _btnToggle       = _root.Q<Button>("btn-toggle");
            _btnTabQuests    = _root.Q<Button>("btn-tab-quests");
            _btnTabRecipes   = _root.Q<Button>("btn-tab-recipes");
            _questsTab       = _root.Q("quests-tab");
            _recipesTab      = _root.Q("recipes-tab");
            _questsList      = _root.Q("quests-list");
            _recipesList     = _root.Q("recipes-list");
            _lblQuestProgress  = _root.Q<Label>("lbl-quest-progress");
            _fillQuestProgress = _root.Q("fill-quest-progress");
            _lblQuestsEmpty    = _root.Q<Label>("lbl-quests-empty");
            _lblRecipesEmpty   = _root.Q<Label>("lbl-recipes-empty");

            // Start fully closed (off-screen to the right)
            SetTranslateX(_panelWidth);
            _panel.RegisterCallback<GeometryChangedEvent>(OnPanelGeometryChanged);

            _btnToggle.clicked     += OnToggle;
            _btnTabQuests.clicked  += () => ShowTab(quests: true);
            _btnTabRecipes.clicked += () => ShowTab(quests: false);

            _btnTabQuests.text  = GameLocalization.GetOptional("menu.tab.quests",  "QUESTS");
            _btnTabRecipes.text = GameLocalization.GetOptional("menu.tab.recipes", "RECIPES");

            if (_lblQuestsEmpty  != null) _lblQuestsEmpty.text  = GameLocalization.GetOptional("menu.quests.empty",  "No active quests.");
            if (_lblRecipesEmpty != null) _lblRecipesEmpty.text = GameLocalization.GetOptional("menu.recipes.empty", "No recipes discovered yet.");
        }

        private void Start()
        {
            UIScaleManager.Register(_doc, _root);
            BuildQuestGroupMap();
            PopulateQuests();
            PopulateRecipes();
            UpdateQuestProgress();
            RefreshEmptyStates();
            SubscribeEvents();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            UIScaleManager.Unregister(_doc);
            _slideTween?.Kill();
            UnsubscribeEvents();
        }

        // ────────────────────────────────────────────────────────────────
        // Open / close
        // ────────────────────────────────────────────────────────────────

        private void OnToggle() => Toggle();

        /// <summary>Toggles the side menu open or closed. Called by keyboard shortcut (Q).</summary>
        public void Toggle()
        {
            if (_isOpen) Close(); else Open();
        }

        private void Open()
        {
            _isOpen = true;
            _btnToggle.text = "«";
            SlideTo(0f, Ease.OutCubic);
            AudioManager.Instance?.PlaySFX(AudioId.Click);
        }

        private void OnPanelGeometryChanged(GeometryChangedEvent evt)
        {
            float w = evt.newRect.width;
            if (w <= 0) return;
            _panelWidth = w;
            if (!_isOpen) SetTranslateX(_panelWidth);
        }

        private void Close()
        {
            _isOpen = false;
            _btnToggle.text = "»";
            SlideTo(_panelWidth, Ease.InCubic);
            AudioManager.Instance?.PlaySFX(AudioId.Click);
        }

        private void SlideTo(float targetX, Ease ease)
        {
            _slideTween?.Kill();
            _slideTween = DOTween.To(
                () => _slideX,
                x  => { _slideX = x; SetTranslateX(x); },
                targetX, SLIDE_SECONDS
            ).SetEase(ease).SetUpdate(true).SetLink(gameObject);
        }

        private void SetTranslateX(float x)
        {
            _panel.style.translate = new StyleTranslate(new Translate(x, 0f));
        }

        // ────────────────────────────────────────────────────────────────
        // Tab switching
        // ────────────────────────────────────────────────────────────────

        private void ShowTab(bool quests)
        {
            _questsTab.EnableInClassList("lk-hidden",  !quests);
            _recipesTab.EnableInClassList("lk-hidden",  quests);
            _btnTabQuests.EnableInClassList("lk-tab--active",   quests);
            _btnTabRecipes.EnableInClassList("lk-tab--active", !quests);
        }

        // ────────────────────────────────────────────────────────────────
        // Quest population
        // ────────────────────────────────────────────────────────────────

        private void BuildQuestGroupMap()
        {
            _questToGroup.Clear();
            if (QuestManager.Instance == null) return;

            foreach (var group in QuestManager.Instance.QuestGroups)
                foreach (var quest in group.Quests)
                    _questToGroup.TryAdd(quest, group);
        }

        private void PopulateQuests()
        {
            _questsList.Clear();
            _groupHeaders.Clear();
            _groupBodies.Clear();
            _questItems.Clear();
            _groupExpanded.Clear();

            if (QuestManager.Instance == null) return;

            foreach (var group in QuestManager.Instance.QuestGroups)
            {
                _groupExpanded[group] = true;
                CreateGroupHeader(group);
            }

            foreach (var quest in QuestManager.Instance.AllQuests)
                CreateQuestItem(quest);
        }

        private void CreateGroupHeader(QuestGroup group)
        {
            var header = new VisualElement();
            header.AddToClassList("lk-list__header");

            var chevron = MakeLabel(SYMBOL_EXPANDED, "lk-label--dim");
            chevron.style.marginRight = 6;
            chevron.style.flexShrink = 0;

            var nameLabel = MakeLabel(GetGroupName(group), "lk-list__header-label");
            nameLabel.style.flexGrow = 1;

            var countLabel = MakeLabel(GetGroupCount(group), "lk-label--dim");

            header.Add(chevron);
            header.Add(nameLabel);
            header.Add(countLabel);

            var body = new VisualElement();
            body.AddToClassList("lk-column");

            _groupHeaders[group] = header;
            _groupBodies[group]  = body;

            header.RegisterCallback<PointerUpEvent>(_ => ToggleGroup(group, chevron, countLabel));

            bool hasActiveQuests = QuestManager.Instance?.AllQuests
                .Any(qi => _questToGroup.TryGetValue(qi.QuestData, out var g) && g == group) ?? false;
            if (!hasActiveQuests)
            {
                header.style.display = DisplayStyle.None;
                body.style.display   = DisplayStyle.None;
            }

            _questsList.Add(header);
            _questsList.Add(body);
        }

        private void CreateQuestItem(QuestInstance quest)
        {
            if (quest == null || _questItems.ContainsKey(quest)) return;
            if (!_questToGroup.TryGetValue(quest.QuestData, out var group)) return;
            if (!_groupBodies.TryGetValue(group, out var body)) return;

            var item = new VisualElement();
            item.AddToClassList("lk-list__item");

            if (quest.IsComplete())
                item.AddToClassList("lk-list__item--completed");

            string itemId = quest.QuestData.Id;
            if (IsNew(itemId))
            {
                item.AddToClassList("lk-list__item--new");
                item.Add(MakeDot());
            }

            item.Add(MakeIconPlaceholder());
            var nameLabel = MakeLabel(quest.QuestData.Title, "lk-list__item-label");
            item.Add(nameLabel);
            AppendQuestRight(item, quest);

            item.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (IsNew(itemId)) MarkSeen(itemId, item);
                ShowQuestInfo(quest);
            });
            item.RegisterCallback<PointerLeaveEvent>(_ => HideInfo());

            item.style.display = _groupExpanded.GetValueOrDefault(group, true)
                ? DisplayStyle.Flex : DisplayStyle.None;

            _questItems[quest] = item;
            body.Add(item);
        }

        private static void AppendQuestRight(VisualElement item, QuestInstance quest)
        {
            // Remove any existing right-side elements (progress / checkmark)
            var stale = item.Children()
                .Where(c => c.ClassListContains("lk-label--dim") || c.ClassListContains("lk-label--accent"))
                .ToList();
            foreach (var s in stale) s.RemoveFromHierarchy();

            if (quest.IsComplete())
            {
                var check = MakeLabel("√", "lk-label--accent"); // √
                check.style.flexShrink = 0;
                item.Add(check);
            }
            else if (quest.QuestData.TargetAmount > 1)
            {
                var prog = MakeLabel($"{quest.CurrentAmount}/{quest.QuestData.TargetAmount}", "lk-label--dim");
                prog.style.flexShrink = 0;
                item.Add(prog);
            }
        }

        private void ToggleGroup(QuestGroup group, Label chevron, Label countLabel)
        {
            bool expanded = !_groupExpanded.GetValueOrDefault(group, true);
            _groupExpanded[group] = expanded;
            chevron.text = expanded ? SYMBOL_EXPANDED : SYMBOL_COLLAPSED;
            countLabel.text = GetGroupCount(group);

            if (_groupBodies.TryGetValue(group, out var body))
                foreach (var child in body.Children())
                    child.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void HandleQuestActivated(QuestInstance quest)
        {
            if (_questToGroup.TryGetValue(quest.QuestData, out var group))
            {
                if (_groupHeaders.TryGetValue(group, out var header))
                    header.style.display = DisplayStyle.Flex;
                if (_groupBodies.TryGetValue(group, out var body))
                    body.style.display   = DisplayStyle.Flex;
            }
            CreateQuestItem(quest);
            RefreshGroupCount(quest.QuestData);
            UpdateQuestProgress();
            RefreshEmptyStates();
        }

        private void HandleQuestCompleted(QuestInstance quest)
        {
            if (!_questItems.TryGetValue(quest, out var item)) return;

            item.AddToClassList("lk-list__item--completed");
            var nameLabel = item.Q<Label>(className: "lk-list__item-label");
            if (nameLabel != null) nameLabel.text = quest.QuestData.Title;
            AppendQuestRight(item, quest);

            RefreshGroupCount(quest.QuestData);
            UpdateQuestProgress();
        }

        private void RefreshGroupCount(Quest questData)
        {
            if (!_questToGroup.TryGetValue(questData, out var group)) return;
            if (!_groupHeaders.TryGetValue(group, out var header)) return;

            var countLabel = header.Children().LastOrDefault() as Label;
            if (countLabel != null) countLabel.text = GetGroupCount(group);
        }

        private void UpdateQuestProgress()
        {
            if (QuestManager.Instance == null) return;

            int total = QuestManager.Instance.AllQuests.Count();
            int done  = QuestManager.Instance.CompletedQuestsCount;

            if (_lblQuestProgress != null)
                _lblQuestProgress.text = GameLocalization.Format("quest.footer.progress", done, total);

            if (_fillQuestProgress != null && total > 0)
                _fillQuestProgress.style.width = new StyleLength(
                    new Length((float)done / total * 100f, LengthUnit.Percent));
        }

        private string GetGroupCount(QuestGroup group)
        {
            if (QuestManager.Instance == null) return "0/0";
            int total = group.Quests.Count;
            int done  = group.Quests.Count(q =>
            {
                var inst = QuestManager.Instance.AllQuests.FirstOrDefault(i => i.QuestData == q);
                return inst != null && inst.IsComplete();
            });
            return $"{done}/{total}";
        }

        // ────────────────────────────────────────────────────────────────
        // Recipe population
        // ────────────────────────────────────────────────────────────────

        private void PopulateRecipes()
        {
            _recipesList.Clear();
            _categoryHeaders.Clear();
            _categoryBodies.Clear();
            _recipeItems.Clear();
            _recipeCategories.Clear();
            _categoryExpanded.Clear();

            if (CraftingManager.Instance == null) return;

            var byCategory = CraftingManager.Instance.AllRecipes
                .OrderBy(r => r.Category)
                .ThenBy(r => r.DisplayName)
                .GroupBy(r => r.Category);

            foreach (var g in byCategory)
            {
                _categoryExpanded[g.Key] = true;
                CreateCategoryHeader(g.Key, g.ToList());
                foreach (var recipe in g) CreateRecipeItem(recipe);
            }
        }

        private void CreateCategoryHeader(RecipeCategory category, List<RecipeDefinition> recipes)
        {
            int discovered = recipes.Count(r => CraftingManager.Instance.IsRecipeDiscovered(r.Id));

            var header = new VisualElement();
            header.AddToClassList("lk-list__header");
            if (discovered == 0) header.style.display = DisplayStyle.None;

            var chevron = MakeLabel(SYMBOL_EXPANDED, "lk-label--dim");
            chevron.style.marginRight = 6;
            chevron.style.flexShrink = 0;

            var nameLabel = MakeLabel(GameLocalization.GetRecipeCategoryLabel(category), "lk-list__header-label");
            nameLabel.style.flexGrow = 1;

            var countLabel = MakeLabel($"{discovered}/{recipes.Count}", "lk-label--dim");

            header.Add(chevron);
            header.Add(nameLabel);
            header.Add(countLabel);

            var body = new VisualElement();
            body.AddToClassList("lk-column");
            if (discovered == 0) body.style.display = DisplayStyle.None;

            _categoryHeaders[category] = header;
            _categoryBodies[category]  = body;

            header.RegisterCallback<PointerUpEvent>(_ => ToggleCategory(category, chevron));

            _recipesList.Add(header);
            _recipesList.Add(body);
        }

        private void CreateRecipeItem(RecipeDefinition recipe)
        {
            if (recipe.ResultingCard == null) return;

            var item = new VisualElement();
            item.AddToClassList("lk-list__item");

            bool discovered = CraftingManager.Instance.IsRecipeDiscovered(recipe.Id);
            item.style.display = discovered ? DisplayStyle.Flex : DisplayStyle.None;

            string itemId = recipe.Id;
            if (discovered && IsNew(itemId))
            {
                item.AddToClassList("lk-list__item--new");
                item.Add(MakeDot());
            }

            item.Add(MakeIconPlaceholder());
            var nameLabel = MakeLabel(recipe.DisplayName, "lk-list__item-label");
            item.Add(nameLabel);

            item.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (!CraftingManager.Instance.IsRecipeDiscovered(itemId)) return;
                if (IsNew(itemId)) MarkSeen(itemId, item);
                ShowRecipeInfo(recipe);
            });
            item.RegisterCallback<PointerLeaveEvent>(_ => HideInfo());

            _recipeItems[itemId]      = item;
            _recipeCategories[itemId] = recipe.Category;

            if (_categoryBodies.TryGetValue(recipe.Category, out var body))
                body.Add(item);
        }

        private void HandleRecipeDiscovered(string recipeId)
        {
            if (!_recipeItems.TryGetValue(recipeId, out var item)) return;
            if (!_recipeCategories.TryGetValue(recipeId, out var category)) return;

            bool expanded = _categoryExpanded.GetValueOrDefault(category, true);
            item.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;

            if (IsNew(recipeId))
            {
                item.AddToClassList("lk-list__item--new");
                if (item.Q(className: "lk-new-dot") == null)
                    item.Add(MakeDot());
            }

            if (_categoryHeaders.TryGetValue(category, out var catHeader))
                catHeader.style.display = DisplayStyle.Flex;
            if (_categoryBodies.TryGetValue(category, out var catBody))
                catBody.style.display = DisplayStyle.Flex;

            RefreshCategoryCount(category);
            RefreshEmptyStates();
        }

        private void ToggleCategory(RecipeCategory category, Label chevron)
        {
            bool expanded = !_categoryExpanded.GetValueOrDefault(category, true);
            _categoryExpanded[category] = expanded;
            chevron.text = expanded ? SYMBOL_EXPANDED : SYMBOL_COLLAPSED;

            foreach (var (recipeId, item) in _recipeItems)
            {
                if (!_recipeCategories.TryGetValue(recipeId, out var cat) || cat != category) continue;
                if (CraftingManager.Instance.IsRecipeDiscovered(recipeId))
                    item.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void RefreshCategoryCount(RecipeCategory category)
        {
            if (!_categoryHeaders.TryGetValue(category, out var header)) return;
            var recipes = CraftingManager.Instance.AllRecipes.Where(r => r.Category == category).ToList();
            int discovered = recipes.Count(r => CraftingManager.Instance.IsRecipeDiscovered(r.Id));
            var countLabel = header.Children().LastOrDefault() as Label;
            if (countLabel != null) countLabel.text = $"{discovered}/{recipes.Count}";
        }

        // ────────────────────────────────────────────────────────────────
        // Info panel
        // ────────────────────────────────────────────────────────────────

        private void ShowQuestInfo(QuestInstance quest)
        {
            if (InfoPanel.Instance == null) return;
            bool complete = quest.IsComplete();
            string header = complete
                ? $"√ {quest.QuestData.Title}"
                : quest.QuestData.Title;
            string body = quest.QuestData.Description;
            if (!complete)
                body += $"\n\n{GameLocalization.Format("quest.progress", quest.CurrentAmount, quest.QuestData.TargetAmount)}";
            else
                body += $"\n\n{GameLocalization.GetOptional("quest.completed", "Completed")}";
            InfoPanel.Instance.RequestInfoDisplay(this, InfoPriority.Hover, (header, body));
        }

        private void ShowRecipeInfo(RecipeDefinition recipe)
        {
            if (InfoPanel.Instance == null) return;
            string ingredients = CraftingManager.Instance?.GetFormattedIngredients(recipe) ?? string.Empty;
            string body = string.IsNullOrEmpty(ingredients)
                ? GameLocalization.GetOptional("recipe.no_ingredients", "No ingredients required.")
                : ingredients;
            InfoPanel.Instance.RequestInfoDisplay(this, InfoPriority.Hover, (recipe.ResultingCard.DisplayName, body));
        }

        private void HideInfo() => InfoPanel.Instance?.ClearInfoRequest(this);

        private void RefreshEmptyStates()
        {
            bool hasQuests = QuestManager.Instance != null &&
                             QuestManager.Instance.AllQuests.Any();
            _lblQuestsEmpty?.EnableInClassList("lk-hidden", hasQuests);

            bool hasRecipes = CraftingManager.Instance != null &&
                              CraftingManager.Instance.AllRecipes.Any(r => CraftingManager.Instance.IsRecipeDiscovered(r.Id));
            _lblRecipesEmpty?.EnableInClassList("lk-hidden", hasRecipes);
        }

        // ────────────────────────────────────────────────────────────────
        // "New" item helpers
        // ────────────────────────────────────────────────────────────────

        private static bool IsNew(string id) =>
            !string.IsNullOrEmpty(id) &&
            GameDirector.Instance?.GameData != null &&
            !GameDirector.Instance.GameData.SeenItems.Contains(id);

        private static void MarkSeen(string id, VisualElement item)
        {
            if (GameDirector.Instance?.GameData == null) return;
            GameDirector.Instance.GameData.SeenItems.Add(id);
            item.RemoveFromClassList("lk-list__item--new");
            item.Q(className: "lk-new-dot")?.RemoveFromHierarchy();
        }

        // ────────────────────────────────────────────────────────────────
        // Event subscription
        // ────────────────────────────────────────────────────────────────

        private void SubscribeEvents()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestActivated += HandleQuestActivated;
                QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;
            }

            if (CraftingManager.Instance != null)
                CraftingManager.Instance.OnRecipeDiscovered += HandleRecipeDiscovered;

            if (TimeManager.Instance != null)
                TimeManager.Instance.OnDayEnded += HandleDayEnded;
        }

        private void UnsubscribeEvents()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestActivated -= HandleQuestActivated;
                QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
            }

            if (CraftingManager.Instance != null)
                CraftingManager.Instance.OnRecipeDiscovered -= HandleRecipeDiscovered;

            if (TimeManager.Instance != null)
                TimeManager.Instance.OnDayEnded -= HandleDayEnded;
        }

        private void HandleDayEnded(int _) { if (_isOpen) Close(); }

        // ────────────────────────────────────────────────────────────────
        // Small helpers
        // ────────────────────────────────────────────────────────────────

        private static Label MakeLabel(string text, string cssClass)
        {
            var lbl = new Label(text);
            lbl.AddToClassList(cssClass);
            return lbl;
        }

        private static VisualElement MakeDot()
        {
            var dot = new VisualElement();
            dot.AddToClassList("lk-new-dot");
            return dot;
        }

        private static VisualElement MakeIconPlaceholder()
        {
            var icon = new VisualElement();
            icon.AddToClassList("lk-list__item-icon");
            return icon;
        }

        private static string GetGroupName(QuestGroup group)
        {
            if (group == null) return string.Empty;
            string key = $"quest.group.{LocalizationKeyBuilder.ToKeySegment(group.GroupName)}";
            return GameLocalization.GetOptional(key, group.GroupName);
        }
    }
}
