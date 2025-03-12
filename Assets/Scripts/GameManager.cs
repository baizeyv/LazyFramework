using System;
using DG.Tweening.Core;
using Lazy;
using Lazy.Log;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace
{
    public class GameManager : MonoBehaviour
    {
        public Image img;

        private void Start()
        {
            AppLauncher.Instance.OnStartGame += OnStart;
        }

        private void OnStart()
        {
            Log.MsgD("START APP GAME !");
            App.Asset.LoadAsync<DOTweenSettings>(
                "DOTweenSettings",
                x =>
                {
                    Log.MsgD(x.debugMode);
                }
            );
            // App.Asset.LoadAsync<Texture2D>(
            //     "jigsaw_0",
            //     texture =>
            //     {
            //         var sprite = Sprite.Create(
            //             texture,
            //             new Rect(0, 0, texture.width, texture.height),
            //             new Vector2(0.5f, 0.5f)
            //         );
            //         img.sprite = sprite;
            //     }
            // );

            // var texture = App.Asset.LoadSync<Texture2D>("jigsaw_0");
            // var sprite = Sprite.Create(
            //     texture,
            //     new Rect(0, 0, texture.width, texture.height),
            //     new Vector2(0.5f, 0.5f)
            // );
            // img.sprite = sprite;
        }
    }
}
