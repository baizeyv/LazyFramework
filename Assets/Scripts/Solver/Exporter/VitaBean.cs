using System;
using System.Collections.Generic;

namespace Solver.Exporter
{
    [Serializable]
    public class VitaBean : Dictionary<string, List<LevelBean>> { }

    [Serializable]
    public class LevelBean
    {
        public string question;
        public int id;
    }
}
