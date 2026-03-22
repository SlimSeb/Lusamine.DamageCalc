using System;
using System.Collections.Generic;
using Lusamine.DamageCalc;
using Lusamine.DamageCalc.Data;

namespace Lusamine.DamageCalc.Mechanics {
  public static class Gen3 {
    public static Result CalculateADV(IGeneration gen, Pokemon attacker, Pokemon defender, Move move, Field field) {
      MechanicsUtil.CheckAirLock(attacker, field);
      MechanicsUtil.CheckAirLock(defender, field);
      MechanicsUtil.CheckForecast(attacker, field.Weather);
      MechanicsUtil.CheckForecast(defender, field.Weather);
      MechanicsUtil.CheckIntimidate(gen, attacker, defender);
      MechanicsUtil.CheckIntimidate(gen, defender, attacker);
      attacker.Stats.Spe = MechanicsUtil.GetFinalSpeed(gen, attacker, field, field.AttackerSide);
      defender.Stats.Spe = MechanicsUtil.GetFinalSpeed(gen, defender, field, field.DefenderSide);

      var desc = new RawDesc {
        AttackerName = attacker.Name,
        MoveName = move.Name,
        DefenderName = defender.Name,
      };

      var result = new Result(gen, attacker, defender, move, field, 0, desc);

      if (move.Category == MoveCategories.Status && !move.Named("Nature Power")) return result;

      if (field.DefenderSide.IsProtected) {
        desc.IsProtected = true;
        return result;
      }

      if (move.Name == "Pain Split") {
        var average = (int)Math.Floor((attacker.CurHP() + defender.CurHP()) / 2.0);
        var dmg = Math.Max(0, defender.CurHP() - average);
        result.Damage = dmg;
        return result;
      }

      if (move.Named("Weather Ball")) {
        move.Type = field.HasWeather("Sun") ? "Fire"
          : field.HasWeather("Rain") ? "Water"
          : field.HasWeather("Sand") ? "Rock"
          : field.HasWeather("Hail") ? "Ice"
          : "Normal";
        move.Category = move.Type == "Rock" ? MoveCategories.Physical : MoveCategories.Special;
        desc.Weather = field.Weather;
        desc.MoveType = move.Type;
        desc.MoveBP = move.Bp;
      } else if (move.Named("Brick Break")) {
        field.DefenderSide.IsReflect = false;
        field.DefenderSide.IsLightScreen = false;
      }

      var typeEffectivenessPrecedenceRules = new List<string> {
        "Normal", "Fire", "Water", "Electric", "Grass", "Ice", "Fighting", "Poison",
        "Ground", "Flying", "Psychic", "Bug", "Rock", "Ghost", "Dragon", "Dark", "Steel",
      };

      var firstDefenderType = defender.Types[0];
      string? secondDefenderType = defender.Types.Length > 1 ? defender.Types[1] : null;

      if (!string.IsNullOrEmpty(secondDefenderType) && firstDefenderType != secondDefenderType) {
        var firstTypePrecedence = typeEffectivenessPrecedenceRules.IndexOf(firstDefenderType);
        var secondTypePrecedence = typeEffectivenessPrecedenceRules.IndexOf(secondDefenderType);
        if (firstTypePrecedence > secondTypePrecedence) {
          var tmp = firstDefenderType;
          firstDefenderType = secondDefenderType;
          secondDefenderType = tmp;
        }
      }

      var type1Effectiveness = MechanicsUtil.GetMoveEffectiveness(
        gen,
        move,
        firstDefenderType,
        field.DefenderSide.IsForesight
      );
      var type2Effectiveness = !string.IsNullOrEmpty(secondDefenderType)
        ? MechanicsUtil.GetMoveEffectiveness(gen, move, secondDefenderType, field.DefenderSide.IsForesight)
        : 1;
      var typeEffectiveness = type1Effectiveness * type2Effectiveness;

      if (typeEffectiveness == 0) return result;

      if ((defender.HasAbility("Flash Fire") && move.HasType("Fire")) ||
          (defender.HasAbility("Levitate") && move.HasType("Ground")) ||
          (defender.HasAbility("Volt Absorb") && move.HasType("Electric")) ||
          (defender.HasAbility("Water Absorb") && move.HasType("Water")) ||
          (defender.HasAbility("Wonder Guard") && !move.HasType("???") && typeEffectiveness <= 1) ||
          (defender.HasAbility("Soundproof") && move.Flags.Sound)) {
        desc.DefenderAbility = defender.Ability;
        return result;
      }

      desc.HPEVs = MechanicsUtil.GetStatDescriptionText(gen, defender, StatId.Hp, null);

      var fixedDamage = MechanicsUtil.HandleFixedDamageMoves(attacker, move);
      if (fixedDamage != 0) {
        result.Damage = fixedDamage;
        return result;
      }

      if (move.Hits > 1) desc.Hits = move.Hits;

      var bp = CalculateBasePowerADV(attacker, defender, move, desc);
      if (bp == 0) return result;
      bp = CalculateBPModsADV(attacker, move, desc, bp);

      var isCritical = move.IsCrit && !defender.HasAbility("Battle Armor") && !defender.HasAbility("Shell Armor");
      var at = CalculateAttackADV(gen, attacker, defender, move, desc, isCritical);
      var df = CalculateDefenseADV(gen, defender, move, desc, isCritical);

      var lv = attacker.Level;
      var baseDamage = (int)Math.Floor(Math.Floor((Math.Floor((2 * lv) / 5.0 + 2) * at * bp) / df) / 50.0);
      baseDamage = CalculateFinalModsADV(baseDamage, attacker, move, field, desc, isCritical);

      baseDamage = (int)Math.Floor(baseDamage * type1Effectiveness);
      baseDamage = (int)Math.Floor(baseDamage * type2Effectiveness);
      var damage = new int[16];
      for (var i = 85; i <= 100; i++) {
        damage[i - 85] = Math.Max(1, (int)Math.Floor((baseDamage * i) / 100.0));
      }
      result.Damage = damage;

      if ((move.TimesUsed ?? 1) > 1 || move.Hits > 1) {
        var origDefBoost = desc.DefenseBoost;
        var origAtkBoost = desc.AttackBoost;
        var numAttacks = 1;
        if (move.DropsStats.HasValue && (move.TimesUsed ?? 1) > 1) {
          desc.MoveTurns = $"over {move.TimesUsed} turns";
          numAttacks = move.TimesUsed ?? 1;
        } else {
          numAttacks = move.Hits;
        }

        var usedItems = (attackerUsed: false, defenderUsed: false);
        var damageMatrix = new int[numAttacks][];
        damageMatrix[0] = damage;
        for (var times = 1; times < numAttacks; times++) {
          usedItems = MechanicsUtil.CheckMultihitBoost(gen, attacker, defender, move, field, desc, usedItems.attackerUsed, usedItems.defenderUsed);
          var newAt = CalculateAttackADV(gen, attacker, defender, move, desc, isCritical);
          var newBp = CalculateBasePowerADV(attacker, defender, move, desc);
          newBp = CalculateBPModsADV(attacker, move, desc, newBp);
          var newBaseDmg = (int)Math.Floor(Math.Floor((Math.Floor((2 * lv) / 5.0 + 2) * newAt * newBp) / df) / 50.0);
          newBaseDmg = CalculateFinalModsADV(newBaseDmg, attacker, move, field, desc, isCritical);
          newBaseDmg = (int)Math.Floor(newBaseDmg * type1Effectiveness);
          newBaseDmg = (int)Math.Floor(newBaseDmg * type2Effectiveness);

          var newDamage = new int[16];
          for (var i = 85; i <= 100; i++) {
            newDamage[i - 85] = Math.Max(1, (int)Math.Floor((newBaseDmg * i) / 100.0));
          }
          damageMatrix[times] = newDamage;
        }
        result.Damage = damageMatrix;
        desc.DefenseBoost = origDefBoost;
        desc.AttackBoost = origAtkBoost;
      }

      return result;
    }

    public static int CalculateBasePowerADV(Pokemon attacker, Pokemon defender, Move move, RawDesc desc, int hit = 1) {
      var bp = move.Bp;
      switch (move.Name) {
        case "Flail":
        case "Reversal":
          var p = (int)Math.Floor((48 * attacker.CurHP()) / (double)attacker.MaxHP());
          bp = p <= 1 ? 200 : p <= 4 ? 150 : p <= 9 ? 100 : p <= 16 ? 80 : p <= 32 ? 40 : 20;
          desc.MoveBP = bp;
          break;
        case "Eruption":
        case "Water Spout":
          bp = Math.Max(1, (int)Math.Floor((150 * attacker.CurHP()) / (double)attacker.MaxHP()));
          desc.MoveBP = bp;
          break;
        case "Low Kick":
          var w = defender.WeightKg;
          bp = w >= 200 ? 120 : w >= 100 ? 100 : w >= 50 ? 80 : w >= 25 ? 60 : w >= 10 ? 40 : 20;
          desc.MoveBP = bp;
          break;
        case "Facade":
          if (attacker.HasStatus("par", "psn", "tox", "brn")) {
            bp = move.Bp * 2;
            desc.MoveBP = bp;
          }
          break;
        case "Nature Power":
          move.Category = MoveCategories.Physical;
          bp = 60;
          desc.MoveName = "Swift";
          break;
        case "Triple Kick":
          bp = hit * 10;
          desc.MoveBP = move.Hits == 2 ? 30 : move.Hits == 3 ? 60 : 10;
          break;
        default:
          bp = move.Bp;
          break;
      }
      return bp;
    }

    public static int CalculateBPModsADV(Pokemon attacker, Move move, RawDesc desc, int basePower) {
      if (attacker.CurHP() <= attacker.MaxHP() / 3 &&
          ((attacker.HasAbility("Overgrow") && move.HasType("Grass")) ||
           (attacker.HasAbility("Blaze") && move.HasType("Fire")) ||
           (attacker.HasAbility("Torrent") && move.HasType("Water")) ||
           (attacker.HasAbility("Swarm") && move.HasType("Bug")))) {
        basePower = (int)Math.Floor(basePower * 1.5);
        desc.AttackerAbility = attacker.Ability;
      }
      return basePower;
    }

    public static int CalculateAttackADV(IGeneration gen, Pokemon attacker, Pokemon defender, Move move, RawDesc desc, bool isCritical = false) {
      var isPhysical = move.Category == MoveCategories.Physical;
      var attackStat = isPhysical ? StatId.Atk : StatId.Spa;
      desc.AttackEVs = MechanicsUtil.GetStatDescriptionText(gen, attacker, attackStat, attacker.Nature);

      var at = attacker.RawStats[attackStat];

      if (isPhysical && (attacker.HasAbility("Huge Power") || attacker.HasAbility("Pure Power"))) {
        at *= 2;
        desc.AttackerAbility = attacker.Ability;
      }

      if (!attacker.HasItem("Sea Incense") && move.HasType(Items.GetItemBoostType(attacker.Item))) {
        at = (int)Math.Floor(at * 1.1);
        desc.AttackerItem = attacker.Item;
      } else if (attacker.HasItem("Sea Incense") && move.HasType("Water")) {
        at = (int)Math.Floor(at * 1.05);
        desc.AttackerItem = attacker.Item;
      } else if ((isPhysical && attacker.HasItem("Choice Band")) ||
                 (!isPhysical && attacker.HasItem("Soul Dew") && attacker.Named("Latios", "Latias"))) {
        at = (int)Math.Floor(at * 1.5);
        desc.AttackerItem = attacker.Item;
      } else if ((!isPhysical && attacker.HasItem("Deep Sea Tooth") && attacker.Named("Clamperl")) ||
                 (!isPhysical && attacker.HasItem("Light Ball") && attacker.Named("Pikachu")) ||
                 (isPhysical && attacker.HasItem("Thick Club") && attacker.Named("Cubone", "Marowak"))) {
        at *= 2;
        desc.AttackerItem = attacker.Item;
      }

      if (defender.HasAbility("Thick Fat") && move.HasType("Fire", "Ice")) {
        at = (int)Math.Floor(at / 2.0);
        desc.DefenderAbility = defender.Ability;
      }

      if ((isPhysical && (attacker.HasAbility("Hustle") || (attacker.HasAbility("Guts") && !string.IsNullOrEmpty(attacker.Status)))) ||
          (!isPhysical && attacker.AbilityOn && (attacker.HasAbility("Plus") || attacker.HasAbility("Minus")))) {
        at = (int)Math.Floor(at * 1.5);
        desc.AttackerAbility = attacker.Ability;
      }

      var attackBoost = attacker.Boosts[attackStat];
      if (attackBoost > 0 || (!isCritical && attackBoost < 0)) {
        at = MechanicsUtil.GetModifiedStat(at, attackBoost);
        desc.AttackBoost = attackBoost;
      }
      return at;
    }

    public static int CalculateDefenseADV(IGeneration gen, Pokemon defender, Move move, RawDesc desc, bool isCritical = false) {
      var isPhysical = move.Category == MoveCategories.Physical;
      var defenseStat = isPhysical ? StatId.Def : StatId.Spd;
      desc.DefenseEVs = MechanicsUtil.GetStatDescriptionText(gen, defender, defenseStat, defender.Nature);

      var df = defender.RawStats[defenseStat];

      if (!isPhysical && defender.HasItem("Soul Dew") && defender.Named("Latios", "Latias")) {
        df = (int)Math.Floor(df * 1.5);
        desc.DefenderItem = defender.Item;
      } else if ((!isPhysical && defender.HasItem("Deep Sea Scale") && defender.Named("Clamperl")) ||
                 (isPhysical && defender.HasItem("Metal Powder") && defender.Named("Ditto"))) {
        df *= 2;
        desc.DefenderItem = defender.Item;
      }

      if (isPhysical && defender.HasAbility("Marvel Scale") && !string.IsNullOrEmpty(defender.Status)) {
        df = (int)Math.Floor(df * 1.5);
        desc.DefenderAbility = defender.Ability;
      }

      if (move.Named("Explosion", "Self-Destruct")) {
        df = (int)Math.Floor(df / 2.0);
      }

      var defenseBoost = defender.Boosts[defenseStat];
      if (defenseBoost < 0 || (!isCritical && defenseBoost > 0)) {
        df = MechanicsUtil.GetModifiedStat(df, defenseBoost);
        desc.DefenseBoost = defenseBoost;
      }
      return df;
    }

    private static int CalculateFinalModsADV(int baseDamage, Pokemon attacker, Move move, Field field, RawDesc desc, bool isCritical = false) {
      var isPhysical = move.Category == MoveCategories.Physical;
      if (!string.IsNullOrEmpty(attacker.Status) && isPhysical && !attacker.HasAbility("Guts")) {
        baseDamage = (int)Math.Floor(baseDamage / 2.0);
        desc.IsBurned = true;
      }

      if (!isCritical) {
        var screenMultiplier = field.GameType != GameTypes.Singles ? 2.0 / 3.0 : 1.0 / 2.0;
        if (isPhysical && field.DefenderSide.IsReflect) {
          baseDamage = (int)Math.Floor(baseDamage * screenMultiplier);
          desc.IsReflect = true;
        } else if (!isPhysical && field.DefenderSide.IsLightScreen) {
          baseDamage = (int)Math.Floor(baseDamage * screenMultiplier);
          desc.IsLightScreen = true;
        }
      }

      if (field.GameType != GameTypes.Singles && move.Target == "allAdjacentFoes") {
        baseDamage = (int)Math.Floor(baseDamage / 2.0);
      }

      if ((field.HasWeather("Sun") && move.HasType("Fire")) || (field.HasWeather("Rain") && move.HasType("Water"))) {
        baseDamage = (int)Math.Floor(baseDamage * 1.5);
        desc.Weather = field.Weather;
      } else if ((field.HasWeather("Sun") && move.HasType("Water")) ||
                 (field.HasWeather("Rain") && move.HasType("Fire")) ||
                 (move.Named("Solar Beam") && field.HasWeather("Rain", "Sand", "Hail"))) {
        baseDamage = (int)Math.Floor(baseDamage / 2.0);
        desc.Weather = field.Weather;
      }

      if (attacker.HasAbility("Flash Fire") && attacker.AbilityOn && move.HasType("Fire")) {
        baseDamage = (int)Math.Floor(baseDamage * 1.5);
        desc.AttackerAbility = "Flash Fire";
      }

      baseDamage = (move.Category == MoveCategories.Physical ? Math.Max(1, baseDamage) : baseDamage) + 2;
      if (isCritical) {
        baseDamage *= 2;
        desc.IsCritical = true;
      }

      if (move.Named("Pursuit") && field.DefenderSide.IsSwitching == "out") {
        baseDamage = (int)Math.Floor(baseDamage * 2.0);
        desc.IsSwitching = "out";
      }

      if (move.Named("Weather Ball") && field.Weather != null) {
        baseDamage *= 2;
        desc.MoveBP = move.Bp * 2;
      }

      if (field.AttackerSide.IsHelpingHand) {
        baseDamage = (int)Math.Floor(baseDamage * 1.5);
        desc.IsHelpingHand = true;
      }

      if (move.HasType(attacker.Types)) {
        baseDamage = (int)Math.Floor(baseDamage * 1.5);
      }

      return baseDamage;
    }
  }
}
