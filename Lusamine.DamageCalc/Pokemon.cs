using System;
using System.Collections.Generic;
using Lusamine.DamageCalc.Data;

namespace Lusamine.DamageCalc {
  /// <summary>
  /// Represents a Pokémon participant in a damage calculation, including its
  /// species, stats, EVs/IVs, nature, ability, item, boosts, and status.
  /// </summary>
  public sealed class Pokemon {
    private static readonly StatId[] STATS = { StatId.Hp, StatId.Atk, StatId.Def, StatId.Spa, StatId.Spd, StatId.Spe };

    /// <summary>The generation data context this Pokémon belongs to.</summary>
    public IGeneration Gen { get; }
    /// <summary>Display name (species name or custom override).</summary>
    public string Name { get; set; }
    /// <summary>Full species data including base stats, types, and formes.</summary>
    public Specie Species { get; set; }

    /// <summary>Active type(s). Replaced by <see cref="TeraType"/> when Terastallized (gen 9).</summary>
    public string[] Types { get; set; }
    /// <summary>Weight in kilograms, used by weight-based moves.</summary>
    public double WeightKg { get; set; }

    /// <summary>Level (1–100). Defaults to 100.</summary>
    public int Level { get; set; }
    /// <summary>Gender (<c>"M"</c>, <c>"F"</c>, or <c>"N"</c>). Defaults to species default.</summary>
    public string? Gender { get; set; }
    /// <summary>Ability name, or <c>null</c> if none.</summary>
    public string? Ability { get; set; }
    /// <summary>Whether the ability is currently active (e.g. Slow Start, Truant).</summary>
    public bool AbilityOn { get; set; }
    /// <summary>Whether the Pokémon is Dynamaxed (gen 8).</summary>
    public bool IsDynamaxed { get; set; }
    /// <summary>Dynamax level (0–10). <c>null</c> when not Dynamaxed.</summary>
    public int? DynamaxLevel { get; set; }
    /// <summary>Number of allies that have fainted this battle (used by Last Respects).</summary>
    public int? AlliesFainted { get; set; }
    /// <summary>Stat currently boosted by a passive ability (e.g. Protosynthesis, Quark Drive).</summary>
    public string? BoostedStat { get; set; }
    /// <summary>Held item name, or <c>null</c> if none.</summary>
    public string? Item { get; set; }
    /// <summary>Temporarily disabled item (e.g. after Knock Off, Embargo).</summary>
    public string? DisabledItem { get; set; }
    /// <summary>Tera type name for gen 9 Terastallization, or <c>null</c> if not Terastallized.</summary>
    public string? TeraType { get; set; }

    /// <summary>Nature name (e.g. <c>"Modest"</c>). Defaults to <c>"Serious"</c> (neutral).</summary>
    public string Nature { get; set; }
    /// <summary>Individual values (0–31 per stat; treated as DVs in gen 1–2).</summary>
    public StatsTable Ivs { get; set; }
    /// <summary>Effort values (0–252 per stat for gen 3+; unused in gen 1–2).</summary>
    public StatsTable Evs { get; set; }
    /// <summary>In-battle stat boosts (−6 to +6 per stat).</summary>
    public StatsTable Boosts { get; set; }
    /// <summary>Computed stats before any in-battle modifications.</summary>
    public StatsTable RawStats { get; set; }
    /// <summary>Effective stats after any modifications applied by the calc engine.</summary>
    public StatsTable Stats { get; set; }

    /// <summary>Current HP (≤ <see cref="RawStats"/>.Hp). Defaults to max HP.</summary>
    public int OriginalCurHP { get; set; }
    /// <summary>Status condition: <c>"brn"</c>, <c>"par"</c>, <c>"slp"</c>, <c>"frz"</c>, <c>"psn"</c>, <c>"tox"</c>, or empty string.</summary>
    public string Status { get; set; }
    /// <summary>Toxic counter (number of turns poisoned, for Badly Poisoned damage scaling).</summary>
    public int ToxicCounter { get; set; }

    /// <summary>Known moves (used by Shell Side Arm and similar mechanic checks).</summary>
    public List<string> Moves { get; set; }

    /// <summary>
    /// Constructs a Pokémon for the given generation and species name,
    /// optionally overriding any fields via <paramref name="options"/>.
    /// </summary>
    /// <param name="gen">Generation data context.</param>
    /// <param name="name">Species name (e.g. <c>"Garchomp"</c>).</param>
    /// <param name="options">Optional stat/item/ability overrides.</param>
    /// <exception cref="InvalidOperationException">Thrown for unknown species names.</exception>
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

    /// <summary>
    /// Returns the maximum HP, accounting for Dynamax scaling when active.
    /// </summary>
    /// <param name="original">
    /// Pass <c>true</c> to return the base (non-Dynamaxed) HP even when Dynamaxed.
    /// </param>
    public int MaxHP(bool original = false) {
      if (!original && IsDynamaxed && Species.BaseStats.Hp != 1) {
        return (int)Math.Floor((RawStats.Hp * (150 + 5 * DynamaxLevel.GetValueOrDefault())) / 100.0);
      }
      return RawStats.Hp;
    }

    /// <summary>
    /// Returns the current HP, accounting for Dynamax scaling when active.
    /// </summary>
    /// <param name="original">
    /// Pass <c>true</c> to return the base (non-Dynamaxed) current HP even when Dynamaxed.
    /// </param>
    public int CurHP(bool original = false) {
      if (!original && IsDynamaxed && Species.BaseStats.Hp != 1) {
        return (int)Math.Ceiling((OriginalCurHP * (150 + 5 * DynamaxLevel.GetValueOrDefault())) / 100.0);
      }
      return OriginalCurHP;
    }

    /// <summary>Returns <c>true</c> if the Pokémon's ability matches any of the given names.</summary>
    public bool HasAbility(params string[] abilities) {
      return !string.IsNullOrEmpty(Ability) && Array.Exists(abilities, a => a == Ability);
    }

    /// <summary>Returns <c>true</c> if the Pokémon's held item matches any of the given names.</summary>
    public bool HasItem(params string[] items) {
      return !string.IsNullOrEmpty(Item) && Array.Exists(items, i => i == Item);
    }

    /// <summary>Returns <c>true</c> if the Pokémon's status matches any of the given condition codes.</summary>
    public bool HasStatus(params string[] statuses) {
      return !string.IsNullOrEmpty(Status) && Array.Exists(statuses, s => s == Status);
    }

    /// <summary>
    /// Returns <c>true</c> if the Pokémon has any of the given types.
    /// When Terastallized (and not Stellar tera), only the Tera type is checked.
    /// </summary>
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

    /// <summary>
    /// Returns <c>true</c> if the Pokémon's original (pre-Tera) types include any of the given types.
    /// Ignores <see cref="TeraType"/>.
    /// </summary>
    public bool HasOriginalType(params string[] types) {
      foreach (var type in types) {
        if (Array.Exists(Types, t => t == type)) return true;
      }
      return false;
    }

    /// <summary>Returns <c>true</c> if this Pokémon's name matches any of the given names.</summary>
    public bool Named(params string[] names) {
      return Array.Exists(names, n => n == Name);
    }

    /// <summary>Creates a deep copy of this Pokémon.</summary>
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
