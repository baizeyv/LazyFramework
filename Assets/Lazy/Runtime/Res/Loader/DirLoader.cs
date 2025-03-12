using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Lazy.Utility;

namespace Lazy.Res.Loader
{
    public class DirLoader : IEnumerator
    {
        public readonly List<ABSLoader> Loaders = new();

        private Action _onCompleted;

        public virtual bool LoadSuccess
        {
            get { return Loaders.All(loader => loader.LoadSuccess); }
        }

        public virtual void OnCompleted()
        {
            _onCompleted.Fire();
            _onCompleted = null;
        }

        public void SetOnCompleted(Action callback)
        {
            _onCompleted = callback;
        }

        public bool MoveNext()
        {
            return !LoadSuccess;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        public object Current
        {
            get
            {
                if (LoadSuccess)
                    Log.Log.MsgE("加载已完成，请使用GetAssetObject方法获取资产！");

                return null;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual LoaderDirAwaiter GetAwaiter()
        {
            return new LoaderDirAwaiter(this);
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("这个结构是异步/等待支持所必需的，你不应该直接使用它。")]
    public readonly struct LoaderDirAwaiter : INotifyCompletion
    {
        private readonly DirLoader _loader;

        internal LoaderDirAwaiter(DirLoader loader)
        {
            _loader = loader;
        }

        public bool IsCompleted
        {
            get { return _loader.Loaders.All(loader => loader.LoadSuccess); }
        }

        public void OnCompleted(Action continuation)
        {
            _loader.SetOnCompleted(() =>
            {
                try
                {
                    continuation.Fire();
                }
                catch (Exception e)
                {
                    Log.Log.MsgE(e.Message);
                }
            });
        }

        public DirLoader GetResult()
        {
            return _loader;
        }
    }
}
