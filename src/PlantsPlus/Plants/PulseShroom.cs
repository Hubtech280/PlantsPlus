using Il2Cpp;
using PlantsPlus.Core;
using System;
using UnityEngine;

namespace PlantsPlus.Plants
{
    public sealed class PulseShroom : MonoBehaviour
    {
        public const int ID = 6039;
        private const float WaveChance = 0.25f;
        private Caltrop? plant;

        public PulseShroom(IntPtr pointer) : base(pointer) { }

        public void Start()
        {
            plant = GetComponent<Caltrop>();
            if (GetComponent<BounceShroom>() == null)
                gameObject.AddComponent<BounceShroom>();
        }

        internal void OnBounce()
        {
            if (plant == null || plant.dying)
                return;

            // One 3x3 boost and one chance roll per bounce, regardless of
            // how many zombies were launched by that single attack.
            BorealSystem.TriggerPulse(plant);
            if (UnityEngine.Random.value >= WaveChance)
                return;
            BorealOrchidBootstrap.SpawnWaveVisual(plant);
            BorealSystem.TriggerWave(plant);
        }
    }
}
