using System;
using UnityEngine;

namespace Project.World
{
    [Serializable]
    public struct DMSplineControlPoint
    {
        public Vector3 localPosition;
        public Vector3 surfaceNormal;
        public GameObject prefab;
        [Tooltip("Extra Y rotation (degrees) after surface alignment.")]
        public float yawDegrees;
    }
}
