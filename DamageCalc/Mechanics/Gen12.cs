using System;
using System.Collections.Generic;
using DamageCalc;
using DamageCalc.Data;

namespace DamageCalc.Mechanics {
  public static class Gen12 {
    public static Result CalculateRBYGSC(IGeneration gen, Pokemon attacker, Pokemon defender, Move move, Field field) {
      MechanicsUtil.ComputeFinalStats(gen, attacker, defender, field, StatId.Atk, StatId.Def, StatId.Spa, StatId.Spd, StatId.Spe);

      var desc = new RawDesc {
        AttackerName = attacker.Name,
        MoveName = move.Name,
        DefenderName = defender.Name,
      };

      var result = new Result(gen, attacker, defender, move, field, 0, desc);

      if (move.Category == MoveCategories.Status) return result;

      if (field.DefenderSide.IsProtected) {
        desc.IsProtected = true;
        return result;
      }

      if (move.Name == "Pain Split") {
        var average = (int)Math.Floor((attacker.CurHP() + defender.CurHP()) / 2.0);
        var damage = Math.Max(0, defender.CurHP() - average);
        result.Damage = damage;
        return result;
      }

      if (gen.Num == 1) {
        var fixedDamage = MechanicsUtil.HandleFixedDamageMoves(attacker, move);
        if (fixedDamage != 0) {
          result.Damage = fixedDamage;
          return result;
        }
      }

      var typeEffectivenessPrecedenceRules = new List<string> {
        "Normal", "Fire", "Water", "Electric", "Grass", "Ice", "Fighting", "Poison",
        "Ground", "Flying", "Psychic", "Bug", "Rock", "Ghost", "Dragon", "Dark", "Steel",
      };

      var firstDefenderType = defender.Types[0];
      string? secondDefenderType = defender.Types.Length > 1 ? defender.Types[1] : null;

      if (!string.IsNullOrEmpty(secondDefenderType) && firstDefenderType != secondDefenderType && gen.Num == 2) {
        var firstTypePrecedence = typeEffectivenessPrecedenceRules.IndexOf(firstDefenderType);
        var secondTypePrecedence = typeEffectivenessPrecedenceRules.IndexOf(secondDefenderType);
        if (firstTypePrecedence > secondTypePrecedence) {
          var tmp = firstDefenderType;
          firstDefenderType = secondDefenderType;
          secondDefenderType = tmp;
        }
      }

      var type1Effectiveness = MechanicsUtil.GetMoveEffectiveness(gen, move, firstDefenderType, field.DefenderSide.IsForesight);
      var type2Effectiveness = !string.IsNullOrEmpty(secondDefenderType)
        ? MechanicsUtil.GetMoveEffectiveness(gen, move, secondDefenderType, field.DefenderSide.IsForesight)
        : 1;
      var typeEffectiveness = type1Effectiveness * type2Effectiveness;

      if (typeEffectiveness == 0) return result;

      if (gen.Num == 2) {
        var fixedDamage = MechanicsUtil.HandleFixedDamageMoves(attacker, move);
        if (fixedDamage != 0) {
          result.Damage = fixedDamage;
          return result;
        }
      }

      if (move.Hits > 1) desc.Hits = move.Hits;

      if (move.Name == "Triple Kick") {
        move.Bp = move.Hits == 2 ? 15 : move.Hits == 3 ? 20 : 10;
        desc.MoveBP = move.Bp;
      }

      if (move.Named("Flail", "Reversal")) {
        move.IsCrit = false;
        var p = (int)Math.Floor((48 * attacker.CurHP()) / (double)attacker.MaxHP());
        move.Bp = p <= 1 ? 200 : p <= 4 ? 150 : p <= 9 ? 100 : p <= 16 ? 80 : p <= 32 ? 40 : 20;
        desc.MoveBP = move.Bp;
      } else if (move.Named("Present") && move.Bp == 0) {
        move.Bp = 40;
      }

      if (move.Bp == 0) return result;

      var isPhysical = move.Category == MoveCategories.Physical;
      var attackStat = isPhysical ? StatId.Atk : StatId.Spa;
      var defenseStat = isPhysical ? StatId.Def : StatId.Spd;
      var at = attacker.Stats[attackStat];
      var df = defender.Stats[defenseStat];

      var ignoreMods = move.IsCrit && (gen.Num == 1 || (gen.Num == 2 && attacker.Boosts[attackStat] <= defender.Boosts[defenseStat]));

      var lv = attacker.Level;
      if (ignoreMods) {
        at = attacker.RawStats[attackStat];
        df = defender.RawStats[defenseStat];
        if (gen.Num == 1) {
          lv *= 2;
          desc.IsCritical = true;
        }
      } else {
        if (attacker.Boosts[attackStat] != 0) desc.AttackBoost = attacker.Boosts[attackStat];
        if (defender.Boosts[defenseStat] != 0) desc.DefenseBoost = defender.Boosts[defenseStat];
        if (isPhysical && attacker.HasStatus("brn")) {
          at = (int)Math.Floor(at / 2.0);
          desc.IsBurned = true;
        }
      }

      if (move.Named("Explosion", "Self-Destruct")) {
        df = (int)Math.Floor(df / 2.0);
      }

      if (!ignoreMods) {
        if (isPhysical && field.DefenderSide.IsReflect) {
          df *= 2;
          desc.IsReflect = true;
        } else if (!isPhysical && field.DefenderSide.IsLightScreen) {
          df *= 2;
          desc.IsLightScreen = true;
        }
      }

      if ((attacker.Named("Pikachu") && attacker.HasItem("Light Ball") && !isPhysical) ||
          (attacker.Named("Cubone", "Marowak") && attacker.HasItem("Thick Club") && isPhysical)) {
        at *= 2;
        desc.AttackerItem = attacker.Item;
      }

      if (at > 255 || df > 255) {
        at = (int)Math.Floor(at / 4.0) % 256;
        df = (int)Math.Floor(df / 4.0) % 256;
      }

      if (move.Named("Present")) {
        var lookup = new Dictionary<string, int> {
          { "Normal", 0 }, { "Fighting", 1 }, { "Flying", 2 }, { "Poison", 3 }, { "Ground", 4 },
          { "Rock", 5 }, { "Bug", 7 }, { "Ghost", 8 }, { "Steel", 9 }, { "???", 19 },
          { "Fire", 20 }, { "Water", 21 }, { "Grass", 22 }, { "Electric", 23 }, { "Psychic", 24 },
          { "Ice", 25 }, { "Dragon", 26 }, { "Dark", 27 },
        };

        at = 10;
        df = Math.Max(lookup[attacker.Types.Length > 1 ? attacker.Types[1] : attacker.Types[0]], 1);
        lv = Math.Max(lookup[defender.Types.Length > 1 ? defender.Types[1] : defender.Types[0]], 1);
      }

      if (defender.Named("Ditto") && defender.HasItem("Metal Powder")) {
        df = (int)Math.Floor(df * 1.5);
        desc.DefenderItem = defender.Item;
      }

      if (move.Named("Low Kick") || move.Named("Grass Knot")) {
        var weight = MechanicsUtil.GetWeight(defender, desc, "defender");
        move.Bp = weight >= 200 ? 120 : weight >= 100 ? 100 : weight >= 50 ? 80 : weight >= 25 ? 60 : weight >= 10 ? 40 : 20;
        desc.MoveBP = move.Bp;
      } else if (move.Named("Hidden Power")) {
        var hp = Stats.GetHiddenPower(gen, attacker.Ivs);
        move.Bp = gen.Num < 2 ? 40 : hp.power;
        move.Type = hp.type;
        desc.MoveBP = move.Bp;
        desc.MoveType = move.Type;
      } else if (move.Named("Weather Ball") && field.Weather != null) {
        move.Bp *= 2;
        desc.MoveBP = move.Bp;
        if (field.HasWeather("Sun", "Harsh Sunshine")) move.Type = "Fire";
        else if (field.HasWeather("Rain", "Heavy Rain")) move.Type = "Water";
        else if (field.HasWeather("Sand")) move.Type = "Rock";
        else if (field.HasWeather("Hail", "Snow")) move.Type = "Ice";
        desc.MoveType = move.Type;
      } else if (move.Named("Psywave")) {
        move.Bp = 0;
      }

      if (move.Bp == 0) return result;

      // Gen 1/2: base damage uses integer level arithmetic; +2 added after min(997, ...)
      var baseDmg = (int)Math.Floor((double)((2 * lv / 5 + 2) * at * move.Bp) / (double)Math.Max(1, df) / 50.0);
      baseDmg = Math.Min(997, baseDmg) + 2;

      // Apply pre-random modifiers (STAB, weather) before the random factor loop
      if (field.HasWeather("Sun") && move.Type == "Fire") baseDmg = (int)Math.Floor(baseDmg * 1.5);
      if (field.HasWeather("Sun") && move.Type == "Water") baseDmg = (int)Math.Floor(baseDmg / 2.0);
      if (field.HasWeather("Rain") && move.Type == "Fire") baseDmg = (int)Math.Floor(baseDmg / 2.0);
      if (field.HasWeather("Rain") && move.Type == "Water") baseDmg = (int)Math.Floor(baseDmg * 1.5);

      if (attacker.HasType(move.Type)) baseDmg = (int)Math.Floor(baseDmg * 1.5);

      if (gen.Num == 1) {
        baseDmg = (int)Math.Floor(baseDmg * type1Effectiveness);
        baseDmg = (int)Math.Floor(baseDmg * type2Effectiveness);
      } else {
        baseDmg = (int)Math.Floor(baseDmg * typeEffectiveness);
      }

      var dmg = new int[39]; // 217-255 random range (Gen 1/2)
      for (var i = 217; i <= 255; i++) {
        int damageAmount;
        if (move.Named("Psywave")) {
          damageAmount = (int)Math.Floor(((2 * lv / 5.0 + 2) * (i - 217)) / 100.0);
        } else if (gen.Num == 2) {
          damageAmount = Math.Max(1, (int)Math.Floor((double)(baseDmg * i) / 255));
        } else {
          damageAmount = baseDmg == 1 ? 1 : (int)Math.Floor((double)(baseDmg * i) / 255);
        }

        if ((attacker.HasAbility("Plus") || attacker.HasAbility("Minus")) &&
            (defender.HasAbility("Plus") || defender.HasAbility("Minus")) &&
            move.Category == MoveCategories.Special) {
          damageAmount = (int)Math.Floor(damageAmount * 1.5);
          desc.AttackerAbility = attacker.Ability;
        }

        if (attacker.HasAbility("Flash Fire") && move.Type == "Fire" && attacker.AbilityOn) {
          damageAmount = (int)Math.Floor(damageAmount * 1.5);
          desc.AttackerAbility = attacker.Ability;
        }

        if (move.Type == "Ice" && field.HasWeather("Hail", "Snow")) {
          damageAmount = (int)Math.Floor(damageAmount * 1.5);
          desc.Weather = field.Weather;
        }

        if (move.Type == "Dragon" && field.HasWeather("Strong Winds")) {
          damageAmount = (int)Math.Floor(damageAmount * 1.5);
          desc.Weather = field.Weather;
        }

        if (attacker.HasAbility("Power Spot")) {
          damageAmount = (int)Math.Floor(damageAmount * 1.3);
          desc.IsPowerSpot = true;
        }

        if (attacker.HasAbility("Solar Power") && field.HasWeather("Sun", "Harsh Sunshine") && move.Category == MoveCategories.Special) {
          damageAmount = (int)Math.Floor(damageAmount * 1.5);
          desc.AttackerAbility = attacker.Ability;
        }

        if (defender.HasAbility("Thick Fat") && (move.Type == "Fire" || move.Type == "Ice")) {
          damageAmount = (int)Math.Floor(damageAmount / 2.0);
          desc.DefenderAbility = defender.Ability;
        }

        if (attacker.HasAbility("Tinted Lens") && typeEffectiveness < 1) {
          damageAmount = (int)Math.Floor((double)(damageAmount * 2));
          desc.AttackerAbility = attacker.Ability;
        }

        if (defender.HasAbility("Filter") && typeEffectiveness > 1) {
          damageAmount = (int)Math.Floor(damageAmount * 0.75);
          desc.DefenderAbility = defender.Ability;
        }

        if (defender.HasAbility("Solid Rock") && typeEffectiveness > 1) {
          damageAmount = (int)Math.Floor(damageAmount * 0.75);
          desc.DefenderAbility = defender.Ability;
        }

        if (defender.HasAbility("Prism Armor") && typeEffectiveness > 1) {
          damageAmount = (int)Math.Floor(damageAmount * 0.75);
          desc.DefenderAbility = defender.Ability;
        }

        if (defender.HasAbility("Tera Shell") && !defender.HasItem("Air Balloon") && typeEffectiveness > 1) {
          damageAmount = (int)Math.Floor(damageAmount * 0.75);
          desc.DefenderAbility = defender.Ability;
        }

        if (defender.HasAbility("Tera Shell") && defender.HasItem("Air Balloon") && typeEffectiveness > 1) {
          damageAmount = (int)Math.Floor(damageAmount * 0.75);
          desc.DefenderAbility = defender.Ability;
        }

        if (defender.HasAbility("Multiscale") && defender.CurHP() == defender.MaxHP()) {
          damageAmount = (int)Math.Floor(damageAmount * 0.5);
          desc.DefenderAbility = defender.Ability;
        }

        if (defender.HasAbility("Shadow Shield") && defender.CurHP() == defender.MaxHP()) {
          damageAmount = (int)Math.Floor(damageAmount * 0.5);
          desc.DefenderAbility = defender.Ability;
        }

        if (defender.HasAbility("Fluffy") && move.Flags.Contact) {
          damageAmount = (int)Math.Floor(damageAmount * 0.5);
          desc.DefenderAbility = defender.Ability;
        }

        if (defender.HasAbility("Fluffy") && move.Type == "Fire") {
          damageAmount = (int)Math.Floor((double)(damageAmount * 2));
          desc.DefenderAbility = defender.Ability;
        }

        if (defender.HasAbility("Tablets of Ruin") && move.Category == MoveCategories.Physical) {
          damageAmount = (int)Math.Floor(damageAmount * 0.75);
          desc.DefenderAbility = defender.Ability;
        }

        if (defender.HasAbility("Vessel of Ruin") && move.Category == MoveCategories.Special) {
          damageAmount = (int)Math.Floor(damageAmount * 0.75);
          desc.DefenderAbility = defender.Ability;
        }

        if (attacker.HasItem("Metronome") && move.TimesUsedWithMetronome.HasValue) {
          var multiplier = Math.Min(move.TimesUsedWithMetronome.Value, 5);
          damageAmount = (int)Math.Floor(damageAmount * (1 + 0.2 * multiplier));
          desc.AttackerItem = attacker.Item;
        }

        var itemBoostType = Items.GetItemBoostType(attacker.Item);
        if (itemBoostType != null && itemBoostType == move.Type) {
          damageAmount = (int)Math.Floor(damageAmount * 1.1);
          desc.AttackerItem = attacker.Item;
        }

        if (attacker.HasItem("Light Ball") && attacker.Named("Pikachu") && move.Category == MoveCategories.Special) {
          damageAmount = (int)Math.Floor((double)(damageAmount * 2));
          desc.AttackerItem = attacker.Item;
        }

        if (attacker.HasItem("Thick Club") && attacker.Named("Cubone", "Marowak") && move.Category == MoveCategories.Physical) {
          damageAmount = (int)Math.Floor((double)(damageAmount * 2));
          desc.AttackerItem = attacker.Item;
        }

        if (attacker.HasItem("Choice Band") && move.Category == MoveCategories.Physical) {
          damageAmount = (int)Math.Floor(damageAmount * 1.5);
          desc.AttackerItem = attacker.Item;
        }

        if (attacker.HasItem("Choice Specs") && move.Category == MoveCategories.Special) {
          damageAmount = (int)Math.Floor(damageAmount * 1.5);
          desc.AttackerItem = attacker.Item;
        }

        dmg[i - 217] = damageAmount;
      }

      result.Damage = dmg;

      if (move.Hits > 1) {
        var damageMatrix = new int[move.Hits][];
        damageMatrix[0] = dmg;
        for (var times = 1; times < move.Hits; times++) {
          var hitDmg = new int[39];
          for (var i = 0; i < 39; i++) hitDmg[i] = dmg[i];
          damageMatrix[times] = hitDmg;
        }
        result.Damage = damageMatrix;
      }

      return result;
    }
  }
}
