using System;
using System.Collections.Generic;
using Lusamine.DamageCalc.Data;

namespace Lusamine.DamageCalc {
  public sealed class Move {
    private static readonly HashSet<string> SPECIAL = new HashSet<string> {
      "Fire", "Water", "Grass", "Electric", "Ice", "Psychic", "Dark", "Dragon",
    };

    public IGeneration Gen { get; }
    public string Name { get; }
    public string OriginalName { get; }
    public string? Ability { get; }
    public string? Item { get; }
    public string? Species { get; }
    public bool? UseZ { get; }
    public bool? UseMax { get; }
    public MoveData? Overrides { get; }

    public int Hits { get; set; }
    public int? TimesUsed { get; set; }
    public int? TimesUsedWithMetronome { get; set; }
    public int Bp { get; set; }
    public string Type { get; set; }
    public string Category { get; set; }
    public MoveFlags Flags { get; set; }
    public object? Secondaries { get; set; }
    public string Target { get; set; }
    public int[]? Recoil { get; set; }
    public bool HasCrashDamage { get; set; }
    public bool MindBlownRecoil { get; set; }
    public bool StruggleRecoil { get; set; }
    public bool IsCrit { get; set; }
    public bool IsStellarFirstUse { get; set; }
    public int[]? Drain { get; set; }
    public int Priority { get; set; }
    public int? DropsStats { get; set; }
    public bool IgnoreDefensive { get; set; }
    public StatId? OverrideOffensiveStat { get; set; }
    public StatId? OverrideDefensiveStat { get; set; }
    public string? OverrideOffensivePokemon { get; set; }
    public string? OverrideDefensivePokemon { get; set; }
    public bool BreaksProtect { get; set; }
    public bool IsZ { get; set; }
    public bool IsMax { get; set; }
    public bool Multiaccuracy { get; set; }

    public Move(IGeneration gen, string name, State.Move? options = null, string? ability = null, string? item = null, string? species = null) {
      options ??= new State.Move();
      name = string.IsNullOrEmpty(options.Name) ? name : options.Name;
      OriginalName = name;

      var data = (gen.Moves.Get(Util.ToId(name)) as MoveData)?.Clone() ?? new MoveData { Name = name };
      if (options.Overrides != null) data.ApplyOverrides(options.Overrides);

      Hits = 1;

      if (options.UseMax == true && data.MaxMove != null) {
        var maxMoveName = GetMaxMoveName(
          data.Type,
          data.Name,
          species,
          data.Category == MoveCategories.Status,
          ability
        );
        var maxMove = gen.Moves.Get(Util.ToId(maxMoveName)) as MoveData;
        if (maxMove != null) {
          var maxPower = () => {
            if (maxMoveName == "G-Max Drum Solo" || maxMoveName == "G-Max Fire Ball" || maxMoveName == "G-Max Hydrosnipe") return 160;
            if (maxMove.BasePower == 10 || maxMoveName == "Max Flare") return data.MaxMove!.BasePower;
            return maxMove.BasePower;
          };
          var merged = maxMove.Clone();
          merged.Name = maxMoveName;
          merged.BasePower = maxPower();
          merged.Category = data.Category;
          data = merged;
        }
      }

      if (options.UseZ == true && data.ZMove?.BasePower != null) {
        var zMoveName = GetZMoveName(data.Name, data.Type, item);
        var zMove = gen.Moves.Get(Util.ToId(zMoveName)) as MoveData;
        if (zMove != null) {
          var merged = zMove.Clone();
          merged.Name = zMoveName;
          merged.BasePower = zMove.BasePower == 1 ? data.ZMove.BasePower!.Value : zMove.BasePower;
          merged.Category = data.Category;
          data = merged;
        }
      } else {
        if (data.Multihit != null) {
          if (data.Multiaccuracy && data.Multihit is int multihitCount) {
            Hits = options.Hits ?? multihitCount;
          } else {
            if (data.Multihit is int fixedHits) {
              Hits = fixedHits;
            } else if (options.Hits.HasValue) {
              Hits = options.Hits.Value;
            } else if (data.Multihit is int[] rangeHits && rangeHits.Length > 1) {
              Hits = (ability == "Skill Link") ? rangeHits[1] : rangeHits[0] + 1;
            }
          }
        }
        TimesUsedWithMetronome = options.TimesUsedWithMetronome;
      }

      Gen = gen;
      Name = data.Name;
      Ability = ability;
      Item = item;
      UseZ = options.UseZ;
      UseMax = options.UseMax;
      Overrides = options.Overrides;
      Species = species;

      Bp = data.BasePower;
      var typelessDamage = (gen.Num >= 2 && data.Id == "struggle") || (gen.Num <= 4 && (data.Id == "futuresight" || data.Id == "doomdesire"));
      Type = typelessDamage ? "???" : data.Type;

      Category = data.Category ?? (gen.Num < 4 ? (SPECIAL.Contains(data.Type) ? MoveCategories.Special : MoveCategories.Physical) : MoveCategories.Status);

      var stat = Category == MoveCategories.Special ? StatId.Spa : StatId.Atk;
      if (data.Self?.Boosts != null) {
        var boosts = data.Self.Boosts;
        var drop = stat == StatId.Spa ? boosts.Spa : boosts.Atk;
        if (drop.HasValue && drop.Value < 0) {
          DropsStats = Math.Abs(drop.Value);
        }
      }

      TimesUsed = options.TimesUsed ?? 1;
      Secondaries = data.Secondaries;
      Target = data.Target ?? "any";
      Recoil = data.Recoil;
      HasCrashDamage = data.HasCrashDamage;
      MindBlownRecoil = data.MindBlownRecoil;
      StruggleRecoil = data.StruggleRecoil;
      IsCrit = options.IsCrit == true || data.WillCrit || (gen.Num == 1 && (data.Id == "crabhammer" || data.Id == "razorleaf" || data.Id == "slash" || data.Id == "karate chop"));
      IsStellarFirstUse = options.IsStellarFirstUse == true;
      Drain = data.Drain;
      Flags = data.Flags;
      Priority = data.Priority ?? 0;

      IgnoreDefensive = data.IgnoreDefensive;
      OverrideOffensiveStat = data.OverrideOffensiveStat;
      OverrideDefensiveStat = data.OverrideDefensiveStat;
      OverrideOffensivePokemon = data.OverrideOffensivePokemon;
      OverrideDefensivePokemon = data.OverrideDefensivePokemon;
      BreaksProtect = data.BreaksProtect;
      IsZ = data.IsZ;
      IsMax = data.IsMax;
      Multiaccuracy = data.Multiaccuracy;

      if (Bp == 0) {
        if (data.Id == "return" || data.Id == "frustration" || data.Id == "pikapapow" || data.Id == "veeveevolley") {
          Bp = 102;
        }
      }
    }

    public bool Named(params string[] names) {
      return Array.Exists(names, n => n == Name);
    }

    public bool HasType(params string[] types) {
      return Array.Exists(types, t => t == Type);
    }

    public Move Clone() {
      return new Move(Gen, OriginalName, new State.Move {
        UseZ = UseZ,
        UseMax = UseMax,
        IsCrit = IsCrit,
        IsStellarFirstUse = IsStellarFirstUse,
        Hits = Hits,
        TimesUsed = TimesUsed,
        TimesUsedWithMetronome = TimesUsedWithMetronome,
        Overrides = Overrides,
      }, ability: Ability, item: Item, species: Species);
    }

    public static string GetZMoveName(string moveName, string moveType, string? item) {
      item ??= "";
      if (moveName.Contains("Hidden Power")) return "Breakneck Blitz";
      if (moveName == "Clanging Scales" && item == "Kommonium Z") return "Clangorous Soulblaze";
      if (moveName == "Darkest Lariat" && item == "Incinium Z") return "Malicious Moonsault";
      if (moveName == "Giga Impact" && item == "Snorlium Z") return "Pulverizing Pancake";
      if (moveName == "Moongeist Beam" && item == "Lunalium Z") return "Menacing Moonraze Maelstrom";
      if (moveName == "Photon Geyser" && item == "Ultranecrozium Z") return "Light That Burns the Sky";
      if (moveName == "Play Rough" && item == "Mimikium Z") return "Let's Snuggle Forever";
      if (moveName == "Psychic" && item == "Mewnium Z") return "Genesis Supernova";
      if (moveName == "Sparkling Aria" && item == "Primarium Z") return "Oceanic Operetta";
      if (moveName == "Spectral Thief" && item == "Marshadium Z") return "Soul-Stealing 7-Star Strike";
      if (moveName == "Spirit Shackle" && item == "Decidium Z") return "Sinister Arrow Raid";
      if (moveName == "Stone Edge" && item == "Lycanium Z") return "Splintered Stormshards";
      if (moveName == "Sunsteel Strike" && item == "Solganium Z") return "Searing Sunraze Smash";
      if (moveName == "Volt Tackle" && item == "Pikanium Z") return "Catastropika";
      if (moveName == "Nature's Madness" && item == "Tapunium Z") return "Guardian of Alola";
      if (moveName == "Thunderbolt") {
        if (item == "Aloraichium Z") return "Stoked Sparksurfer";
        if (item == "Pikashunium Z") return "10,000,000 Volt Thunderbolt";
      }
      return ZMovesTyping[moveType]!;
    }

    private static readonly Dictionary<string, string> ZMovesTyping = new Dictionary<string, string> {
      { "Bug", "Savage Spin-Out" },
      { "Dark", "Black Hole Eclipse" },
      { "Dragon", "Devastating Drake" },
      { "Electric", "Gigavolt Havoc" },
      { "Fairy", "Twinkle Tackle" },
      { "Fighting", "All-Out Pummeling" },
      { "Fire", "Inferno Overdrive" },
      { "Flying", "Supersonic Skystrike" },
      { "Ghost", "Never-Ending Nightmare" },
      { "Grass", "Bloom Doom" },
      { "Ground", "Tectonic Rage" },
      { "Ice", "Subzero Slammer" },
      { "Normal", "Breakneck Blitz" },
      { "Poison", "Acid Downpour" },
      { "Psychic", "Shattered Psyche" },
      { "Rock", "Continental Crush" },
      { "Steel", "Corkscrew Crash" },
      { "Water", "Hydro Vortex" },
    };

    public static string GetMaxMoveName(string moveType, string? moveName, string? pokemonSpecies, bool isStatus, string? pokemonAbility) {
      if (isStatus) return "Max Guard";
      if (pokemonAbility == "Normalize") return "Max Strike";
      if (moveType == "Fire") {
        if (pokemonSpecies == "Charizard-Gmax") return "G-Max Wildfire";
        if (pokemonSpecies == "Centiskorch-Gmax") return "G-Max Centiferno";
        if (pokemonSpecies == "Cinderace-Gmax") return "G-Max Fire Ball";
      }
      if (moveType == "Normal") {
        if (pokemonSpecies == "Eevee-Gmax") return "G-Max Cuddle";
        if (pokemonSpecies == "Meowth-Gmax") return "G-Max Gold Rush";
        if (pokemonSpecies == "Snorlax-Gmax") return "G-Max Replenish";
        if (moveName != "Weather Ball" && moveName != "Terrain Pulse") {
          if (pokemonAbility == "Pixilate") return "Max Starfall";
          if (pokemonAbility == "Aerilate") return "Max Airstream";
          if (pokemonAbility == "Refrigerate") return "Max Hailstorm";
          if (pokemonAbility == "Galvanize") return "Max Lightning";
        }
      }
      if (moveType == "Fairy") {
        if (pokemonSpecies == "Alcremie-Gmax") return "G-Max Finale";
        if (pokemonSpecies == "Hatterene-Gmax") return "G-Max Smite";
      }
      if (moveType == "Steel") {
        if (pokemonSpecies == "Copperajah-Gmax") return "G-Max Steelsurge";
        if (pokemonSpecies == "Melmetal-Gmax") return "G-Max Meltdown";
      }
      if (moveType == "Electric") {
        if (pokemonSpecies == "Pikachu-Gmax") return "G-Max Volt Crash";
        if (pokemonSpecies != null && pokemonSpecies.StartsWith("Toxtricity") && pokemonSpecies.EndsWith("Gmax")) return "G-Max Stun Shock";
      }
      if (moveType == "Grass") {
        if (pokemonSpecies == "Appletun-Gmax") return "G-Max Sweetness";
        if (pokemonSpecies == "Flapple-Gmax") return "G-Max Tartness";
        if (pokemonSpecies == "Rillaboom-Gmax") return "G-Max Drum Solo";
        if (pokemonSpecies == "Venusaur-Gmax") return "G-Max Vine Lash";
      }
      if (moveType == "Water") {
        if (pokemonSpecies == "Blastoise-Gmax") return "G-Max Cannonade";
        if (pokemonSpecies == "Drednaw-Gmax") return "G-Max Stonesurge";
        if (pokemonSpecies == "Inteleon-Gmax") return "G-Max Hydrosnipe";
        if (pokemonSpecies == "Kingler-Gmax") return "G-Max Foam Burst";
        if (pokemonSpecies == "Urshifu-Rapid-Strike-Gmax") return "G-Max Rapid Flow";
      }
      if (moveType == "Dark") {
        if (pokemonSpecies == "Grimmsnarl-Gmax") return "G-Max Snooze";
        if (pokemonSpecies == "Urshifu-Gmax") return "G-Max One Blow";
      }
      if (moveType == "Poison" && pokemonSpecies == "Garbodor-Gmax") return "G-Max Malodor";
      if (moveType == "Fighting" && pokemonSpecies == "Machamp-Gmax") return "G-Max Chi Strike";
      if (moveType == "Ghost" && pokemonSpecies == "Gengar-Gmax") return "G-Max Terror";
      if (moveType == "Ice" && pokemonSpecies == "Lapras-Gmax") return "G-Max Resonance";
      if (moveType == "Flying" && pokemonSpecies == "Corviknight-Gmax") return "G-Max Wind Rage";
      if (moveType == "Dragon" && pokemonSpecies == "Duraludon-Gmax") return "G-Max Depletion";
      if (moveType == "Psychic" && pokemonSpecies == "Orbeetle-Gmax") return "G-Max Gravitas";
      if (moveType == "Rock" && pokemonSpecies == "Coalossal-Gmax") return "G-Max Volcalith";
      if (moveType == "Ground" && pokemonSpecies == "Sandaconda-Gmax") return "G-Max Sandblast";
      if (moveType == "Dark" && pokemonSpecies == "Grimmsnarl-Gmax") return "G-Max Snooze";
      return "Max " + MaxMovesTyping[moveType];
    }

    private static readonly Dictionary<string, string> MaxMovesTyping = new Dictionary<string, string> {
      { "Bug", "Flutterby" },
      { "Dark", "Darkness" },
      { "Dragon", "Wyrmwind" },
      { "Electric", "Lightning" },
      { "Fairy", "Starfall" },
      { "Fighting", "Knuckle" },
      { "Fire", "Flare" },
      { "Flying", "Airstream" },
      { "Ghost", "Phantasm" },
      { "Grass", "Overgrowth" },
      { "Ground", "Quake" },
      { "Ice", "Hailstorm" },
      { "Normal", "Strike" },
      { "Poison", "Ooze" },
      { "Psychic", "Mindstorm" },
      { "Rock", "Rockfall" },
      { "Steel", "Steelspike" },
      { "Water", "Geyser" },
    };
  }
}
