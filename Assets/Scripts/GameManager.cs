using System;
using Lazy;
using Lazy.Log;
using UnityEngine;

namespace DefaultNamespace
{
    public class GameManager : MonoBehaviour
    {
        private void Start()
        {
            AppLauncher.Instance.OnStartGame += () =>
            {
                Log.Enable();
                Log.MsgD("START APP GAME !");
            };
        }
    }
}
