using System.Collections.Generic;
using System.IO;
using System.Text;
using Lazy;

namespace Solver.Exporter
{
    public class SpiderExporter
    {
        private readonly string _file;

        private static readonly object FileLock = new();

        public SpiderExporter(string file)
        {
            _file = file;
        }

        public void Export(int id, Poker poker, int suitCount, int calc)
        {
            lock (FileLock)
            {
                var data = new LevelData(
                    id,
                    poker.Mark,
                    suitCount,
                    calc,
                    poker.CollectionStep,
                    poker.History
                );
                var content = data.ToString();
                // var filePath = @"C:\Users\baizeyv\Documents\a\TestSpiderSolver.csv";
                var filePath = _file;
                FileUtility.CheckFileAndCreateDirWhenNeeded(filePath);
                var f = !File.Exists(filePath);
                using var writer = new StreamWriter(filePath, true, Encoding.UTF8);
                if (f)
                    writer.WriteLine(
                        "id,seed,calc,difficulty,step1,step2,step3,step4,step5,step6,step7,step8,suitCount,history"
                    );
                writer.WriteLine(content);
            }
        }

        public void ExportNull(int id, Poker poker, int suitCount, int calc)
        {
            lock (FileLock)
            {
                var data = new LevelData(
                    id,
                    poker.Mark,
                    suitCount,
                    calc,
                    new List<int> { -1, -1, -1, -1, -1, -1, -1, -1 },
                    new List<(int, int, int, bool)> { (-1, -1, -1, false) }
                );
                var content = data.ToString();
                var filePath = _file;
                FileUtility.CheckFileAndCreateDirWhenNeeded(filePath);
                var f = !File.Exists(filePath);
                using var writer = new StreamWriter(filePath, true, Encoding.UTF8);
                if (f)
                    writer.WriteLine(
                        "id,seed,calc,difficulty,step1,step2,step3,step4,step5,step6,step7,step8,suitCount,history"
                    );
                writer.WriteLine(content);
            }
        }
    }
}
