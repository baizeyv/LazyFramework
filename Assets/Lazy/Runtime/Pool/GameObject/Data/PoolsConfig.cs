using System.Collections.Generic;
using UnityEngine;

namespace Lazy
{
    public sealed class PoolsConfig : ScriptableObject
    {
        [SerializeField]
        private List<PoolConfig> configs = new();

        public IReadOnlyList<PoolConfig> Configs => configs;
    }
}
