using System;
using System.Collections.Generic;
using Lusamine.DamageCalc;
using Lusamine.DamageCalc.Data;

namespace Lusamine.DamageCalc.Mechanics {
  public static class MechanicsUtil {
    private static readonly HashSet<string> EV_ITEMS = new HashSet<string> {
      "Macho Brace",
      "Power Anklet",
      "Power Band",
      "Power Belt",
      "Power Bracer",
      "Power Lens",
      "Power Weight",
    };

    public static bool IsGrounded(Pokemon pokemon, Field field) {
      return field.IsGravity || pokemon.HasItem("Iron Ball") ||
        (!pokemon.HasType("Flying") && !pokemon.HasAbility("Levitate") && !pokemon.HasItem("Air Balloon"));
    }

    public static int GetModifiedStat(int stat, int mod, IGeneration? gen = null) {
      if (gen != null && gen.Num < 3) {
        if (mod >= 0) {
          var pastGenBoostTable = new[] { 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0 };
          stat = (int)Math.Floor(stat * pastGenBoostTable[mod]);
        } else {
          var numerators = new[] { 100, 66, 50, 40, 33, 28, 25 };
          stat = (int)Math.Floor((stat * numerators[-mod]) / 100.0);
        }
        return Math.Min(999, Math.Max(1, stat));
      }

      var modernGenBoostTable = new[] {
        new[] { 2, 8 },
        new[] { 2, 7 },
        new[] { 2, 6 },
        new[] { 2, 5 },
        new[] { 2, 4 },
        new[] { 2, 3 },
        new[] { 2, 2 },
        new[] { 3, 2 },
        new[] { 4, 2 },
        new[] { 5, 2 },
        new[] { 6, 2 },
        new[] { 7, 2 },
        new[] { 8, 2 },
      };
      stat = OF16(stat * modernGenBoostTable[6 + mod][0]);
      stat = (int)Math.Floor(stat / (double)modernGenBoostTable[6 + mod][1]);

      return stat;
    }

    public static void ComputeFinalStats(IGeneration gen, Pokemon attacker, Pokemon defender, Field field, params StatId[] stats) {
      var sides = new List<(Pokemon pokemon, Side side)> {
        (attacker, field.AttackerSide),
        (defender, field.DefenderSide),
      };

      foreach (var (pokemon, side) in sides) {
        foreach (var stat in stats) {
          if (stat == StatId.Spe) {
            pokemon.Stats.Spe = GetFinalSpeed(gen, pokemon, field, side);
          } else {
            pokemon.Stats[stat] = GetModifiedStat(pokemon.RawStats[stat], pokemon.Boosts[stat], gen);
          }
        }
      }
    }

    public static int GetFinalSpeed(IGeneration gen, Pokemon pokemon, Field field, Side side) {
      var weather = field.Weather ?? "";
      var terrain = field.Terrain;
      var speed = GetModifiedStat(pokemon.RawStats.Spe, pokemon.Boosts.Spe, gen);
      var speedMods = new List<int>();

      if (side.IsTailwind) speedMods.Add(8192);

      if ((pokemon.HasAbility("Unburden") && pokemon.AbilityOn) ||
          (pokemon.HasAbility("Chlorophyll") && weather.Contains("Sun")) ||
          (pokemon.HasAbility("Sand Rush") && weather == "Sand") ||
          (pokemon.HasAbility("Swift Swim") && weather.Contains("Rain")) ||
          (pokemon.HasAbility("Slush Rush") && (weather == "Hail" || weather == "Snow")) ||
          (pokemon.HasAbility("Surge Surfer") && terrain == "Electric")) {
        speedMods.Add(8192);
      } else if (pokemon.HasAbility("Quick Feet") && !string.IsNullOrEmpty(pokemon.Status)) {
        speedMods.Add(6144);
      } else if (pokemon.HasAbility("Slow Start") && pokemon.AbilityOn) {
        speedMods.Add(2048);
      } else if (IsQPActive(pokemon, field) && GetQPBoostedStat(pokemon, gen) == StatId.Spe) {
        speedMods.Add(6144);
      }

      if (!(pokemon.HasAbility("Unburden") && pokemon.AbilityOn)) {
        if (pokemon.HasItem("Choice Scarf")) {
          speedMods.Add(6144);
        } else if (pokemon.HasItem("Iron Ball") || EV_ITEMS.Contains(pokemon.Item ?? "")) {
          speedMods.Add(2048);
        } else if (pokemon.HasItem("Quick Powder") && pokemon.Named("Ditto")) {
          speedMods.Add(8192);
        }
      }

      speed = OF32(PokeRound((speed * ChainMods(speedMods, 410, 131172)) / 4096.0));
      if (pokemon.HasStatus("par") && !pokemon.HasAbility("Quick Feet")) {
        speed = (int)Math.Floor(OF32(speed * (gen.Num < 7 ? 25 : 50)) / 100.0);
      }

      speed = Math.Min(gen.Num <= 2 ? 999 : 10000, speed);
      return Math.Max(0, speed);
    }

    public static double GetMoveEffectiveness(IGeneration gen, Move move, string type, bool isGhostRevealed = false, bool isGravity = false, bool isRingTarget = false) {
      if (isGhostRevealed && type == "Ghost" && move.HasType("Normal", "Fighting")) {
        return 1;
      }
      if (isGravity && type == "Flying" && move.HasType("Ground")) {
        return 1;
      }
      if (move.Named("Freeze-Dry") && type == "Water") {
        return 2;
      }
      if (move.Named("Nihil Light") && type == "Fairy") {
        return 1;
      }

      var moveTypeData = gen.Types.Get(Util.ToId(move.Type));
      var effectiveness = moveTypeData?.Effectiveness.TryGetValue(type, out var eff) == true ? eff : 1.0;
      if (effectiveness == 0 && isRingTarget) effectiveness = 1;
      if (move.Named("Flying Press")) {
        var flyingData = gen.Types.Get("flying");
        effectiveness *= flyingData?.Effectiveness.TryGetValue(type, out var flyEff) == true ? flyEff : 1.0;
      }
      return effectiveness;
    }

    public static void CheckAirLock(Pokemon pokemon, Field field) {
      if (pokemon.HasAbility("Air Lock") || pokemon.HasAbility("Cloud Nine")) field.Weather = null;
    }

    public static void CheckTeraformZero(Pokemon pokemon, Field field) {
      if (pokemon.HasAbility("Teraform Zero") && pokemon.AbilityOn) {
        field.Weather = null;
        field.Terrain = null;
      }
    }

    public static void CheckForecast(Pokemon pokemon, string? weather) {
      if (pokemon.HasAbility("Forecast") && pokemon.Named("Castform")) {
        switch (weather) {
          case "Sun":
          case "Harsh Sunshine":
            pokemon.Types = new[] { "Fire" };
            break;
          case "Rain":
          case "Heavy Rain":
            pokemon.Types = new[] { "Water" };
            break;
          case "Hail":
          case "Snow":
            pokemon.Types = new[] { "Ice" };
            break;
          default:
            pokemon.Types = new[] { "Normal" };
            break;
        }
      }
    }

    public static void CheckItem(Pokemon pokemon, bool magicRoomActive = false) {
      if (pokemon.Gen.Num == 4 && pokemon.HasItem("Iron Ball")) return;
      if ((pokemon.HasAbility("Klutz") && !EV_ITEMS.Contains(pokemon.Item ?? "")) || magicRoomActive) {
        pokemon.DisabledItem = pokemon.Item;
        pokemon.Item = "";
      }
    }

    public static void CheckWonderRoom(Pokemon pokemon, bool wonderRoomActive = false) {
      if (wonderRoomActive) {
        var def = pokemon.RawStats.Def;
        pokemon.RawStats.Def = pokemon.RawStats.Spd;
        pokemon.RawStats.Spd = def;
      }
    }

    public static void CheckIntimidate(IGeneration gen, Pokemon source, Pokemon target) {
      var blocked =
        target.HasAbility("Clear Body") ||
        target.HasAbility("White Smoke") ||
        target.HasAbility("Hyper Cutter") ||
        target.HasAbility("Full Metal Body") ||
        (gen.Num >= 8 && (target.HasAbility("Inner Focus") || target.HasAbility("Own Tempo") || target.HasAbility("Oblivious") || target.HasAbility("Scrappy"))) ||
        target.HasItem("Clear Amulet");

      if (source.HasAbility("Intimidate") && source.AbilityOn && !blocked) {
        if (target.HasAbility("Contrary") || target.HasAbility("Defiant") || target.HasAbility("Guard Dog")) {
          target.Boosts.Atk = Math.Min(6, target.Boosts.Atk + 1);
        } else if (target.HasAbility("Simple")) {
          target.Boosts.Atk = Math.Max(-6, target.Boosts.Atk - 2);
        } else {
          target.Boosts.Atk = Math.Max(-6, target.Boosts.Atk - 1);
        }
        if (target.HasAbility("Competitive")) {
          target.Boosts.Spa = Math.Min(6, target.Boosts.Spa + 2);
        }
      }
    }

    public static void CheckDownload(Pokemon source, Pokemon target, bool wonderRoomActive = false) {
      if (source.HasAbility("Download")) {
        var def = target.Stats.Def;
        var spd = target.Stats.Spd;
        if (wonderRoomActive) {
          var tmp = def;
          def = spd;
          spd = tmp;
        }
        if (spd <= def) {
          source.Boosts.Spa = Math.Min(6, source.Boosts.Spa + 1);
        } else {
          source.Boosts.Atk = Math.Min(6, source.Boosts.Atk + 1);
        }
      }
    }

    public static void CheckIntrepidSword(Pokemon source, IGeneration gen) {
      if (source.HasAbility("Intrepid Sword") && gen.Num > 7) {
        source.Boosts.Atk = Math.Min(6, source.Boosts.Atk + 1);
      }
    }

    public static void CheckDauntlessShield(Pokemon source, IGeneration gen) {
      if (source.HasAbility("Dauntless Shield") && gen.Num > 7) {
        source.Boosts.Def = Math.Min(6, source.Boosts.Def + 1);
      }
    }

    public static void CheckWindRider(Pokemon source, Side attackingSide) {
      if (source.HasAbility("Wind Rider") && attackingSide.IsTailwind) {
        source.Boosts.Atk = Math.Min(6, source.Boosts.Atk + 1);
      }
    }

    public static void CheckEmbody(Pokemon source, IGeneration gen) {
      if (gen.Num < 9) return;
      switch (source.Ability) {
        case "Embody Aspect (Cornerstone)":
          source.Boosts.Def = Math.Min(6, source.Boosts.Def + 1);
          break;
        case "Embody Aspect (Hearthflame)":
          source.Boosts.Atk = Math.Min(6, source.Boosts.Atk + 1);
          break;
        case "Embody Aspect (Teal)":
          source.Boosts.Spe = Math.Min(6, source.Boosts.Spe + 1);
          break;
        case "Embody Aspect (Wellspring)":
          source.Boosts.Spd = Math.Min(6, source.Boosts.Spd + 1);
          break;
      }
    }

    public static void CheckInfiltrator(Pokemon pokemon, Side affectedSide) {
      if (pokemon.HasAbility("Infiltrator")) {
        affectedSide.IsReflect = false;
        affectedSide.IsLightScreen = false;
        affectedSide.IsAuroraVeil = false;
      }
    }

    public static void CheckSeedBoost(Pokemon pokemon, Field field) {
      if (string.IsNullOrEmpty(pokemon.Item)) return;
      if (!string.IsNullOrEmpty(field.Terrain) && pokemon.Item.Contains("Seed")) {
        var terrainSeed = pokemon.Item.Substring(0, pokemon.Item.IndexOf(' '));
        if (field.HasTerrain(terrainSeed)) {
          if (terrainSeed == "Grassy" || terrainSeed == "Electric") {
            pokemon.Boosts.Def = pokemon.HasAbility("Contrary")
              ? Math.Max(-6, pokemon.Boosts.Def - 1)
              : Math.Min(6, pokemon.Boosts.Def + 1);
          } else {
            pokemon.Boosts.Spd = pokemon.HasAbility("Contrary")
              ? Math.Max(-6, pokemon.Boosts.Spd - 1)
              : Math.Min(6, pokemon.Boosts.Spd + 1);
          }
          pokemon.Item = "";
        }
      }
    }

    public static (bool attackerUsedItem, bool defenderUsedItem) CheckMultihitBoost(
      IGeneration gen,
      Pokemon attacker,
      Pokemon defender,
      Move move,
      Field field,
      RawDesc desc,
      bool attackerUsedItem = false,
      bool defenderUsedItem = false
    ) {
      if (move.Named("Gyro Ball", "Electro Ball") && defender.HasAbility("Gooey", "Tangling Hair")) {
        if (attacker.HasItem("White Herb") && !attackerUsedItem) {
          desc.AttackerItem = attacker.Item;
          attackerUsedItem = true;
        } else {
          attacker.Boosts.Spe = Math.Max(attacker.Boosts.Spe - 1, -6);
          attacker.Stats.Spe = GetFinalSpeed(gen, attacker, field, field.AttackerSide);
          desc.DefenderAbility = defender.Ability;
        }
      } else if (move.Named("Power-Up Punch")) {
        attacker.Boosts.Atk = Math.Min(attacker.Boosts.Atk + 1, 6);
        attacker.Stats.Atk = GetModifiedStat(attacker.RawStats.Atk, attacker.Boosts.Atk, gen);
      }

      var atkSimple = attacker.HasAbility("Simple") ? 2 : 1;
      var defSimple = defender.HasAbility("Simple") ? 2 : 1;

      if ((!defenderUsedItem) &&
        ((defender.HasItem("Luminous Moss") && move.HasType("Water")) ||
         (defender.HasItem("Maranga Berry") && move.Category == MoveCategories.Special) ||
         (defender.HasItem("Kee Berry") && move.Category == MoveCategories.Physical))) {
        var defStat = defender.HasItem("Kee Berry") ? StatId.Def : StatId.Spd;
        if (attacker.HasAbility("Unaware")) {
          desc.AttackerAbility = attacker.Ability;
        } else {
          if (defender.HasAbility("Contrary")) {
            desc.DefenderAbility = defender.Ability;
            if (defender.HasItem("White Herb") && !defenderUsedItem) {
              desc.DefenderItem = defender.Item;
              defenderUsedItem = true;
            } else {
              defender.Boosts[defStat] = Math.Max(-6, defender.Boosts[defStat] - defSimple);
            }
          } else {
            defender.Boosts[defStat] = Math.Min(6, defender.Boosts[defStat] + defSimple);
          }
          if (defSimple == 2) desc.DefenderAbility = defender.Ability;
          defender.Stats[defStat] = GetModifiedStat(defender.RawStats[defStat], defender.Boosts[defStat], gen);
          desc.DefenderItem = defender.Item;
          defenderUsedItem = true;
        }
      }

      if (defender.HasAbility("Seed Sower")) field.Terrain = "Grassy";
      if (defender.HasAbility("Sand Spit")) field.Weather = "Sand";

      if (defender.HasAbility("Stamina")) {
        if (attacker.HasAbility("Unaware")) {
          desc.AttackerAbility = attacker.Ability;
        } else {
          defender.Boosts.Def = Math.Min(defender.Boosts.Def + 1, 6);
          defender.Stats.Def = GetModifiedStat(defender.RawStats.Def, defender.Boosts.Def, gen);
          desc.DefenderAbility = defender.Ability;
        }
      } else if (defender.HasAbility("Water Compaction") && move.HasType("Water")) {
        if (attacker.HasAbility("Unaware")) {
          desc.AttackerAbility = attacker.Ability;
        } else {
          defender.Boosts.Def = Math.Min(defender.Boosts.Def + 2, 6);
          defender.Stats.Def = GetModifiedStat(defender.RawStats.Def, defender.Boosts.Def, gen);
          desc.DefenderAbility = defender.Ability;
        }
      } else if (defender.HasAbility("Weak Armor")) {
        if (attacker.HasAbility("Unaware")) {
          desc.AttackerAbility = attacker.Ability;
        } else {
          if (defender.HasItem("White Herb") && !defenderUsedItem && defender.Boosts.Def == 0) {
            desc.DefenderItem = defender.Item;
            defenderUsedItem = true;
          } else {
            defender.Boosts.Def = Math.Max(defender.Boosts.Def - 1, -6);
            defender.Stats.Def = GetModifiedStat(defender.RawStats.Def, defender.Boosts.Def, gen);
          }
          desc.DefenderAbility = defender.Ability;
        }
        defender.Boosts.Spe = Math.Min(defender.Boosts.Spe + 2, 6);
        defender.Stats.Spe = GetFinalSpeed(gen, defender, field, field.DefenderSide);
      }

      if (move.DropsStats.HasValue) {
        if (attacker.HasAbility("Unaware")) {
          desc.AttackerAbility = attacker.Ability;
        } else {
          var stat = move.Category == MoveCategories.Special ? StatId.Spa : StatId.Atk;
          var boosts = attacker.Boosts[stat];
          if (attacker.HasAbility("Contrary")) {
            boosts = Math.Min(6, boosts + move.DropsStats.Value);
            desc.AttackerAbility = attacker.Ability;
          } else {
            boosts = Math.Max(-6, boosts - move.DropsStats.Value * atkSimple);
          }
          if (atkSimple == 2) desc.AttackerAbility = attacker.Ability;

          if (attacker.HasItem("White Herb") && attacker.Boosts[stat] < 0 && !attackerUsedItem) {
            boosts += move.DropsStats.Value * atkSimple;
            desc.AttackerItem = attacker.Item;
            attackerUsedItem = true;
          }

          attacker.Boosts[stat] = boosts;
          attacker.Stats[stat] = GetModifiedStat(attacker.RawStats[stat], defender.Boosts[stat], gen);
        }
      }

      if (defender.HasAbility("Mummy") || defender.HasAbility("Wandering Spirit") || defender.HasAbility("Lingering Aroma")) {
        if (move.Flags.Contact) {
          var oldAttackerAbility = attacker.Ability;
          attacker.Ability = defender.Ability;
          if (!string.IsNullOrEmpty(desc.AttackerAbility)) {
            desc.DefenderAbility = defender.Ability;
          }
          if (defender.HasAbility("Wandering Spirit")) defender.Ability = oldAttackerAbility;
        }
      }

      return (attackerUsedItem, defenderUsedItem);
    }

    public static int ChainMods(List<int> mods, int lowerBound, int upperBound) {
      var M = 4096;
      foreach (var mod in mods) {
        if (mod != 4096) M = (M * mod + 2048) >> 12;
      }
      return Math.Max(Math.Min(M, upperBound), lowerBound);
    }

    public static int GetBaseDamage(int level, int basePower, int attack, int defense) {
      return (int)Math.Floor(
        (double)OF32(Math.Floor(OF32(OF32(Math.Floor(((2 * level) / 5.0 + 2) * basePower) * attack) / defense) / 50.0 + 2)
        ));
    }

    public static StatId GetQPBoostedStat(Pokemon pokemon, IGeneration? gen = null) {
      if (!string.IsNullOrEmpty(pokemon.BoostedStat) && pokemon.BoostedStat != "auto") {
        return pokemon.BoostedStat switch {
          "atk" => StatId.Atk,
          "def" => StatId.Def,
          "spa" => StatId.Spa,
          "spd" => StatId.Spd,
          "spe" => StatId.Spe,
          _ => StatId.Atk,
        };
      }
      var bestStat = StatId.Atk;
      foreach (var stat in new[] { StatId.Def, StatId.Spa, StatId.Spd, StatId.Spe }) {
        if (GetModifiedStat(pokemon.RawStats[stat], pokemon.Boosts[stat], gen) >
            GetModifiedStat(pokemon.RawStats[bestStat], pokemon.Boosts[bestStat], gen)) {
          bestStat = stat;
        }
      }
      return bestStat;
    }

    public static bool IsQPActive(Pokemon pokemon, Field field) {
      if (string.IsNullOrEmpty(pokemon.BoostedStat)) return false;
      var weather = field.Weather ?? "";
      var terrain = field.Terrain;

      return (pokemon.HasAbility("Protosynthesis") && (weather.Contains("Sun") || pokemon.HasItem("Booster Energy"))) ||
        (pokemon.HasAbility("Quark Drive") && (terrain == "Electric" || pokemon.HasItem("Booster Energy"))) ||
        (pokemon.BoostedStat != "auto");
    }

    public static int GetFinalDamage(int baseAmount, int i, double effectiveness, bool isBurned, int stabMod, int finalMod, bool protect = false) {
      var damageAmount = (int)Math.Floor(OF32(baseAmount * (85 + i)) / 100.0);
      if (stabMod != 4096) damageAmount = (int)(OF32(damageAmount * stabMod) / 4096.0);
      damageAmount = (int)Math.Floor((double)OF32(PokeRound(damageAmount) * effectiveness));

      if (isBurned) damageAmount = (int)Math.Floor(damageAmount / 2.0);
      if (protect) damageAmount = PokeRound(OF32(damageAmount * 1024) / 4096.0);
      return OF16(PokeRound(Math.Max(1, OF32(damageAmount * finalMod) / 4096.0)));
    }

    public static string GetShellSideArmCategory(Pokemon source, Pokemon target) {
      var physicalDamage = source.Stats.Atk / (double)target.Stats.Def;
      var specialDamage = source.Stats.Spa / (double)target.Stats.Spd;
      return physicalDamage > specialDamage ? MoveCategories.Physical : MoveCategories.Special;
    }

    public static double GetWeight(Pokemon pokemon, RawDesc desc, string role) {
      var weightHG = pokemon.WeightKg * 10;
      var abilityFactor = pokemon.HasAbility("Heavy Metal") ? 2 : pokemon.HasAbility("Light Metal") ? 0.5 : 1;
      if (abilityFactor != 1) {
        weightHG = Math.Max(Math.Truncate(weightHG * abilityFactor), 1);
        if (role == "defender") desc.DefenderAbility = pokemon.Ability; else desc.AttackerAbility = pokemon.Ability;
      }

      if (pokemon.HasItem("Float Stone")) {
        weightHG = Math.Max(Math.Truncate(weightHG * 0.5), 1);
        if (role == "defender") desc.DefenderItem = pokemon.Item; else desc.AttackerItem = pokemon.Item;
      }

      return weightHG / 10;
    }

    public static int GetStabMod(Pokemon pokemon, Move move, RawDesc desc) {
      var stabMod = 4096;
      if (pokemon.HasOriginalType(move.Type)) {
        stabMod += 2048;
      } else if ((pokemon.HasAbility("Protean") || pokemon.HasAbility("Libero")) && string.IsNullOrEmpty(pokemon.TeraType)) {
        stabMod += 2048;
        desc.AttackerAbility = pokemon.Ability;
      }
      var teraType = pokemon.TeraType;
      if (teraType == move.Type && teraType != "Stellar") {
        stabMod += 2048;
        desc.AttackerTera = teraType;
      }
      if (pokemon.HasAbility("Adaptability") && pokemon.HasType(move.Type)) {
        stabMod += teraType != null && pokemon.HasOriginalType(teraType) ? 1024 : 2048;
        desc.AttackerAbility = pokemon.Ability;
      }
      return stabMod;
    }

    public static int GetStellarStabMod(Pokemon pokemon, Move move, int stabMod = 1, int turns = 0) {
      var isStellarBoosted = pokemon.TeraType == "Stellar" &&
        ((move.IsStellarFirstUse && turns == 0) || pokemon.Named("Terapagos-Stellar"));
      if (isStellarBoosted) {
        if (pokemon.HasOriginalType(move.Type)) {
          stabMod += 2048;
        } else {
          stabMod = 4915;
        }
      }
      return stabMod;
    }

    public static int CountBoosts(IGeneration gen, StatsTable boosts) {
      var sum = 0;
      var stats = gen.Num == 1
        ? new[] { StatId.Atk, StatId.Def, StatId.Spa, StatId.Spe }
        : new[] { StatId.Atk, StatId.Def, StatId.Spa, StatId.Spd, StatId.Spe };

      foreach (var stat in stats) {
        var boost = boosts[stat];
        if (boost > 0) sum += boost;
      }
      return sum;
    }

    public static string GetStatDescriptionText(IGeneration gen, Pokemon pokemon, StatId stat, string? natureName) {
      var nature = gen.Natures.Get(Util.ToId(natureName));
      var desc = pokemon.Evs[stat] +
        ((stat == StatId.Hp || (nature?.Plus == nature?.Minus)) ? "" :
        (nature?.Plus == stat ? "+" : nature?.Minus == stat ? "-" : "")) + " " +
        Stats.DisplayStat(stat);
      var iv = pokemon.Ivs[stat];
      if (iv != 31) desc += $" {iv} IVs";
      return desc;
    }

    public static int HandleFixedDamageMoves(Pokemon attacker, Move move) {
      if (move.Named("Seismic Toss", "Night Shade")) {
        return attacker.Level;
      }
      if (move.Named("Dragon Rage")) return 40;
      if (move.Named("Sonic Boom")) return 20;
      return 0;
    }

    public static int PokeRound(double num) {
      return num % 1 > 0.5 ? (int)Math.Ceiling(num) : (int)Math.Floor(num);
    }

    public static int OF16(double n) {
      return n > 65535 ? (int)(n % 65536) : (int)n;
    }

    public static int OF32(double n) {
      return n > 4294967295 ? (int)(n % 4294967296) : (int)n;
    }
  }
}
