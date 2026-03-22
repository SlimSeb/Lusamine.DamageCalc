using System;
using System.Collections.Generic;
using Lusamine.DamageCalc;
using Lusamine.DamageCalc.Data;

namespace Lusamine.DamageCalc.Mechanics {
  public static class Gen4 {
    public static Result CalculateDPP(IGeneration gen, Pokemon attacker, Pokemon defender, Move move, Field field) {
      MechanicsUtil.CheckAirLock(attacker, field);
      MechanicsUtil.CheckAirLock(defender, field);
      MechanicsUtil.CheckForecast(attacker, field.Weather);
      MechanicsUtil.CheckForecast(defender, field.Weather);
      MechanicsUtil.CheckItem(attacker);
      MechanicsUtil.CheckItem(defender);
      MechanicsUtil.CheckIntimidate(gen, attacker, defender);
      MechanicsUtil.CheckIntimidate(gen, defender, attacker);
      MechanicsUtil.CheckDownload(attacker, defender);
      MechanicsUtil.CheckDownload(defender, attacker);
      attacker.Stats.Spe = MechanicsUtil.GetFinalSpeed(gen, attacker, field, field.AttackerSide);
      defender.Stats.Spe = MechanicsUtil.GetFinalSpeed(gen, defender, field, field.DefenderSide);

      var desc = new RawDesc {
        AttackerName = attacker.Name,
        MoveName = move.Name,
        DefenderName = defender.Name,
      };

      var result = new Result(gen, attacker, defender, move, field, 0, desc);

      if (move.Category == MoveCategories.Status && !move.Named("Nature Power")) return result;

      if (field.DefenderSide.IsProtected && !move.BreaksProtect) {
        desc.IsProtected = true;
        return result;
      }

      if (move.Name == "Pain Split") {
        var average = (int)Math.Floor((attacker.CurHP() + defender.CurHP()) / 2.0);
        var damage = Math.Max(0, defender.CurHP() - average);
        result.Damage = damage;
        return result;
      }

      var defenderAbilityIgnored = defender.HasAbility(
        "Battle Armor", "Clear Body", "Damp", "Dry Skin",
        "Filter", "Flash Fire", "Flower Gift", "Heatproof",
        "Hyper Cutter", "Immunity", "Inner Focus", "Insomnia",
        "Keen Eye", "Leaf Guard", "Levitate", "Lightning Rod",
        "Limber", "Magma Armor", "Marvel Scale", "Motor Drive",
        "Oblivious", "Own Tempo", "Sand Veil", "Shell Armor",
        "Shield Dust", "Simple", "Snow Cloak", "Solid Rock",
        "Soundproof", "Sticky Hold", "Storm Drain", "Sturdy",
        "Suction Cups", "Tangled Feet", "Thick Fat", "Unaware",
        "Vital Spirit", "Volt Absorb", "Water Absorb", "Water Veil",
        "White Smoke", "Wonder Guard"
      );

      if (attacker.HasAbility("Mold Breaker") && defenderAbilityIgnored) {
        defender.Ability = "";
        desc.AttackerAbility = attacker.Ability;
      }

      var isCritical = move.IsCrit && !defender.HasAbility("Battle Armor") && !defender.HasAbility("Shell Armor");

      if (move.Named("Weather Ball")) {
        move.Type = field.HasWeather("Sun") ? "Fire"
          : field.HasWeather("Rain") ? "Water"
          : field.HasWeather("Sand") ? "Rock"
          : field.HasWeather("Hail") ? "Ice"
          : "Normal";
        desc.Weather = field.Weather;
        desc.MoveType = move.Type;
      } else if (move.Named("Judgment") && !string.IsNullOrEmpty(attacker.Item) && attacker.Item.Contains("Plate")) {
        move.Type = Items.GetItemBoostType(attacker.Item) ?? move.Type;
      } else if (move.Named("Natural Gift") && attacker.Item != null && attacker.Item.EndsWith("Berry")) {
        var gift = Items.GetNaturalGift(gen, attacker.Item);
        move.Type = gift.type;
        move.Bp = gift.power;
        desc.AttackerItem = attacker.Item;
        desc.MoveBP = move.Bp;
        desc.MoveType = move.Type;
      } else if (move.Named("Brick Break")) {
        field.DefenderSide.IsReflect = false;
        field.DefenderSide.IsLightScreen = false;
      }

      if (attacker.HasAbility("Normalize") && !move.Named("Struggle")) {
        move.Type = "Normal";
        desc.AttackerAbility = attacker.Ability;
      }

      var isGhostRevealed = attacker.HasAbility("Scrappy") || field.DefenderSide.IsForesight;

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

      var type1Effectiveness = MechanicsUtil.GetMoveEffectiveness(gen, move, firstDefenderType, isGhostRevealed, field.IsGravity);
      var type2Effectiveness = !string.IsNullOrEmpty(secondDefenderType)
        ? MechanicsUtil.GetMoveEffectiveness(gen, move, secondDefenderType, isGhostRevealed, field.IsGravity)
        : 1;
      var typeEffectiveness = type1Effectiveness * type2Effectiveness;

      if (typeEffectiveness == 0 && move.HasType("Ground") &&
          defender.HasItem("Iron Ball") && !defender.HasAbility("Klutz")) {
        if (type1Effectiveness == 0) type1Effectiveness = 1;
        else if (!string.IsNullOrEmpty(secondDefenderType) && type2Effectiveness == 0) type2Effectiveness = 1;
        typeEffectiveness = type1Effectiveness * type2Effectiveness;
      }

      if (typeEffectiveness == 0) return result;

      var ignoresWonderGuard = move.HasType("???") || move.Named("Fire Fang");
      if ((!ignoresWonderGuard && defender.HasAbility("Wonder Guard") && typeEffectiveness <= 1) ||
          (move.HasType("Fire") && defender.HasAbility("Flash Fire")) ||
          (move.HasType("Water") && defender.HasAbility("Dry Skin", "Water Absorb")) ||
          (move.HasType("Electric") && defender.HasAbility("Motor Drive", "Volt Absorb")) ||
          (move.HasType("Ground") && !field.IsGravity && !defender.HasItem("Iron Ball") && defender.HasAbility("Levitate")) ||
          (move.Flags.Sound && defender.HasAbility("Soundproof"))) {
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

      var isPhysical = move.Category == MoveCategories.Physical;

      var basePower = CalculateBasePowerDPP(gen, attacker, defender, move, field, desc);
      if (basePower == 0) return result;
      basePower = CalculateBPModsDPP(attacker, defender, move, field, desc, basePower);

      var attack = CalculateAttackDPP(gen, attacker, defender, move, field, desc, isCritical);
      var defense = CalculateDefenseDPP(gen, attacker, defender, move, field, desc, isCritical);

      var baseDamage = (int)Math.Floor(
        Math.Floor((Math.Floor((2 * attacker.Level) / 5.0 + 2) * basePower * attack) / 50.0) / defense
      );

      if (!string.IsNullOrEmpty(attacker.Status) && isPhysical && !attacker.HasAbility("Guts")) {
        baseDamage = (int)Math.Floor(baseDamage * 0.5);
        desc.IsBurned = true;
      }

      baseDamage = CalculateFinalModsDPP(baseDamage, attacker, move, field, desc, isCritical);

      var stabMod = 1.0;
      if (move.HasType(attacker.Types)) {
        if (attacker.HasAbility("Adaptability")) {
          stabMod = 2;
          desc.AttackerAbility = attacker.Ability;
        } else {
          stabMod = 1.5;
        }
      }

      var filterMod = 1.0;
      if ((defender.HasAbility("Filter") || defender.HasAbility("Solid Rock")) && typeEffectiveness > 1) {
        filterMod = 0.75;
        desc.DefenderAbility = defender.Ability;
      }
      var ebeltMod = 1.0;
      if (attacker.HasItem("Expert Belt") && typeEffectiveness > 1) {
        ebeltMod = 1.2;
        desc.AttackerItem = attacker.Item;
      }
      var tintedMod = 1.0;
      if (attacker.HasAbility("Tinted Lens") && typeEffectiveness < 1) {
        tintedMod = 2;
        desc.AttackerAbility = attacker.Ability;
      }
      var berryMod = 1.0;
      if (move.HasType(Items.GetBerryResistType(defender.Item)) && (typeEffectiveness > 1 || move.HasType("Normal"))) {
        berryMod = 0.5;
        desc.DefenderItem = defender.Item;
      }

      var dmg = new int[16];
      for (var i = 0; i < 16; i++) {
        dmg[i] = (int)Math.Floor((baseDamage * (85 + i)) / 100.0);
        dmg[i] = (int)Math.Floor(dmg[i] * stabMod);
        dmg[i] = (int)Math.Floor(dmg[i] * type1Effectiveness);
        dmg[i] = (int)Math.Floor(dmg[i] * type2Effectiveness);
        dmg[i] = (int)Math.Floor(dmg[i] * filterMod);
        dmg[i] = (int)Math.Floor(dmg[i] * ebeltMod);
        dmg[i] = (int)Math.Floor(dmg[i] * tintedMod);
        dmg[i] = (int)Math.Floor(dmg[i] * berryMod);
        dmg[i] = Math.Max(1, dmg[i]);
      }
      result.Damage = dmg;

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
        damageMatrix[0] = dmg;
        for (var times = 1; times < numAttacks; times++) {
          usedItems = MechanicsUtil.CheckMultihitBoost(gen, attacker, defender, move, field, desc, usedItems.attackerUsed, usedItems.defenderUsed);
          var newBasePower = CalculateBasePowerDPP(gen, attacker, defender, move, field, desc);
          newBasePower = CalculateBPModsDPP(attacker, defender, move, field, desc, newBasePower);
          var newAtk = CalculateAttackDPP(gen, attacker, defender, move, field, desc, isCritical);
          var newBaseDamage = (int)Math.Floor(
            Math.Floor((Math.Floor((2 * attacker.Level) / 5.0 + 2) * newBasePower * newAtk) / 50.0) / defense
          );
          if (!string.IsNullOrEmpty(attacker.Status) && isPhysical && !attacker.HasAbility("Guts")) {
            newBaseDamage = (int)Math.Floor(newBaseDamage * 0.5);
            desc.IsBurned = true;
          }
          newBaseDamage = CalculateFinalModsDPP(newBaseDamage, attacker, move, field, desc, isCritical);

          var damageArray = new int[16];
          for (var i = 0; i < 16; i++) {
            var newFinalDamage = (int)Math.Floor((newBaseDamage * (85 + i)) / 100.0);
            newFinalDamage = (int)Math.Floor(newFinalDamage * stabMod);
            newFinalDamage = (int)Math.Floor(newFinalDamage * type1Effectiveness);
            newFinalDamage = (int)Math.Floor(newFinalDamage * type2Effectiveness);
            newFinalDamage = (int)Math.Floor(newFinalDamage * filterMod);
            newFinalDamage = (int)Math.Floor(newFinalDamage * ebeltMod);
            newFinalDamage = (int)Math.Floor(newFinalDamage * tintedMod);
            newFinalDamage = Math.Max(1, newFinalDamage);
            damageArray[i] = newFinalDamage;
          }
          damageMatrix[times] = damageArray;
        }
        result.Damage = damageMatrix;
        desc.DefenseBoost = origDefBoost;
        desc.AttackBoost = origAtkBoost;
      }

      return result;
    }

    public static int CalculateBasePowerDPP(IGeneration gen, Pokemon attacker, Pokemon defender, Move move, Field field, RawDesc desc, int hit = 1) {
      var basePower = move.Bp;
      var turnOrder = attacker.Stats.Spe > defender.Stats.Spe ? "first" : "last";
      switch (move.Name) {
        case "Brine":
          if (defender.CurHP() <= defender.MaxHP() / 2) {
            basePower *= 2;
            desc.MoveBP = basePower;
          }
          break;
        case "Eruption":
        case "Water Spout":
          basePower = Math.Max(1, (int)Math.Floor((basePower * attacker.CurHP()) / (double)attacker.MaxHP()));
          desc.MoveBP = basePower;
          break;
        case "Facade":
          if (attacker.HasStatus("par", "psn", "tox", "brn")) {
            basePower = move.Bp * 2;
            desc.MoveBP = basePower;
          }
          break;
        case "Flail":
        case "Reversal":
          var p = (int)Math.Floor((64 * attacker.CurHP()) / (double)attacker.MaxHP());
          basePower = p <= 1 ? 200 : p <= 5 ? 150 : p <= 12 ? 100 : p <= 21 ? 80 : p <= 42 ? 40 : 20;
          desc.MoveBP = basePower;
          break;
        case "Fling":
          basePower = Items.GetFlingPower(attacker.Item, gen.Num);
          desc.MoveBP = basePower;
          desc.AttackerItem = attacker.Item;
          break;
        case "Grass Knot":
        case "Low Kick":
          var w = defender.WeightKg;
          basePower = w >= 200 ? 120 : w >= 100 ? 100 : w >= 50 ? 80 : w >= 25 ? 60 : w >= 10 ? 40 : 20;
          desc.MoveBP = basePower;
          break;
        case "Gyro Ball":
          basePower = Math.Min(150, (int)Math.Floor((25 * defender.Stats.Spe) / (double)attacker.Stats.Spe));
          desc.MoveBP = basePower;
          break;
        case "Payback":
          if (turnOrder != "first") {
            basePower *= 2;
            desc.MoveBP = basePower;
          }
          break;
        case "Punishment":
          basePower = Math.Min(200, 60 + 20 * MechanicsUtil.CountBoosts(gen, defender.Boosts));
          desc.MoveBP = basePower;
          break;
        case "Pursuit":
          var switching = field.DefenderSide.IsSwitching == "out";
          basePower = move.Bp * (switching ? 2 : 1);
          if (switching) desc.IsSwitching = "out";
          desc.MoveBP = basePower;
          break;
        case "Wake-Up Slap":
          if (defender.HasStatus("slp")) {
            basePower *= 2;
            desc.MoveBP = basePower;
          }
          break;
        case "Nature Power":
          move.Category = MoveCategories.Special;
          move.Secondaries = true;
          basePower = 80;
          desc.MoveName = "Tri Attack";
          break;
        case "Crush Grip":
        case "Wring Out":
          basePower = (int)Math.Floor((defender.CurHP() * 120.0) / defender.MaxHP()) + 1;
          desc.MoveBP = basePower;
          break;
        case "Triple Kick":
          basePower = hit * 10;
          desc.MoveBP = move.Hits == 2 ? 30 : move.Hits == 3 ? 60 : 10;
          break;
        case "Weather Ball":
          basePower = move.Bp * (field.Weather != null ? 2 : 1);
          desc.MoveBP = basePower;
          break;
        default:
          basePower = move.Bp;
          break;
      }
      return basePower;
    }

    public static int CalculateBPModsDPP(Pokemon attacker, Pokemon defender, Move move, Field field, RawDesc desc, int basePower) {
      if (field.AttackerSide.IsHelpingHand) {
        basePower = (int)Math.Floor(basePower * 1.5);
        desc.IsHelpingHand = true;
      }

      if (attacker.HasAbility("Technician") && basePower <= 60) {
        basePower = (int)Math.Floor(basePower * 1.5);
        desc.AttackerAbility = attacker.Ability;
      }

      var isPhysical = move.Category == MoveCategories.Physical;
      if ((attacker.HasItem("Muscle Band") && isPhysical) ||
          (attacker.HasItem("Wise Glasses") && !isPhysical)) {
        basePower = (int)Math.Floor(basePower * 1.1);
        desc.AttackerItem = attacker.Item;
      } else if (move.HasType(Items.GetItemBoostType(attacker.Item)) ||
                 (attacker.HasItem("Adamant Orb") && attacker.Named("Dialga") && move.HasType("Steel", "Dragon")) ||
                 (attacker.HasItem("Lustrous Orb") && attacker.Named("Palkia") && move.HasType("Water", "Dragon")) ||
                 (attacker.HasItem("Griseous Orb") && attacker.Named("Giratina-Origin") && move.HasType("Ghost", "Dragon"))) {
        basePower = (int)Math.Floor(basePower * 1.2);
        desc.AttackerItem = attacker.Item;
      }

      if ((attacker.HasAbility("Reckless") && (move.Recoil != null || move.HasCrashDamage)) ||
          (attacker.HasAbility("Iron Fist") && move.Flags.Punch)) {
        basePower = (int)Math.Floor(basePower * 1.2);
        desc.AttackerAbility = attacker.Ability;
      } else if (attacker.CurHP() <= attacker.MaxHP() / 3 &&
                 ((attacker.HasAbility("Overgrow") && move.HasType("Grass")) ||
                  (attacker.HasAbility("Blaze") && move.HasType("Fire")) ||
                  (attacker.HasAbility("Torrent") && move.HasType("Water")) ||
                  (attacker.HasAbility("Swarm") && move.HasType("Bug")))) {
        basePower = (int)Math.Floor(basePower * 1.5);
        desc.AttackerAbility = attacker.Ability;
      }

      if ((defender.HasAbility("Heatproof") && move.HasType("Fire")) ||
          (defender.HasAbility("Thick Fat") && move.HasType("Fire", "Ice"))) {
        basePower = (int)Math.Floor(basePower * 0.5);
        desc.DefenderAbility = defender.Ability;
      } else if (defender.HasAbility("Dry Skin") && move.HasType("Fire")) {
        basePower = (int)Math.Floor(basePower * 1.25);
        desc.DefenderAbility = defender.Ability;
      }

      if (attacker.HasAbility("Rivalry") && !(attacker.Gender == "N" || defender.Gender == "N")) {
        if (attacker.Gender == defender.Gender) {
          basePower = (int)Math.Floor(basePower * 1.25);
          desc.Rivalry = "buffed";
        } else {
          basePower = (int)Math.Floor(basePower * 0.75);
          desc.Rivalry = "nerfed";
        }
        desc.AttackerAbility = attacker.Ability;
      }

      return basePower;
    }

    public static int CalculateAttackDPP(IGeneration gen, Pokemon attacker, Pokemon defender, Move move, Field field, RawDesc desc, bool isCritical = false) {
      var isPhysical = move.Category == MoveCategories.Physical;
      var attackStat = isPhysical ? StatId.Atk : StatId.Spa;
      desc.AttackEVs = MechanicsUtil.GetStatDescriptionText(gen, attacker, attackStat, attacker.Nature);

      var attackBoost = attacker.Boosts[attackStat];
      var rawAttack = attacker.RawStats[attackStat];

      if (field.AttackerSide.IsPowerTrick == true && isPhysical) {
        desc.IsPowerTrickAttacker = true;
        rawAttack = attacker.RawStats.Def;
      }

      int attack;
      if (attackBoost == 0 || (isCritical && attackBoost < 0)) {
        attack = rawAttack;
      } else if (defender.HasAbility("Unaware")) {
        attack = rawAttack;
        desc.DefenderAbility = defender.Ability;
      } else if (attacker.HasAbility("Simple")) {
        attack = GetSimpleModifiedStat(rawAttack, attackBoost);
        desc.AttackerAbility = attacker.Ability;
        desc.AttackBoost = attackBoost;
      } else {
        attack = MechanicsUtil.GetModifiedStat(rawAttack, attackBoost);
        desc.AttackBoost = attackBoost;
      }

      if (isPhysical && (attacker.HasAbility("Pure Power") || attacker.HasAbility("Huge Power"))) {
        attack *= 2;
        desc.AttackerAbility = attacker.Ability;
      } else if (field.HasWeather("Sun") && (attacker.HasAbility(isPhysical ? "Flower Gift" : "Solar Power"))) {
        attack = (int)Math.Floor(attack * 1.5);
        desc.AttackerAbility = attacker.Ability;
        desc.Weather = field.Weather;
      } else if (isPhysical && (attacker.HasAbility("Hustle") || (attacker.HasAbility("Guts") && !string.IsNullOrEmpty(attacker.Status))) ||
                 (!isPhysical && attacker.AbilityOn && (attacker.HasAbility("Plus") || attacker.HasAbility("Minus")))) {
        attack = (int)Math.Floor(attack * 1.5);
        desc.AttackerAbility = attacker.Ability;
      } else if (isPhysical && attacker.HasAbility("Slow Start") && attacker.AbilityOn) {
        attack = (int)Math.Floor(attack / 2.0);
        desc.AttackerAbility = attacker.Ability;
      }

      if (field.AttackerSide.IsFlowerGift && !attacker.HasAbility("Flower Gift") && field.HasWeather("Sun") && isPhysical) {
        attack = (int)Math.Floor(attack * 1.5);
        desc.Weather = field.Weather;
        desc.IsFlowerGiftAttacker = true;
      }

      if ((isPhysical ? attacker.HasItem("Choice Band") : attacker.HasItem("Choice Specs")) ||
          (!isPhysical && attacker.HasItem("Soul Dew") && attacker.Named("Latios", "Latias"))) {
        attack = (int)Math.Floor(attack * 1.5);
        desc.AttackerItem = attacker.Item;
      } else if ((attacker.HasItem("Light Ball") && attacker.Named("Pikachu")) ||
                 (attacker.HasItem("Thick Club") && attacker.Named("Cubone", "Marowak") && isPhysical) ||
                 (attacker.HasItem("Deep Sea Tooth") && attacker.Named("Clamperl") && !isPhysical)) {
        attack *= 2;
        desc.AttackerItem = attacker.Item;
      }

      return attack;
    }

    public static int CalculateDefenseDPP(IGeneration gen, Pokemon attacker, Pokemon defender, Move move, Field field, RawDesc desc, bool isCritical = false) {
      var isPhysical = move.Category == MoveCategories.Physical;
      var defenseStat = isPhysical ? StatId.Def : StatId.Spd;
      desc.DefenseEVs = MechanicsUtil.GetStatDescriptionText(gen, defender, defenseStat, defender.Nature);

      var defenseBoost = defender.Boosts[defenseStat];
      var rawDefense = defender.RawStats[defenseStat];

      if (field.DefenderSide.IsPowerTrick == true && isPhysical) {
        desc.IsPowerTrickDefender = true;
        rawDefense = defender.RawStats.Atk;
      }

      int defense;
      if (defenseBoost == 0 || (isCritical && defenseBoost > 0)) {
        defense = rawDefense;
      } else if (attacker.HasAbility("Unaware")) {
        defense = rawDefense;
        desc.AttackerAbility = attacker.Ability;
      } else if (defender.HasAbility("Simple")) {
        defense = GetSimpleModifiedStat(rawDefense, defenseBoost);
        desc.DefenderAbility = defender.Ability;
        desc.DefenseBoost = defenseBoost;
      } else {
        defense = MechanicsUtil.GetModifiedStat(rawDefense, defenseBoost);
        desc.DefenseBoost = defenseBoost;
      }

      if (defender.HasAbility("Marvel Scale") && !string.IsNullOrEmpty(defender.Status) && isPhysical) {
        defense = (int)Math.Floor(defense * 1.5);
        desc.DefenderAbility = defender.Ability;
      } else if (defender.HasAbility("Flower Gift") && field.HasWeather("Sun") && !isPhysical) {
        defense = (int)Math.Floor(defense * 1.5);
        desc.DefenderAbility = defender.Ability;
        desc.Weather = field.Weather;
      } else if (field.DefenderSide.IsFlowerGift && field.HasWeather("Sun") && !isPhysical) {
        defense = (int)Math.Floor(defense * 1.5);
        desc.Weather = field.Weather;
        desc.IsFlowerGiftDefender = true;
      }

      if (defender.HasItem("Soul Dew") && defender.Named("Latios", "Latias") && !isPhysical) {
        defense = (int)Math.Floor(defense * 1.5);
        desc.DefenderItem = defender.Item;
      } else if ((defender.HasItem("Deep Sea Scale") && defender.Named("Clamperl") && !isPhysical) ||
                 (defender.HasItem("Metal Powder") && defender.Named("Ditto") && isPhysical)) {
        defense *= 2;
        desc.DefenderItem = defender.Item;
      }

      if (field.HasWeather("Sand") && defender.HasType("Rock") && !isPhysical) {
        defense = (int)Math.Floor(defense * 1.5);
        desc.Weather = field.Weather;
      }

      if (move.Named("Explosion") || move.Named("Self-Destruct")) {
        defense = (int)Math.Floor(defense * 0.5);
      }

      if (defense < 1) defense = 1;
      return defense;
    }

    private static int CalculateFinalModsDPP(int baseDamage, Pokemon attacker, Move move, Field field, RawDesc desc, bool isCritical = false) {
      var isPhysical = move.Category == MoveCategories.Physical;
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

      if (field.GameType != GameTypes.Singles && (move.Target == "allAdjacent" || move.Target == "allAdjacentFoes")) {
        baseDamage = (int)Math.Floor((baseDamage * 3.0) / 4.0);
      }

      if ((field.HasWeather("Sun") && move.HasType("Fire")) || (field.HasWeather("Rain") && move.HasType("Water"))) {
        baseDamage = (int)Math.Floor(baseDamage * 1.5);
        desc.Weather = field.Weather;
      } else if ((field.HasWeather("Sun") && move.HasType("Water")) ||
                 (field.HasWeather("Rain") && move.HasType("Fire")) ||
                 (move.Named("Solar Beam") && field.HasWeather("Rain", "Sand", "Hail"))) {
        baseDamage = (int)Math.Floor(baseDamage * 0.5);
        desc.Weather = field.Weather;
      }

      if (attacker.HasAbility("Flash Fire") && attacker.AbilityOn && move.HasType("Fire")) {
        baseDamage = (int)Math.Floor(baseDamage * 1.5);
        desc.AttackerAbility = "Flash Fire";
      }

      baseDamage += 2;

      if (isCritical) {
        if (attacker.HasAbility("Sniper")) {
          baseDamage *= 3;
          desc.AttackerAbility = attacker.Ability;
        } else {
          baseDamage *= 2;
        }
        desc.IsCritical = isCritical;
      }

      if (attacker.HasItem("Life Orb")) {
        baseDamage = (int)Math.Floor(baseDamage * 1.3);
        desc.AttackerItem = attacker.Item;
      }

      return baseDamage;
    }

    private static int GetSimpleModifiedStat(int stat, int mod) {
      var simpleMod = Math.Min(6, Math.Max(-6, mod * 2));
      if (simpleMod > 0) return (int)Math.Floor((stat * (2.0 + simpleMod)) / 2.0);
      if (simpleMod < 0) return (int)Math.Floor((stat * 2.0) / (2.0 - simpleMod));
      return stat;
    }
  }
}
