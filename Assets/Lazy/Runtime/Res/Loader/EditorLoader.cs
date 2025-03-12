using UnityEngine;

namespace Lazy.Res.Loader
{
    public class EditorLoader : ABSLoader
    {
        public override bool LoadSuccess { get; }

        public readonly Object Asset;

        public EditorLoader(Object asset)
        {
            LoadSuccess = true;
            Asset = asset;
        }

        public override T GetAssetObject<T>(string subAssetName = null)
        {
            return Asset as T;
        }

        public override Object GetAssetObject(string subAssetName = null)
        {
            return Asset;
        }
    }
}
