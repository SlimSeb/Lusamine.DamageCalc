using System;
using Lusamine.DamageCalc.Data;

namespace Lusamine.DamageCalc {
  public sealed class Result {
    public IGeneration Gen { get; }
    public Pokemon Attacker { get; }
    public Pokemon Defender { get; }
    public Move Move { get; }
    public Field Field { get; }
    public object Damage { get; set; }
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

    public string Desc() {
      return FullDesc();
    }

    public (int min, int max) Range() {
      return DamageUtil.DamageRange(Damage);
    }

    public string FullDesc(string notation = "%", bool err = true) {
      return DescUtil.Display(Gen, Attacker, Defender, Move, Field, Damage, RawDesc, notation, err);
    }

    public string MoveDesc(string notation = "%") {
      return DescUtil.DisplayMove(Gen, Attacker, Defender, Move, Damage, notation);
    }

    public (double[] recovery, string text) Recovery(string notation = "%") {
      return DescUtil.GetRecovery(Gen, Attacker, Defender, Move, Damage, notation);
    }

    public (double[] recoil, string text) Recoil(string notation = "%") {
      return DescUtil.GetRecoil(Gen, Attacker, Defender, Move, Damage, notation);
    }

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
