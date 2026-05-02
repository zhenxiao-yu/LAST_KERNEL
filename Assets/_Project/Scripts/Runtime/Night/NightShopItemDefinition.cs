using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    public enum NightShopEffect
    {
        /// <summary>Add to the target fighter's attack.</summary>
        AddAttack,
        /// <summary>Add to the target fighter's max health (and current health).</summary>
        AddMaxHealth,
        /// <summary>Set the target fighter's starting HP to their (potentially boosted) max.</summary>
        FullHeal,
        /// <summary>Add a temporary hired fighter to the next empty slot. No target needed.</summary>
        HireGuard,
    }

    /// <summary>
    /// Defines one purchasable item in the Night Shop.
    /// Create via: Right-click in Project → LastKernel → Night Shop Item.
    ///
    /// Assign a pool of these to NightBattleManager.shopPool in the Inspector.
    /// NightBattleManager randomly selects from the pool each night.
    /// </summary>
    [CreateAssetMenu(menuName = "LastKernel/Night Shop Item", fileName = "ShopItem_New")]
    public class NightShopItemDefinition : ScriptableObject
    {
        [BoxGroup("Identity")]
        [SerializeField] private string displayName = "Shop Item";

        [BoxGroup("Identity")]
        [SerializeField, TextArea(2, 3)] private string description = "Effect description.";

        [BoxGroup("Economy")]
        [SerializeField, Min(0)] public int goldCost = 10;

        [BoxGroup("Effect")]
        public NightShopEffect effect;

        [BoxGroup("Effect")]
        [Tooltip("Amount added for AddAttack and AddMaxHealth effects.")]
        [ShowIf("@effect == NightShopEffect.AddAttack || effect == NightShopEffect.AddMaxHealth")]
        [SerializeField, Min(1)] public int effectValue = 2;

        [BoxGroup("Effect")]
        [Tooltip("If true the player must click a fighter after clicking this item.")]
        public bool requiresTarget = true;

        // ── Hired Guard stats (only relevant when effect == HireGuard) ────────────
        [BoxGroup("Hired Guard"), ShowIf("@effect == NightShopEffect.HireGuard")]
        [SerializeField, Min(1)] public int hireAttack = 3;

        [BoxGroup("Hired Guard"), ShowIf("@effect == NightShopEffect.HireGuard")]
        [SerializeField, Min(1)] public int hireHealth = 6;

        [BoxGroup("Hired Guard"), ShowIf("@effect == NightShopEffect.HireGuard")]
        [SerializeField] public string hireDisplayName = "Hired Guard";

        // ── Properties ────────────────────────────────────────────────────────────

        public string DisplayName  => displayName;
        public string Description  => description;

        public void ConfigureRuntime(
            string displayName,
            string description,
            int goldCost,
            NightShopEffect effect,
            int effectValue,
            bool requiresTarget,
            int hireAttack = 0,
            int hireHealth = 0,
            string hireDisplayName = null)
        {
            this.displayName = displayName;
            this.description = description;
            this.goldCost = Mathf.Max(0, goldCost);
            this.effect = effect;
            this.effectValue = Mathf.Max(1, effectValue);
            this.requiresTarget = requiresTarget;
            this.hireAttack = Mathf.Max(1, hireAttack);
            this.hireHealth = Mathf.Max(1, hireHealth);
            this.hireDisplayName = string.IsNullOrWhiteSpace(hireDisplayName)
                ? displayName
                : hireDisplayName;
            name = displayName;
        }

        /// <summary>Apply this shop item's effect to a fighter and/or team.</summary>
        public void Apply(NightFighter target, NightTeam team)
        {
            switch (effect)
            {
                case NightShopEffect.AddAttack:
                    target?.AddAttackBonus(effectValue);
                    break;

                case NightShopEffect.AddMaxHealth:
                    target?.AddMaxHealthBonus(effectValue);
                    break;

                case NightShopEffect.FullHeal:
                    target?.RequestFullHeal();
                    break;

                case NightShopEffect.HireGuard:
                    if (team == null) break;
                    int slot = team.FirstEmptySlot();
                    if (slot < 0)
                    {
                        Debug.LogWarning("NightShopItemDefinition: No empty slot for Hired Guard.");
                        break;
                    }
                    team.Assign(slot, NightFighter.Temporary(hireDisplayName, hireAttack, hireHealth));
                    break;
            }
        }
    }
}
