using System;
using Il2Cpp;
using UnityEngine;

namespace PlantsPlus.Plants
{
    public sealed class SolsticeShroom : MonoBehaviour
    {
        public const int ID = 6041;
        private Caltrop? plant;
        public SolsticeShroom(IntPtr pointer) : base(pointer) { }
        public void Start()
        {
            plant = GetComponent<Caltrop>();
            if (GetComponent<BounceShroom>() == null) gameObject.AddComponent<BounceShroom>();
        }

        internal void OnBounce(int count)
        {
            if (count <= 0 || plant == null || !SuperaquaShroom.IsLiveBoardPlant(plant)) return;
            CreateItem creator = CreateItem.Instance;
            if (creator == null) return;
            // One normal 25-Sun pickup per successfully launched zombie, not
            // one per animation, attempt, frame, or passive production cycle.
            for (int i = 0; i < count; i++)
                creator.SetCoin(plant.thePlantColumn, plant.thePlantRow,
                    (int)ItemType.NormalSun, 0);
        }
    }
}
