using System.Collections;
using Lazy.Melody;
using UnityEngine;
using UnityEngine.UI;

namespace Lazy.Example
{
    public class GameManager : ABSGameManager
    {
        protected override void OnStart()
        {
            Application.targetFrameRate = 30;
            Log.MsgD("START APP GAME !");
            /*
            StorageManager.Instance.Set("test", 5);
            App.Timer.SubscribeClock(
                9,
                _ =>
                {
                    Log.MsgI("A Time: 9");
                }
            );
            */

            // App.Asset.LoadAsync<DOTweenSettings>(
            //     "DOTweenSettings",
            //     x =>
            //     {
            //         Log.VarI(this, "DOTweenSettings debugMode:", x.debugMode);
            //     }
            // );
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

            // var path = Application.persistentDataPath + "/a.png";
            // App.Download.SafeDownload(
            //     "https://wangjunyong.cdn-doodlemobile.com/yahtzee/skin/skin_19.png",
            //     path,
            //     (v) =>
            //     {
            //         Log.MsgD("SUCCESS!");
            //     }
            // );
            // App.UI.OpenSync<MyPanel>(prefabName: "MyPanelUUU");
            // StartCoroutine(TestPanelClose());
            // App.UI.OpenSync<DialogA>();
            // App.UI.OpenSync<DialogB>();
            // App.UI.OpenSync<DialogC>();

            /*
            App.HotUpdate.Launch(() =>
            {
                var prefab = App.Asset.LoadSync<GameObject>("Image");
                Instantiate(prefab, UIRoot.Instance.transform);
            });
            */
            // StartCoroutine(TestPanelClose());
            App.UI.OpenSync<UIOperationDialog>();
        }

        private IEnumerator TestPanelClose()
        {
            yield return new WaitForSeconds(5f);
            // App.UI.Close();
            App.RedDot.SetValue("root/test", 1);
            yield return new WaitForSeconds(2f);
            App.RedDot.SetValue("root/test", 0);
        }

        /*
        private void Update()
        {
            // var ts = App.Timer.GetServerTime(out var utc, out var local);
            // App.Timer.GetLocalTime(out var utcLocal, out var localLocal);
            // if (ts)
            // {
            //     Log.VarD(this, nameof(utc), utc);
            //     Log.VarD(this, nameof(local), local);
            // }
            //
            // Log.VarD(this, nameof(utcLocal), utcLocal);
            // Log.VarD(this, nameof(localLocal), localLocal);
        }
        */
    }
}