using System;
using System.Collections.Generic;
using Lusamine.DamageCalc.Data;

namespace Lusamine.DamageCalc {
  public sealed class Pokemon {
    private static readonly StatId[] STATS = { StatId.Hp, StatId.Atk, StatId.Def, StatId.Spa, StatId.Spd, StatId.Spe };

    public IGeneration Gen { get; }
    public string Name { get; set; }
    public Specie Species { get; set; }

    public string[] Types { get; set; }
    public double WeightKg { get; set; }

    public int Level { get; set; }
    public string? Gender { get; set; }
    public string? Ability { get; set; }
    public bool AbilityOn { get; set; }
    public bool IsDynamaxed { get; set; }
    public int? DynamaxLevel { get; set; }
    public int? AlliesFainted { get; set; }
    public string? BoostedStat { get; set; }
    public string? Item { get; set; }
    public string? DisabledItem { get; set; }
    public string? TeraType { get; set; }

    public string Nature { get; set; }
    public StatsTable Ivs { get; set; }
    public StatsTable Evs { get; set; }
    public StatsTable Boosts { get; set; }
    public StatsTable RawStats { get; set; }
    public StatsTable Stats { get; set; }

    public int OriginalCurHP { get; set; }
    public string Status { get; set; }
    public int ToxicCounter { get; set; }

    public List<string> Moves { get; set; }

    public Pokemon(IGeneration gen, string name, State.Pokemon? options = null) {
      options ??= new State.Pokemon();
      var speciesData = gen.Species.Get(Util.ToId(name)) as Specie;
      if (speciesData == null) throw new InvalidOperationException($"Unknown species: {name}");
      var merged = speciesData.Clone();
      if (options.Overrides != null) merged.ApplyOverrides(options.Overrides);
      Species = merged;

      Gen = gen;
      Name = string.IsNullOrEmpty(options.Name) ? name : options.Name;
      Types = Species.Types;
      WeightKg = Species.WeightKg;

      Level = options.Level ?? 100;
      Gender = options.Gender ?? Species.Gender ?? "M";
      Ability = options.Ability ?? (Species.Abilities != null && Species.Abilities.TryGetValue(0, out var ability) ? ability : null);
      AbilityOn = options.AbilityOn ?? false;

      IsDynamaxed = options.IsDynamaxed ?? false;
      DynamaxLevel = IsDynamaxed ? (options.DynamaxLevel ?? 10) : null;
      AlliesFainted = options.AlliesFainted;
      BoostedStat = options.BoostedStat;
      TeraType = options.TeraType;
      Item = options.Item;
      Nature = options.Nature ?? "Serious";
      Ivs = WithDefault(gen, options.Ivs, 31);
      Evs = WithDefault(gen, options.Evs, gen.Num >= 3 ? 0 : 252);
      Boosts = WithDefault(gen, options.Boosts, 0, match: false);

      if (WeightKg == 0 && !IsDynamaxed && Species.BaseSpecies != null) {
        var baseSpecies = gen.Species.Get(Util.ToId(Species.BaseSpecies)) as Specie;
        if (baseSpecies != null) WeightKg = baseSpecies.WeightKg;
      }

      if (gen.Num < 3) {
        Ivs.Hp = DamageCalc.Stats.DVToIV(DamageCalc.Stats.GetHPDV(new StatsTable { Atk = Ivs.Atk, Def = Ivs.Def, Spe = Ivs.Spe, Spc = Ivs.Spa }));
      }

      RawStats = new StatsTable();
      Stats = new StatsTable();
      foreach (var stat in STATS) {
        var val = CalcStat(gen, stat);
        RawStats[stat] = val;
        Stats[stat] = val;
      }

      var providedCur = options.CurHP ?? options.OriginalCurHP;
      if (providedCur.HasValue && providedCur.Value <= RawStats.Hp) {
        OriginalCurHP = providedCur.Value;
      } else {
        OriginalCurHP = RawStats.Hp;
      }
      Status = options.Status ?? "";
      ToxicCounter = options.ToxicCounter ?? 0;
      Moves = options.Moves ?? new List<string>();
    }

    public int MaxHP(bool original = false) {
      if (!original && IsDynamaxed && Species.BaseStats.Hp != 1) {
        return (int)Math.Floor((RawStats.Hp * (150 + 5 * DynamaxLevel.GetValueOrDefault())) / 100.0);
      }
      return RawStats.Hp;
    }

    public int CurHP(bool original = false) {
      if (!original && IsDynamaxed && Species.BaseStats.Hp != 1) {
        return (int)Math.Ceiling((OriginalCurHP * (150 + 5 * DynamaxLevel.GetValueOrDefault())) / 100.0);
      }
      return OriginalCurHP;
    }

    public bool HasAbility(params string[] abilities) {
      return !string.IsNullOrEmpty(Ability) && Array.Exists(abilities, a => a == Ability);
    }

    public bool HasItem(params string[] items) {
      return !string.IsNullOrEmpty(Item) && Array.Exists(items, i => i == Item);
    }

    public bool HasStatus(params string[] statuses) {
      return !string.IsNullOrEmpty(Status) && Array.Exists(statuses, s => s == Status);
    }

    public bool HasType(params string[] types) {
      foreach (var type in types) {
        if (!string.IsNullOrEmpty(TeraType) && TeraType != "Stellar") {
          if (TeraType == type) return true;
        } else if (Array.Exists(Types, t => t == type)) {
          return true;
        }
      }
      return false;
    }

    public bool HasOriginalType(params string[] types) {
      foreach (var type in types) {
        if (Array.Exists(Types, t => t == type)) return true;
      }
      return false;
    }

    public bool Named(params string[] names) {
      return Array.Exists(names, n => n == Name);
    }

    public Pokemon Clone() {
      return new Pokemon(Gen, Name, new State.Pokemon {
        Level = Level,
        Ability = Ability,
        AbilityOn = AbilityOn,
        IsDynamaxed = IsDynamaxed,
        DynamaxLevel = DynamaxLevel,
        AlliesFainted = AlliesFainted,
        BoostedStat = BoostedStat,
        Item = Item,
        Gender = Gender,
        Nature = Nature,
        Ivs = new StatsTableInput { Hp = Ivs.Hp, Atk = Ivs.Atk, Def = Ivs.Def, Spa = Ivs.Spa, Spd = Ivs.Spd, Spe = Ivs.Spe, Spc = Ivs.Spc },
        Evs = new StatsTableInput { Hp = Evs.Hp, Atk = Evs.Atk, Def = Evs.Def, Spa = Evs.Spa, Spd = Evs.Spd, Spe = Evs.Spe, Spc = Evs.Spc },
        Boosts = new StatsTableInput { Hp = Boosts.Hp, Atk = Boosts.Atk, Def = Boosts.Def, Spa = Boosts.Spa, Spd = Boosts.Spd, Spe = Boosts.Spe, Spc = Boosts.Spc },
        OriginalCurHP = OriginalCurHP,
        Status = Status,
        TeraType = TeraType,
        ToxicCounter = ToxicCounter,
        Moves = new List<string>(Moves),
        Overrides = Species,
      });
    }

    private int CalcStat(IGeneration gen, StatId stat) {
      return DamageCalc.Stats.CalcStat(
        gen,
        stat,
        Species.BaseStats[stat],
        Ivs[stat],
        Evs[stat],
        Level,
        Nature
      );
    }

    public static string GetForme(IGeneration gen, string speciesName, string? item = null, string? moveName = null) {
      var species = gen.Species.Get(Util.ToId(speciesName)) as Specie;
      if (species?.OtherFormes == null) return speciesName;

      var i = 0;
      if ((item != null && ((item.Contains("ite") && !item.Contains("ite Y")) ||
            (speciesName == "Groudon" && item == "Red Orb") ||
            (speciesName == "Kyogre" && item == "Blue Orb"))) ||
          (moveName != null && speciesName == "Meloetta" && moveName == "Relic Song") ||
          (speciesName == "Rayquaza" && moveName == "Dragon Ascent")) {
        i = 1;
      } else if (item != null && item.Contains("ite Y")) {
        i = 2;
      }

      return i > 0 ? species.OtherFormes[i - 1] : species.Name;
    }

    private static StatsTable WithDefault(IGeneration gen, StatsTableInput? current, int val, bool match = true) {
      var result = new StatsTable {
        Hp = val,
        Atk = val,
        Def = val,
        Spa = val,
        Spd = val,
        Spe = val,
        Spc = val,
      };

      if (current != null) {
        if (current.Spc.HasValue) {
          result.Spa = current.Spc.Value;
          result.Spd = current.Spc.Value;
          result.Spc = current.Spc.Value;
        }
        if (current.Hp.HasValue) result.Hp = current.Hp.Value;
        if (current.Atk.HasValue) result.Atk = current.Atk.Value;
        if (current.Def.HasValue) result.Def = current.Def.Value;
        if (current.Spa.HasValue) result.Spa = current.Spa.Value;
        if (current.Spd.HasValue) result.Spd = current.Spd.Value;
        if (current.Spe.HasValue) result.Spe = current.Spe.Value;
        if (match && gen.Num <= 2 && current.Spa.HasValue && current.Spd.HasValue && current.Spa != current.Spd) {
          throw new InvalidOperationException("Special Attack and Special Defense must match before Gen 3");
        }
      }

      return result;
    }
  }
}
