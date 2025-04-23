using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lazy;
using Lazy.Log;
using Solver.Exporter;
using UnityEditor;
using UnityEngine;

namespace Solver
{
    public class SpiderSolver
    {
        // ! 使用HashSet而不用List的原因是在于Contains的查询速度,HashSet为O(1),List为O(n),随着数组越来越大会导致执行效率大幅度下降
        private readonly HashSet<Poker> _allStates = new();

        /// <summary>
        /// * 所有Calc步骤
        /// </summary>
        public readonly List<Poker> AllStep = new();

        private int _calc;

        public int SuitCount = 1;

        private bool _breakFlag;

        private bool _threadEndFlag;

        public IEnumerator DepthFirstSearch(
            Poker root,
            Action onCompleted,
            string file = "",
            int id = 0
        )
        {
            if (_breakFlag)
                yield break;
            _calc++;
            AllStep.Add(root);
            root.Calc = _calc;
            // Debug.Log(_calc + " || " + root);
            // # 在当前合理的可能步骤数组中找到没有试过的扑克状态
            var states = TakeAStep(root, this)
                .FindAll(x => !StateExists(_allStates, x))
                .FindAll(x => x.SecondaryValuation());

            // if (_calc >= 500000)
            // {
            //     if (!string.IsNullOrEmpty(file))
            //     {
            //         var se = new SpiderExporter(file);
            //         se.ExportNull(id, root, SuitCount, _calc);
            //     }
            //
            //     _breakFlag = true;
            //     yield break;
            // }

            // # 遍历所有没有试过的游戏状态
            foreach (var state in states)
            {
                if (state.GameCompleted)
                {
                    // # 完成游戏
                    Log.MsgD("Game Completed !!!");
                    onCompleted.Fire();
                    // var se = new SpiderExporter(
                    //     @"C:\Users\baizeyv\Documents\a\TestSpiderSolver.csv"
                    // );
                    if (!string.IsNullOrEmpty(file))
                    {
                        var se = new SpiderExporter(file);
                        se.Export(id, state, SuitCount, _calc);
                    }

                    _breakFlag = true;
                    yield break;
                }

                foreach (var item in states)
                    _allStates.Add(item);
                yield return DepthFirstSearch(state, onCompleted, file, id);
                if (_breakFlag)
                    yield break;
            }

            if (_breakFlag)
                yield break;
        }

        public void ThreadDfs(
            Poker root,
            Action onCompleted,
            string file = "",
            int id = 0,
            bool exportNull = true,
            int stepLimit = 1000000
        )
        {
            if (_threadEndFlag)
                return;
            _calc++;
            AllStep.Add(root);
            root.Calc = _calc;
            // # 在当前合理的可能步骤数组中找到没有试过的扑克状态
            var states = TakeAStep(root, this)
                .FindAll(x => !StateExists(_allStates, x))
                .FindAll(x => x.SecondaryValuation());

            if (_calc >= stepLimit && stepLimit > 0) // # 百万步
            {
                if (!string.IsNullOrEmpty(file) && exportNull)
                {
                    var se = new SpiderExporter(file);
                    se.ExportNull(id, root, SuitCount, _calc);
                }

                _threadEndFlag = true;
                return;
            }

            // # 遍历所有没有试过的游戏状态
            foreach (var state in states)
            {
                if (state.GameCompleted)
                {
                    // # 完成游戏
                    EditorApplication.delayCall += () => Log.MsgD("Game Completed !!!");
                    onCompleted.Fire();
                    // var se = new SpiderExporter(
                    //     @"C:\Users\baizeyv\Documents\a\TestSpiderSolver.csv"
                    // );
                    if (!string.IsNullOrEmpty(file))
                    {
                        var se = new SpiderExporter(file);
                        se.Export(id, state, SuitCount, _calc);
                    }

                    _threadEndFlag = true;
                    return;
                }

                foreach (var item in states)
                    _allStates.Add(item);
                ThreadDfs(state, onCompleted, file, id, exportNull, stepLimit);
                if (_threadEndFlag)
                    return;
            }
        }

        /// <summary>
        /// * 走一步
        /// </summary>
        /// <param name="poker"></param>
        /// <param name="solver"></param>
        /// <returns>排序后的所有合理的可能走的步骤</returns>
        public static List<Poker> TakeAStep(Poker poker, SpiderSolver solver)
        {
            var results = new HashSet<Poker>();
            for (var i = 0; i < poker.VisibleGroup.Count; i++)
            {
                // # find all the movable cards
                var movableCards = FindMovableCardInColumn(poker.VisibleGroup[i], null);
                if (movableCards.Count > 0)
                {
                    // # 尝试移动
                    var newPokers = MoveMovableCards(movableCards, i, poker, solver);
                    foreach (var item in newPokers)
                        if (!StateExists(results, item))
                            results.Add(item);
                }
            }

            // # 添加发牌的可能
            var newPoker = CreateNewPoker(poker);
            var playDeckFlag = newPoker.PlayDeck();
            if (!playDeckFlag)
                return Sort(results, solver);
            if (!StateExists(results, newPoker))
                results.Add(newPoker);

            return Sort(results, solver);
        }

        /// <summary>
        /// * 创建新的扑克状态
        /// </summary>
        /// <param name="poker"></param>
        /// <param name="cards"></param>
        /// <param name="fromIndex"></param>
        /// <param name="toIndex"></param>
        /// <returns></returns>
        public static Poker CreateNewPoker(
            Poker poker,
            List<Card> cards = null,
            int fromIndex = -1,
            int toIndex = -1
        )
        {
            var newVisibleGroup = poker.VisibleGroup.Select(x => new List<Card>(x)).ToList();
            var newHiddenGroup = poker.HiddenGroup.Select(x => new List<Card>(x)).ToList();
            var newDeck = new List<Card>(poker.Deck);
            var newPoker = new Poker(newVisibleGroup, newHiddenGroup, newDeck)
            {
                History = new List<(int, int, int, bool)>(poker.History),
                PreviousPoker = poker,
                CardCount = poker.CardCount,
                CollectionStep = new List<int>(poker.CollectionStep),
                Mark = poker.Mark,
            };
            if (fromIndex < 0 || toIndex < 0)
                return newPoker;
            newPoker.MoveCard(fromIndex, cards.Count, toIndex);
            return newPoker;
        }

        /// <summary>
        /// * 找到一列中可以移动的牌
        /// </summary>
        /// <param name="column">指定列的VisibleGroup</param>
        /// <param name="firstCard">用于迭代的,null为第一层Stack调用</param>
        /// <returns></returns>
        public static List<Card> FindMovableCardInColumn(List<Card> column, Card firstCard)
        {
            if (column.Count == 0)
                return new List<Card>();
            if (firstCard == null)
            {
                if (column.Count == 1)
                    // # 此时只有一张可见的牌,则可移动的也就这一张
                    return new List<Card> { column[0] };
                // # 可见牌数>1
                var result = new List<Card> { column[0] };
                var tail = column.Skip(1).ToList();
                result.AddRange(FindMovableCardInColumn(tail, column[0]));
                return result;
            }

            // # 迭代调用
            // # 花色相同且值差1,此时可以移动
            if (
                column[0].GetType() == firstCard.GetType()
                && column[0].Value == firstCard.Value + 1
            )
            {
                var result = new List<Card> { column[0] };
                var tail = column.Skip(1).ToList();
                result.AddRange(FindMovableCardInColumn(tail, column[0]));
                return result;
            }

            return new List<Card>();
        }

        /// <summary>
        /// * 对可移动的牌进行移动
        /// </summary>
        /// <param name="movableCards"></param>
        /// <param name="fromIndex"></param>
        /// <param name="poker"></param>
        /// <param name="solver"></param>
        /// <returns>所有移动后的状态</returns>
        public static List<Poker> MoveMovableCards(
            List<Card> movableCards,
            int fromIndex,
            Poker poker,
            SpiderSolver solver
        )
        {
            var result = new HashSet<Poker>();
            for (var column = 0; column < poker.VisibleGroup.Count; column++)
            {
                // # 遍历非自身的每一列
                if (column == fromIndex)
                    continue;
                // # 从全部移动到只移动一张进行遍历 (前提是目标列不是空列,如若是空列则全部移动,这个规则可以进行修改)
                if (poker.IsBlank(column))
                {
                    // # 目标列为空列
                    if (
                        movableCards.Count == poker.VisibleGroup[fromIndex].Count
                        && poker.HiddenGroup[fromIndex].Count == 0
                    )
                    {
                        // # 当前列没有Hidden的牌了，并且要全部移动到另一个空列的情况,不添加进数组
                    }
                    else
                    {
                        var newPoker = CreateNewPoker(poker, movableCards, fromIndex, column);
                        if (!StateExists(result, newPoker))
                            result.Add(newPoker);
                    }
                }
                else
                {
                    // # 目标列不为空
                    for (var i = movableCards.Count; i >= 1; i--)
                    {
                        var cards = movableCards.Take(i).ToList();
                        if (poker.VisibleGroup[column][0].Value == cards.Last().Value + 1)
                        {
                            // # 可以放到目标列 (符合差值为1的条件)
                            var newPoker = CreateNewPoker(poker, cards, fromIndex, column);
                            if (!StateExists(result, newPoker))
                                result.Add(newPoker);
                        }
                    }
                }
            }

            return result.ToList();
        }

        /// <summary>
        /// * 检测新的扑克状态是否存在了
        /// </summary>
        /// <param name="results"></param>
        /// <param name="newPoker"></param>
        /// <returns></returns>
        public static bool StateExists(HashSet<Poker> results, Poker newPoker)
        {
            return results.Contains(newPoker);
        }

        /// <summary>
        /// * 对指定的所有扑克状态进行降序排序
        /// </summary>
        /// <param name="pokers"></param>
        /// <returns></returns>
        public static List<Poker> Sort(HashSet<Poker> pokers, SpiderSolver solver)
        {
            /*
            if (solver.SuitCount > 1)
            {
                var groups = pokers.GroupBy(x =>
                {
                    var (from, count, to, _) = x.History[0];
                    if (from < 0 || to < 0 || count < 0)
                        return (to: to, -1, typeof(Card));
                    var topCard = x.PreviousPoker.VisibleGroup[from][count - 1];
                    return (to, topCard.Value, topCard.GetType());
                });
                foreach (var group in groups)
                {
                    // # 最大移动牌数
                    var maxCount = group.Max(c => c.History[0].Item2);
                    // # 最大估值
                    var maxValuation = group.Max(c => c.Valuation);
                    // # 有几个最大移动牌数的
                    var cc = group.Count(x => x.History[0].Item2 == maxCount);
                    if (cc == 1)
                    {
                        // # 只有一个
                        var x = group.FirstOrDefault(x => x.History[0].Item2 == maxCount);
                        if (x != null)
                            if (x.Valuation < maxValuation)
                                x.Valuation = maxValuation + 1;
                    }
                    else
                    {
                        var list = group.Where(x => x.History[0].Item2 == maxCount);
                        var od = list.OrderByDescending(x => x.Valuation).ToList();
                        for (var i = 0; i < od.Count; i++)
                            if (od[i].Valuation < maxValuation)
                                od[i].Valuation = maxValuation + i + 1;
                    }
                }
            }
            */

            // # sorting games in descending order based on Valuation
            var valuationSortList = pokers.OrderByDescending(x => x.Valuation).ToList();
            var result = valuationSortList
                .OrderByDescending(x =>
                {
                    if (x.PreviousPoker == null || x.GetSuitCount() <= 1)
                        return int.MinValue;
                    var history = x.History;
                    var from = history[0].Item1;
                    var count = history[0].Item2;
                    var to = history[0].Item3;
                    if (from < 0 || count < 0 || to < 0)
                        return int.MinValue;
                    if (
                        x.PreviousPoker.VisibleGroup[to].Count > 0
                        && x.PreviousPoker.VisibleGroup[from][0].GetType()
                            == x.PreviousPoker.VisibleGroup[to][0].GetType()
                    )
                        return int.MaxValue;
                    return int.MinValue;
                })
                .ToList();
            return result;
        }
    }
}
