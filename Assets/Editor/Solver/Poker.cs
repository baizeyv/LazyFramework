using System;
using System.Collections.Generic;
using System.Linq;
using Lazy.Log;

namespace Solver
{
    public class Poker
    {
        // "✅❎❇️";
        // "#️⃣";1️;2️⃣;
        // 3️⃣   4️⃣ 5️⃣ 6️⃣ 7️⃣ 8️⃣ 9️⃣ 🔟
        // "♠️"♣️;♠️;♥️;♦️
        // 🔴  ⚫  🟠  🟤   🈳
        public const string EmptyCard = "🎮<color=#383838>＃</color>";

        public const int PadWidth = 30;

        /// <summary>
        /// * 牌堆
        /// </summary>
        public readonly List<Card> Deck;

        /// <summary>
        /// * 隐藏牌
        /// </summary>
        public readonly List<List<Card>> HiddenGroup;

        /// <summary>
        /// * 可见牌
        /// </summary>
        public readonly List<List<Card>> VisibleGroup;

        /// <summary>
        /// * 历史记录 (fromColumnIndex, Count, toColumnIndex, 是否收一套牌)
        /// </summary>
        public List<(int, int, int, bool)> History = new();

        /// <summary>
        /// * 上一步状态
        /// </summary>
        public Poker PreviousPoker;

        /// <summary>
        /// * 当前的牌数
        /// </summary>
        private int _cardCount = 104;

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
                // # 没有翻开的牌值: -10, -9, -8, -7, -6, -5
                for (var i = 0; i < HiddenGroup.Count; i++)
                {
                    // # 未翻开牌减分机制
                    var num = 10;
                    foreach (var _ in HiddenGroup[i])
                    {
                        value -= num;
                        num--;
                    }

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
                }

                _valuation = value;
                return _valuation;

                void AddValue(int num, int topPoint, ref int result)
                {
                    if (num != 0)
                        result += topPoint * num;
                }
            }
        }

        /// <summary>
        /// * 完成了几套牌了
        /// </summary>
        public int FinishedCount
        {
            get
            {
                var currentCount = _cardCount / 13;
                return 8 - currentCount;
            }
        }

        /// <summary>
        /// * 剩余几套牌
        /// </summary>
        public int RemainingCount => _cardCount / 13;

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
        /// <exception cref="ArgumentException"></exception>
        public Poker(int seed)
        {
            var deck = GenerateDeck(seed);
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
            foreach (var card in VisibleGroup[index].Take(13))
                if (card.Value == set)
                {
                    set++;
                }
                else
                {
                    set = -1;
                    break;
                }

            var collection = false;
            if (set == 14)
            {
                collection = true;
                // # 1-13全了 (收一套牌)
                VisibleGroup[index] = VisibleGroup[index].Skip(13).ToList();
                _cardCount -= 13;
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

        public override string ToString()
        {
            var result =
                $"Valuation: <color=green>{Valuation}</color>🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏🃏 Step: <color=green>{History.Count}</color>";
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
                // # deep check
                for (var i = 0; i <= 9; i++)
                    if (VisibleGroup.Count <= i || that.VisibleGroup.Count <= i)
                        return VisibleGroup.Count == that.VisibleGroup.Count;
                    else if (VisibleGroup[i].Count != that.VisibleGroup[i].Count)
                        return false;
                    else
                        for (var j = 0; j < VisibleGroup[i].Count; j++)
                            // # if the card number and the suit do not match, the cards are different
                            if (
                                VisibleGroup[i][j].Value != that.VisibleGroup[i][j].Value
                                && VisibleGroup[i][j].GetType() != that.VisibleGroup[i][j].GetType()
                            )
                                return false;
                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            var state = new List<object>() { Deck, HiddenGroup, VisibleGroup };

            return state.Aggregate(
                0,
                (current, obj) => 31 * current + (obj != null ? obj.GetHashCode() : 0)
            );
        }

        /// <summary>
        /// * 生成牌堆
        /// </summary>
        private static List<Card> GenerateDeck(int seed)
        {
            var random = new Random(seed);
            var cards = new List<int>();
            for (var i = 0; i < 13; i++)
            for (var j = 1; j <= 8; j++)
                cards.Add(i + 1);

            var cardValues = cards.OrderBy(_ => random.Next()).ToList();
            var result = cardValues
                .Select(x =>
                {
                    switch (x)
                    {
                        case >= 1
                        and <= 13:
                            return BuildCard(1, x);
                        case >= 14
                        and <= 26:
                            return BuildCard(2, x);
                        case >= 27
                        and <= 39:
                            return BuildCard(3, x);
                        case >= 40
                        and <= 53:
                            return BuildCard(4, x);
                        default:
                            Log.MsgE("Card Pile ERROR !");
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
    }
}
