using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldGeneration.Data
{
    [CreateAssetMenu]
    public class HeightMapSettings : UpdatableData
    {
        public NoiseSettings noiseSettings;
        public bool useFallof;
        public float heightMultiplier;
        public AnimationCurve heightCurve;

        public float MinHeight => heightMultiplier * heightCurve.Evaluate(0);
        public float MaxHeight => heightMultiplier * heightCurve.Evaluate(1);

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            noiseSettings.ValidateValues();
            base.OnValidate();
        }
#endif
    }
}