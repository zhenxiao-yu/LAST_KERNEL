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

        [BoxGroup("Combat")]
        [SerializeField, Min(1)] private int maxHP = 10;

        [BoxGroup("Combat")]
        [SerializeField, Min(0)] private int attack = 2;

        [BoxGroup("Combat")]
        [SerializeField, Min(0)] private int defense = 0;

        [BoxGroup("Combat")]
        [SerializeField, Min(1), Tooltip("Attacks per second as a percentage. 100 = 1 attack/sec, 200 = 2/sec.")]
        private int attackSpeed = 80;

        [BoxGroup("Combat")]
        [SerializeField, Range(0f, 100f)] private float accuracy = 90f;

        [BoxGroup("Combat")]
        [SerializeField, Range(0f, 100f)] private float dodge = 0f;

        [BoxGroup("Combat")]
        [SerializeField, Range(0f, 100f)] private float critChance = 5f;

        [BoxGroup("Combat")]
        [SerializeField, Range(100f, 300f)] private float critMultiplier = 150f;

        [BoxGroup("Defense Lane")]
        [SerializeField, Min(0.1f), Tooltip("World-units per second this enemy moves toward the base.")]
        private float moveSpeed = 2f;

        [BoxGroup("Defense Lane")]
        [SerializeField, Min(1), Tooltip("Damage dealt to the base core when this enemy reaches it.")]
        private int damageToBase = 1;

        [BoxGroup("Defense Lane")]
        [SerializeField, Min(0), Tooltip("Scrap/currency reward when this enemy is killed.")]
        private int rewardAmount = 1;

        public string DisplayName => GameLocalization.GetOptional(
            LocalizationKeyBuilder.ForAsset(this, "night.enemy", "name"),
            displayName);
        public int MaxHP => maxHP;
        public int Attack => attack;
        public int Defense => defense;
        public int AttackSpeed => attackSpeed;
        public float Accuracy => accuracy;
        public float Dodge => dodge;
        public float CritChance => critChance;
        public float CritMultiplier => critMultiplier;
        public Texture2D ArtTexture => artTexture;
        public Sprite Sprite => sprite;
        public float MoveSpeed => moveSpeed;
        public int DamageToBase => damageToBase;
        public int RewardAmount => rewardAmount;

        public static EnemyDefinition CreateRuntime(
            string name, int hp, int atk, int def,
            int speed = 80, float accuracy = 90f, float dodge = 0f,
            float critChance = 5f, float critMult = 150f)
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
            return e;
        }
    }
}
