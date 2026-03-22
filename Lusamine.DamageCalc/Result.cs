using System;
using Lusamine.DamageCalc.Data;

namespace Lusamine.DamageCalc {
  /// <summary>
  /// Holds the outcome of a damage calculation: the damage rolls, the post-calc
  /// participant state, and helpers for descriptions, KO chance, recoil, and recovery.
  /// </summary>
  public sealed class Result {
    /// <summary>The generation data context used for this calculation.</summary>
    public IGeneration Gen { get; }
    /// <summary>The attacking Pokémon as modified by the calculation (e.g. Meteor Beam boosts).</summary>
    public Pokemon Attacker { get; }
    /// <summary>The defending Pokémon as modified by the calculation.</summary>
    public Pokemon Defender { get; }
    /// <summary>The move as used in the calculation.</summary>
    public Move Move { get; }
    /// <summary>The field as modified by the calculation (e.g. screens broken by Brick Break).</summary>
    public Field Field { get; }
    /// <summary>
    /// Raw damage output. May be <c>int[]</c> (16-roll standard), <c>int</c> (fixed damage),
    /// or <c>int[][]</c> (multi-hit, one array per hit).
    /// </summary>
    public object Damage { get; set; }
    /// <summary>Structured description components used to build the description string.</summary>
    public RawDesc RawDesc { get; }

    public Result(IGeneration gen, Pokemon attacker, Pokemon defender, Move move, Field field, object damage, RawDesc rawDesc) {
      Gen = gen;
      Attacker = attacker;
      Defender = defender;
      Move = move;
      Field = field;
      Damage = damage;
      RawDesc = rawDesc;
    }

    /// <summary>
    /// Returns the full description string in percent notation
    /// (e.g. <c>"252+ SpA Life Orb Gengar Sludge Bomb vs. 100 HP / 100 SpD Eviolite Chansey: 204-242 (30.6 - 36.3%)"</c>).
    /// Equivalent to <c>FullDesc()</c>.
    /// </summary>
    public string Desc() {
      return FullDesc();
    }

    /// <summary>
    /// Returns the minimum and maximum damage values summed across all hits.
    /// </summary>
    public (int min, int max) Range() {
      return DamageUtil.DamageRange(Damage);
    }

    /// <summary>
    /// Returns the full description string.
    /// </summary>
    /// <param name="notation"><c>"%"</c> for percentage (default) or <c>"px"</c> for pixel (HP) notation.</param>
    /// <param name="err">When <c>true</c>, appends the damage roll variance information.</param>
    public string FullDesc(string notation = "%", bool err = true) {
      return DescUtil.Display(Gen, Attacker, Defender, Move, Field, Damage, RawDesc, notation, err);
    }

    /// <summary>Returns a short move description (damage range only, no Pokémon info).</summary>
    /// <param name="notation"><c>"%"</c> or <c>"px"</c>.</param>
    public string MoveDesc(string notation = "%") {
      return DescUtil.DisplayMove(Gen, Attacker, Defender, Move, Damage, notation);
    }

    /// <summary>
    /// Returns the HP recovery percentages and a summary text
    /// (e.g. for draining moves or items).
    /// </summary>
    /// <param name="notation"><c>"%"</c> or <c>"px"</c>.</param>
    public (double[] recovery, string text) Recovery(string notation = "%") {
      return DescUtil.GetRecovery(Gen, Attacker, Defender, Move, Damage, notation);
    }

    /// <summary>
    /// Returns the recoil damage percentages and a summary text.
    /// </summary>
    /// <param name="notation"><c>"%"</c> or <c>"px"</c>.</param>
    public (double[] recoil, string text) Recoil(string notation = "%") {
      return DescUtil.GetRecoil(Gen, Attacker, Defender, Move, Damage, notation);
    }

    /// <summary>
    /// Returns the KO chance, the number of hits required to KO (OHKO / 2HKO / …),
    /// and a human-readable text (e.g. <c>"guaranteed OHKO"</c>, <c>"52.9% chance to 3HKO"</c>).
    /// </summary>
    /// <param name="err">Whether to account for damage roll variance.</param>
    public (double chance, int n, string text) Kochance(bool err = true) {
      return DescUtil.GetKOChance(Gen, Attacker, Defender, Move, Field, Damage, err);
    }
  }

  public static class DamageUtil {
    public static (int min, int max) DamageRange(object damage) {
      var range = MultiDamageRange(damage);
      if (range.min is int && range.max is int) return ((int)range.min, (int)range.max);
      var mins = (int[])range.min;
      var maxs = (int[])range.max;
      var summedMin = 0;
      var summedMax = 0;
      for (var i = 0; i < mins.Length; i++) {
        summedMin += mins[i];
        summedMax += maxs[i];
      }
      return (summedMin, summedMax);
    }

    public static (object min, object max) MultiDamageRange(object damage) {
      if (damage is int fixedDamage) return (fixedDamage, fixedDamage);
      if (damage is int[][] multiHitDamage) {
        var mins = new int[multiHitDamage.Length];
        var maxs = new int[multiHitDamage.Length];
        for (var i = 0; i < multiHitDamage.Length; i++) {
          mins[i] = multiHitDamage[i][0];
          maxs[i] = multiHitDamage[i][multiHitDamage[i].Length - 1];
        }
        return (mins, maxs);
      }
      if (damage is int[] dmg) {
        if (dmg.Length < 16) return (dmg, dmg);
        return (dmg[0], dmg[dmg.Length - 1]);
      }
      throw new InvalidOperationException("Unknown damage format");
    }
  }
}
