using System;
using System.Collections.Generic;
using DamageCalc;
using DamageCalc.Data;

namespace DamageCalc.Mechanics {
  public static class Gen56 {
    public static Result CalculateBWXY(IGeneration gen, Pokemon attacker, Pokemon defender, Move move, Field field) {
      MechanicsUtil.CheckAirLock(attacker, field);
      MechanicsUtil.CheckAirLock(defender, field);
      MechanicsUtil.CheckForecast(attacker, field.Weather);
      MechanicsUtil.CheckForecast(defender, field.Weather);
      MechanicsUtil.CheckItem(attacker, field.IsMagicRoom);
      MechanicsUtil.CheckItem(defender, field.IsMagicRoom);
      MechanicsUtil.CheckWonderRoom(attacker, field.IsWonderRoom);
      MechanicsUtil.CheckWonderRoom(defender, field.IsWonderRoom);
      MechanicsUtil.CheckSeedBoost(attacker, field);
      MechanicsUtil.CheckSeedBoost(defender, field);

      MechanicsUtil.ComputeFinalStats(gen, attacker, defender, field, StatId.Def, StatId.Spd, StatId.Spe);

      MechanicsUtil.CheckIntimidate(gen, attacker, defender);
      MechanicsUtil.CheckIntimidate(gen, defender, attacker);
      MechanicsUtil.CheckDownload(attacker, defender, field.IsWonderRoom);
      MechanicsUtil.CheckDownload(defender, attacker, field.IsWonderRoom);

      MechanicsUtil.ComputeFinalStats(gen, attacker, defender, field, StatId.Atk, StatId.Spa);

      MechanicsUtil.CheckInfiltrator(attacker, field.DefenderSide);
      MechanicsUtil.CheckInfiltrator(defender, field.AttackerSide);

      var desc = new RawDesc {
        AttackerName = attacker.Name,
        MoveName = move.Name,
        DefenderName = defender.Name,
        IsWonderRoom = field.IsWonderRoom,
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
        "Aroma Veil", "Aura Break", "Battle Armor", "Big Pecks",
        "Bulletproof", "Clear Body", "Contrary", "Damp",
        "Dark Aura", "Dry Skin", "Fairy Aura", "Filter",
        "Flash Fire", "Flower Gift", "Flower Veil", "Friend Guard",
        "Fur Coat", "Grass Pelt", "Heatproof", "Heavy Metal",
        "Hyper Cutter", "Immunity", "Inner Focus", "Insomnia",
        "Keen Eye", "Leaf Guard", "Levitate", "Light Metal",
        "Lightning Rod", "Limber", "Magic Bounce", "Magma Armor",
        "Marvel Scale", "Motor Drive", "Multiscale", "Oblivious",
        "Overcoat", "Own Tempo", "Sand Veil", "Sap Sipper",
        "Shell Armor", "Shield Dust", "Simple", "Snow Cloak",
        "Solid Rock", "Soundproof", "Sticky Hold", "Storm Drain",
        "Sturdy", "Suction Cups", "Sweet Veil", "Tangled Feet",
        "Telepathy", "Thick Fat", "Unaware", "Vital Spirit",
        "Volt Absorb", "Water Absorb", "Water Veil", "White Smoke",
        "Wonder Guard", "Wonder Skin"
      );

      if (attacker.HasAbility("Mold Breaker") || attacker.HasAbility("Teravolt") || attacker.HasAbility("Turboblaze")) {
        if (defenderAbilityIgnored) {
          defender.Ability = "";
          desc.AttackerAbility = attacker.Ability;
        }
      }

      var isCritical = move.IsCrit && !defender.HasAbility("Battle Armor", "Shell Armor") && (move.TimesUsed ?? 1) == 1;

      if (move.Named("Weather Ball")) {
        move.Type = field.HasWeather("Sun", "Harsh Sunshine") ? "Fire"
          : field.HasWeather("Rain", "Heavy Rain") ? "Water"
          : field.HasWeather("Sand") ? "Rock"
          : field.HasWeather("Hail") ? "Ice"
          : "Normal";
        desc.Weather = field.Weather;
        desc.MoveType = move.Type;
      } else if (move.Named("Judgment") && !string.IsNullOrEmpty(attacker.Item) && attacker.Item.Contains("Plate")) {
        move.Type = Items.GetItemBoostType(attacker.Item) ?? move.Type;
      } else if (move.Named("Techno Blast") && !string.IsNullOrEmpty(attacker.Item) && attacker.Item.Contains("Drive")) {
        move.Type = Items.GetTechnoBlast(attacker.Item) ?? move.Type;
      } else if (move.Named("Natural Gift") && attacker.Item != null && attacker.Item.EndsWith("Berry")) {
        var gift = Items.GetNaturalGift(gen, attacker.Item);
        move.Type = gift.type;
        move.Bp = gift.power;
        desc.AttackerItem = attacker.Item;
        desc.MoveBP = move.Bp;
        desc.MoveType = move.Type;
      } else if (move.Named("Nature Power")) {
        if (gen.Num == 5) {
          move.Type = "Ground";
        } else {
          move.Type = field.HasTerrain("Electric") ? "Electric"
            : field.HasTerrain("Grassy") ? "Grass"
            : field.HasTerrain("Misty") ? "Fairy"
            : "Normal";
        }
      } else if (move.Named("Brick Break")) {
        field.DefenderSide.IsReflect = false;
        field.DefenderSide.IsLightScreen = false;
      }

      var hasAteAbilityTypeChange = false;
      var isAerilate = false;
      var isPixilate = false;
      var isRefrigerate = false;
      var isNormalize = false;
      var noTypeChange = move.Named("Judgment", "Nature Power", "Techo Blast", "Natural Gift", "Weather Ball", "Struggle");

      if (!move.IsZ && !noTypeChange) {
        var normal = move.HasType("Normal");
        if ((isAerilate = attacker.HasAbility("Aerilate") && normal)) {
          move.Type = "Flying";
        } else if ((isPixilate = attacker.HasAbility("Pixilate") && normal)) {
          move.Type = "Fairy";
        } else if ((isRefrigerate = attacker.HasAbility("Refrigerate") && normal)) {
          move.Type = "Ice";
        } else if ((isNormalize = attacker.HasAbility("Normalize"))) {
          move.Type = "Normal";
        }
        if (isPixilate || isRefrigerate || isAerilate || isNormalize) {
          desc.AttackerAbility = attacker.Ability;
        }
        if (isPixilate || isRefrigerate || isAerilate) {
          hasAteAbilityTypeChange = true;
        }
      }

      if (attacker.HasAbility("Gale Wings") && move.HasType("Flying")) {
        move.Priority = 1;
        desc.AttackerAbility = attacker.Ability;
      }

      var isGhostRevealed = attacker.HasAbility("Scrappy") || field.DefenderSide.IsForesight;
      var isRingTarget = defender.HasItem("Ring Target") && !defender.HasAbility("Klutz");
      var type1Effectiveness = MechanicsUtil.GetMoveEffectiveness(
        gen,
        move,
        defender.Types[0],
        isGhostRevealed,
        field.IsGravity,
        isRingTarget
      );
      var type2Effectiveness = defender.Types.Length > 1
        ? MechanicsUtil.GetMoveEffectiveness(gen, move, defender.Types[1], isGhostRevealed, field.IsGravity, isRingTarget)
        : 1;
      var typeEffectiveness = type1Effectiveness * type2Effectiveness;

      if (typeEffectiveness == 0 && move.Named("Thousand Arrows")) {
        typeEffectiveness = 1;
      } else if (typeEffectiveness == 0 && move.HasType("Ground") && defender.HasItem("Iron Ball") && !defender.HasAbility("Klutz")) {
        typeEffectiveness = 1;
      }

      if (typeEffectiveness == 0) return result;

      if ((move.Named("Sky Drop") && (defender.HasType("Flying") || defender.WeightKg >= 200 || field.IsGravity)) ||
          (move.Named("Synchronoise") && !defender.HasType(attacker.Types[0]) && (attacker.Types.Length < 2 || !defender.HasType(attacker.Types[1]))) ||
          (move.Named("Dream Eater") && !defender.HasStatus("slp"))) {
        return result;
      }

      if ((field.HasWeather("Harsh Sunshine") && move.HasType("Water")) ||
          (field.HasWeather("Heavy Rain") && move.HasType("Fire"))) {
        desc.Weather = field.Weather;
        return result;
      }

      if (field.HasWeather("Strong Winds") && defender.HasType("Flying") &&
          (gen.Types.Get(Util.ToId(move.Type))?.Effectiveness["Flying"] ?? 1) > 1) {
        typeEffectiveness /= 2;
        desc.Weather = field.Weather;
      }

      if ((defender.HasAbility("Wonder Guard") && typeEffectiveness <= 1) ||
          (move.HasType("Grass") && defender.HasAbility("Sap Sipper")) ||
          (move.HasType("Fire") && defender.HasAbility("Flash Fire")) ||
          (move.HasType("Water") && defender.HasAbility("Dry Skin", "Storm Drain", "Water Absorb")) ||
          (move.HasType("Electric") && defender.HasAbility("Lightning Rod", "Motor Drive", "Volt Absorb")) ||
          (move.HasType("Ground") && !field.IsGravity && !move.Named("Thousand Arrows") && !defender.HasItem("Iron Ball") && defender.HasAbility("Levitate")) ||
          (move.Flags.Bullet && defender.HasAbility("Bulletproof")) ||
          (move.Flags.Sound && defender.HasAbility("Soundproof"))) {
        desc.DefenderAbility = defender.Ability;
        return result;
      }

      if (move.HasType("Ground") && !move.Named("Thousand Arrows") && !field.IsGravity && defender.HasItem("Air Balloon")) {
        desc.DefenderItem = defender.Item;
        return result;
      }

      if (move.Priority > 0 && field.HasTerrain("Psychic") && MechanicsUtil.IsGrounded(defender, field)) {
        desc.Terrain = field.Terrain;
        return result;
      }

      desc.HPEVs = MechanicsUtil.GetStatDescriptionText(gen, defender, StatId.Hp, null);

      var fixedDamage = MechanicsUtil.HandleFixedDamageMoves(attacker, move);
      if (fixedDamage != 0) {
        if (attacker.HasAbility("Parental Bond")) {
          result.Damage = new[] { fixedDamage, fixedDamage };
          desc.AttackerAbility = attacker.Ability;
        } else {
          result.Damage = fixedDamage;
        }
        return result;
      }

      if (move.Named("Final Gambit")) {
        result.Damage = attacker.CurHP();
        return result;
      }

      if (move.Hits > 1) desc.Hits = move.Hits;

      var basePower = CalculateBasePowerBWXY(gen, attacker, defender, move, field, hasAteAbilityTypeChange, desc);
      if (basePower == 0) return result;

      var attack = CalculateAttackBWXY(gen, attacker, defender, move, field, desc, isCritical);
      var attackStat = move.Category == MoveCategories.Special ? StatId.Spa : StatId.Atk;

      var defense = CalculateDefenseBWXY(gen, attacker, defender, move, field, desc, isCritical);

      var baseDamage = CalculateBaseDamageBWXY(gen, attacker, basePower, attack, defense, move, field, desc, isCritical);

      var stabMod = MechanicsUtil.GetStabMod(attacker, move, desc);

      var applyBurn = !string.IsNullOrEmpty(attacker.Status) &&
        move.Category == MoveCategories.Physical &&
        !attacker.HasAbility("Guts") &&
        !(move.Named("Facade") && gen.Num == 6);
      desc.IsBurned = applyBurn;

      var finalMods = CalculateFinalModsBWXY(gen, attacker, defender, move, field, desc, isCritical, typeEffectiveness);
      var finalMod = MechanicsUtil.ChainMods(finalMods, 41, 131072);

      var isSpread = field.GameType != GameTypes.Singles && (move.Target == "allAdjacent" || move.Target == "allAdjacentFoes");

      int[]? childDamage = null;
      if (attacker.HasAbility("Parental Bond") && move.Hits == 1 && !isSpread) {
        var child = attacker.Clone();
        child.Ability = "Parental Bond (Child)";
        MechanicsUtil.CheckMultihitBoost(gen, child, defender, move, field, desc);
        childDamage = (int[])CalculateBWXY(gen, child, defender, move, field).Damage;
        desc.AttackerAbility = attacker.Ability;
      }

      var dmg = new int[16];
      for (var i = 0; i < 16; i++) {
        dmg[i] = MechanicsUtil.GetFinalDamage(baseDamage, i, typeEffectiveness, applyBurn, stabMod, finalMod);
      }
      result.Damage = childDamage != null ? (object)new int[][] { dmg, childDamage } : dmg;

      desc.AttackBoost = move.Named("Foul Play") ? defender.Boosts[attackStat] : attacker.Boosts[attackStat];

      if ((move.TimesUsed ?? 1) > 1 || move.Hits > 1) {
        var damageMatrix = new int[Math.Max(move.Hits, move.TimesUsed ?? 1)][];
        damageMatrix[0] = dmg;
        var origDefBoost = desc.DefenseBoost;
        var origAtkBoost = desc.AttackBoost;
        var numAttacks = 1;
        if ((move.TimesUsed ?? 1) > 1) {
          desc.MoveTurns = $"over {move.TimesUsed} turns";
          numAttacks = move.TimesUsed ?? 1;
        } else {
          numAttacks = move.Hits;
        }
        var usedItems = (attackerUsed: false, defenderUsed: false);
        for (var times = 1; times < numAttacks; times++) {
          usedItems = MechanicsUtil.CheckMultihitBoost(gen, attacker, defender, move, field, desc, usedItems.attackerUsed, usedItems.defenderUsed);
          var newAtk = CalculateAttackBWXY(gen, attacker, defender, move, field, desc, isCritical);
          var newDef = CalculateDefenseBWXY(gen, attacker, defender, move, field, desc, isCritical);

          hasAteAbilityTypeChange = hasAteAbilityTypeChange && attacker.HasAbility("Aerilate", "Galvanize", "Pixilate", "Refrigerate");

          if ((move.TimesUsed ?? 1) > 1) {
            stabMod = MechanicsUtil.GetStabMod(attacker, move, desc);
          }

          var newBasePower = CalculateBasePowerBWXY(gen, attacker, defender, move, field, hasAteAbilityTypeChange, desc);
          var newBaseDamage = MechanicsUtil.GetBaseDamage(attacker.Level, newBasePower, newAtk, newDef);
          var newFinalMods = CalculateFinalModsBWXY(gen, attacker, defender, move, field, desc, isCritical, typeEffectiveness, times);
          var newFinalMod = MechanicsUtil.ChainMods(newFinalMods, 41, 131072);

          var damageArray = new int[16];
          for (var i = 0; i < 16; i++) {
            damageArray[i] = MechanicsUtil.GetFinalDamage(newBaseDamage, i, typeEffectiveness, applyBurn, stabMod, newFinalMod);
          }
          damageMatrix[times] = damageArray;
        }
        result.Damage = damageMatrix;
        desc.DefenseBoost = origDefBoost;
        desc.AttackBoost = origAtkBoost;
      }

      return result;
    }

    public static int CalculateBasePowerBWXY(
      IGeneration gen,
      Pokemon attacker,
      Pokemon defender,
      Move move,
      Field field,
      bool hasAteAbilityTypeChange,
      RawDesc desc,
      int hit = 1
    ) {
      int basePower;
      var turnOrder = attacker.Stats.Spe > defender.Stats.Spe ? "first" : "last";

      switch (move.Name) {
        case "Payback":
          basePower = move.Bp * (turnOrder == "last" ? 2 : 1);
          desc.MoveBP = basePower;
          break;
        case "Pursuit":
          var switching = field.DefenderSide.IsSwitching == "out";
          basePower = move.Bp * (switching ? 2 : 1);
          if (switching) desc.IsSwitching = "out";
          desc.MoveBP = basePower;
          break;
        case "Electro Ball":
          if (defender.Stats.Spe == 0) defender.Stats.Spe = 1;
          var r = (int)Math.Floor(attacker.Stats.Spe / (double)defender.Stats.Spe);
          basePower = r >= 4 ? 150 : r >= 3 ? 120 : r >= 2 ? 80 : r >= 1 ? 60 : 40;
          desc.MoveBP = basePower;
          break;
        case "Gyro Ball":
          if (attacker.Stats.Spe == 0) attacker.Stats.Spe = 1;
          basePower = Math.Min(150, (int)Math.Floor((25 * defender.Stats.Spe) / (double)attacker.Stats.Spe) + 1);
          desc.MoveBP = basePower;
          break;
        case "Punishment":
          basePower = Math.Min(200, 60 + 20 * MechanicsUtil.CountBoosts(gen, defender.Boosts));
          desc.MoveBP = basePower;
          break;
        case "Low Kick":
        case "Grass Knot":
          var w = MechanicsUtil.GetWeight(defender, desc, "defender");
          basePower = w >= 200 ? 120 : w >= 100 ? 100 : w >= 50 ? 80 : w >= 25 ? 60 : w >= 10 ? 40 : 20;
          desc.MoveBP = basePower;
          break;
        case "Hex":
          basePower = move.Bp * (!string.IsNullOrEmpty(defender.Status) ? 2 : 1);
          desc.MoveBP = basePower;
          break;
        case "Heavy Slam":
        case "Heat Crash":
          var wr = MechanicsUtil.GetWeight(attacker, desc, "attacker") / MechanicsUtil.GetWeight(defender, desc, "defender");
          basePower = wr >= 5 ? 120 : wr >= 4 ? 100 : wr >= 3 ? 80 : wr >= 2 ? 60 : 40;
          desc.MoveBP = basePower;
          break;
        case "Stored Power":
        case "Power Trip":
          basePower = 20 + 20 * MechanicsUtil.CountBoosts(gen, attacker.Boosts);
          desc.MoveBP = basePower;
          break;
        case "Acrobatics":
          basePower = move.Bp * (attacker.HasItem("Flying Gem") || string.IsNullOrEmpty(attacker.Item) ? 2 : 1);
          desc.MoveBP = basePower;
          break;
        case "Assurance":
          basePower = move.Bp * (defender.HasAbility("Parental Bond (Child)") ? 2 : 1);
          break;
        case "Wake-Up Slap":
          basePower = move.Bp * (defender.HasStatus("slp") ? 2 : 1);
          desc.MoveBP = basePower;
          break;
        case "Smelling Salts":
          basePower = move.Bp * (defender.HasStatus("par") ? 2 : 1);
          desc.MoveBP = basePower;
          break;
        case "Weather Ball":
          basePower = move.Bp * (field.Weather != null && !field.HasWeather("Strong Winds") ? 2 : 1);
          desc.MoveBP = basePower;
          break;
        case "Fling":
          basePower = Items.GetFlingPower(attacker.Item, gen.Num);
          desc.MoveBP = basePower;
          desc.AttackerItem = attacker.Item;
          break;
        case "Eruption":
        case "Water Spout":
          basePower = Math.Max(1, (int)Math.Floor((150 * attacker.CurHP()) / (double)attacker.MaxHP()));
          desc.MoveBP = basePower;
          break;
        case "Flail":
        case "Reversal":
          var p = (int)Math.Floor((48 * attacker.CurHP()) / (double)attacker.MaxHP());
          basePower = p <= 1 ? 200 : p <= 4 ? 150 : p <= 9 ? 100 : p <= 16 ? 80 : p <= 32 ? 40 : 20;
          desc.MoveBP = basePower;
          break;
        case "Nature Power":
          if (gen.Num == 5) {
            move.Category = MoveCategories.Physical;
            move.Target = "allAdjacent";
            basePower = 100;
            desc.MoveName = "Earthquake";
          } else {
            move.Category = MoveCategories.Special;
            move.Secondaries = true;
            switch (field.Terrain) {
              case "Electric":
                basePower = 90;
                desc.MoveName = "Thunderbolt";
                break;
              case "Grassy":
                basePower = 90;
                desc.MoveName = "Energy Ball";
                break;
              case "Misty":
                basePower = 95;
                desc.MoveName = "Moonblast";
                break;
              default:
                basePower = 80;
                desc.MoveName = "Tri Attack";
                break;
            }
          }
          break;
        case "Triple Kick":
          basePower = hit * 10;
          desc.MoveBP = move.Hits == 2 ? 30 : move.Hits == 3 ? 60 : 10;
          break;
        case "Crush Grip":
        case "Wring Out":
          basePower = 100 * (int)Math.Floor((defender.CurHP() * 4096.0) / defender.MaxHP());
          basePower = (int)Math.Floor(Math.Floor((120 * basePower + 2048 - 1) / 4096.0) / 100.0);
          basePower = basePower == 0 ? 1 : basePower;
          desc.MoveBP = basePower;
          break;
        default:
          basePower = move.Bp;
          break;
      }

      if (basePower == 0) return 0;

      var bpMods = CalculateBPModsBWXY(gen, attacker, defender, move, field, desc, basePower, hasAteAbilityTypeChange, turnOrder, hit);
      basePower = MechanicsUtil.OF16(Math.Max(1, MechanicsUtil.PokeRound((basePower * MechanicsUtil.ChainMods(bpMods, 41, 2097152)) / 4096.0)));
      return basePower;
    }

    public static List<int> CalculateBPModsBWXY(
      IGeneration gen,
      Pokemon attacker,
      Pokemon defender,
      Move move,
      Field field,
      RawDesc desc,
      int basePower,
      bool hasAteAbilityTypeChange,
      string turnOrder,
      int hit
    ) {
      var bpMods = new List<int>();

      var defenderItem = !string.IsNullOrEmpty(defender.Item) ? defender.Item : defender.DisabledItem;
      var resistedKnockOffDamage = string.IsNullOrEmpty(defenderItem) ||
        (defender.Named("Giratina-Origin") && defenderItem == "Griseous Orb") ||
        (defender.Name.Contains("Arceus") && defenderItem != null && defenderItem.Contains("Plate")) ||
        (defender.Name.Contains("Genesect") && defenderItem != null && defenderItem.Contains("Drive")) ||
        (defender.Named("Groudon", "Groudon-Primal") && defenderItem == "Red Orb") ||
        (defender.Named("Kyogre", "Kyogre-Primal") && defenderItem == "Blue Orb");

      if (!resistedKnockOffDamage && !string.IsNullOrEmpty(defenderItem)) {
        var item = gen.Items.Get(Util.ToId(defenderItem)) as Item;
        if (item?.MegaStone != null) {
          if (item.MegaStone.TryGetValue(defender.Name, out var _) || new List<string>(item.MegaStone.Values).Contains(defender.Name)) {
            resistedKnockOffDamage = true;
          }
        }
      }

      if (!resistedKnockOffDamage && hit > 1 && !defender.HasAbility("Sticky Hold")) {
        resistedKnockOffDamage = true;
      }

      if ((attacker.HasAbility("Technician") && basePower <= 60) ||
          (attacker.HasAbility("Flare Boost") && attacker.HasStatus("brn") && move.Category == MoveCategories.Special) ||
          (attacker.HasAbility("Toxic Boost") && attacker.HasStatus("psn", "tox") && move.Category == MoveCategories.Physical)) {
        bpMods.Add(6144);
        desc.AttackerAbility = attacker.Ability;
      } else if (attacker.HasAbility("Analytic") && turnOrder != "first") {
        bpMods.Add(5325);
        desc.AttackerAbility = attacker.Ability;
      } else if (attacker.HasAbility("Sand Force") && field.HasWeather("Sand") && move.HasType("Rock", "Ground", "Steel")) {
        bpMods.Add(5325);
        desc.AttackerAbility = attacker.Ability;
        desc.Weather = field.Weather;
      } else if ((attacker.HasAbility("Reckless") && (move.Recoil != null || move.HasCrashDamage)) ||
                 (attacker.HasAbility("Iron Fist") && move.Flags.Punch)) {
        bpMods.Add(4915);
        desc.AttackerAbility = attacker.Ability;
      }

      if (defender.HasAbility("Heatproof") && move.HasType("Fire")) {
        bpMods.Add(2048);
        desc.DefenderAbility = defender.Ability;
      } else if (defender.HasAbility("Dry Skin") && move.HasType("Fire")) {
        bpMods.Add(5120);
        desc.DefenderAbility = defender.Ability;
      }

      if (attacker.HasAbility("Sheer Force") && move.Secondaries != null) {
        bpMods.Add(5325);
        desc.AttackerAbility = attacker.Ability;
      }

      if (attacker.HasAbility("Rivalry") && !(attacker.Gender == "N" || defender.Gender == "N")) {
        if (attacker.Gender == defender.Gender) {
          bpMods.Add(5120);
          desc.Rivalry = "buffed";
        } else {
          bpMods.Add(3072);
          desc.Rivalry = "nerfed";
        }
        desc.AttackerAbility = attacker.Ability;
      }

      if (!string.IsNullOrEmpty(attacker.Item) && Items.GetItemBoostType(attacker.Item) == move.Type) {
        bpMods.Add(4915);
        desc.AttackerItem = attacker.Item;
      } else if ((attacker.HasItem("Muscle Band") && move.Category == MoveCategories.Physical) ||
                 (attacker.HasItem("Wise Glasses") && move.Category == MoveCategories.Special)) {
        bpMods.Add(4505);
        desc.AttackerItem = attacker.Item;
      } else if ((attacker.HasItem("Adamant Orb") && attacker.Named("Dialga") && move.HasType("Steel", "Dragon")) ||
                 (attacker.HasItem("Lustrous Orb") && attacker.Named("Palkia") && move.HasType("Water", "Dragon")) ||
                 (attacker.HasItem("Griseous Orb") && attacker.Named("Giratina-Origin") && move.HasType("Ghost", "Dragon"))) {
        bpMods.Add(4915);
        desc.AttackerItem = attacker.Item;
      } else if (attacker.HasItem($"{move.Type} Gem")) {
        bpMods.Add(gen.Num > 5 ? 5325 : 6144);
        desc.AttackerItem = attacker.Item;
      }

      if ((move.Named("Facade") && attacker.HasStatus("brn", "par", "psn", "tox")) ||
          (move.Named("Brine") && defender.CurHP() <= defender.MaxHP() / 2) ||
          (move.Named("Venoshock") && defender.HasStatus("psn", "tox"))) {
        bpMods.Add(8192);
        desc.MoveBP = basePower * 2;
      } else if (gen.Num > 5 && move.Named("Knock Off") && !resistedKnockOffDamage) {
        bpMods.Add(6144);
        desc.MoveBP = basePower * 1.5;
      } else if (move.Named("Solar Beam") && field.HasWeather("Rain", "Heavy Rain", "Sand", "Hail")) {
        bpMods.Add(2048);
        desc.MoveBP = basePower / 2;
        desc.Weather = field.Weather;
      }

      if (field.AttackerSide.IsHelpingHand) {
        bpMods.Add(6144);
        desc.IsHelpingHand = true;
      }

      if (hasAteAbilityTypeChange) {
        bpMods.Add(5325);
        desc.AttackerAbility = attacker.Ability;
      } else if ((attacker.HasAbility("Mega Launcher") && move.Flags.Pulse) ||
                 (attacker.HasAbility("Strong Jaw") && move.Flags.Bite)) {
        bpMods.Add(6144);
        desc.AttackerAbility = attacker.Ability;
      } else if (attacker.HasAbility("Tough Claws") && move.Flags.Contact) {
        bpMods.Add(5325);
        desc.AttackerAbility = attacker.Ability;
      }

      var aura = $"{move.Type} Aura";
      var isAttackerAura = attacker.HasAbility(aura);
      var isDefenderAura = defender.HasAbility(aura);
      var isUserAuraBreak = attacker.HasAbility("Aura Break") || defender.HasAbility("Aura Break");
      var isFieldAuraBreak = field.IsAuraBreak == true;
      var isFieldFairyAura = field.IsFairyAura == true && move.Type == "Fairy";
      var isFieldDarkAura = field.IsDarkAura == true && move.Type == "Dark";
      var auraActive = isAttackerAura || isDefenderAura || isFieldFairyAura || isFieldDarkAura;
      var auraBreak = isFieldAuraBreak || isUserAuraBreak;
      if (auraActive) {
        if (auraBreak) {
          bpMods.Add(3072);
          desc.AttackerAbility = attacker.Ability;
          desc.DefenderAbility = defender.Ability;
        } else {
          bpMods.Add(5448);
          if (isAttackerAura) desc.AttackerAbility = attacker.Ability;
          if (isDefenderAura) desc.DefenderAbility = defender.Ability;
        }
      }

      if (MechanicsUtil.IsGrounded(attacker, field)) {
        if ((field.HasTerrain("Electric") && move.HasType("Electric")) ||
            (field.HasTerrain("Grassy") && move.HasType("Grass"))) {
          bpMods.Add(6144);
          desc.Terrain = field.Terrain;
        }
      }
      if (MechanicsUtil.IsGrounded(defender, field)) {
        if ((field.HasTerrain("Misty") && move.HasType("Dragon")) ||
            (field.HasTerrain("Grassy") && move.Named("Bulldoze", "Earthquake"))) {
          bpMods.Add(2048);
          desc.Terrain = field.Terrain;
        }
      }

      return bpMods;
    }

    public static int CalculateAttackBWXY(IGeneration gen, Pokemon attacker, Pokemon defender, Move move, Field field, RawDesc desc, bool isCritical = false) {
      var attackSource = move.Named("Foul Play") ? defender : attacker;
      var attackStat = move.Category == MoveCategories.Special ? StatId.Spa : StatId.Atk;
      desc.AttackEVs = move.Named("Foul Play")
        ? MechanicsUtil.GetStatDescriptionText(gen, defender, attackStat, defender.Nature)
        : MechanicsUtil.GetStatDescriptionText(gen, attacker, attackStat, attacker.Nature);

      if (field.AttackerSide.IsPowerTrick == true && !move.Named("Foul Play") && move.Category == MoveCategories.Physical) {
        desc.IsPowerTrickAttacker = true;
        attackSource.RawStats[attackStat] = attacker.RawStats.Def;
      }

      int attack;
      if (attackSource.Boosts[attackStat] == 0 || (isCritical && attackSource.Boosts[attackStat] < 0)) {
        attack = attackSource.RawStats[attackStat];
      } else if (defender.HasAbility("Unaware")) {
        attack = attackSource.RawStats[attackStat];
        desc.DefenderAbility = defender.Ability;
      } else {
        attack = MechanicsUtil.GetModifiedStat(attackSource.RawStats[attackStat], attackSource.Boosts[attackStat]);
        desc.AttackBoost = attackSource.Boosts[attackStat];
      }

      if (attacker.HasAbility("Hustle") && move.Category == MoveCategories.Physical) {
        attack = MechanicsUtil.PokeRound((attack * 3.0) / 2.0);
        desc.AttackerAbility = attacker.Ability;
      }

      var atMods = CalculateAtModsBWXY(attacker, defender, move, field, desc);
      attack = MechanicsUtil.OF16(Math.Max(1, MechanicsUtil.PokeRound((attack * MechanicsUtil.ChainMods(atMods, 410, 131072)) / 4096.0)));
      return attack;
    }

    public static List<int> CalculateAtModsBWXY(Pokemon attacker, Pokemon defender, Move move, Field field, RawDesc desc) {
      var atMods = new List<int>();
      if (defender.HasAbility("Thick Fat") && move.HasType("Fire", "Ice")) {
        atMods.Add(2048);
        desc.DefenderAbility = defender.Ability;
      }

      if ((attacker.HasAbility("Guts") && !string.IsNullOrEmpty(attacker.Status) && move.Category == MoveCategories.Physical) ||
          (attacker.CurHP() <= attacker.MaxHP() / 3 &&
           ((attacker.HasAbility("Overgrow") && move.HasType("Grass")) ||
            (attacker.HasAbility("Blaze") && move.HasType("Fire")) ||
            (attacker.HasAbility("Torrent") && move.HasType("Water")) ||
            (attacker.HasAbility("Swarm") && move.HasType("Bug")))) ||
          (move.Category == MoveCategories.Special && attacker.AbilityOn && attacker.HasAbility("Plus", "Minus"))) {
        atMods.Add(6144);
        desc.AttackerAbility = attacker.Ability;
      } else if (attacker.HasAbility("Flash Fire") && attacker.AbilityOn && move.HasType("Fire")) {
        atMods.Add(6144);
        desc.AttackerAbility = "Flash Fire";
      } else if ((attacker.HasAbility("Solar Power") && field.HasWeather("Sun", "Harsh Sunshine") && move.Category == MoveCategories.Special) ||
                 (attacker.Named("Cherrim") && attacker.HasAbility("Flower Gift") && field.HasWeather("Sun", "Harsh Sunshine") && move.Category == MoveCategories.Physical)) {
        atMods.Add(6144);
        desc.AttackerAbility = attacker.Ability;
        desc.Weather = field.Weather;
      } else if ((attacker.HasAbility("Defeatist") && attacker.CurHP() <= attacker.MaxHP() / 2) ||
                 (attacker.HasAbility("Slow Start") && attacker.AbilityOn && move.Category == MoveCategories.Physical)) {
        atMods.Add(2048);
        desc.AttackerAbility = attacker.Ability;
      } else if (attacker.HasAbility("Huge Power", "Pure Power") && move.Category == MoveCategories.Physical) {
        atMods.Add(8192);
        desc.AttackerAbility = attacker.Ability;
      }

      if (field.AttackerSide.IsFlowerGift && !attacker.HasAbility("Flower Gift") && field.HasWeather("Sun", "Harsh Sunshine") && move.Category == MoveCategories.Physical) {
        atMods.Add(6144);
        desc.Weather = field.Weather;
        desc.IsFlowerGiftAttacker = true;
      }

      if ((attacker.HasItem("Thick Club") && attacker.Named("Cubone", "Marowak", "Marowak-Alola") && move.Category == MoveCategories.Physical) ||
          (attacker.HasItem("Deep Sea Tooth") && attacker.Named("Clamperl") && move.Category == MoveCategories.Special) ||
          (attacker.HasItem("Light Ball") && attacker.Name.StartsWith("Pikachu") && !move.IsZ)) {
        atMods.Add(8192);
        desc.AttackerItem = attacker.Item;
      } else if ((attacker.HasItem("Soul Dew") && attacker.Named("Latios", "Latias", "Latios-Mega", "Latias-Mega") && move.Category == MoveCategories.Special) ||
                 (attacker.HasItem("Choice Band") && move.Category == MoveCategories.Physical) ||
                 (attacker.HasItem("Choice Specs") && move.Category == MoveCategories.Special)) {
        atMods.Add(6144);
        desc.AttackerItem = attacker.Item;
      }

      return atMods;
    }

    public static int CalculateDefenseBWXY(IGeneration gen, Pokemon attacker, Pokemon defender, Move move, Field field, RawDesc desc, bool isCritical = false) {
      var defenseStat = move.OverrideDefensiveStat ?? (move.Category == MoveCategories.Physical ? StatId.Def : StatId.Spd);
      var hitsPhysical = defenseStat == StatId.Def;

      var boosts = defender.Boosts[field.IsWonderRoom ? (defenseStat == StatId.Spd ? StatId.Def : StatId.Spd) : defenseStat];

      if (field.DefenderSide.IsPowerTrick == true && hitsPhysical) {
        desc.IsPowerTrickDefender = true;
        defender.RawStats[defenseStat] = defender.RawStats.Atk;
      }

      desc.DefenseEVs = MechanicsUtil.GetStatDescriptionText(gen, defender, defenseStat, defender.Nature);
      int defense;
      if (boosts == 0 || (isCritical && boosts > 0) || move.IgnoreDefensive) {
        defense = defender.RawStats[defenseStat];
      } else if (attacker.HasAbility("Unaware")) {
        defense = defender.RawStats[defenseStat];
        desc.AttackerAbility = attacker.Ability;
      } else {
        defense = MechanicsUtil.GetModifiedStat(defender.RawStats[defenseStat], boosts);
        desc.DefenseBoost = boosts;
      }

      if (field.HasWeather("Sand") && defender.HasType("Rock") && !hitsPhysical) {
        defense = MechanicsUtil.PokeRound((defense * 3.0) / 2.0);
        desc.Weather = field.Weather;
      }

      var dfMods = CalculateDfModsBWXY(gen, defender, field, desc, hitsPhysical);
      defense = MechanicsUtil.OF16(Math.Max(1, MechanicsUtil.PokeRound((defense * MechanicsUtil.ChainMods(dfMods, 410, 131072)) / 4096.0)));
      return defense;
    }

    public static List<int> CalculateDfModsBWXY(IGeneration gen, Pokemon defender, Field field, RawDesc desc, bool hitsPhysical = false) {
      var dfMods = new List<int>();
      if (defender.HasAbility("Marvel Scale") && !string.IsNullOrEmpty(defender.Status) && hitsPhysical) {
        dfMods.Add(6144);
        desc.DefenderAbility = defender.Ability;
      } else if (defender.Named("Cherrim") && defender.HasAbility("Flower Gift") && field.HasWeather("Sun", "Harsh Sunshine") && !hitsPhysical) {
        dfMods.Add(6144);
        desc.DefenderAbility = defender.Ability;
        desc.Weather = field.Weather;
      } else if (field.DefenderSide.IsFlowerGift && field.HasWeather("Sun", "Harsh Sunshine") && !hitsPhysical) {
        dfMods.Add(6144);
        desc.Weather = field.Weather;
        desc.IsFlowerGiftDefender = true;
      }

      if (field.HasTerrain("Grassy") && defender.HasAbility("Grass Pelt") && hitsPhysical) {
        dfMods.Add(6144);
        desc.DefenderAbility = defender.Ability;
      }

      if ((!hitsPhysical && defender.HasItem("Soul Dew") && defender.Named("Latios", "Latias", "Latios-Mega", "Latias-Mega")) ||
          (defender.HasItem("Eviolite") && (gen.Species.Get(Util.ToId(defender.Name)) as Specie)?.Nfe == true) ||
          (!hitsPhysical && defender.HasItem("Assault Vest"))) {
        dfMods.Add(6144);
        desc.DefenderItem = defender.Item;
      }

      if ((defender.HasItem("Metal Powder") && defender.Named("Ditto") && hitsPhysical) ||
          (defender.HasItem("Deep Sea Scale") && defender.Named("Clamperl") && !hitsPhysical)) {
        dfMods.Add(8192);
        desc.DefenderItem = defender.Item;
      }

      if (defender.HasAbility("Fur Coat") && hitsPhysical) {
        dfMods.Add(8192);
        desc.DefenderAbility = defender.Ability;
      }

      return dfMods;
    }

    private static int CalculateBaseDamageBWXY(IGeneration gen, Pokemon attacker, int basePower, int attack, int defense, Move move, Field field, RawDesc desc, bool isCritical = false) {
      var baseDamage = MechanicsUtil.GetBaseDamage(attacker.Level, basePower, attack, defense);

      var isSpread = field.GameType != GameTypes.Singles && (move.Target == "allAdjacent" || move.Target == "allAdjacentFoes");
      if (isSpread) {
        baseDamage = MechanicsUtil.PokeRound(MechanicsUtil.OF32(baseDamage * 3072) / 4096.0);
      }

      if (attacker.HasAbility("Parental Bond (Child)")) {
        baseDamage = MechanicsUtil.PokeRound(MechanicsUtil.OF32(baseDamage * 2048) / 4096.0);
      }

      if ((field.HasWeather("Sun", "Harsh Sunshine") && move.HasType("Fire")) ||
          (field.HasWeather("Rain", "Heavy Rain") && move.HasType("Water"))) {
        baseDamage = MechanicsUtil.PokeRound(MechanicsUtil.OF32(baseDamage * 6144) / 4096.0);
        desc.Weather = field.Weather;
      } else if ((field.HasWeather("Sun") && move.HasType("Water")) ||
                 (field.HasWeather("Rain") && move.HasType("Fire"))) {
        baseDamage = MechanicsUtil.PokeRound(MechanicsUtil.OF32(baseDamage * 2048) / 4096.0);
        desc.Weather = field.Weather;
      }

      if (isCritical) {
        baseDamage = (int)Math.Floor((double)MechanicsUtil.OF32(baseDamage * (gen.Num > 5 ? 1.5 : 2)));
        desc.IsCritical = isCritical;
      }

      return baseDamage;
    }

    private static List<int> CalculateFinalModsBWXY(
      IGeneration gen,
      Pokemon attacker,
      Pokemon defender,
      Move move,
      Field field,
      RawDesc desc,
      bool isCritical,
      double typeEffectiveness,
      int hitCount = 0
    ) {
      var finalMods = new List<int>();

      if (field.DefenderSide.IsReflect && move.Category == MoveCategories.Physical && !isCritical) {
        finalMods.Add(field.GameType != GameTypes.Singles ? (gen.Num > 5 ? 2732 : 2703) : 2048);
        desc.IsReflect = true;
      } else if (field.DefenderSide.IsLightScreen && move.Category == MoveCategories.Special && !isCritical) {
        finalMods.Add(field.GameType != GameTypes.Singles ? (gen.Num > 5 ? 2732 : 2703) : 2048);
        desc.IsLightScreen = true;
      }

      if (defender.HasAbility("Multiscale") && defender.CurHP() == defender.MaxHP() && hitCount == 0 &&
          !field.DefenderSide.IsSR && (field.DefenderSide.Spikes == 0 || defender.HasType("Flying")) &&
          !attacker.HasAbility("Parental Bond (Child)")) {
        finalMods.Add(2048);
        desc.DefenderAbility = defender.Ability;
      }

      if (attacker.HasAbility("Tinted Lens") && typeEffectiveness < 1) {
        finalMods.Add(8192);
        desc.AttackerAbility = attacker.Ability;
      }

      if (field.DefenderSide.IsFriendGuard) {
        finalMods.Add(3072);
        desc.IsFriendGuard = true;
      }

      if (attacker.HasAbility("Sniper") && isCritical) {
        finalMods.Add(6144);
        desc.AttackerAbility = attacker.Ability;
      }

      if (defender.HasAbility("Solid Rock", "Filter") && typeEffectiveness > 1) {
        finalMods.Add(3072);
        desc.DefenderAbility = defender.Ability;
      }

      if (attacker.HasItem("Metronome") && (move.TimesUsedWithMetronome ?? 0) >= 1) {
        var timesUsedWithMetronome = (int)Math.Floor((double)(move.TimesUsedWithMetronome ?? 0));
        if (timesUsedWithMetronome <= 4) {
          finalMods.Add(4096 + timesUsedWithMetronome * 819);
        } else {
          finalMods.Add(8192);
        }
        desc.AttackerItem = attacker.Item;
      }

      if (attacker.HasItem("Expert Belt") && typeEffectiveness > 1 && !move.IsZ) {
        finalMods.Add(4915);
        desc.AttackerItem = attacker.Item;
      } else if (attacker.HasItem("Life Orb")) {
        finalMods.Add(5324);
        desc.AttackerItem = attacker.Item;
      }

      if (move.HasType(Items.GetBerryResistType(defender.Item)) &&
          (typeEffectiveness > 1 || move.HasType("Normal")) && hitCount == 0 &&
          !attacker.HasAbility("Unnerve")) {
        finalMods.Add(2048);
        desc.DefenderItem = defender.Item;
      }

      return finalMods;
    }
  }
}
