using System;
using UnityEngine;

namespace Markyu.FortStack
{
    public enum GamePhase
    {
        Dawn,
        Day,
        Dusk,
        Night
    }

    [Serializable]
    public class RunStateData
    {
        public GamePhase CurrentPhase = GamePhase.Day;

        public int Morale = 65;
        public int Fatigue;
        public int InjuredPersonnel;
        public int StructuralDamage;
        public int PowerDeficit;
        public int Corruption;

        public int LastResolvedDay = 1;
        public int NightsSurvived;
        public int Casualties;
        public int SalvageValue;

        public void Clamp()
        {
            Morale = Mathf.Clamp(Morale, 0, 100);
            Fatigue = Mathf.Clamp(Fatigue, 0, 100);
            InjuredPersonnel = Mathf.Max(0, InjuredPersonnel);
            StructuralDamage = Mathf.Clamp(StructuralDamage, 0, 100);
            PowerDeficit = Mathf.Clamp(PowerDeficit, 0, 100);
            Corruption = Mathf.Clamp(Corruption, 0, 100);
            LastResolvedDay = Mathf.Max(1, LastResolvedDay);
            NightsSurvived = Mathf.Max(0, NightsSurvived);
            Casualties = Mathf.Max(0, Casualties);
            SalvageValue = Mathf.Max(0, SalvageValue);
        }

        public void ApplyDuskPressure(StatsSnapshot stats)
        {
            int rationShortfall = Mathf.Max(0, stats.NutritionNeed - stats.TotalNutrition);
            int overload = Mathf.Max(0, stats.ExcessCards);

            Fatigue += Mathf.Max(1, Mathf.CeilToInt(stats.TotalCharacters * 0.5f));

            if (rationShortfall > 0)
            {
                Morale -= Mathf.Min(12, rationShortfall * 3);
                Fatigue += Mathf.Min(16, rationShortfall * 4);
            }

            if (overload > 0)
            {
                Morale -= Mathf.Min(6, overload);
            }

            Clamp();
        }

        public void RecordNightContact(bool hostileContact)
        {
            if (hostileContact)
            {
                Fatigue += 6;
                Morale -= 3;
                StructuralDamage += 1;
                SalvageValue += 1;
            }
            else
            {
                Morale += 1;
            }

            NightsSurvived++;
            Clamp();
        }

        public void ApplyDawnRecovery(StatsSnapshot stats)
        {
            Fatigue -= Mathf.Max(1, stats.TotalCharacters / 3);

            if (stats.TotalNutrition >= stats.NutritionNeed)
            {
                Morale += 2;
            }

            if (PowerDeficit > 0)
            {
                Morale -= 1;
            }

            Clamp();
        }

        public string GetStatusLine()
        {
            return $"Morale {Morale} / Fatigue {Fatigue} / Injured {InjuredPersonnel} / Damage {StructuralDamage} / Power Deficit {PowerDeficit}";
        }
    }
}
