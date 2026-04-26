using UnityEngine;

namespace Markyu.LastKernel
{
    [CreateAssetMenu(menuName = "LastKernel/Enemy Definition", fileName = "Enemy_")]
    public class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Enemy";
        [SerializeField, Min(1)] private int maxHP = 10;
        [SerializeField, Min(0)] private int attack = 2;
        [SerializeField, Min(0)] private int defense = 0;
        [SerializeField, Min(1), Tooltip("Attacks per second as a percentage. 100 = 1 attack/sec, 200 = 2/sec.")]
        private int attackSpeed = 80;
        [SerializeField, Range(0f, 100f)] private float accuracy = 90f;
        [SerializeField, Range(0f, 100f)] private float dodge = 0f;
        [SerializeField, Range(0f, 100f)] private float critChance = 5f;
        [SerializeField, Range(100f, 300f)] private float critMultiplier = 150f;
        [SerializeField] private Texture2D artTexture;

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

        public static EnemyDefinition CreateRuntime(
            string name, int hp, int atk, int def,
            int speed = 80, float accuracy = 90f, float dodge = 0f,
            float critChance = 5f, float critMult = 150f)
        {
            var e = ScriptableObject.CreateInstance<EnemyDefinition>();
            e.displayName  = name;
            e.maxHP        = Mathf.Max(1, hp);
            e.attack       = Mathf.Max(0, atk);
            e.defense      = Mathf.Max(0, def);
            e.attackSpeed  = Mathf.Max(1, speed);
            e.accuracy     = accuracy;
            e.dodge        = dodge;
            e.critChance   = critChance;
            e.critMultiplier = critMult;
            return e;
        }
    }
}
