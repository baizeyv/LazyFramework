using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

public enum Suit
{
    Hearts,
    Diamonds,
    Clubs,
    Spades,
}

public class Card : IEquatable<Card>
{
    public Suit Suit { get; set; }
    public int Rank { get; set; } // 1 = Ace, 13 = King

    public bool Equals(Card other)
    {
        return Suit == other.Suit && Rank == other.Rank;
    }

    public override int GetHashCode()
    {
        return (Suit.GetHashCode() * 397) ^ Rank;
    }

    public override string ToString()
    {
        return $"{Rank} of {Suit}";
    }
}

public class Pile : IEquatable<Pile>
{
    public List<Card> Cards { get; set; } = new();
    public int VisibleIndex { get; set; }

    public bool Equals(Pile other)
    {
        if (Cards.Count != other.Cards.Count || VisibleIndex != other.VisibleIndex)
            return false;

        for (var i = 0; i < Cards.Count; i++)
            if (!Cards[i].Equals(other.Cards[i]))
                return false;
        return true;
    }

    public override int GetHashCode()
    {
        var hash = VisibleIndex;
        foreach (var card in Cards)
            hash = hash * 31 + card.GetHashCode();
        return hash;
    }

    public Pile Clone()
    {
        return new Pile { Cards = new List<Card>(Cards), VisibleIndex = VisibleIndex };
    }
}

public class GameState : IEquatable<GameState>
{
    public List<Pile> Piles { get; set; } = new(10);
    public List<Card> DrawPile { get; set; } = new();
    public int CompletedSequences { get; set; }

    public GameState Clone()
    {
        return new GameState
        {
            Piles = Piles.Select(p => p.Clone()).ToList(),
            DrawPile = new List<Card>(DrawPile),
            CompletedSequences = CompletedSequences,
        };
    }

    public bool Equals(GameState other)
    {
        if (other == null)
            return false;
        if (CompletedSequences != other.CompletedSequences)
            return false;
        if (DrawPile.Count != other.DrawPile.Count)
            return false;

        for (var i = 0; i < DrawPile.Count; i++)
            if (!DrawPile[i].Equals(other.DrawPile[i]))
                return false;

        for (var i = 0; i < 10; i++)
            if (!Piles[i].Equals(other.Piles[i]))
                return false;

        return true;
    }

    public override int GetHashCode()
    {
        var hash = CompletedSequences;
        foreach (var card in DrawPile)
            hash = hash * 31 + card.GetHashCode();
        foreach (var pile in Piles)
            hash = hash * 31 + pile.GetHashCode();
        return hash;
    }
}

public class SpiderSolver
{
    public GameState Solve(GameState initialState, bool isSingleSuit)
    {
        var queue = new Queue<GameState>();
        var visited = new HashSet<GameState>();

        queue.Enqueue(initialState);
        visited.Add(initialState.Clone());

        while (queue.Count > 0)
        {
            var currentState = queue.Dequeue();

            if (currentState.CompletedSequences >= 8)
                return currentState;

            // 处理所有可能的移动
            foreach (var newState in GenerateAllMoves(currentState, isSingleSuit))
                if (!visited.Contains(newState))
                {
                    visited.Add(newState.Clone());
                    queue.Enqueue(newState);
                }

            // 处理发牌操作
            if (currentState.DrawPile.Count >= 10)
            {
                var dealtState = DealCards(currentState.Clone());
                dealtState = RemoveCompletedSequences(dealtState);
                if (!visited.Contains(dealtState))
                {
                    visited.Add(dealtState.Clone());
                    queue.Enqueue(dealtState);
                }
            }
        }

        return null; // 无解
    }

    private IEnumerable<GameState> GenerateAllMoves(GameState state, bool isSingleSuit)
    {
        for (var from = 0; from < 10; from++)
        {
            var fromPile = state.Piles[from];
            if (fromPile.Cards.Count == 0)
                continue;

            var visibleCards = fromPile.Cards.Skip(fromPile.VisibleIndex).ToList();
            if (visibleCards.Count == 0)
                continue;

            for (var moveLength = 1; moveLength <= visibleCards.Count; moveLength++)
            {
                var sequence = visibleCards.Take(moveLength).ToList();
                if (IsValidSequence(sequence, isSingleSuit))
                    for (var to = 0; to < 10; to++)
                    {
                        if (to == from)
                            continue;

                        if (CanMoveTo(sequence, state.Piles[to]))
                        {
                            var newState = ApplyMove(state.Clone(), from, to, moveLength);
                            newState = RemoveCompletedSequences(newState);
                            yield return newState;
                        }
                    }
            }
        }
    }

    private bool IsValidSequence(List<Card> sequence, bool isSingleSuit)
    {
        var expectedSuit = sequence[0].Suit;
        var expectedRank = sequence[0].Rank;

        for (var i = 1; i < sequence.Count; i++)
        {
            expectedRank--;
            if (sequence[i].Rank != expectedRank)
                return false;
            if (isSingleSuit && sequence[i].Suit != expectedSuit)
                return false;
            if (!isSingleSuit && sequence[i].Suit != expectedSuit)
                return false;
        }

        return true;
    }

    private bool CanMoveTo(List<Card> sequence, Pile toPile)
    {
        if (toPile.Cards.Count == 0)
            return true;

        var topCard = toPile.Cards.Last();
        var bottomCard = sequence.First();
        return topCard.Rank == bottomCard.Rank + 1 && topCard.Suit == bottomCard.Suit;
    }

    private GameState ApplyMove(GameState state, int from, int to, int moveLength)
    {
        var fromPile = state.Piles[from];
        var toPile = state.Piles[to];

        var removeIndex = fromPile.VisibleIndex;
        var movedCards = fromPile.Cards.Skip(removeIndex).Take(moveLength).ToList();

        // 更新原牌堆
        fromPile.Cards = fromPile.Cards.Take(removeIndex).ToList();
        if (fromPile.Cards.Count > 0)
            fromPile.VisibleIndex = Math.Max(0, fromPile.VisibleIndex - moveLength);

        // 更新目标牌堆
        toPile.Cards.AddRange(movedCards);
        toPile.VisibleIndex = toPile.Cards.Count - 1;

        return state;
    }

    private GameState DealCards(GameState state)
    {
        if (state.DrawPile.Count < 10)
            return state;

        for (var i = 0; i < 10; i++)
        {
            var card = state.DrawPile.First();
            state.DrawPile.RemoveAt(0);
            state.Piles[i].Cards.Add(card);
            state.Piles[i].VisibleIndex = state.Piles[i].Cards.Count - 1;
        }

        return state;
    }

    private GameState RemoveCompletedSequences(GameState state)
    {
        foreach (var pile in state.Piles)
            while (pile.Cards.Count >= 13)
            {
                var startIndex = Math.Max(0, pile.Cards.Count - 13);
                var candidate = pile.Cards.Skip(startIndex).Take(13).ToList();

                if (IsCompleteSequence(candidate))
                {
                    pile.Cards = pile.Cards.Take(startIndex).ToList();
                    state.CompletedSequences++;
                    pile.VisibleIndex = pile.Cards.Count > 0 ? pile.Cards.Count - 1 : 0;
                }
                else
                {
                    break;
                }
            }

        return state;
    }

    private bool IsCompleteSequence(List<Card> sequence)
    {
        if (sequence.Count != 13)
            return false;
        var suit = sequence[0].Suit;

        for (var i = 0; i < 13; i++)
            if (sequence[i].Suit != suit || sequence[i].Rank != 13 - i)
                return false;
        return true;
    }
}

// 使用示例
internal class Program
{
    [MenuItem("Spider/Test main")]
    private static void Main()
    {
        // 初始化游戏状态（需要根据具体游戏设置填充）
        var initialState = new GameState();
        // 填充牌堆和发牌堆...

        var solver = new SpiderSolver();
        var solution = solver.Solve(initialState, true);

        if (solution != null)
            Console.WriteLine("Solution found!");
        else
            Console.WriteLine("No solution exists.");
    }
}
