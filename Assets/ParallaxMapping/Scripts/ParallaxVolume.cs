using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ParallaxMapping
{
    [ExecuteInEditMode]
    public class ParallaxVolume : MonoBehaviour
    {
        [HideInInspector] public Bounds BoundingBox = new Bounds(Vector3.zero, Vector3.one);

        [Header("Material")]
        [Range(0.0f, 1.0f)] public float Roughness = 0.5f;
        [Range(0.0f, 1.0f)] public float Metalness = 0.0f;
                            public Color Albedo = Color.grey;

        [Header("Parallax")]
        [Range(1, 5)] public int FrequencyU = 1;
        [Range(1, 5)] public int FrequencyV = 1;
        [Range(1.0f, 10.0f)] public float NormalFactor = 5.0f;
    }
}
