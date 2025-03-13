using Core.Equipment;
using Identifiables;
using Unity.Entities;
using UnityEngine.UIElements;

namespace Units
{
    public class UnitButtonWithMood : SignalButton
    {
        PercentProgressBar MoodBar;
        bool hasMood = false;

        public UnitButtonWithMood() : base()
        {
            Add(MoodBar = new PercentProgressBar());
        }
        
        public void Update(Entity unit, EntityManager em)
        {
            if (unit != Entity)
            {
                Entity = unit;
                Target = em.GetComponentData<Identifiable>(unit);
                hasMood = em.HasComponent<Mood>(unit);
            }

            if (hasMood)
            {
                var mood = em.GetComponentData<Mood>(unit);
                MoodBar.value = mood.Fullness * 1f / mood.Capacity;
            }
        }
    }
}