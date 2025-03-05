using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Lazy.Utility;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lazy.Res.Loader
{
    /// <summary>
    /// * 抽象加载器
    /// </summary>
    public abstract class ABSLoader : IEnumerator
    {
        /// <summary>
        /// * 加载器是否加载成功
        /// </summary>
        public virtual bool LoaderSuccess => false;

        /// <summary>
        /// * 加载完成回调方法
        /// </summary>
        private Action _onCompleted;

        protected virtual void OnCompleted()
        {
            _onCompleted.Fire();
            _onCompleted = null;
        }

        public void SetCallback(Action onCompleted)
        {
            _onCompleted = onCompleted;
        }

        public virtual T GetAssetObject<T>(string subAssetName = null)
            where T : Object
        {
            return null;
        }

        public virtual Object GetAssetObject(string subAssetName = null)
        {
            return null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual LoaderAwaiter GetAwaiter()
        {
            return new LoaderAwaiter(this);
        }

        public bool MoveNext()
        {
            return !LoaderSuccess;
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }

        public object Current
        {
            get
            {
                if (LoaderSuccess)
                    Log.Log.MsgE(
                        "Load Completed, Please use 'GetAssetObject' method to get asset."
                    );

                return null;
            }
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("这个结构是异步/等待支持所必需的，你不应该直接使用它。")]
    public readonly struct LoaderAwaiter : INotifyCompletion
    {
        private readonly ABSLoader _loader;

        public bool IsCompleted => _loader.LoaderSuccess;

        internal LoaderAwaiter(ABSLoader loader)
        {
            _loader = loader;
        }

        public void OnCompleted(Action continuation)
        {
            _loader.SetCallback(() =>
            {
                try
                {
                    continuation.Fire();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            });
        }

        public ABSLoader GetResult()
        {
            return _loader;
        }
    }
}
