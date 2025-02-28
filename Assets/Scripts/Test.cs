using System;
using Lazy.Rx;
using Lazy.Rx.Variables;
using UnityEngine;

namespace DefaultNamespace
{
    public class Test : MonoBehaviour
    {
        private void Start()
        {
            int a = 5;
            IntVariable bb = new(3);
            if (bb == a)
            {

            }
        }
    }
}