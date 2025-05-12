using System;
using System.Collections.Generic;
using System.Text;

namespace Solver.Exporter
{
    public class LevelData
    {
        private readonly int _calc;

        private readonly float _difficulty;

        private readonly string _history;
        private readonly int _id;

        private readonly string _level;

        private readonly string _seed;

        private readonly string _serialized;

        private readonly int _step1;

        private readonly int _step2;

        private readonly int _step3;

        private readonly int _step4;

        private readonly int _step5;

        private readonly int _step6;

        private readonly int _step7;

        private readonly int _step8;

        private readonly int _suitCount;

        public LevelData
        (
            int id, string seed, int suitCount, int calc, List<int> collectionSteps, List<(int, int, int, bool)> history, string level,
            string serialized
        )
        {
            _id = id;
            if (int.TryParse(seed, out _))
                _seed = seed;
            else
                _seed = $"{seed}";

            _suitCount = suitCount;
            _calc = calc;
            _difficulty = -100000f / calc + 1000f;
            if (collectionSteps.Count != 8)
                throw new ArgumentException("CollectionSteps List Count Error !");
            _step1 = collectionSteps[0];
            _step2 = collectionSteps[1];
            _step3 = collectionSteps[2];
            _step4 = collectionSteps[3];
            _step5 = collectionSteps[4];
            _step6 = collectionSteps[5];
            _step7 = collectionSteps[6];
            _step8 = collectionSteps[7];
            StringBuilder sb = new();
            if (history != null)
                for (var i = history.Count - 1; i >= 0; i--)
                {
                    var item = history[i];
                    var from = item.Item1;
                    var count = item.Item2;
                    var to = item.Item3;
                    sb.Append("[F:")
                        .Append(from)
                        .Append(",T:")
                        .Append(to)
                        .Append(",N:")
                        .Append(count)
                        .Append("]>");
                }

            _history = sb.ToString();
            _level = level;
            _serialized = serialized;
        }

        public override string ToString()
        {
            return
                $"\"{_id}\",\"{_seed}\",\"{_calc}\",\"{_difficulty}\",\"{_step1}\",\"{_step2}\",\"{_step3}\",\"{_step4}\",\"{_step5}\",\"{_step6}\",\"{_step7}\",\"{_step8}\",\"{_suitCount}\",\"{_history}\",\"{_level}\",\"{_serialized}\"";
        }
    }
}