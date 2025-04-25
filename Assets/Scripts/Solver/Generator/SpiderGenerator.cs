using System.Threading;

namespace Solver
{
    /// <summary>
    /// * 蜘蛛纸牌关卡生成器
    /// </summary>
    public class SpiderGenerator
    {
        private Thread _thread;

        /// <summary>
        /// * 生成指定范围的关卡
        /// </summary>
        /// <param name="minSeed"></param>
        /// <param name="maxSeed"></param>
        /// <param name="suitCount"></param>
        /// <param name="file"></param>
        /// <param name="stepLimit"></param>
        public void GenerateLevel(
            int minSeed,
            int maxSeed,
            int suitCount,
            string file,
            int stepLimit
        )
        {
            if (_thread != null)
                return;
            if (minSeed > maxSeed)
                return;
            _thread = new Thread(() =>
            {
                for (var i = minSeed; i <= maxSeed; i++)
                {
                    var solver = new SpiderSolver { SuitCount = suitCount };
                    var poker = new Poker(i, suitCount);
                    solver.ThreadDepthFirstSearch(
                        poker,
                        () =>
                        {
                            _thread = null;
                        },
                        file,
                        0,
                        false,
                        stepLimit
                    );
                }
            });
            _thread.Start();
        }

        /// <summary>
        /// * 停止生成关卡
        /// </summary>
        public void StopGeneration()
        {
            if (_thread == null)
                return;
            _thread.Abort();
            _thread = null;
        }
    }
}
