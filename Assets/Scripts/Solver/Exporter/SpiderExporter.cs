using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Lazy;

namespace Solver.Exporter
{
    public class SpiderExporter
    {
        private static readonly object FileLock = new();
        private readonly string _file;

        public SpiderExporter(string file)
        {
            _file = file;
        }

        public void Export(int id, Poker poker, int suitCount, int calc)
        {
            lock (FileLock)
            {
                var data = new LevelData(id, poker.Mark, suitCount, calc, poker.CollectionStep, poker.History, poker.Level, poker.Serialized);
                var content = data.ToString();
                // var filePath = @"C:\Users\baizeyv\Documents\a\TestSpiderSolver.csv";
                var filePath = _file;
                FileUtility.CheckFileAndCreateDirWhenNeeded(filePath);
                var f = !File.Exists(filePath);
                using var writer = new StreamWriter(filePath, true, Encoding.UTF8);
                if (f)
                    writer.WriteLine("id,seed,calc,difficulty,step1,step2,step3,step4,step5,step6,step7,step8,suitCount,history,level,serialized");
                writer.WriteLine(content);
            }
        }

        public void ExportNull(int id, Poker poker, int suitCount, int calc)
        {
            lock (FileLock)
            {
                var data = new LevelData(id, poker.Mark, suitCount, calc, new List<int> { -1, -1, -1, -1, -1, -1, -1, -1 },
                    new List<(int, int, int, bool)> { (-1, -1, -1, false) }, poker.Level, poker.Serialized);
                var content = data.ToString();
                FileUtility.CheckFileAndCreateDirWhenNeeded(_file);
                var f = !File.Exists(_file);
                using var writer = new StreamWriter(_file, true, Encoding.UTF8);
                if (f)
                    writer.WriteLine("id,seed,calc,difficulty,step1,step2,step3,step4,step5,step6,step7,step8,suitCount,history,level,serialized");
                writer.WriteLine(content);
            }
        }

        public async Task ExportAsync(int id, Poker poker, int suitCount, int calc)
        {
            var data = new LevelData(id, poker.Mark, suitCount, calc, poker.CollectionStep, poker.History, poker.Level, poker.Serialized);
            var content = data.ToString();
            FileUtility.CheckFileAndCreateDirWhenNeeded(_file);
            var f = !File.Exists(_file);
            using var writer = new StreamWriter(_file, true, Encoding.UTF8);
            if (f)
                await writer.WriteLineAsync(
                    "id,seed,calc,difficulty,step1,step2,step3,step4,step5,step6,step7,step8,suitCount,history,level,serialized");
            await writer.WriteLineAsync(content);
        }

        public async Task ExportNullAsync(int id, Poker poker, int suitCount, int calc)
        {
            var data = new LevelData(id, poker.Mark, suitCount, calc, new List<int> { -1, -1, -1, -1, -1, -1, -1, -1 },
                new List<(int, int, int, bool)> { (-1, -1, -1, false) }, poker.Level, poker.Serialized);
            var content = data.ToString();
            FileUtility.CheckFileAndCreateDirWhenNeeded(_file);
            var f = !File.Exists(_file);
            using var writer = new StreamWriter(_file, true, Encoding.UTF8);
            if (f)
                await writer.WriteLineAsync(
                    "id,seed,calc,difficulty,step1,step2,step3,step4,step5,step6,step7,step8,suitCount,history,level,serialized");
            await writer.WriteLineAsync(content);
        }
    }
}