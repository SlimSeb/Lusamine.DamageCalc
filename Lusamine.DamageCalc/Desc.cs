using System;
using System.Collections.Generic;
using Lusamine.DamageCalc.Data;
using Lusamine.DamageCalc.Mechanics;

namespace Lusamine.DamageCalc {
  public sealed class RawDesc {
    public string? HPEVs { get; set; }
    public int? AttackBoost { get; set; }
    public string? AttackEVs { get; set; }
    public string? AttackerAbility { get; set; }
    public string? AttackerItem { get; set; }
    public string AttackerName { get; set; } = "";
    public string? AttackerTera { get; set; }
    public string? DefenderAbility { get; set; }
    public string? DefenderItem { get; set; }
    public string DefenderName { get; set; } = "";
    public string? DefenderTera { get; set; }
    public int? DefenseBoost { get; set; }
    public string? DefenseEVs { get; set; }
    public int? Hits { get; set; }
    public int? AlliesFainted { get; set; }
    public bool? IsStellarFirstUse { get; set; }
    public bool? IsBeadsOfRuin { get; set; }
    public bool? IsSwordOfRuin { get; set; }
    public bool? IsTabletsOfRuin { get; set; }
    public bool? IsVesselOfRuin { get; set; }
    public bool? IsAuroraVeil { get; set; }
    public bool? IsFlowerGiftAttacker { get; set; }
    public bool? IsFlowerGiftDefender { get; set; }
    public bool? IsPowerTrickAttacker { get; set; }
    public bool? IsPowerTrickDefender { get; set; }
    public bool? IsSteelySpiritAttacker { get; set; }
    public bool? IsFriendGuard { get; set; }
    public bool? IsHelpingHand { get; set; }
    public bool? IsCritical { get; set; }
    public bool? IsLightScreen { get; set; }
    public bool? IsBurned { get; set; }
    public bool? IsProtected { get; set; }
    public bool? IsReflect { get; set; }
    public bool? IsBattery { get; set; }
    public bool? IsPowerSpot { get; set; }
    public bool? IsWonderRoom { get; set; }
    public string? IsSwitching { get; set; }
    public double? MoveBP { get; set; }
    public string MoveName { get; set; } = "";
    public string? MoveTurns { get; set; }
    public string? MoveType { get; set; }
    public string? Rivalry { get; set; }
    public string? Terrain { get; set; }
    public string? Weather { get; set; }
    public bool? IsDefenderDynamaxed { get; set; }
  }

  public static class DescUtil {
    public static string Display(
      IGeneration gen,
      Pokemon attacker,
      Pokemon defender,
      Move move,
      Field field,
      object damage,
      RawDesc rawDesc,
      string notation = "%",
      bool err = true
    ) {
      var range = DamageUtil.DamageRange(damage);
      var minDisplay = ToDisplay(notation, range.min, defender.MaxHP());
      var maxDisplay = ToDisplay(notation, range.max, defender.MaxHP());

      var desc = BuildDescription(rawDesc, attacker, defender);
      var damageText = $"{range.min}-{range.max} ({FormatDisplay(minDisplay)} - {FormatDisplay(maxDisplay)}{notation})";

      if (move.Category == MoveCategories.Status && !move.Named("Nature Power")) return $"{desc}: {damageText}";

      var ko = GetKOChance(gen, attacker, defender, move, field, damage, err);
      if (!string.IsNullOrEmpty(ko.text)) return $"{desc}: {damageText} -- {ko.text}";
      return $"{desc}: {damageText}";
    }

    public static string DisplayMove(
      IGeneration gen,
      Pokemon attacker,
      Pokemon defender,
      Move move,
      object damage,
      string notation = "%"
    ) {
      var range = DamageUtil.DamageRange(damage);
      var minDisplay = ToDisplay(notation, range.min, defender.MaxHP());
      var maxDisplay = ToDisplay(notation, range.max, defender.MaxHP());

      var recoveryText = GetRecovery(gen, attacker, defender, move, damage, notation).text;
      var recoilText = GetRecoil(gen, attacker, defender, move, damage, notation).text;

      var extra = "";
      if (!string.IsNullOrEmpty(recoveryText)) extra += $" ({recoveryText})";
      if (!string.IsNullOrEmpty(recoilText)) extra += $" ({recoilText})";
      return $"{FormatDisplay(minDisplay)} - {FormatDisplay(maxDisplay)}{notation}{extra}";
    }

    public static (double[] recovery, string text) GetRecovery(
      IGeneration gen,
      Pokemon attacker,
      Pokemon defender,
      Move move,
      object damage,
      string notation = "%"
    ) {
      var range = DamageUtil.DamageRange(damage);
      var minDamage = range.min;
      var maxDamage = range.max;

      int[] minD;
      int[] maxD;
      if ((move.TimesUsed ?? 1) > 1) {
        var multi = DamageUtil.MultiDamageRange(damage);
        minD = (int[])multi.min;
        maxD = (int[])multi.max;
      } else {
        minD = new[] { minDamage };
        maxD = new[] { maxDamage };
      }

      var recovery = new[] { 0.0, 0.0 };
      var text = "";

      var ignoresShellBell = gen.Num == 3 && move.Named("Doom Desire", "Future Sight");
      if (attacker.HasItem("Shell Bell") && !ignoresShellBell) {
        for (var i = 0; i < minD.Length; i++) {
          recovery[0] += minD[i] > 0 ? Math.Max(Math.Round(minD[i] / 8.0, MidpointRounding.AwayFromZero), 1) : 0;
          recovery[1] += maxD[i] > 0 ? Math.Max(Math.Round(maxD[i] / 8.0, MidpointRounding.AwayFromZero), 1) : 0;
        }
        var maxHealing = Math.Round(defender.CurHP() / 8.0, MidpointRounding.AwayFromZero);
        recovery[0] = Math.Min(recovery[0], maxHealing);
        recovery[1] = Math.Min(recovery[1], maxHealing);
      }

      if (move.Named("G-Max Finale")) {
        recovery[0] += Math.Round(attacker.MaxHP() / 6.0, MidpointRounding.AwayFromZero);
        recovery[1] += Math.Round(attacker.MaxHP() / 6.0, MidpointRounding.AwayFromZero);
      }

      if (move.Named("Pain Split")) {
        var average = (int)Math.Floor((attacker.CurHP() + defender.CurHP()) / 2.0);
        recovery[0] = average - attacker.CurHP();
        recovery[1] = recovery[0];
      }

      if (move.Drain != null) {
        if (attacker.HasAbility("Parental Bond") || move.Hits > 1) {
          var multi = DamageUtil.MultiDamageRange(damage);
          minD = (int[])multi.min;
          maxD = (int[])multi.max;
        }
        var percentHealed = move.Drain[0] / (double)move.Drain[1];
        var attackerHasBigRoot = attacker.HasItem("Big Root");
        var maxDrain = Math.Round(defender.CurHP() * percentHealed, MidpointRounding.AwayFromZero);
        if (attackerHasBigRoot) maxDrain = Math.Truncate(maxDrain * 5324 / 4096.0);
        for (var i = 0; i < minD.Length; i++) {
          var dmgRange = new[] { minD[i], maxD[i] };
          for (var j = 0; j < recovery.Length; j++) {
            var drained = Math.Max(Math.Round(dmgRange[j] * percentHealed, MidpointRounding.AwayFromZero), 1);
            if (attackerHasBigRoot) drained = Math.Truncate(drained * 5324 / 4096.0);
            recovery[j] += Math.Min(drained, maxDrain);
          }
        }
      }

      if (recovery[1] == 0) return (recovery, text);

      var minHealthRecovered = ToDisplay(notation, recovery[0], attacker.MaxHP());
      var maxHealthRecovered = ToDisplay(notation, recovery[1], attacker.MaxHP());
      var change = recovery[0] > 0 ? "recovered" : "lost";
      text = $"{FormatDisplay(minHealthRecovered)} - {FormatDisplay(maxHealthRecovered)}{notation} {change}";
      return (recovery, text);
    }

    public static (double[] recoil, string text) GetRecoil(
      IGeneration gen,
      Pokemon attacker,
      Pokemon defender,
      Move move,
      object damage,
      string notation = "%"
    ) {
      var range = DamageUtil.DamageRange(damage);
      var min = range.min;
      var max = range.max;

      var recoil = new[] { 0.0, 0.0 };
      var text = "";

      var damageOverflow = min > defender.CurHP() || max > defender.CurHP();
      if (move.Recoil != null) {
        var mod = (move.Recoil[0] / (double)move.Recoil[1]) * 100;
        double minRecoilDamage;
        double maxRecoilDamage;
        if (damageOverflow) {
          minRecoilDamage = ToDisplay(notation, defender.CurHP() * mod, attacker.MaxHP(), 100);
          maxRecoilDamage = ToDisplay(notation, defender.CurHP() * mod, attacker.MaxHP(), 100);
        } else {
          minRecoilDamage = ToDisplay(notation, Math.Min(min, defender.CurHP()) * mod, attacker.MaxHP(), 100);
          maxRecoilDamage = ToDisplay(notation, Math.Min(max, defender.CurHP()) * mod, attacker.MaxHP(), 100);
        }
        if (!attacker.HasAbility("Rock Head")) {
          recoil = new[] { minRecoilDamage, maxRecoilDamage };
          text = $"{FormatDisplay(minRecoilDamage)} - {FormatDisplay(maxRecoilDamage)}{notation} recoil damage";
        }
      } else if (move.HasCrashDamage) {
        var genMultiplier = gen.Num == 2 ? 12.5 : gen.Num >= 3 ? 50 : 1;
        double minRecoilDamage;
        double maxRecoilDamage;
        if (damageOverflow && gen.Num != 2) {
          minRecoilDamage = ToDisplay(notation, defender.CurHP() * genMultiplier, attacker.MaxHP(), 100);
          maxRecoilDamage = ToDisplay(notation, defender.CurHP() * genMultiplier, attacker.MaxHP(), 100);
        } else {
          minRecoilDamage = ToDisplay(notation, Math.Min(min, defender.MaxHP()) * genMultiplier, attacker.MaxHP(), 100);
          maxRecoilDamage = ToDisplay(notation, Math.Min(max, defender.MaxHP()) * genMultiplier, attacker.MaxHP(), 100);
        }

        recoil = new[] { minRecoilDamage, maxRecoilDamage };
        switch (gen.Num) {
          case 1:
            var oneHp = ToDisplay(notation, 1, attacker.MaxHP());
            recoil = new[] { oneHp, oneHp };
            text = "1hp damage on miss";
            break;
          case 2:
          case 3:
          case 4:
            if (defender.HasType("Ghost")) {
              if (gen.Num == 4) {
                var gen4CrashDamage = Math.Floor(((defender.MaxHP() * 0.5) / attacker.MaxHP()) * 100);
                var val = notation == "%" ? gen4CrashDamage : Math.Floor((gen4CrashDamage / 100.0) * 48);
                recoil = new[] { val, val };
                text = $"{FormatDisplay(gen4CrashDamage)}% crash damage";
              } else {
                recoil = new[] { 0.0, 0.0 };
                text = "no crash damage on Ghost types";
              }
            } else {
              text = $"{FormatDisplay(minRecoilDamage)} - {FormatDisplay(maxRecoilDamage)}{notation} crash damage on miss";
            }
            break;
          default:
            var defaultVal = notation == "%" ? 24.0 : 50.0;
            recoil = new[] { defaultVal, defaultVal };
            text = "50% crash damage";
            break;
        }
      } else if (move.StruggleRecoil) {
        var val = notation == "%" ? 12.0 : 25.0;
        recoil = new[] { val, val };
        text = "25% struggle damage";
        if (gen.Num == 4) text += " (rounded down)";
      } else if (move.MindBlownRecoil) {
        var val = notation == "%" ? 24.0 : 50.0;
        recoil = new[] { val, val };
        text = "50% recoil damage";
      }

      return (recoil, text);
    }

    public static (double chance, int n, string text) GetKOChance(
      IGeneration gen,
      Pokemon attacker,
      Pokemon defender,
      Move move,
      Field field,
      object damageObj,
      bool err = true
    ) {
      var combined = Combine(damageObj);
      var damage = combined.damage;
      var approximate = combined.approximate;

      if (double.IsNaN(damage[0])) {
        Util.Error(err, "damage[0] must be a number.");
        return (0, 0, "");
      }
      if (damage[damage.Length - 1] == 0) {
        Util.Error(err, "damage[damage.length - 1] === 0.");
        return (0, 0, "");
      }

      move.TimesUsed ??= 1;
      move.TimesUsedWithMetronome ??= 1;

      if (damage[0] >= defender.MaxHP() && move.TimesUsed == 1 && move.TimesUsedWithMetronome == 1) {
        return (1, 1, "guaranteed OHKO");
      }

      var hazards = GetHazards(gen, defender, field.DefenderSide);
      var eot = GetEndOfTurn(gen, attacker, defender, move, field);
      var toxicCounter = defender.HasStatus("tox") && !defender.HasAbility("Magic Guard", "Poison Heal")
        ? defender.ToxicCounter : 0;

      var qualifier = approximate ? "approx. " : "";

      var hazardsText = hazards.texts.Count > 0 ? " after " + SerializeText(hazards.texts) : "";
      var afterList = new List<string>();
      afterList.AddRange(hazards.texts);
      afterList.AddRange(eot.texts);
      var afterText = afterList.Count > 0 ? " after " + SerializeText(afterList) : "";
      var afterTextNoHazards = eot.texts.Count > 0 ? " after " + SerializeText(eot.texts) : "";

      double RoundChance(double chance) {
        return Math.Max(Math.Min(Math.Round(chance * 1000, MidpointRounding.AwayFromZero), 999), 1) / 10.0;
      }

      (double chance, int n, string text) KOChance(double? chanceWithoutEot, double? chanceWithEot, int n, bool multipleTurns = false) {
        var koTurnText = n == 1 ? "OHKO" : (multipleTurns ? $"KO in {n} turns" : $"{n}HKO");
        var text = qualifier;
        double? chance = null;

        if (!chanceWithoutEot.HasValue || !chanceWithEot.HasValue) {
          text += $"possible {koTurnText}";
        } else if (chanceWithoutEot.Value + chanceWithEot.Value == 0) {
          chance = 0;
          text += "not a KO";
        } else if (chanceWithoutEot.Value == 1) {
          chance = chanceWithoutEot;
          text = "guaranteed ";
          text += $"OHKO{hazardsText}";
        } else if (chanceWithoutEot.Value > 0) {
          chance = chanceWithEot;
          if (chanceWithEot == 1) {
            text += $"{RoundChance(chanceWithoutEot.Value)}% chance to {koTurnText}{hazardsText} (guaranteed {koTurnText}{afterTextNoHazards})";
          } else if (chanceWithEot > chanceWithoutEot) {
            text += $"{RoundChance(chanceWithoutEot.Value)}% chance to {koTurnText}{hazardsText} ({qualifier}{RoundChance(chanceWithEot.Value)}% chance to {koTurnText}{afterTextNoHazards})";
          } else if (chanceWithoutEot.Value > 0) {
            text += $"{RoundChance(chanceWithoutEot.Value)}% chance to {koTurnText}{hazardsText}";
          }
        } else if (chanceWithoutEot.Value == 0) {
          chance = chanceWithEot;
          if (chanceWithEot == 1) {
            text = "guaranteed ";
            text += $"{koTurnText}{afterText}";
          } else if (chanceWithEot > 0) {
            text += $"{RoundChance(chanceWithEot.Value)}% chance to {koTurnText}{afterText}";
          }
        }
        return (chance ?? 0, n, text);
      }

      if ((move.TimesUsed == 1 && move.TimesUsedWithMetronome == 1) || move.IsZ) {
        var chance = ComputeKOChance(damage, defender.CurHP() - hazards.damage, 0, 1, 1, defender.MaxHP(), 0);
        var chanceWithEot = ComputeKOChance(damage, defender.CurHP() - hazards.damage, eot.damage, 1, 1, defender.MaxHP(), toxicCounter);

        if (chance + chanceWithEot > 0) return KOChance(chance, chanceWithEot, 1);

        for (var i = 2; i <= 4; i++) {
          var c = ComputeKOChance(damage, defender.CurHP() - hazards.damage, eot.damage, i, 1, defender.MaxHP(), toxicCounter);
          if (c > 0) return KOChance(0, c, i);
        }

        for (var i = 5; i <= 9; i++) {
          if (PredictTotal(damage[0], eot.damage, i, 1, toxicCounter, defender.MaxHP()) >= defender.CurHP() - hazards.damage) {
            return KOChance(0, 1, i);
          } else if (PredictTotal(damage[damage.Length - 1], eot.damage, i, 1, toxicCounter, defender.MaxHP()) >= defender.CurHP() - hazards.damage) {
            return KOChance(null, null, i);
          }
        }
      } else {
        var chance = ComputeKOChance(
          damage,
          defender.MaxHP() - hazards.damage,
          eot.damage,
          move.Hits == 0 ? 1 : move.Hits,
          move.TimesUsed ?? 1,
          defender.MaxHP(),
          toxicCounter
        );
        if (chance > 0) return KOChance(0, chance, move.TimesUsed ?? 1, chance == 1);

        if (PredictTotal(damage[0], eot.damage, 1, move.TimesUsed ?? 1, toxicCounter, defender.MaxHP()) >= defender.CurHP() - hazards.damage) {
          return KOChance(0, 1, move.TimesUsed ?? 1, true);
        } else if (PredictTotal(damage[damage.Length - 1], eot.damage, 1, move.TimesUsed ?? 1, toxicCounter, defender.MaxHP()) >= defender.CurHP() - hazards.damage) {
          return KOChance(null, null, move.TimesUsed ?? 1, true);
        }
        return KOChance(0, 0, move.TimesUsed ?? 1);
      }

      return (0, 0, "");
    }

    private static (int[] damage, bool approximate) Combine(object damage) {
      if (damage is int fixedDamage) return (new[] { fixedDamage }, false);
      if (damage is int[] standard && standard.Length >= 16) return (standard, false);
      if (damage is int[] fixedMulti && fixedMulti.Length == 2) return (new[] { fixedMulti[0] + fixedMulti[1] }, false);

      int[] Reduce(int[] dist, int scaleValue) {
        var newLength = dist.Length / scaleValue;
        var reduced = new int[newLength];
        reduced[0] = dist[0];
        reduced[newLength - 1] = dist[dist.Length - 1];
        for (var i = 1; i < newLength - 1; i++) {
          reduced[i] = dist[(int)Math.Round(i * scaleValue + scaleValue / 2.0)];
        }
        return reduced;
      }

      int[] CombineTwo(int[] dist1, int[] dist2) {
        var combined = new List<int>();
        foreach (var v1 in dist1) {
          foreach (var v2 in dist2) combined.Add(v1 + v2);
        }
        combined.Sort();
        return combined.ToArray();
      }

      (int[] dist, bool approximate) CombineDistributions(List<int[]> dists) {
        var combined = new[] { 0 };
        var numRolls = dists[0].Length;
        var numAccuracy = (numRolls == 16 && dists.Count == 3) ? 3 : 2;
        var approximate = false;
        for (var i = 0; i < dists.Count; i++) {
          var distribution = dists[i];
          combined = CombineTwo(combined, distribution);
          if (i >= numAccuracy) {
            combined = Reduce(combined, distribution.Length);
            approximate = true;
          }
        }
        return (combined, approximate);
      }

      if (damage is int[][] multi) {
        var dists = new List<int[]>(multi);
        return CombineDistributions(dists);
      }

      throw new InvalidOperationException("Unknown damage format");
    }

    private static readonly string[] TRAPPING = {
      "Bind", "Clamp", "Fire Spin", "Infestation", "Magma Storm", "Sand Tomb",
      "Thunder Cage", "Whirlpool", "Wrap", "G-Max Sandblast", "G-Max Centiferno",
    };

    private static (int damage, List<string> texts) GetHazards(IGeneration gen, Pokemon defender, Side defenderSide) {
      var damage = 0;
      var texts = new List<string>();

      if (defender.HasItem("Heavy-Duty Boots")) return (damage, texts);

      if (defenderSide.IsSR && !defender.HasAbility("Magic Guard", "Mountaineer")) {
        var rockType = gen.Types.Get("rock")!;
        double effectiveness;
        if (!string.IsNullOrEmpty(defender.TeraType) && defender.TeraType != "Stellar") {
          effectiveness = rockType.Effectiveness[defender.TeraType];
        } else {
          effectiveness = rockType.Effectiveness[defender.Types[0]] * (defender.Types.Length > 1 ? rockType.Effectiveness[defender.Types[1]] : 1);
        }
        damage += (int)Math.Floor((effectiveness * defender.MaxHP()) / 8.0);
        texts.Add("Stealth Rock");
      }

      if (defenderSide.Steelsurge && !defender.HasAbility("Magic Guard", "Mountaineer")) {
        var steelType = gen.Types.Get("steel")!;
        double effectiveness;
        if (!string.IsNullOrEmpty(defender.TeraType) && defender.TeraType != "Stellar") {
          effectiveness = steelType.Effectiveness[defender.TeraType];
        } else {
          effectiveness = steelType.Effectiveness[defender.Types[0]] * (defender.Types.Length > 1 ? steelType.Effectiveness[defender.Types[1]] : 1);
        }
        damage += (int)Math.Floor((effectiveness * defender.MaxHP()) / 8.0);
        texts.Add("Steelsurge");
      }

      if (!defender.HasType("Flying") &&
          !defender.HasAbility("Magic Guard", "Levitate") &&
          !defender.HasItem("Air Balloon")) {
        if (defenderSide.Spikes == 1) {
          damage += (int)Math.Floor(defender.MaxHP() / 8.0);
          texts.Add(gen.Num == 2 ? "Spikes" : "1 layer of Spikes");
        } else if (defenderSide.Spikes == 2) {
          damage += (int)Math.Floor(defender.MaxHP() / 6.0);
          texts.Add("2 layers of Spikes");
        } else if (defenderSide.Spikes == 3) {
          damage += (int)Math.Floor(defender.MaxHP() / 4.0);
          texts.Add("3 layers of Spikes");
        }
      }

      if (double.IsNaN(damage)) damage = 0;
      return (damage, texts);
    }

    private static (int damage, List<string> texts) GetEndOfTurn(
      IGeneration gen,
      Pokemon attacker,
      Pokemon defender,
      Move move,
      Field field
    ) {
      var damage = 0;
      var texts = new List<string>();

      var loseItem = move.Named("Knock Off") && !defender.HasAbility("Sticky Hold");
      var healBlock = move.Named("Psychic Noise") &&
        !(attacker.HasAbility("Sheer Force") || defender.HasItem("Covert Cloak") || defender.HasAbility("Shield Dust", "Aroma Veil"));

      if (field.HasWeather("Sun", "Harsh Sunshine")) {
        if (defender.HasAbility("Dry Skin", "Solar Power")) {
          damage -= (int)Math.Floor(defender.MaxHP() / 8.0);
          texts.Add(defender.Ability + " damage");
        }
      } else if (field.HasWeather("Rain", "Heavy Rain") && !healBlock) {
        if (defender.HasAbility("Dry Skin")) {
          damage += (int)Math.Floor(defender.MaxHP() / 8.0);
          texts.Add("Dry Skin recovery");
        } else if (defender.HasAbility("Rain Dish")) {
          damage += (int)Math.Floor(defender.MaxHP() / 16.0);
          texts.Add("Rain Dish recovery");
        }
      } else if (field.HasWeather("Sand")) {
        if (!defender.HasType("Rock", "Ground", "Steel") &&
            !defender.HasAbility("Magic Guard", "Overcoat", "Sand Force", "Sand Rush", "Sand Veil") &&
            !defender.HasItem("Safety Goggles")) {
          damage -= (int)Math.Floor(defender.MaxHP() / (gen.Num == 2 ? 8.0 : 16.0));
          texts.Add("sandstorm damage");
        }
      } else if (field.HasWeather("Hail", "Snow")) {
        if (defender.HasAbility("Ice Body") && !healBlock) {
          damage += (int)Math.Floor(defender.MaxHP() / 16.0);
          texts.Add("Ice Body recovery");
        } else if (!defender.HasType("Ice") &&
                   !defender.HasAbility("Magic Guard", "Overcoat", "Snow Cloak") &&
                   !defender.HasItem("Safety Goggles") &&
                   field.HasWeather("Hail")) {
          damage -= (int)Math.Floor(defender.MaxHP() / 16.0);
          texts.Add("hail damage");
        }
      }

      if (defender.HasItem("Leftovers") && !loseItem && !healBlock) {
        damage += (int)Math.Floor(defender.MaxHP() / 16.0);
        texts.Add("Leftovers recovery");
      } else if (defender.HasItem("Black Sludge") && !loseItem) {
        if (defender.HasType("Poison")) {
          if (!healBlock) {
            damage += (int)Math.Floor(defender.MaxHP() / 16.0);
            texts.Add("Black Sludge recovery");
          }
        } else if (!defender.HasAbility("Magic Guard", "Klutz")) {
          damage -= (int)Math.Floor(defender.MaxHP() / 8.0);
          texts.Add("Black Sludge damage");
        }
      } else if (defender.HasItem("Sticky Barb") && !loseItem && !defender.HasAbility("Magic Guard", "Klutz")) {
        damage -= (int)Math.Floor(defender.MaxHP() / 8.0);
        texts.Add("Sticky Barb damage");
      }

      if (field.DefenderSide.IsSeeded) {
        if (!defender.HasAbility("Magic Guard")) {
          damage -= (int)Math.Floor(defender.MaxHP() / (gen.Num >= 2 ? 8.0 : 16.0));
          texts.Add("Leech Seed damage");
        }
      }

      if (field.AttackerSide.IsSeeded && !attacker.HasAbility("Magic Guard")) {
        var recovery = (int)Math.Floor(attacker.MaxHP() / (gen.Num >= 2 ? 8.0 : 16.0));
        if (defender.HasItem("Big Root")) recovery = (int)Math.Truncate(recovery * 5324 / 4096.0);
        if (attacker.HasAbility("Liquid Ooze")) {
          damage -= recovery;
          texts.Add("Liquid Ooze damage");
        } else if (!healBlock) {
          damage += recovery;
          texts.Add("Leech Seed recovery");
        }
      }

      if (field.HasTerrain("Grassy")) {
        if (MechanicsUtil.IsGrounded(defender, field) && !healBlock) {
          damage += (int)Math.Floor(defender.MaxHP() / 16.0);
          texts.Add("Grassy Terrain recovery");
        }
      }

      if (defender.HasStatus("psn")) {
        if (defender.HasAbility("Poison Heal")) {
          if (!healBlock) {
            damage += (int)Math.Floor(defender.MaxHP() / 8.0);
            texts.Add("Poison Heal");
          }
        } else if (!defender.HasAbility("Magic Guard")) {
          damage -= (int)Math.Floor(defender.MaxHP() / (gen.Num == 1 ? 16.0 : 8.0));
          texts.Add("poison damage");
        }
      } else if (defender.HasStatus("tox")) {
        if (defender.HasAbility("Poison Heal")) {
          if (!healBlock) {
            damage += (int)Math.Floor(defender.MaxHP() / 8.0);
            texts.Add("Poison Heal");
          }
        } else if (!defender.HasAbility("Magic Guard")) {
          texts.Add("toxic damage");
        }
      } else if (defender.HasStatus("brn")) {
        if (defender.HasAbility("Heatproof")) {
          damage -= (int)Math.Floor(defender.MaxHP() / (gen.Num > 6 ? 32.0 : 16.0));
          texts.Add("reduced burn damage");
        } else if (!defender.HasAbility("Magic Guard")) {
          damage -= (int)Math.Floor(defender.MaxHP() / (gen.Num == 1 || gen.Num > 6 ? 16.0 : 8.0));
          texts.Add("burn damage");
        }
      } else if ((defender.HasStatus("slp") || defender.HasAbility("Comatose")) &&
                 attacker.HasAbility("Bad Dreams") &&
                 !defender.HasAbility("Magic Guard")) {
        damage -= (int)Math.Floor(defender.MaxHP() / 8.0);
        texts.Add("Bad Dreams");
      }

      if (!defender.HasAbility("Magic Guard") && Array.Exists(TRAPPING, m => m == move.Name) && gen.Num > 1) {
        if (attacker.HasItem("Binding Band")) {
          damage -= gen.Num > 5 ? (int)Math.Floor(defender.MaxHP() / 6.0) : (int)Math.Floor(defender.MaxHP() / 8.0);
          texts.Add("trapping damage");
        } else {
          damage -= gen.Num > 5 ? (int)Math.Floor(defender.MaxHP() / 8.0) : (int)Math.Floor(defender.MaxHP() / 16.0);
          texts.Add("trapping damage");
        }
      }
      if (field.DefenderSide.IsSaltCured && !defender.HasAbility("Magic Guard")) {
        var isWaterOrSteel = defender.HasType("Water", "Steel");
        damage -= (int)Math.Floor(defender.MaxHP() / (isWaterOrSteel ? 4.0 : 8.0));
        texts.Add("Salt Cure");
      }
      if (!defender.HasType("Fire") && !defender.HasAbility("Magic Guard") &&
          move.Named("Fire Pledge (Grass Pledge Boosted)", "Grass Pledge (Fire Pledge Boosted)")) {
        damage -= (int)Math.Floor(defender.MaxHP() / 8.0);
        texts.Add("Sea of Fire damage");
      }

      if (!defender.HasAbility("Magic Guard") && !defender.HasType("Grass") &&
          (field.DefenderSide.Vinelash || move.Named("G-Max Vine Lash"))) {
        damage -= (int)Math.Floor(defender.MaxHP() / 6.0);
        texts.Add("Vine Lash damage");
      }

      if (!defender.HasAbility("Magic Guard") && !defender.HasType("Fire") &&
          (field.DefenderSide.Wildfire || move.Named("G-Max Wildfire"))) {
        damage -= (int)Math.Floor(defender.MaxHP() / 6.0);
        texts.Add("Wildfire damage");
      }

      if (!defender.HasAbility("Magic Guard") && !defender.HasType("Water") &&
          (field.DefenderSide.Cannonade || move.Named("G-Max Cannonade"))) {
        damage -= (int)Math.Floor(defender.MaxHP() / 6.0);
        texts.Add("Cannonade damage");
      }

      if (!defender.HasAbility("Magic Guard") && !defender.HasType("Rock") &&
          (field.DefenderSide.Volcalith || move.Named("G-Max Volcalith"))) {
        damage -= (int)Math.Floor(defender.MaxHP() / 6.0);
        texts.Add("Volcalith damage");
      }

      return (damage, texts);
    }

    private static double ComputeKOChance(int[] damage, int hp, int eot, int hits, int timesUsed, int maxHP, int toxicCounter) {
      var toxicDamage = 0;
      if (toxicCounter > 0) {
        toxicDamage = (int)Math.Floor((toxicCounter * maxHP) / 16.0);
        toxicCounter++;
      }
      var n = damage.Length;
      if (hits == 1) {
        if (eot - toxicDamage > 0) {
          eot = 0;
          toxicDamage = 0;
        }
        for (var i = 0; i < n; i++) {
          if (damage[n - 1] - eot + toxicDamage < hp) return 0;
          if (damage[i] - eot + toxicDamage >= hp) {
            return (n - i) / (double)n;
          }
        }
      }

      double sum = 0;
      double lastc = 0;
      for (var i = 0; i < n; i++) {
        double c;
        if (i == 0 || damage[i] != damage[i - 1]) {
          c = ComputeKOChance(damage, hp - damage[i] + eot - toxicDamage, eot, hits - 1, timesUsed, maxHP, toxicCounter);
        } else {
          c = lastc;
        }
        if (c == 1) {
          sum += n - i;
          break;
        } else {
          sum += c;
        }
        lastc = c;
      }
      return sum / n;
    }

    private static int PredictTotal(int damage, int eot, int hits, int timesUsed, int toxicCounter, int maxHP) {
      var toxicDamage = 0;
      var lastTurnEot = eot;
      if (toxicCounter > 0) {
        for (var i = 0; i < hits - 1; i++) {
          toxicDamage += (int)Math.Floor(((toxicCounter + i) * maxHP) / 16.0);
        }
        lastTurnEot -= (int)Math.Floor(((toxicCounter + (hits - 1)) * maxHP) / 16.0);
      }
      var total = 0;
      if (hits > 1 && timesUsed == 1) {
        total = damage * hits - eot * (hits - 1) + toxicDamage;
      } else {
        total = damage - eot * (hits - 1) + toxicDamage;
      }
      if (lastTurnEot < 0) total -= lastTurnEot;
      return total;
    }

    public static int[] SquashMultihit(IGeneration gen, int[] d, int hits, bool err = true) {
      if (d.Length == 1) {
        return new[] { d[0] * hits };
      } else if (gen.Num == 1) {
        var r = new int[d.Length];
        for (var i = 0; i < d.Length; i++) r[i] = d[i] * hits;
        return r;
      } else if (d.Length == 16) {
        switch (hits) {
          case 2:
            return new[] {
              2 * d[0], d[2] + d[3], d[4] + d[4], d[4] + d[5], d[5] + d[6], d[6] + d[6],
              d[6] + d[7], d[7] + d[7], d[8] + d[8], d[8] + d[9], d[9] + d[9], d[9] + d[10],
              d[10] + d[11], d[11] + d[11], d[12] + d[13], 2 * d[15],
            };
          case 3:
            return new[] {
              3 * d[0], d[3] + d[3] + d[4], d[4] + d[4] + d[5], d[5] + d[5] + d[6],
              d[5] + d[6] + d[6], d[6] + d[6] + d[7], d[6] + d[7] + d[7], d[7] + d[7] + d[8],
              d[7] + d[8] + d[8], d[8] + d[8] + d[9], d[8] + d[9] + d[9], d[9] + d[9] + d[10],
              d[9] + d[10] + d[10], d[10] + d[11] + d[11], d[11] + d[12] + d[12], 3 * d[15],
            };
          case 4:
            return new[] {
              4 * d[0], 4 * d[4], d[4] + d[5] + d[5] + d[5], d[5] + d[5] + d[6] + d[6],
              4 * d[6], d[6] + d[6] + d[7] + d[7], 4 * d[7], d[7] + d[7] + d[7] + d[8],
              d[7] + d[8] + d[8] + d[8], 4 * d[8], d[8] + d[8] + d[9] + d[9], 4 * d[9],
              d[9] + d[9] + d[10] + d[10], d[10] + d[10] + d[10] + d[11], 4 * d[11], 4 * d[15],
            };
          case 5:
            return new[] {
              5 * d[0], d[4] + d[4] + d[4] + d[5] + d[5], d[5] + d[5] + d[5] + d[5] + d[6],
              d[5] + d[6] + d[6] + d[6] + d[6], d[6] + d[6] + d[6] + d[6] + d[7],
              d[6] + d[6] + d[7] + d[7] + d[7], 5 * d[7], d[7] + d[7] + d[7] + d[8] + d[8],
              d[7] + d[7] + d[8] + d[8] + d[8], 5 * d[8], d[8] + d[8] + d[8] + d[9] + d[9],
              d[8] + d[9] + d[9] + d[9] + d[9], d[9] + d[9] + d[9] + d[9] + d[10],
              d[9] + d[10] + d[10] + d[10] + d[10], d[10] + d[10] + d[11] + d[11] + d[11], 5 * d[15],
            };
          case 10:
            return new[] {
              10 * d[0], 10 * d[4], 3 * d[4] + 7 * d[5], 5 * d[5] + 5 * d[6], 10 * d[6],
              5 * d[6] + 5 * d[7], 10 * d[7], 7 * d[7] + 3 * d[8], 3 * d[7] + 7 * d[8], 10 * d[8],
              5 * d[8] + 5 * d[9], 4 * d[9], 5 * d[9] + 5 * d[10], 7 * d[10] + 3 * d[11], 10 * d[11],
              10 * d[15],
            };
          default:
            Util.Error(err, $"Unexpected # of hits: {hits}");
            return d;
        }
      } else if (d.Length == 39) {
        switch (hits) {
          case 2:
            return new[] {
              2 * d[0], 2 * d[7], 2 * d[10], 2 * d[12], 2 * d[14], d[15] + d[16],
              2 * d[17], d[18] + d[19], d[19] + d[20], 2 * d[21], d[22] + d[23],
              2 * d[24], 2 * d[26], 2 * d[28], 2 * d[31], 2 * d[38],
            };
          case 3:
            return new[] {
              3 * d[0], 3 * d[9], 3 * d[12], 3 * d[13], 3 * d[15], 3 * d[16],
              3 * d[17], 3 * d[18], 3 * d[20], 3 * d[21], 3 * d[22], 3 * d[23],
              3 * d[25], 3 * d[26], 3 * d[29], 3 * d[38],
            };
          case 4:
            return new[] {
              4 * d[0], 2 * d[10] + 2 * d[11], 4 * d[13], 4 * d[14], 2 * d[15] + 2 * d[16],
              2 * d[16] + 2 * d[17], 2 * d[17] + 2 * d[18], 2 * d[18] + 2 * d[19],
              2 * d[19] + 2 * d[20], 2 * d[20] + 2 * d[21], 2 * d[21] + 2 * d[22],
              2 * d[22] + 2 * d[23], 4 * d[24], 4 * d[25], 2 * d[27] + 2 * d[28], 4 * d[38],
            };
          case 5:
            return new[] {
              5 * d[0], 5 * d[11], 5 * d[13], 5 * d[15], 5 * d[16], 5 * d[17],
              5 * d[18], 5 * d[19], 5 * d[19], 5 * d[20], 5 * d[21], 5 * d[22],
              5 * d[23], 5 * d[25], 5 * d[27], 5 * d[38],
            };
          case 10:
            return new[] {
              10 * d[0], 10 * d[11], 10 * d[13], 10 * d[15], 10 * d[16], 10 * d[17],
              10 * d[18], 10 * d[19], 10 * d[19], 10 * d[20], 10 * d[21], 10 * d[22],
              10 * d[23], 10 * d[25], 10 * d[27], 10 * d[38],
            };
          default:
            Util.Error(err, $"Unexpected # of hits: {hits}");
            return d;
        }
      } else if (d.Length == 256) {
        if (hits > 1) {
          Util.Error(err, $"Unexpected # of hits for Parental Bond: {hits}");
        }
        var r = new int[16];
        for (var i = 0; i < 16; i++) {
          var val = 0;
          for (var j = 0; j < 16; j++) val += d[i + j];
          r[i] = (int)Math.Round(val / 16.0);
        }
        return r;
      } else {
        Util.Error(err, $"Unexpected # of possible damage values: {d.Length}");
        return d;
      }
    }

    private static string BuildDescription(RawDesc description, Pokemon attacker, Pokemon defender) {
      var levels = GetDescriptionLevels(attacker, defender);
      var attackerLevel = levels.attacker;
      var defenderLevel = levels.defender;
      var output = "";
      if (description.AttackBoost.HasValue && description.AttackBoost != 0) {
        if (description.AttackBoost > 0) output += "+";
        output += description.AttackBoost + " ";
      }
      output = AppendIfSet(output, attackerLevel);
      output = AppendIfSet(output, description.AttackEVs);
      output = AppendIfSet(output, description.AttackerItem);
      output = AppendIfSet(output, description.AttackerAbility);
      output = AppendIfSet(output, description.Rivalry);
      if (description.IsBurned == true) output += "burned ";
      if (description.AlliesFainted.HasValue) {
        var count = Math.Min(5, description.AlliesFainted.Value);
        output += count + (description.AlliesFainted.Value == 1 ? " ally" : " allies") + " fainted ";
      }
      if (!string.IsNullOrEmpty(description.AttackerTera)) output += $"Tera {description.AttackerTera} ";
      if (description.IsStellarFirstUse == true) output += "(First Use) ";
      if (description.IsBeadsOfRuin == true) output += "Beads of Ruin ";
      if (description.IsSwordOfRuin == true) output += "Sword of Ruin ";
      output += description.AttackerName + " ";
      if (description.IsHelpingHand == true) output += "Helping Hand ";
      if (description.IsFlowerGiftAttacker == true) output += "with an ally's Flower Gift ";
      if (description.IsPowerTrickAttacker == true) output += "with Power Trick ";
      if (description.IsSteelySpiritAttacker == true) output += "with an ally's Steely Spirit ";
      if (description.IsBattery == true) output += "Battery boosted ";
      if (description.IsPowerSpot == true) output += "Power Spot boosted ";
      if (description.IsSwitching != null) output += "switching boosted ";
      output += description.MoveName + " ";
      if (description.MoveBP.HasValue && !string.IsNullOrEmpty(description.MoveType)) {
        output += $"({FormatBP(description.MoveBP.Value)} BP {description.MoveType}) ";
      } else if (description.MoveBP.HasValue) {
        output += $"({FormatBP(description.MoveBP.Value)} BP) ";
      } else if (!string.IsNullOrEmpty(description.MoveType)) {
        output += $"({description.MoveType}) ";
      }
      if (description.Hits.HasValue) output += $"({description.Hits} hits) ";
      output = AppendIfSet(output, description.MoveTurns);
      output += "vs. ";
      if (description.DefenseBoost.HasValue && description.DefenseBoost != 0) {
        if (description.DefenseBoost > 0) output += "+";
        output += description.DefenseBoost + " ";
      }
      output = AppendIfSet(output, defenderLevel);
      output = AppendIfSet(output, description.HPEVs);
      if (!string.IsNullOrEmpty(description.DefenseEVs)) output += "/ " + description.DefenseEVs + " ";
      output = AppendIfSet(output, description.DefenderItem);
      output = AppendIfSet(output, description.DefenderAbility);
      if (description.IsTabletsOfRuin == true) output += "Tablets of Ruin ";
      if (description.IsVesselOfRuin == true) output += "Vessel of Ruin ";
      if (description.IsProtected == true) output += "protected ";
      if (description.IsDefenderDynamaxed == true) output += "Dynamax ";
      if (!string.IsNullOrEmpty(description.DefenderTera)) output += $"Tera {description.DefenderTera} ";
      output += description.DefenderName;
      if (!string.IsNullOrEmpty(description.Weather) && !string.IsNullOrEmpty(description.Terrain)) {
        output += " in " + description.Weather + " and " + description.Terrain + " Terrain";
      } else if (!string.IsNullOrEmpty(description.Weather)) {
        output += " in " + description.Weather;
      } else if (!string.IsNullOrEmpty(description.Terrain)) {
        output += " in " + description.Terrain + " Terrain";
      }
      if (description.IsReflect == true) output += " through Reflect";
      else if (description.IsLightScreen == true) output += " through Light Screen";
      if (description.IsFlowerGiftDefender == true) output += " with an ally's Flower Gift";
      if (description.IsPowerTrickDefender == true) output += " with Power Trick";
      if (description.IsFriendGuard == true) output += " with an ally's Friend Guard";
      if (description.IsAuroraVeil == true) output += " with an ally's Aurora Veil";
      if (description.IsCritical == true) output += " on a critical hit";
      if (description.IsWonderRoom == true) output += " in Wonder Room";
      return output;
    }

    private static (string attacker, string defender) GetDescriptionLevels(Pokemon attacker, Pokemon defender) {
      if (attacker.Level != defender.Level) {
        return (
          attacker.Level == 100 ? "" : $"Lvl {attacker.Level}",
          defender.Level == 100 ? "" : $"Lvl {defender.Level}"
        );
      }
      var elide = attacker.Level == 100 || attacker.Level == 50 || attacker.Level == 5;
      var level = elide ? "" : $"Lvl {attacker.Level}";
      return (level, level);
    }

    private static string SerializeText(List<string> arr) {
      if (arr.Count == 0) return "";
      if (arr.Count == 1) return arr[0];
      if (arr.Count == 2) return arr[0] + " and " + arr[1];
      var text = "";
      for (var i = 0; i < arr.Count - 1; i++) {
        text += arr[i] + ", ";
      }
      return text + "and " + arr[arr.Count - 1];
    }

    private static string FormatBP(double bp) {
      return bp == Math.Floor(bp) ? ((int)bp).ToString() : bp.ToString("0.#");
    }

    private static string AppendIfSet(string str, string? toAppend) {
      return !string.IsNullOrEmpty(toAppend) ? $"{str}{toAppend} " : str;
    }

    private static double ToDisplay(string notation, double a, double b, double f = 1) {
      return notation == "%"
        ? Math.Floor((a * (1000 / f)) / b) / 10.0
        : Math.Floor((a * (48 / f)) / b);
    }

    private static string FormatDisplay(double value) {
      return Math.Abs(value % 1) < 0.00001 ? value.ToString("0") : value.ToString("0.0");
    }
  }
}
