using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lazy.Log;
using Lazy.Utility;

namespace Solver
{
    public class Poker
    {
        // "✅❎❇️";
        // "#️⃣";1️;2️⃣;
        // 3️⃣   4️⃣ 5️⃣ 6️⃣ 7️⃣ 8️⃣ 9️⃣ 🔟
        // "♠️"♣️;♠️;♥️;♦️
        // 🔴  ⚫  🟠  🟤   🈳
        public const string EmptyCard = "🎮<color=#474747>＃</color>";

        public const int PadWidth = 30;

        public string Mark = "";

        public int Calc = 0;

        /// <summary>
        /// * 牌堆
        /// </summary>
        public List<Card> Deck;

        /// <summary>
        /// * 隐藏牌
        /// </summary>
        public List<List<Card>> HiddenGroup;

        /// <summary>
        /// * 可见牌
        /// </summary>
        public List<List<Card>> VisibleGroup;

        /// <summary>
        /// * 历史记录 (fromColumnIndex, Count, toColumnIndex, 是否收一套牌)
        /// </summary>
        public List<(int, int, int, bool)> History = new();

        /// <summary>
        /// * 收牌的步骤
        /// </summary>
        public List<int> CollectionStep = new();

        /// <summary>
        /// * 上一步状态
        /// </summary>
        public Poker PreviousPoker;

        /// <summary>
        /// * 当前的牌数
        /// </summary>
        public int CardCount = 104;

        private int _suitCount;

        private readonly List<int> _columnValuation = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        /// <summary>
        /// * 私有估值
        /// </summary>
        private int _valuation = -9999;

        /// <summary>
        /// * 估值
        /// </summary>
        public int Valuation
        {
            get
            {
                if (_valuation != -9999)
                    return _valuation;

                // # 完成一套牌 + 200分
                var value = FinishedCount * 200;
                for (var i = 0; i < HiddenGroup.Count; i++)
                {
                    // # 没有翻开的牌值: -10, -9, -8, -7, -6, -5
                    // # 未翻开牌减分机制
                    var num = 10;
                    foreach (var _ in HiddenGroup[i])
                    {
                        value -= num;
                        num--;
                    }

                    var tmp = value;
                    if (VisibleGroup[i].Count > 0)
                    {
                        var val = 0;
                        var top = VisibleGroup[i][0];
                        for (var x = 1; x < VisibleGroup[i].Count; x++)
                        {
                            top = VisibleGroup[i][x];
                            var down = VisibleGroup[i][x - 1];
                            // # 点数相差1
                            if (top.Value == down.Value + 1)
                            {
                                if (top.GetType() == down.GetType())
                                {
                                    // # 花色相同
                                    val++;
                                }
                                else
                                {
                                    // # 花色不同
                                    AddValue(val, down.Value, ref value);
                                    AddValue(1, 1, ref value);
                                    val = 0;
                                }
                            }
                            else
                            {
                                AddValue(val, down.Value, ref value);
                                val = 0;
                                //一个乱序组
                                //eg. 7 1 -> -7
                                //eg. 1 7 -> -14
                                //eg. 5 5 -> -10
                                var dv = -Math.Max(top.Value, down.Value);
                                if (top.Value < down.Value)
                                    dv *= 2;
                                AddValue(1, dv, ref value);
                            }
                        }

                        AddValue(val, top.Value, ref value);
                    }

                    _columnValuation[i] = value - tmp;
                }

                var flop = FlopValuation(6, false);
                var extra = ExtraValuationMoreSuit();
                // var moveMore = MoveValuationMoreSuit(6, false);
                // _valuation = value + flop + extra + moveMore;
                _valuation = value + flop + extra;
                return _valuation;

                void AddValue(int num, int topPoint, ref int result)
                {
                    if (num != 0)
                        result += topPoint * num;
                }
            }
            set => _valuation = value;
        }

        /// <summary>
        /// * 多花色的向其他地方移动牌的预测估值
        /// ! 加上这个之后之前很多可以解出来的变得解不出来了,所以舍去
        /// </summary>
        /// <returns></returns>
        private int MoveValuationMoreSuit(int limit, bool divide)
        {
            if (GetSuitCount() <= 1) // # 单花色不执行这个预测估值
                return 0;
            if (PreviousPoker == null)
                // # 没有上一步
                return 0;
            var from = History[0].Item1;
            var count = History[0].Item2;
            var to = History[0].Item3;
            if (from < 0 || count < 0 || to < 0)
                // # 忽略发牌
                return 0;
            if (PreviousPoker.VisibleGroup[from].Count != count)
            {
                // # 不能翻下边的隐藏牌,这种情况才需要计算这个预测估值
                var depth = 0;
                return CheckMoveVal(this, from, ref depth, divide ? limit / 2 : limit);
            }

            return 0;
        }

        private int CheckMoveVal(Poker poker, int column, ref int depth, int limit)
        {
            var result = 0;
            if (depth++ > limit)
                return result;
            if (poker.IsBlank(column))
                // # 空列了,不执行这个预测估值
                return 0;

            List<Card> canMove = new();
            var low = poker.VisibleGroup[column][0];
            canMove.Add(low);
            for (var i = 1; i < poker.VisibleGroup[column].Count; i++)
                if (
                    poker.VisibleGroup[column][i].GetType() == canMove[^1].GetType()
                    && poker.VisibleGroup[column][i].Value - 1 == canMove[^1].Value
                )
                    canMove.Add(poker.VisibleGroup[column][i]);
                else
                    break;

            for (var i = 0; i < poker.VisibleGroup.Count; i++)
            {
                if (i == column)
                    continue;
                if (poker.VisibleGroup[i].Count == 0)
                    // # 向空列移动不加分
                    continue;
                if (poker.VisibleGroup[i][0].Value == canMove[^1].Value + 1)
                {
                    var newPoker = SpiderSolver.CreateNewPoker(poker, canMove, column, i);
                    if (!newPoker.InValidMove())
                    {
                        // # 可以向其他列完整移动
                        if (poker.VisibleGroup[i][0].GetType() == canMove[^1].GetType())
                            // # 相同花色
                            result += 40;
                        else
                            // # 不同花色
                            result += 20;
                    }
                }

                List<Card> moveList = new();
                for (var x = 0; x < poker.VisibleGroup[i].Count; x++)
                {
                    if (x == 0)
                    {
                        moveList.Add(poker.VisibleGroup[i][0]);
                        continue;
                    }

                    var cur = poker.VisibleGroup[i][x];
                    if (
                        cur.GetType() == moveList[^1].GetType()
                        && cur.Value == moveList[^1].Value + 1
                    )
                        moveList.Add(cur);
                    else
                        break;
                }

                if (moveList[^1].Value + 1 == low.Value)
                {
                    // # 可以移动到新翻开的位置
                    var newPoker = SpiderSolver.CreateNewPoker(poker, moveList, i, column);
                    if (!newPoker.InValidMove())
                    {
                        if (moveList[^1].GetType() == low.GetType())
                            // # 相同花色
                            result += 30;
                        else
                            // # 不同花色
                            result += 10;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// * 是否是无效移动
        /// </summary>
        /// <returns></returns>
        public bool InValidMove()
        {
            if (History.Count < 2)
                return false;
            if (History[0].Item1 < 0 || History[0].Item2 < 0 || History[0].Item3 < 0)
                return false;
            if (
                History[0].Item2 == History[1].Item2
                && History[0].Item1 == History[1].Item3
                && History[0].Item3 == History[1].Item1
            )
                return true;
            return false;
        }

        /// <summary>
        /// * 多花色的向空列移动的情况的额外估值 (可以整理牌型)
        /// </summary>
        /// <returns></returns>
        private int ExtraValuationMoreSuit()
        {
            if (GetSuitCount() <= 1)
                return 0;
            var result = BlankColumnCount * 200; // # 空列加200
            if (History.Count <= 0)
                return result;
            if (PreviousPoker == null)
                return result;
            var from = History[0].Item1;
            var count = History[0].Item2;
            var to = History[0].Item3;
            if (from < 0 || count < 0 || to < 0)
                return result;
            if (IsBlank(from))
                return result;
            if (PreviousPoker.IsBlank(to))
                // # 向空列移动
                for (var i = 0; i < PreviousPoker.VisibleGroup.Count; i++)
                {
                    if (PreviousPoker.VisibleGroup[i].Count == 0)
                        continue;
                    if (i == to)
                        continue;
                    List<Card> list = new();
                    for (var x = 0; x < PreviousPoker.VisibleGroup[i].Count; x++)
                        if (x == 0)
                        {
                            list.Add(PreviousPoker.VisibleGroup[i][0]);
                        }
                        else
                        {
                            if (
                                PreviousPoker.VisibleGroup[i][x].GetType() == list[^1].GetType()
                                && PreviousPoker.VisibleGroup[i][x].Value - 1 == list[^1].Value
                            )
                                list.Add(PreviousPoker.VisibleGroup[i][x]);
                            else
                                break;
                        }

                    if (VisibleGroup[from][0].GetType() == list[^1].GetType())
                    {
                        // # 同花色
                        if (list[^1].Value + 1 == VisibleGroup[from][0].Value)
                        {
                            result += 100;
                        }
                        else
                        {
                            List<Card> st = new();
                            for (var c = 0; c < VisibleGroup[from].Count; c++)
                                if (c == 0)
                                {
                                    st.Add(VisibleGroup[from][0]);
                                }
                                else
                                {
                                    if (
                                        VisibleGroup[from][c].GetType() == st[^1].GetType()
                                        && VisibleGroup[from][c].Value - 1 == st[^1].Value
                                    )
                                        st.Add(VisibleGroup[from][c]);
                                    else
                                        break;
                                }

                            if (list[0].Value - 1 == st[^1].Value)
                                result += 100;
                        }
                    }
                }

            return result;
        }

        /// <summary>
        /// * 翻牌额外估值
        /// </summary>
        private int FlopValuation(int limit, bool divide)
        {
            if (PreviousPoker == null)
                // # 没有上一步
                return 0;
            // if (_solver.FlopValuations.TryGetValue(this, out var val))
            //     return val;
            var from = History[0].Item1;
            var count = History[0].Item2;
            var to = History[0].Item3;
            if (from < 0 || count < 0 || to < 0)
                // # 忽略发牌
                return 0;
            if (
                PreviousPoker.VisibleGroup[from].Count == count
                && PreviousPoker.HiddenGroup[from].Count != 0
            )
            {
                // # 可以翻出新牌 flop new card
                var depth = 0;
                return CheckFlop(this, from, ref depth, divide ? limit / 2 : limit);
            }

            return 0;
        }

        /// <summary>
        /// * 完成了几套牌了
        /// </summary>
        public int FinishedCount
        {
            get
            {
                var currentCount = CardCount / 13;
                return 8 - currentCount;
            }
        }

        /// <summary>
        /// * 剩余几套牌
        /// </summary>
        public int RemainingCount => CardCount / 13;

        /// <summary>
        /// * 游戏是否完成
        /// </summary>
        public bool GameCompleted
        {
            get
            {
                return Deck.Count == 0
                    && !VisibleGroup.SelectMany(x => x).Any()
                    && !HiddenGroup.SelectMany(x => x).Any();
            }
        }

        /// <summary>
        /// * 空白列的数量
        /// </summary>
        public int BlankColumnCount
        {
            get
            {
                return VisibleGroup.Where((t, i) => t.Count + HiddenGroup[i].Count == 0).Count();
            }
        }

        public bool HasHidden => HiddenGroup.Sum(x => x.Count) != 0;

        /// <summary>
        /// * Constructor
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="suitCount"></param>
        /// <exception cref="ArgumentException"></exception>
        public Poker(int seed, int suitCount)
        {
            Mark = seed.ToString();
            _suitCount = suitCount;
            var deck = GenerateDeck(seed, suitCount);
            Build(deck);
        }

        public Poker(string vitaLevel)
        {
            Mark = vitaLevel;
            var deck = VitaLevelConvertToPoker(vitaLevel);
            Build(deck);
        }

        public int GetSuitCount()
        {
            if (_suitCount > 0)
                return _suitCount;
            HashSet<int> values = new();
            foreach (var x in Deck)
                values.Add(x.OriginalValue);

            foreach (var c in HiddenGroup.SelectMany(x => x))
                values.Add(c.OriginalValue);
            foreach (var c in VisibleGroup.SelectMany(x => x))
                values.Add(c.OriginalValue);

            if (values.Count == 13)
            {
                _suitCount = 1;
                return 1;
            }

            if (values.Count == 26)
            {
                _suitCount = 2;
                return 2;
            }

            if (values.Count == 39)
            {
                _suitCount = 3;
                return 3;
            }

            if (values.Count == 52)
            {
                _suitCount = 4;
                return 4;
            }

            _suitCount = 1;
            return 1;
        }

        private void Build(List<Card> deck)
        {
            var hidden = deck.GetRange(0, deck.Count - 60);
            var begin4 = deck.GetRange(deck.Count - 54, 4);
            var last6 = deck.GetRange(deck.Count - 60, 6);
            var visible = begin4.Concat(last6);
            var corner = deck.GetRange(deck.Count - 50, 50);
            var hiddenTable = new List<List<Card>>();
            var visibleTable = new List<List<Card>>();
            for (var i = 0; i < 10; i++)
            {
                hiddenTable.Add(new List<Card>());
                visibleTable.Add(new List<Card>());
            }

            var idx = 0;
            foreach (var item in hidden)
            {
                if (idx > 9)
                    idx = 0;
                hiddenTable[idx++].Add(item);
            }

            idx = 0;
            foreach (var item in visible)
            {
                if (idx > 9)
                    idx = 0;
                visibleTable[idx++].Add(item);
            }

            foreach (var item in hiddenTable)
                item.Reverse();

            Deck = corner;
            HiddenGroup = hiddenTable;
            VisibleGroup = visibleTable;
            if (Deck.Count > 50)
                throw new ArgumentException("deck.Count must be <= 50");
            if (HiddenGroup.Count > 12)
                throw new ArgumentException("hiddenGroup.Count must be <= 12");
            if (VisibleGroup.Count > 12)
                throw new ArgumentException("visibleGroup.Count must be <= 12");
        }

        public Poker(List<List<Card>> visibleGroup, List<List<Card>> hiddenGroup, List<Card> deck)
        {
            VisibleGroup = visibleGroup;
            HiddenGroup = hiddenGroup;
            Deck = deck;
            if (deck.Count > 50)
                throw new ArgumentException("deck.Count must be <= 50");

            if (hiddenGroup.Count > 12)
                throw new ArgumentException("hiddenGroup.Count must be <= 12");

            if (visibleGroup.Count > 12)
                throw new ArgumentException("visibleGroup.Count must be <= 12");
        }

        /// <summary>
        /// * 二次估值,用于解决在发牌前的很多无效移动 (各种顺子的移动)
        /// </summary>
        /// <returns></returns>
        public bool SecondaryValuation()
        {
            if (PreviousPoker == null)
                return true;
            var from = History[0].Item1;
            var count = History[0].Item2;
            var to = History[0].Item3;
            var collection = History[0].Item4;
            if (from < 0 || count < 0 || to < 0)
                // # 发牌
                return true;
            if (PreviousPoker.IsBlank(to))
                // # 目标列为空
                return true;
            if (PreviousPoker.VisibleGroup[from].Count == count || collection)
                // # 一列全部都移动或收牌了
                return true;
            // # 上一步的列的二次估值
            var previousValue = Calculate(PreviousPoker.VisibleGroup[from]);
            // # 当前的列的二次估值
            var currentValue = Calculate(VisibleGroup[to]);

            // if (
            //     PreviousPoker.VisibleGroup[from].Count > VisibleGroup[to].Count
            //     && currentValue == previousValue
            // )
            //     return true;

            return currentValue > previousValue;

            int Calculate(List<Card> cards)
            {
                var value = 0;
                var val = 0;
                for (var i = 1; i < cards.Count; i++)
                {
                    var top = cards[i];
                    var down = cards[i - 1];
                    // # 点数相差1
                    if (top.Value == down.Value + 1)
                    {
                        if (top.GetType() == down.GetType())
                        // # 花色相同
                        {
                            val++;
                            if (i + 1 == cards.Count)
                                // # 一直是顺子到最上一张了
                                AddValue(val, top.Value, ref value);
                        }
                        else
                        {
                            AddValue(val, down.Value, ref value);
                            break;
                        }
                    }
                    else
                    {
                        AddValue(val, down.Value, ref value);
                        break;
                    }
                }

                return value;
            }

            void AddValue(int num, int topPoint, ref int result)
            {
                if (num != 0)
                    result += topPoint * num;
            }
        }

        private int CheckFlop(Poker poker, int column, ref int depth, int limit)
        {
            if (depth++ > limit)
                return 0;
            var result = 0;
            var value = poker.VisibleGroup[column][0].Value;
            HashSet<Poker> setTo = new();
            HashSet<Poker> setCome = new();
            for (var i = 0; i < poker.VisibleGroup.Count; i++)
            {
                if (i == column)
                    continue;
                if (poker.VisibleGroup[i].Count == 0)
                    // # 向空列移动不加分
                    continue;
                if (poker.VisibleGroup[i][0].Value == value + 1)
                {
                    // # 可以向其他列移动
                    var newPoker = SpiderSolver.CreateNewPoker(
                        poker,
                        new List<Card>() { poker.VisibleGroup[column][0] },
                        column,
                        i
                    );
                    setTo.Add(newPoker);
                    result += 2;
                }

                List<Card> moveList = new();
                for (var x = 0; x < poker.VisibleGroup[i].Count; x++)
                {
                    if (x == 0)
                    {
                        moveList.Add(poker.VisibleGroup[i][0]);
                        continue;
                    }

                    var cur = poker.VisibleGroup[i][x];
                    if (
                        cur.GetType() == moveList[^1].GetType()
                        && cur.Value == moveList[^1].Value + 1
                    )
                        moveList.Add(cur);
                    else
                        break;
                }

                if (moveList[^1].Value + 1 == value)
                {
                    // # 可以移动到新翻开的牌的位置
                    var newPoker = SpiderSolver.CreateNewPoker(poker, moveList, i, column);
                    setCome.Add(newPoker);
                    result += 1;
                }
            }

            // # 计算额外翻牌分
            foreach (var item in setCome)
                result += item.FlopValuation(limit, true);

            foreach (var item in setTo)
                result += item.FlopValuation(limit, false);

            return result;
        }

        public bool IsBlank(int index)
        {
            return VisibleGroup[index].Count + HiddenGroup[index].Count == 0;
        }

        /// <summary>
        /// * 检测是否可以收集一套牌
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private bool DetectCollection(int index)
        {
            var set = 1;
            if (VisibleGroup[index].Count > 0)
            {
                var suit = VisibleGroup[index][0].GetType();
                foreach (var card in VisibleGroup[index].Take(13))
                    if (card.Value == set && suit == card.GetType()) // # 同色才能收牌
                    {
                        set++;
                    }
                    else
                    {
                        set = -1;
                        break;
                    }
            }

            var collection = false;
            if (set == 14)
            {
                collection = true;
                // # 1-13全了 (收一套牌)
                VisibleGroup[index] = VisibleGroup[index].Skip(13).ToList();
                CardCount -= 13;
                CollectionStep.Add(History.Count + 1);
                if (VisibleGroup[index].Count == 0 && HiddenGroup[index].Count > 0)
                {
                    // # 移动后新的列收牌了,如若没有可见的了则展示新的牌
                    VisibleGroup[index] = new List<Card>() { HiddenGroup[index].First() };
                    HiddenGroup[index] = HiddenGroup[index].Skip(1).ToList();
                }
            }

            return collection;
        }

        /// <summary>
        /// * 移动牌
        /// </summary>
        /// <param name="from"></param>
        /// <param name="count"></param>
        /// <param name="to"></param>
        public void MoveCard(int from, int count, int to)
        {
            if (VisibleGroup.Count == 0)
                // # 没有可见的牌了
                return;

            var fromList = VisibleGroup[from];
            var toList = VisibleGroup[to];
            var movingCards = fromList.Take(count).ToList();
            var newToList = movingCards.Concat(toList).ToList();
            VisibleGroup[to] = newToList;
            VisibleGroup[from] = fromList.Skip(count).ToList();
            // # 检测收牌
            var collection = DetectCollection(to);

            // # 来源列在没有可见牌的时候要翻开隐藏牌
            if (VisibleGroup[from].Count == 0 && HiddenGroup[from].Count > 0)
            {
                VisibleGroup[from] = new List<Card>() { HiddenGroup[from].First() };
                HiddenGroup[from] = HiddenGroup[from].Skip(1).ToList();
            }

            // # 添加历史记录
            History.Insert(0, (from, count, to, collection));
        }

        /// <summary>
        /// * 发牌
        /// </summary>
        /// <returns></returns>
        public bool PlayDeck()
        {
            if (Deck.Count == 0)
                return false;
            var collection = false;
            for (var i = 0; i < VisibleGroup.Count; i++)
            {
                VisibleGroup[i].Insert(0, Deck.First());
                Deck.RemoveAt(0);
                collection |= DetectCollection(i);
            }

            History.Insert(0, (-1, -1, -1, collection));
            return true;
        }

        public void WriteHistory(string file = @"C:\Users\baizeyv\Documents\a\History.txt")
        {
            FileUtility.CheckFileAndCreateDirWhenNeeded(file);
            using (StreamWriter writer = new(file))
            {
                var step = 0;
                foreach (var item in History)
                    writer.WriteLine(
                        $"Step:{++step}  From:{item.Item1}  To:{item.Item3}  Count:{item.Item2}"
                    );
            }
        }

        public override string ToString()
        {
            var result =
                $"Calc: {Calc} Valuation: <color=green>{Valuation}</color>🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏 Step: <color=green>{History.Count}</color>";
            result += $" Collection:{FinishedCount}";
            if (PreviousPoker != null)
            {
                var tuple = History[0];
                var from = tuple.Item1;
                var count = tuple.Item2;
                var to = tuple.Item3;

                result += $" from:{from} count:{count} to:{to}";

                if (!(from < 0 || count < 0 || to < 0))
                {
                    var previousCount = PreviousPoker.FinishedCount;
                    var currentCount = FinishedCount;
                    var xx = 0;
                    foreach (var item in PreviousPoker.VisibleGroup[from])
                    {
                        item.Highlight();
                        xx++;
                        if (xx == count)
                            break;
                    }

                    if (currentCount <= previousCount)
                    {
                        // # 没有收牌
                        var yy = 0;
                        foreach (var item in VisibleGroup[to])
                        {
                            item.Highlight();
                            yy++;
                            if (yy == count)
                                break;
                        }
                    }
                }
                else
                {
                    // # 发牌了
                    foreach (var item in VisibleGroup)
                        if (item.Count > 0)
                            item[0]?.Highlight();

                    for (var i = 0; i < 10; i++)
                        PreviousPoker.Deck[i]?.Highlight();
                }

                result += "      Previous ⏩⏩⏩ Current";
                if (
                    HiddenGroup.Any(x => x.Count > 0)
                    || PreviousPoker.HiddenGroup.Any(x => x.Count > 0)
                )
                {
                    var currentMaxHidden = HiddenGroup
                        .Select(x => x.Count)
                        .Aggregate(0, (currentMax, size) => size > currentMax ? size : currentMax);
                    var previousMaxHidden = PreviousPoker
                        .HiddenGroup.Select(x => x.Count)
                        .Aggregate(0, (currentMax, size) => size > currentMax ? size : currentMax);
                    var max = Math.Max(currentMaxHidden, previousMaxHidden);
                    result += "\n\n❎ Hidden Poker Cards: \n";
                    var previousArray = PreviousPoker.GetHiddenString(0, max).Split('\n');
                    var currentArray = GetHiddenString(0, max).Split('\n');
                    for (var i = 0; i < previousArray.Length; i++)
                    {
                        result += previousArray[i].PadRight(305, ' ');
                        result += "➡️".PadRight(10, ' ');
                        result += currentArray[i];
                        result += "\n";
                    }
                }

                if (
                    VisibleGroup.Any(x => x.Count > 0)
                    || PreviousPoker.VisibleGroup.Any(x => x.Count > 0)
                )
                {
                    var currentVisibleMax = VisibleGroup
                        .Select(x => x.Count)
                        .Aggregate(0, (currentMax, size) => size > currentMax ? size : currentMax);
                    var previousVisibleMax = PreviousPoker
                        .VisibleGroup.Select(x => x.Count)
                        .Aggregate(0, (currentMax, size) => size > currentMax ? size : currentMax);
                    var max = Math.Max(currentVisibleMax, previousVisibleMax);
                    result += "\n✅ Visible Poker Cards: \n";
                    var previousArray = PreviousPoker.GetVisibleString(0, max).Split('\n');
                    var currentArray = GetVisibleString(0, max).Split('\n');
                    for (var i = 0; i < previousArray.Length; i++)
                    {
                        result += previousArray[i].PadRight(305, ' ');
                        result += "➡️".PadRight(10, ' ');
                        result += currentArray[i];
                        result += "\n";
                    }
                }

                if (Deck.Count > 0 || PreviousPoker.Deck.Count > 0)
                {
                    result += "\n❇️ Deck Poker Cards: \n";
                    var max = Math.Max(Deck.Count, PreviousPoker.Deck.Count);
                    var previousStr = string.Empty;
                    for (var i = 0; i < max; i++)
                    {
                        if (i % 10 == 0 && i != 0)
                            previousStr += "\n";
                        previousStr +=
                            i < PreviousPoker.Deck.Count
                                ? PreviousPoker.Deck[i].ToString().PadRight(PadWidth, ' ')
                                : EmptyCard.PadRight(PadWidth, ' ');
                    }

                    var currentStr = string.Empty;
                    for (var i = 0; i < max; i++)
                    {
                        if (i % 10 == 0 && i != 0)
                            currentStr += "\n";
                        currentStr +=
                            i < Deck.Count
                                ? Deck[i].ToString().PadRight(PadWidth, ' ')
                                : EmptyCard.PadRight(PadWidth, ' ');
                    }

                    var previousArray = previousStr.Split('\n');
                    var currentArray = currentStr.Split('\n');
                    for (var i = 0; i < previousArray.Length; i++)
                    {
                        result += previousArray[i].PadRight(305, ' ');
                        result += "➡️".PadRight(10, ' ');
                        result += currentArray[i];
                        result += "\n";
                    }
                }

                foreach (var y in PreviousPoker.VisibleGroup.SelectMany(x => x))
                    y.NoHighlight();
                foreach (var y in PreviousPoker.HiddenGroup.SelectMany(x => x))
                    y.NoHighlight();

                foreach (var x in PreviousPoker.Deck)
                    x.NoHighlight();

                return result;
            }
            else
            {
                // # 没有上一步
                if (HiddenGroup.Any(x => x.Count > 0))
                {
                    var maxHidden = HiddenGroup
                        .Select(x => x.Count)
                        .Aggregate(0, (currentMax, size) => size > currentMax ? size : currentMax);
                    result += "\n\n❎ Hidden Poker Cards: \n";
                    result += GetHiddenString(0, maxHidden);
                }

                if (VisibleGroup.Any(x => x.Count > 0))
                {
                    var max = VisibleGroup
                        .Select(x => x.Count)
                        .Aggregate(0, (currentMax, size) => size > currentMax ? size : currentMax);
                    result += "\n\n✅ Visible Poker Cards: \n";
                    result += GetVisibleString(0, max);
                }

                if (Deck.Count > 0)
                    result += "\n\n❇️ Deck Poker Cards: ";
                result += GetDeckString();
                return result;
            }
        }

        private string GetDeckString()
        {
            var result = string.Empty;
            for (var i = 0; i < Deck.Count; i++)
            {
                if (i % 10 == 0)
                    result += "\n";
                result += Deck[i].ToString().PadRight(PadWidth, ' ');
            }

            return result;
        }

        private string GetVisibleString(int row, int max)
        {
            if (max == 0)
                return string.Empty;
            if (row == max - 1)
                return FloorVisibleString(row);
            return FloorVisibleString(row) + "\n" + GetVisibleString(row + 1, max);
        }

        private string GetHiddenString(int row, int max)
        {
            if (max == 0)
                return string.Empty;
            if (row == max - 1)
                return FloorHiddenString(row);
            return FloorHiddenString(row) + "\n" + GetHiddenString(row + 1, max);
        }

        private string FloorVisibleString(int row)
        {
            var result = string.Empty;
            foreach (var column in VisibleGroup)
                if (column.Count > row)
                {
                    var card = column[column.Count - row - 1];
                    result += card.ToString().PadRight(PadWidth, ' ');
                }
                else
                {
                    result += EmptyCard.PadRight(PadWidth, ' ');
                }

            return result;
        }

        private string FloorHiddenString(int row)
        {
            var result = string.Empty;
            foreach (var column in HiddenGroup)
                if (column.Count > row)
                {
                    var card = column[column.Count - row - 1];
                    result += card.ToString().PadRight(PadWidth, ' ');
                }
                else
                {
                    result += EmptyCard.PadRight(PadWidth, ' ');
                }

            return result;
        }

        public override bool Equals(object obj)
        {
            if (obj is Poker that)
            {
                // # fast check if not equal
                if (Deck.Count != that.Deck.Count)
                    return false;
                for (var i = 0; i < VisibleGroup.Count; i++)
                {
                    if (VisibleGroup[i].Count != that.VisibleGroup[i].Count)
                        return false;
                    if (HiddenGroup[i].Count != that.HiddenGroup[i].Count)
                        return false;
                    for (var x = 0; x < VisibleGroup[i].Count; x++)
                    {
                        if (VisibleGroup[i][x].GetType() != that.VisibleGroup[i][x].GetType())
                            return false;
                        if (VisibleGroup[i][x].Value != that.VisibleGroup[i][x].Value)
                            return false;
                    }

                    for (var x = 0; x < HiddenGroup[i].Count; x++)
                    {
                        if (HiddenGroup[i][x].GetType() != that.HiddenGroup[i][x].GetType())
                            return false;
                        if (HiddenGroup[i][x].Value != that.HiddenGroup[i][x].Value)
                            return false;
                    }
                }

                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            var hash = Deck.Aggregate(
                17,
                (current, item) => current * 31 + (item?.GetHashCode() ?? 0)
            );
            hash = HiddenGroup
                .SelectMany(item => item)
                .Aggregate(hash, (current, card) => current * 31 + (card?.GetHashCode() ?? 0));
            hash = VisibleGroup
                .SelectMany(item => item)
                .Aggregate(hash, (current, card) => current * 31 + (card?.GetHashCode() ?? 0));
            return hash;
        }

        /// <summary>
        /// * 生成牌堆
        /// </summary>
        private static List<Card> GenerateDeck(int seed, int suitCount)
        {
            var random = new Random(seed);
            var cards = new List<int>();
            for (var i = 0; i < 13; i++)
            for (var j = 1; j <= 8; j++)
            {
                var tmp = j % suitCount;
                cards.Add(i + 1 + tmp * 13);
            }

            var cardValues = cards.OrderBy(_ => random.Next()).ToList();
            var result = cardValues
                .Select(x =>
                {
                    switch (x)
                    {
                        case >= 1
                        and <= 13:
                            return BuildCard(2, x);
                        case >= 14
                        and <= 26:
                            return BuildCard(1, x);
                        case >= 27
                        and <= 39:
                            return BuildCard(4, x);
                        case >= 40
                        and <= 53:
                            return BuildCard(3, x);
                        default:
                            // Log.MsgE("Card Pile ERROR !");
                            return BuildCard(999, 999);
                    }
                })
                .ToList();
            // Log.MsgD(string.Join(", ", cardValues));
            return result;
        }

        /// <summary>
        /// 新建牌
        /// </summary>
        /// <param name="suit">花色 0红桃 1黑桃 2方片 3梅花</param>
        /// <param name="num"></param>
        /// <returns></returns>
        private static Card BuildCard(int suit, int num)
        {
            return suit switch
            {
                1 => new HeartCard(num),
                2 => new SpadeCard(num),
                3 => new DiamondCard(num),
                4 => new ClubsCard(num),
                _ => new HeartCard(num),
            };
        }

        private static List<Card> VitaLevelConvertToPoker(string vitaLevel)
        {
            var array = vitaLevel.Split(",1;");
            var deck = string.Empty;
            // # 牌堆
            var deckString = array[^1].Substring(0, array[^1].Length - 2); // # -2 是为了去掉最后的,0
            for (var i = deckString.Length - 1; i >= 0; i--)
                deck += deckString[i];

            var value = string.Empty;
            var idx = 0;
            // # 遍历10列
            for (var x = 0; x < 6; x++)
            {
                for (var i = 0; i < 10; i++)
                {
                    var str = array[i];
                    if (str.Length <= x)
                        continue;
                    var c = str[x];
                    value += c;
                }

                idx++;
            }

            var s = value + deck;

            return s.Select(VitaCharToCardValue).Select(GetCard).ToList();

            Card GetCard(int x)
            {
                switch (x)
                {
                    case >= 1
                    and <= 13:
                        return BuildCard(2, x);
                    case >= 14
                    and <= 26:
                        return BuildCard(1, x);
                    case >= 27
                    and <= 39:
                        return BuildCard(4, x);
                    case >= 40
                    and <= 53:
                        return BuildCard(3, x);
                    default:
                        // Log.MsgE("Card Pile ERROR !");
                        return BuildCard(999, 999);
                }
            }
        }

        //方片A-K（A-M）
        //黑桃A-K（N-Z）
        //梅花A-K（a-m）
        //红桃A-K（n-z）
        //Vita Spider 第一关: "TNRSQW,1;RYXOXP,1;WQQWOW,1;ZRXPNZ,1;SVTRP,1;VOXYY,1;NOYZV,1;PUZXO,1;PUWPS,1;WRSYU,1;XYUTNZZOUNSVXVOQPZSVZWRQUTRRQNSQXVUSTWPUTNTNQVYOYT,0",
        //第一列从下往上是：TNRSQW
        //排堆的从下往上是：XYUTNZZOUNSVXVOQPZSVZWRQUTRRQNSQXVUSTWPUTNTNQVYOYT可以理解最后一个放在最上面，会最先发下去

        /// <summary>
        /// * 牌面值转换为字符
        /// # 黑桃1-13，红桃14-26，梅花27-39，方片40-54
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private static char CardValueToChar(int x)
        {
            if (x < 14)
                return (char)('N' + (x - 1));
            if (x < 27)
                return (char)('n' + (x - 14));
            if (x < 40)
                return (char)('a' + (x - 27));
            return (char)('A' + (x - 40));
        }

        private static Dictionary<char, int> VitaCharDic;

        /// <summary>
        /// * Vita的关卡中的牌值转为自己的Poker的值
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private static int VitaCharToCardValue(char c)
        {
            if (VitaCharDic == null)
            {
                VitaCharDic = new Dictionary<char, int>();
                for (var i = 1; i <= 54; i++)
                {
                    var val = CardValueToChar(i);
                    VitaCharDic.TryAdd(val, i);
                }
            }

            return VitaCharDic[c];
            /*
            var v4 = c - 'N' + 1;
            if (v4 is >= 1 and <= 54)
                // # 有效值
                return v4;
            var v3 = c - 'n' + 14;
            if (v3 is >= 1 and <= 54)
                // # 有效值
                return v3;
            var v2 = c - 'a' + 27;
            if (v2 is >= 1 and <= 54)
                // # 有效值
                return v2;
            var v1 = c - 'A' + 40;
            if (v1 is >= 1 and <= 54)
                // # 有效值
                return v1;
            return 0;
            */
        }
    }
}
