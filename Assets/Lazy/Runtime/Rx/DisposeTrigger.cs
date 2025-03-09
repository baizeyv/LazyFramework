using System;
using System.Collections.Generic;
using Lazy.Utility;
using UnityEngine;

namespace Lazy.Rx
{
    public abstract class DisposeTrigger : MonoBehaviour, IDisposable
    {
        private readonly HashSet<IDisposable> _disposables = new();

        public IDisposable Add(IDisposable disposable)
        {
            _disposables.Add(disposable);
            return disposable;
        }

        public void Remove(IDisposable disposable)
        {
            _disposables.Remove(disposable);
        }

        public void Dispose()
        {
            foreach (var item in _disposables)
            {
                item.Dispose();
            }

            _disposables.Clear();
        }
    }

    public class DisposeOnDestroyTrigger : DisposeTrigger
    {
        private void OnDestroy() => Dispose();
    }

    public class DisposeOnDisableTrigger : DisposeTrigger
    {
        private void OnDisable() => Dispose();
    }

    public static class DisposeExtensions
    {
        public static IDisposable DisposeOnDestroy(this IDisposable disposable, GameObject gameObject)
        {
            return gameObject.GetOrAddComponent<DisposeOnDestroyTrigger>().Add(disposable);
        }

        public static IDisposable DisposeOnDestroy<T>(this IDisposable disposable, T component) where T : Component
        {
            return component.gameObject.GetOrAddComponent<DisposeOnDestroyTrigger>().Add(disposable);
        }

        public static IDisposable DisposeOnDisable(this IDisposable disposable, GameObject gameObject)
        {
            return gameObject.GetOrAddComponent<DisposeOnDisableTrigger>().Add(disposable);
        }

        public static IDisposable DisposeOnDisable<T>(this IDisposable disposable, T component) where T : Component
        {
            return component.gameObject.GetOrAddComponent<DisposeOnDisableTrigger>().Add(disposable);
        }
    }

}