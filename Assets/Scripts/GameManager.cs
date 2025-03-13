using System;
using System.IO;
using System.Net;
using System.Net.Http;
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
                    Log.VarI(this, "DOTweenSettings debugMode:", x.debugMode);
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

            // var url = "https://www.google.com/";
            // using (var client = new HttpClient())
            // {
            //     var response = client.GetAsync(url).Result;
            //     Log.VarI(null, nameof(response), response.StatusCode);
            // }

            var path = Application.persistentDataPath + "/a.png";
            App.Download.SafeDownload(
                "https://wangjunyong.cdn-doodlemobile.com/yahtzee/skin/skin_19.png",
                path,
                (v) =>
                {
                    Log.MsgD("SUCCESS!");
                }
            );
        }
    }
}
