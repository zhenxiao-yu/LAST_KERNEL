using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    [CreateAssetMenu(menuName = "LastKernel/Enemy Definition", fileName = "Enemy_")]
    public class EnemyDefinition : ScriptableObject
    {
        [BoxGroup("Identity")]
        [SerializeField] private string displayName = "Enemy";

        [BoxGroup("Identity")]
        [SerializeField] private Texture2D artTexture;

        [BoxGroup("Identity")]
        [SerializeField] private Sprite sprite;

        // ── Faction ───────────────────────────────────────────────────────────────

        [BoxGroup("Faction")]
        [Tooltip("Faction this enemy belongs to. Drives HUD badge colour and player tactic hints.")]
        [SerializeField] private EnemyFactionDefinition faction;

        // ── Abilities ─────────────────────────────────────────────────────────────

        [BoxGroup("Abilities")]
        [Tooltip("Active combat abilities resolved by AbilityResolver.")]
        [SerializeField] private UnitAbilityDefinition[] abilities = System.Array.Empty<UnitAbilityDefinition>();

        // ── Combat stats ──────────────────────────────────────────────────────────

        [BoxGroup("Combat")]
        [SerializeField, Min(1)] private int maxHP = 10;

        [BoxGroup("Combat")]
        [SerializeField, Min(0)] private int attack = 2;

        [BoxGroup("Combat")]
        [SerializeField, Min(0)] private int defense = 0;

        [BoxGroup("Combat")]
        [SerializeField, Min(1), Tooltip("100 = 1 attack/sec, 200 = 2/sec.")]
        private int attackSpeed = 80;

        [BoxGroup("Combat")]
        [SerializeField, Range(0f, 100f)] private float accuracy = 90f;

        [BoxGroup("Combat")]
        [SerializeField, Range(0f, 100f)] private float dodge = 0f;

        [BoxGroup("Combat")]
        [SerializeField, Range(0f, 100f)] private float critChance = 5f;

        [BoxGroup("Combat")]
        [SerializeField, Range(100f, 300f)] private float critMultiplier = 150f;

        // ── Day scaling ───────────────────────────────────────────────────────────

        [BoxGroup("Scaling")]
        [Tooltip("Flat HP added per day (day 1 = base stats, day 2 = +hpFlatPerDay, etc.).")]
        [SerializeField, Min(0f)] private float hpFlatPerDay = 5f;

        [BoxGroup("Scaling")]
        [Tooltip("Flat ATK added per day.")]
        [SerializeField, Min(0f)] private float atkFlatPerDay = 1f;

        [BoxGroup("Scaling")]
        [Tooltip("Exponent applied to (day-1) for non-linear HP curve. 1 = linear, >1 = accelerates.")]
        [SerializeField, Range(0.5f, 3f)] private float scalingExponent = 1f;

        [BoxGroup("Scaling")]
        [Tooltip("Multiplier for the curve component: curve = (day-1)^exponent * curvePower. 0 disables the curve.")]
        [SerializeField, Min(0f)] private float scalingCurvePower = 0f;

        // ── Defense Lane ──────────────────────────────────────────────────────────

        [BoxGroup("Defense Lane")]
        [SerializeField, Min(0.1f)]
        private float moveSpeed = 2f;

        [BoxGroup("Defense Lane")]
        [SerializeField, Min(1)]
        private int damageToBase = 1;

        [BoxGroup("Defense Lane")]
        [SerializeField, Min(0)]
        private int rewardAmount = 1;

        // ── Public properties ─────────────────────────────────────────────────────

        public string DisplayName => GameLocalization.GetOptional(
            LocalizationKeyBuilder.ForAsset(this, "night.enemy", "name"),
            displayName);

        public EnemyFactionDefinition      Faction    => faction;
        public UnitAbilityDefinition[]     Abilities  => abilities ?? System.Array.Empty<UnitAbilityDefinition>();

        public int   MaxHP         => maxHP;
        public int   Attack        => attack;
        public int   Defense       => defense;
        public int   AttackSpeed   => attackSpeed;
        public float Accuracy      => accuracy;
        public float Dodge         => dodge;
        public float CritChance    => critChance;
        public float CritMultiplier => critMultiplier;
        public Texture2D ArtTexture => artTexture;
        public Sprite    Sprite     => sprite;
        public float MoveSpeed     => moveSpeed;
        public int   DamageToBase  => damageToBase;
        public int   RewardAmount  => rewardAmount;

        // ── Scaling ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a runtime EnemyDefinition with HP and ATK scaled for the given day.
        /// Day 1 returns this instance unchanged. Days beyond 1 apply linear + curve scaling.
        ///
        /// Formula:
        ///   HP  = baseHP  + (day-1) * hpFlatPerDay  + pow(day-1, scalingExponent) * scalingCurvePower
        ///   ATK = baseATK + (day-1) * atkFlatPerDay
        /// </summary>
        public EnemyDefinition ScaledForDay(int day)
        {
            if (day <= 1) return this;

            float d = day - 1;
            int scaledHP  = Mathf.Max(1,  Mathf.RoundToInt(maxHP  + d * hpFlatPerDay  + Mathf.Pow(d, scalingExponent) * scalingCurvePower));
            int scaledAtk = Mathf.Max(0, Mathf.RoundToInt(attack + d * atkFlatPerDay));

            return CreateRuntime(
                displayName, scaledHP, scaledAtk, defense,
                attackSpeed, accuracy, dodge, critChance, critMultiplier,
                faction, abilities);
        }

        // ── Factory ───────────────────────────────────────────────────────────────

        public static EnemyDefinition CreateRuntime(
            string name, int hp, int atk, int def,
            int speed = 80, float accuracy = 90f, float dodge = 0f,
            float critChance = 5f, float critMult = 150f,
            EnemyFactionDefinition faction = null,
            UnitAbilityDefinition[] abilities = null,
            float hpFlatPerDay = 0f,
            float atkFlatPerDay = 0f,
            float scalingExponent = 1f,
            float scalingCurvePower = 0f)
        {
            var e = ScriptableObject.CreateInstance<EnemyDefinition>();
            e.displayName    = name;
            e.maxHP          = Mathf.Max(1, hp);
            e.attack         = Mathf.Max(0, atk);
            e.defense        = Mathf.Max(0, def);
            e.attackSpeed    = Mathf.Max(1, speed);
            e.accuracy       = accuracy;
            e.dodge          = dodge;
            e.critChance     = critChance;
            e.critMultiplier = critMult;
            e.faction        = faction;
            e.abilities      = abilities ?? System.Array.Empty<UnitAbilityDefinition>();
            e.hpFlatPerDay   = Mathf.Max(0f, hpFlatPerDay);
            e.atkFlatPerDay  = Mathf.Max(0f, atkFlatPerDay);
            e.scalingExponent = Mathf.Clamp(scalingExponent, 0.5f, 3f);
            e.scalingCurvePower = Mathf.Max(0f, scalingCurvePower);
            return e;
        }
    }
}
