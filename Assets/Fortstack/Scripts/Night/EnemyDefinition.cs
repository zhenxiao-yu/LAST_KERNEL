using UnityEngine;

namespace Markyu.FortStack
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

        public string DisplayName => displayName;
        public int MaxHP => maxHP;
        public int Attack => attack;
        public int Defense => defense;
        public int AttackSpeed => attackSpeed;
        public float Accuracy => accuracy;
        public float Dodge => dodge;
        public float CritChance => critChance;
        public float CritMultiplier => critMultiplier;
        public Texture2D ArtTexture => artTexture;
    }
}
