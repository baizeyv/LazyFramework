using System;
using Lazy.Rx;
using Lazy.Rx.Operator;
using Lazy.Rx.Variables;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace
{
    public class Test : MonoBehaviour
    {
        private Button btn;

        private void Start()
        {
            var a = 5;
            IntVariable bb = new(3);
            if (bb == a) { }

            bb.Where(x => x > 5).Where(x => x < 10);

            ReactiveProperty<int> ccc = new();
        }
    }
}
