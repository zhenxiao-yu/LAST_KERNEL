namespace Markyu.LastKernel
{
    public enum UnitAbilityKeyword
    {
        None = 0,

        // ── Enemy abilities ─────────────────────────────────────────────────
        // GangUp(N): each alive GangUp ally grants +N ATK to all GangUp enemies
        GangUp      = 10,
        // Poison(N): on-hit, apply N poison stacks to the target (1 dmg/stack/tick)
        Poison      = 11,
        // Armor(N): flat damage reduction applied before shield and HP
        Armor       = 12,
        // Repair(N): each tick, heals N HP if below 50% HP
        Repair      = 13,
        // Resilient: first death is survived at 1 HP (once per battle)
        Resilient   = 14,
        // Infect(N): on death, applies N poison stacks to the front defender
        Infect      = 15,
        // Ethereal: the first hit that would deal damage is evaded (once per battle)
        Ethereal    = 16,
        // Rally(N): when any ally dies, all remaining alive allies gain +N ATK permanently
        Rally       = 17,

        // ── Defender abilities ──────────────────────────────────────────────
        // Veteran(N): gains +N ATK permanently each time it kills an enemy
        Veteran     = 30,
        // Healer(N): each tick, heals the most-wounded ally for N HP
        Healer      = 31,
        // Shield(N): starts battle with N shield HP that absorbs damage before HP
        Shield      = 32,
        // Executioner(N): deals +N% bonus damage to targets below 25% HP
        Executioner = 33,
        // Taunt: enemies always target this unit first (while alive)
        Taunt       = 34,
        // Berserker(N): while below 50% HP, attack speed is increased by N%
        Berserker   = 35,
    }
}
