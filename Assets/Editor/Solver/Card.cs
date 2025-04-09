using System;

namespace Solver
{
    // "Ａ　２　３　４　５　６　７　８　９　Ｘ　Ｊ　Ｑ　Ｋ"
    public abstract class Card
    {
        public readonly int Value;

        private bool _highlight;

        protected Card(int value)
        {
            if (value is <= 0 or > 13)
                throw new ArgumentException("The number must be non-negative.");
            Value = value;
        }

        public void Highlight()
        {
            _highlight = true;
        }

        public void NoHighlight()
        {
            _highlight = false;
        }

        protected string GetValueString()
        {
            var result = _highlight ? "<color=#00ff00>" : "<color=#ffffff>";
            result += Value switch
            {
                1 => "Ａ",
                2 => "２",
                3 => "３",
                4 => "４",
                5 => "５",
                6 => "６",
                7 => "７",
                8 => "８",
                9 => "９",
                10 => "Ｘ",
                11 => "Ｊ",
                12 => "Ｑ",
                13 => "Ｋ",
                _ => "＃",
            };
            result += "</color>";
            return result;
        }
    }

    /// <summary>
    /// * 红桃牌 1-13
    /// </summary>
    public sealed class HeartCard : Card
    {
        public HeartCard(int value)
            : base(value) { }

        public override string ToString()
        {
            return "♥️" + GetValueString();
        }
    }

    /// <summary>
    /// * 方片牌 27-39
    /// </summary>
    public sealed class DiamondCard : Card
    {
        public DiamondCard(int value)
            : base(value) { }

        public override string ToString()
        {
            return "♦️" + GetValueString();
        }
    }

    /// <summary>
    /// * 黑桃牌 14-26
    /// </summary>
    public sealed class SpadeCard : Card
    {
        public SpadeCard(int value)
            : base(value) { }

        public override string ToString()
        {
            return "♠️" + GetValueString();
        }
    }

    /// <summary>
    /// * 梅花牌 40-53
    /// </summary>
    public sealed class ClubsCard : Card
    {
        public ClubsCard(int value)
            : base(value) { }

        public override string ToString()
        {
            return "♣️" + GetValueString();
        }
    }
}
