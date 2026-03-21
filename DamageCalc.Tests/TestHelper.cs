using System;
using System.Collections.Generic;
using DamageCalc.Data;
using NUnit.Framework;

namespace DamageCalc.Tests {
  public sealed class ResultBreakdown {
    public (int min, int max)? Range { get; set; }
    public string? Desc { get; set; }
    public string? Result { get; set; }
  }

  public static class TestHelper {
    private static readonly Dictionary<int, IGeneration> Generations = new Dictionary<int, IGeneration>();

    public static IGeneration Gen(int gen) {
      if (!Generations.TryGetValue(gen, out var g)) {
        g = DataIndex.Create(gen);
        Generations[gen] = g;
      }
      return g;
    }

    public static Pokemon Pokemon(int gen, string name, State.Pokemon? options = null) {
      return new Pokemon(Gen(gen), name, options);
    }

    public static Move Move(int gen, string name, State.Move? options = null) {
      return new Move(Gen(gen), name, options);
    }

    public static Field Field(State.Field? field = null) {
      return new Field(field);
    }

    public static Side Side(State.Side? side = null) {
      return new Side(side);
    }

    public static Result Calculate(int gen, Pokemon attacker, Pokemon defender, Move move, Field? field = null) {
      return Calc.Calculate(Gen(gen), attacker, defender, move, field);
    }

    public static void AssertMatch(Result result, int gen, Dictionary<int, ResultBreakdown> diff, string notation = "%") {
      if (diff == null || diff.Count == 0) throw new ArgumentException("diff is empty", nameof(diff));

      ResultBreakdown? expected = null;
      foreach (var key in new SortedSet<int>(diff.Keys)) {
        if (key > gen) break;
        expected = diff[key];
      }
      if (expected == null) throw new ArgumentException($"No diff entry for gen {gen}");

      if (expected.Range.HasValue) {
        Assert.That(result.Range(), Is.EqualTo(expected.Range.Value));
      }
      if (!string.IsNullOrEmpty(expected.Desc)) {
        var r = result.FullDesc(notation).Split(new[] { ": " }, StringSplitOptions.None)[0];
        Assert.That(r, Is.EqualTo(expected.Desc));
      }
      if (!string.IsNullOrEmpty(expected.Result)) {
        var post = result.FullDesc(notation).Split(new[] { ": " }, StringSplitOptions.None)[1];
        var r = $"({post.Split('(')[1]}";
        Assert.That(r, Is.EqualTo(expected.Result));
      }
    }
  }
}
