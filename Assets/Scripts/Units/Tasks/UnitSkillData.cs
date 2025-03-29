using Identifiables;
using Unity.Collections;
using Unity.Mathematics;

namespace Units
{
    public struct UnitSkillData
    {   
        public FixedList32Bytes<Identifiable> LovedSkills;
        public FixedList32Bytes<Identifiable> HatedSkills;
        public FixedList32Bytes<Skill> LearnedSkills;
    }

    public struct Skill
    {
        public Identifiable Id;
        public int Level;
        public int CurrentExp;
        public int TotalExpForNextLevel;
    }

    public static class SkillDataGenerator
    {
        public static UnitSkillData Generate(uint seed)
        {
            var random  =new Random(seed);
            var allTraits = Identifiable.FromName("TagTrait").TagDef().WithThisTag.ToArray();
            var nextChanceForPlusTrait = 1f;
            var nextChanceForMinusTrait = 1f;
            NativeList<Identifiable> allPlusTraits  = new();
            NativeList<Identifiable> allMinusTraits = new();
            while (true)
            {
                var x = random.NextFloat();
                if (x < nextChanceForPlusTrait && allPlusTraits.Length > 0)
                {
                    nextChanceForPlusTrait *= 0.3f;
                    var trait = allPlusTraits[random.NextInt(allPlusTraits.Length)];
                }
                else if (x < nextChanceForMinusTrait && allMinusTraits.Length > 0)
                {
                    nextChanceForMinusTrait *= 0.3f;
                    var trait = allMinusTraits[random.NextInt(allMinusTraits.Length)];
                }
                else
                    break;
            }
            var data = new UnitSkillData();
            return data;
        }
    }
}