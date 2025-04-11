using System.IO;
using System.Text;
using Lazy.Utility;

namespace Solver.Exporter
{
    public class SpiderExporter
    {
        private readonly string _file;

        public SpiderExporter(string file)
        {
            _file = file;
        }

        public void Export(int id, Poker poker, int suitCount, int calc)
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
}
