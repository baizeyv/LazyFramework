using System;
using Lazy.Rx;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lazy.Utility
{
    public static class UnityReactiveExtensions
    {
        /// <summary>
        /// * Unity Text 绑定 RxVariable
        /// </summary>
        /// <param name="source"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static IDisposable SubscribeToText(this Observable<string> source, Text text)
        {
            return source.Subscribe<string, Text>(text, (x, t) => t.text = x);
        }

        /// <summary>
        /// * Unity Text 绑定 RxVariable 带有选择器
        /// </summary>
        /// <param name="source"></param>
        /// <param name="text"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static IDisposable SubscribeToText<T>(this Observable<T> source, Text text, Func<T, string> selector)
        {
            return source.Subscribe<T, Text>(text, (x, t) => t.text = selector.Fire(x));
        }

        /// <summary>
        /// * Unity TMP_Text 绑定 RxVariable
        /// </summary>
        /// <param name="source"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static IDisposable SubscribeToTMPText(this Observable<string> source, TMP_Text text)
        {
            return source.Subscribe<string, TMP_Text>(text, (x, t) => t.text = x);
        }

        /// <summary>
        /// * Unity TMP_Text 绑定 RxVariable 带有选择器
        /// </summary>
        /// <param name="source"></param>
        /// <param name="text"></param>
        /// <param name="selector"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IDisposable SubscribeToTMPText<T>(this Observable<T> source, TMP_Text text, Func<T, string> selector)
        {
            return source.Subscribe<T, TMP_Text>(text, (x, t) => t.text = selector.Fire(x));
        }

        /// <summary>
        /// * Unity Text 绑定 RxVariable
        /// </summary>
        /// <param name="source"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static IDisposable SubscribeToText(this Observable<int> source, Text text)
        {
            return source.Subscribe<int, Text>(text, (x, t) => t.text = x.ToString());
        }

        /// <summary>
        /// * Unity TMP_Text 绑定 RxVariable
        /// </summary>
        /// <param name="source"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static IDisposable SubscribeToTMPText(this Observable<int> source, TMP_Text text)
        {
            return source.Subscribe<int, TMP_Text>(text, (x, t) => t.text = x.ToString());
        }

        /// <summary>
        /// * Unity GameObject 可见性绑定 RxVariable
        /// </summary>
        /// <param name="source"></param>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public static IDisposable SubscribeToVisible(this Observable<bool> source, GameObject gameObject)
        {
            return source.Subscribe<bool, GameObject>(gameObject, (x, o) => o.SetVisible(x));
        }

        /// <summary>
        /// * Unity MonoBehaviour的GameObject 可见性绑定 RxVariable
        /// </summary>
        /// <param name="source"></param>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public static IDisposable SubscribeToVisible(this Observable<bool> source, MonoBehaviour gameObject)
        {
            return source.Subscribe<bool, MonoBehaviour>(gameObject, (x, o) => o.SetVisible(x));
        }

    }
}