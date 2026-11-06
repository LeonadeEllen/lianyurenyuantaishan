using System;

namespace HearTheTide.Core
{
    [Serializable]
    public struct CombatStats
    {
        public float Attack;
        public float MaxHp;
        public float Defense;

        public CombatStats(float attack, float maxHp, float defense)
        {
            Attack = attack;
            MaxHp = maxHp;
            Defense = defense;
        }

        public static CombatStats operator +(CombatStats a, CombatStats b)
        {
            return new CombatStats(a.Attack + b.Attack, a.MaxHp + b.MaxHp, a.Defense + b.Defense);
        }

        public CombatStats Scale(float attackMul, float hpMul, float defMul)
        {
            return new CombatStats(Attack * attackMul, MaxHp * hpMul, Defense * defMul);
        }
    }

    public static class DamageFormula
    {
        /// <summary>防御减伤常数：防御=300 时减伤 50%。</summary>
        public const float DefenseConstant = 300f;

        public static float Compute(float attack, float skillMultiplier, float targetDefense)
        {
            float reduction = targetDefense / (targetDefense + DefenseConstant);
            return Math.Max(1f, attack * skillMultiplier * (1f - reduction));
        }
    }
}
