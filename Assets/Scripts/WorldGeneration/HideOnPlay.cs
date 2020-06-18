using System;
using UnityEngine;

namespace WorldGeneration
{
    public class HideOnPlay : MonoBehaviour
    {
        private void Start()
        {
            gameObject.SetActive(false);
        }
    }
}