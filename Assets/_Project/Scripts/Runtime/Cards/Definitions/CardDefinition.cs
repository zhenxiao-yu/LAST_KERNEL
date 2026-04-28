using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    [CreateAssetMenu(menuName = "Last Kernel/Card", fileName = "Card_")]
    public class CardDefinition : ScriptableObject
    {
        // ── Identification ────────────────────────────────────────────────────

        [BoxGroup("Identification")]
        [SerializeField, ReadOnly, Tooltip("Unique identifier. Auto-generated on first OnValidate.")]
        private string id;

        [BoxGroup("Identification")]
        [SerializeField, Required]
        private string displayName;

        [BoxGroup("Identification")]
        [SerializeField, TextArea(2, 5)]
        private string description;

        // ── Art ───────────────────────────────────────────────────────────────

        [BoxGroup("Art")]
        [SerializeField, PreviewField(100, ObjectFieldAlignment.Left)]
        [ValidateInput("@artTexture != null", "Art texture is missing — card will appear blank in-game.")]
        private Texture2D artTexture;

        // ── Classification ────────────────────────────────────────────────────

        [BoxGroup("Classification")]
        [SerializeField]
        [ValidateInput("@category != CardCategory.None", "Category None is reserved for non-card objects (e.g. Packs).")]
        private CardCategory category;

        [BoxGroup("Classification")]
        [SerializeField]
        private CardFaction faction;

        [BoxGroup("Classification")]
        [SerializeField]
        private CombatType combatType = CombatType.None;

        // ── Loot ──────────────────────────────────────────────────────────────

        [FoldoutGroup("Loot")]
        [SerializeField, TableList(AlwaysExpanded = true, ShowIndexLabels = true)]
        private List<LootEntry> loot;

        // ── Mob AI ────────────────────────────────────────────────────────────

        [FoldoutGroup("Mob AI")]
        [SerializeField, Tooltip("FALSE = Passive Mob — uses Produce instead.")]
        private bool isAggressive = false;

        [FoldoutGroup("Mob AI"), ShowIf("isAggressive")]
        [SerializeField, Range(0f, 20f)]
        private float aggroRadius = 5f;

        [FoldoutGroup("Mob AI"), ShowIf("isAggressive")]
        [SerializeField, Range(0f, 10f)]
        private float attackRadius = 1.5f;

        // ── Produce (Passive Mob) ─────────────────────────────────────────────

        [FoldoutGroup("Produce"), HideIf("isAggressive")]
        [SerializeField]
        private CardDefinition produceCard;

        [FoldoutGroup("Produce"), HideIf("isAggressive")]
        [SerializeField, Min(1f)]
        private float produceInterval = 10f;

        // ── Economy ───────────────────────────────────────────────────────────

        [BoxGroup("Economy")]
        [SerializeField]
        private bool isSellable = true;

        [BoxGroup("Economy"), ShowIf("isSellable")]
        [SerializeField, Min(0)]
        [ValidateInput("@!isSellable || sellPrice > 0", "Sellable card should have sellPrice > 0.")]
        private int sellPrice = 1;

        [BoxGroup("Economy")]
        [SerializeField, Tooltip("Has limited uses (e.g. Trees, Rocks). False = single item.")]
        private bool hasDurability = false;

        [BoxGroup("Economy"), ShowIf("hasDurability")]
        [SerializeField, Min(1)]
        private int uses = 1;

        // ── Food ──────────────────────────────────────────────────────────────

        [BoxGroup("Food")]
        [SerializeField, Min(0)]
        private int nutrition;

        // ── Combat Stats ──────────────────────────────────────────────────────

        [FoldoutGroup("Combat Stats")]
        [SerializeField, Min(1)]
        private int maxHealth = 15;

        [FoldoutGroup("Combat Stats")]
        [SerializeField, Min(0)]
        private int attack = 2;

        [FoldoutGroup("Combat Stats")]
        [SerializeField, Min(0)]
        private int defense = 1;

        [FoldoutGroup("Combat Stats")]
        [SerializeField, Range(1, 300), Tooltip("Attacks per second in percent (100 = 1/s).")]
        private int attackSpeed = 100;

        [FoldoutGroup("Combat Stats")]
        [SerializeField, Range(0, 100)]
        private int accuracy = 95;

        [FoldoutGroup("Combat Stats")]
        [SerializeField, Range(0, 100)]
        private int dodge = 5;

        [FoldoutGroup("Combat Stats")]
        [SerializeField, Range(0, 100)]
        private int criticalChance = 5;

        [FoldoutGroup("Combat Stats")]
        [SerializeField, Range(100, 500), Tooltip("Critical damage multiplier in percent.")]
        private int criticalMultiplier = 150;

        // ── Equipment ─────────────────────────────────────────────────────────

        [FoldoutGroup("Equipment"), ShowIf("@category == CardCategory.Equipment")]
        [SerializeField]
        private EquipmentSlot equipmentSlot;

        [FoldoutGroup("Equipment"), ShowIf("@category == CardCategory.Equipment")]
        [SerializeField, TableList(ShowIndexLabels = true)]
        private List<StatModifier> statModifiers;

        [FoldoutGroup("Equipment"), ShowIf("@category == CardCategory.Equipment")]
        [SerializeField, InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        [Tooltip("If equipped, transforms the character into this definition.")]
        private CardDefinition classChangeResult;

        // ── Properties ────────────────────────────────────────────────────────

        public string Id => id;
        public string DisplayName => GetLocalizedDisplayName();
        public string Description => GetLocalizedDescription();
        public Texture2D ArtTexture => artTexture;

        public CardCategory Category => category;
        public CardFaction Faction => faction;
        public CombatType CombatType => combatType;

        public bool IsAggressive => isAggressive;
        public float AggroRadius => aggroRadius;
        public float AttackRadius => attackRadius;

        public CardDefinition ProduceCard => produceCard;
        public float ProduceInterval => produceInterval;

        public bool IsSellable => isSellable;
        public int SellPrice => sellPrice;

        public int Uses => hasDurability ? uses : 1;

        public int Nutrition => nutrition;

        public EquipmentSlot EquipmentSlot => equipmentSlot;
        public List<StatModifier> StatModifiers => statModifiers;
        public CardDefinition ClassChangeResult => classChangeResult;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id))
                id = System.Guid.NewGuid().ToString("N");
        }

        public CombatStats CreateCombatStats()
        {
            return new CombatStats(
                maxHealth, attack, defense, attackSpeed,
                accuracy, dodge, criticalChance, criticalMultiplier
            );
        }

        public CardDefinition GetRandomLoot()
        {
            if (loot == null || loot.Count == 0) return null;

            int totalWeight = 0;
            foreach (var entry in loot)
            {
                if (entry != null && entry.Weight > 0)
                    totalWeight += entry.Weight;
            }

            if (totalWeight <= 0) return null;

            int randomPoint = UnityEngine.Random.Range(0, totalWeight);

            foreach (var entry in loot)
            {
                if (entry == null || entry.Weight <= 0) continue;

                if (randomPoint < entry.Weight)
                    return entry.Card;

                randomPoint -= entry.Weight;
            }

            return null;
        }

        public void SetId(string id) => this.id = id;
        public void SetDisplayName(string displayName) => this.displayName = displayName;
        public void SetDescription(string description) => this.description = description;

        private string GetLocalizedDisplayName()
        {
            if (!ShouldUseAssetLocalization())
                return displayName;

            return GameLocalization.GetOptional(
                LocalizationKeyBuilder.ForAsset(this, GetLocalizationCategory(), "name"),
                displayName);
        }

        private string GetLocalizedDescription()
        {
            if (!ShouldUseAssetLocalization())
                return description;

            return GameLocalization.GetOptional(
                LocalizationKeyBuilder.ForAsset(this, GetLocalizationCategory(), "description"),
                description);
        }

        private string GetLocalizationCategory()
        {
            return this is PackDefinition ? "pack" : "card";
        }

        private bool ShouldUseAssetLocalization()
        {
            return !string.IsNullOrWhiteSpace(id) &&
                !id.StartsWith("Recipe:", StringComparison.OrdinalIgnoreCase);
        }
    }

    [System.Serializable]
    public class LootEntry
    {
        public CardDefinition Card;
        [Min(1)] public int Weight = 1;
    }

    public enum CardCategory
    {
        None,       // Non-card (e.g. Pack)
        Resource,   // Scrap Heap, Rubble Slab
        Character,  // Recruit, Enforcer, Scavenger, Netrunner
        Consumable, // Rations, Med Gel
        Material,   // Scrap, Circuit Parts, Power Cell
        Equipment,  // Weapon, Armor, Accessory
        Structure,  // Logistics Yard, Hab Pod
        Currency,   // Credits, trade goods
        Recipe,     // Recipe exclusive
        Mob,        // Protein Drone, Null Anomaly, Glitched Raider
        Area,       // Area exclusive
        Valuable    // Encrypted Cache, Root Keycard, Kernel artifacts
    }

    public enum CardFaction
    {
        Neutral,
        Player,
        Mob
    }

    public enum CombatType
    {
        None,
        Melee,
        Ranged,
        Magic
    }

    public enum EquipmentSlot
    {
        Weapon,
        Armor,
        Accessory
    }
}
