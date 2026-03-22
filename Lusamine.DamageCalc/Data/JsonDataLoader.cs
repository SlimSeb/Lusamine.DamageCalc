using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace DamageCalc.Data {
  // RawData maps entityId -> (fieldName -> JsonElement)
  using EntryFields = Dictionary<string, JsonElement>;
  using RawData = Dictionary<string, Dictionary<string, JsonElement>>;

  public static class JsonDataLoader {
    private const int MaxGen = 9;

    // Per-gen caches for resolved raw data
    private static readonly RawData?[] _rawSpecies = new RawData?[MaxGen + 1];
    private static readonly RawData?[] _rawMoves = new RawData?[MaxGen + 1];
    private static readonly RawData?[] _rawItems = new RawData?[MaxGen + 1];
    private static readonly RawData?[] _rawAbilities = new RawData?[MaxGen + 1];
    private static readonly RawData?[] _rawTypes = new RawData?[MaxGen + 1];
    private static readonly RawData?[] _rawNatures = new RawData?[MaxGen + 1];

    // Per-gen caches for built tables
    private static readonly IDataTable<IAbility>?[] _abilityTables = new IDataTable<IAbility>?[MaxGen + 1];
    private static readonly IDataTable<IItem>?[] _itemTables = new IDataTable<IItem>?[MaxGen + 1];
    private static readonly IDataTable<IMove>?[] _moveTables = new IDataTable<IMove>?[MaxGen + 1];
    private static readonly IDataTable<ISpecie>?[] _specieTables = new IDataTable<ISpecie>?[MaxGen + 1];
    private static readonly IDataTable<IType>?[] _typeTables = new IDataTable<IType>?[MaxGen + 1];
    private static IDataTable<INature>? _natureTable;

    // Raw JSON documents (loaded once per type)
    private static JsonDocument? _speciesDoc;
    private static JsonDocument? _movesDoc;
    private static JsonDocument? _itemsDoc;
    private static JsonDocument? _abilitiesDoc;
    private static JsonDocument? _typesDoc;
    private static JsonDocument? _naturesDoc;

    // ----------------------------------------------------------------
    // Public API
    // ----------------------------------------------------------------

    public static IDataTable<IAbility> GetAbilitiesTable(int gen) {
      if (gen < 1 || gen > MaxGen) throw new ArgumentOutOfRangeException(nameof(gen));
      return _abilityTables[gen] ??= BuildAbilitiesTable(gen);
    }

    public static IDataTable<IItem> GetItemsTable(int gen) {
      if (gen < 1 || gen > MaxGen) throw new ArgumentOutOfRangeException(nameof(gen));
      return _itemTables[gen] ??= BuildItemsTable(gen);
    }

    public static IDataTable<IMove> GetMovesTable(int gen) {
      if (gen < 1 || gen > MaxGen) throw new ArgumentOutOfRangeException(nameof(gen));
      return _moveTables[gen] ??= BuildMovesTable(gen);
    }

    public static IDataTable<ISpecie> GetSpeciesTable(int gen) {
      if (gen < 1 || gen > MaxGen) throw new ArgumentOutOfRangeException(nameof(gen));
      return _specieTables[gen] ??= BuildSpeciesTable(gen);
    }

    public static IDataTable<IType> GetTypesTable(int gen) {
      if (gen < 1 || gen > MaxGen) throw new ArgumentOutOfRangeException(nameof(gen));
      return _typeTables[gen] ??= BuildTypesTable(gen);
    }

    public static IDataTable<INature> GetNaturesTable() {
      return _natureTable ??= BuildNaturesTable();
    }

    // ----------------------------------------------------------------
    // Resource loading
    // ----------------------------------------------------------------

    private static JsonDocument LoadEmbeddedJson(string resourceName) {
      var asm = Assembly.GetExecutingAssembly();
      var stream = asm.GetManifestResourceStream(resourceName)
        ?? throw new FileNotFoundException($"Embedded resource not found: {resourceName}");
      using (stream) {
        return JsonDocument.Parse(stream);
      }
    }

    private static JsonDocument GetSpeciesDoc() => _speciesDoc ??= LoadEmbeddedJson("Lusamine.DamageCalc.Data.dex-species.json");
    private static JsonDocument GetMovesDoc() => _movesDoc ??= LoadEmbeddedJson("Lusamine.DamageCalc.Data.dex-moves.json");
    private static JsonDocument GetItemsDoc() => _itemsDoc ??= LoadEmbeddedJson("Lusamine.DamageCalc.Data.dex-items.json");
    private static JsonDocument GetAbilitiesDoc() => _abilitiesDoc ??= LoadEmbeddedJson("Lusamine.DamageCalc.Data.dex-abilities.json");
    private static JsonDocument GetTypesDoc() => _typesDoc ??= LoadEmbeddedJson("Lusamine.DamageCalc.Data.dex-types.json");
    private static JsonDocument GetNaturesDoc() => _naturesDoc ??= LoadEmbeddedJson("Lusamine.DamageCalc.Data.dex-natures.json");

    // ----------------------------------------------------------------
    // Inheritance resolution
    // ----------------------------------------------------------------

    // Parses the top-level JSON document into a per-gen dictionary of entries.
    // genJsonRoot is an object where each key is a gen number (as string)
    // and the value is an object of entityId -> fields.
    private static Dictionary<int, RawData> ParseGenBuckets(JsonElement root) {
      var buckets = new Dictionary<int, RawData>();
      foreach (var genProp in root.EnumerateObject()) {
        if (!int.TryParse(genProp.Name, out var genNum)) continue;
        var entries = new RawData();
        foreach (var entryProp in genProp.Value.EnumerateObject()) {
          var fields = new EntryFields();
          foreach (var fieldProp in entryProp.Value.EnumerateObject()) {
            fields[fieldProp.Name] = fieldProp.Value.Clone();
          }
          entries[entryProp.Name] = fields;
        }
        buckets[genNum] = entries;
      }
      return buckets;
    }

    // Resolve gen N data by walking gen N -> gen N+1 -> ... -> gen 9
    private static RawData ResolveGen(Dictionary<int, RawData> buckets, int targetGen) {
      // Base case: gen 9
      if (targetGen == MaxGen) {
        var base9 = buckets.TryGetValue(MaxGen, out var b9) ? b9 : new RawData();
        // Clone all entries into a new RawData
        var result9 = new RawData(base9.Count);
        foreach (var kvp in base9) {
          var cloned = new EntryFields(kvp.Value.Count);
          foreach (var f in kvp.Value) cloned[f.Key] = f.Value;
          result9[kvp.Key] = cloned;
        }
        return result9;
      }

      // Recurse to get parent (gen targetGen+1)
      var parent = ResolveGen(buckets, targetGen + 1);
      var genNDict = buckets.TryGetValue(targetGen, out var gnd) ? gnd : new RawData();

      var resolved = new RawData(parent.Count + genNDict.Count);

      // Process all parent entries
      foreach (var kvp in parent) {
        var id = kvp.Key;
        if (genNDict.TryGetValue(id, out var childEntry)) {
          // Check if child has inherit:true
          if (childEntry.TryGetValue("inherit", out var inheritEl) &&
              inheritEl.ValueKind == JsonValueKind.True) {
            // Merge: start from parent, override with child fields (except "inherit")
            var merged = new EntryFields(kvp.Value.Count + childEntry.Count);
            foreach (var f in kvp.Value) merged[f.Key] = f.Value;
            foreach (var f in childEntry) {
              if (f.Key != "inherit") merged[f.Key] = f.Value;
            }
            resolved[id] = merged;
          } else {
            // genNDict has id without inherit: complete standalone entry, replaces parent
            var copy = new EntryFields(childEntry.Count);
            foreach (var f in childEntry) copy[f.Key] = f.Value;
            resolved[id] = copy;
          }
        } else {
          // Not in genNDict: inherit parent unchanged
          resolved[id] = kvp.Value;
        }
      }

      // Add entries ONLY in genNDict (not in parent) = older-gen-exclusive entities
      foreach (var kvp in genNDict) {
        if (!resolved.ContainsKey(kvp.Key)) {
          var copy = new EntryFields(kvp.Value.Count);
          foreach (var f in kvp.Value) {
            if (f.Key != "inherit") copy[f.Key] = f.Value;
          }
          resolved[kvp.Key] = copy;
        }
      }

      return resolved;
    }

    // ----------------------------------------------------------------
    // Helper utilities
    // ----------------------------------------------------------------

    private static bool TryGetString(EntryFields entry, string key, out string value) {
      if (entry.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.String) {
        value = el.GetString()!;
        return true;
      }
      value = "";
      return false;
    }

    private static bool TryGetInt(EntryFields entry, string key, out int value) {
      if (entry.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.Number) {
        value = el.GetInt32();
        return true;
      }
      value = 0;
      return false;
    }

    private static bool TryGetBool(EntryFields entry, string key) {
      return entry.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.True;
    }

    private static bool IsTruthy(EntryFields entry, string key) {
      if (!entry.TryGetValue(key, out var el)) return false;
      return el.ValueKind == JsonValueKind.True ||
             (el.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(el.GetString()));
    }

    private static StatId? ParseStatId(string s) {
      return s switch {
        "hp" => StatId.Hp,
        "atk" => StatId.Atk,
        "def" => StatId.Def,
        "spa" => StatId.Spa,
        "spd" => StatId.Spd,
        "spe" => StatId.Spe,
        _ => (StatId?)null
      };
    }

    private static string Capitalize(string s) {
      if (string.IsNullOrEmpty(s)) return s;
      return char.ToUpperInvariant(s[0]) + s.Substring(1);
    }

    private static double DecodeDamageTaken(int encoded) {
      return encoded switch {
        0 => 1.0,
        1 => 2.0,
        2 => 0.5,
        3 => 0.0,
        _ => 1.0
      };
    }

    // ----------------------------------------------------------------
    // Gen-from-num logic
    // ----------------------------------------------------------------

    private static int AbilityIntroGen(int num) {
      if (num >= 268) return 9;
      if (num >= 234) return 8;
      if (num >= 192) return 7;
      if (num >= 165) return 6;
      if (num >= 124) return 5;
      if (num >= 77) return 4;
      if (num >= 1) return 3;
      return -1; // skip
    }

    private static int ItemIntroGen(EntryFields entry) {
      // Items all have explicit gen field
      if (TryGetInt(entry, "gen", out var g) && g > 0) return g;
      // Fallback by num
      TryGetInt(entry, "num", out var num);
      if (num >= 1124) return 9;
      if (num >= 927) return 8;
      if (num >= 689) return 7;
      if (num >= 577) return 6;
      if (num >= 537) return 5;
      if (num >= 377) return 4;
      return 3;
    }

    private static int MoveIntroGen(EntryFields entry) {
      TryGetInt(entry, "num", out var num);
      bool isMax = IsTruthy(entry, "isMax");
      if (num >= 827 && !isMax) return 9;
      if (num >= 743) return 8;
      if (num >= 622) return 7;
      if (num >= 560) return 6;
      if (num >= 468) return 5;
      if (num >= 355) return 4;
      if (num >= 252) return 3;
      if (num >= 166) return 2;
      if (num >= 1) return 1;
      return -1; // skip
    }

    // Z-power chart: for a damaging move with the given base power, what Z-power does it have?
    private static int ComputeZPower(int basePower) {
      if (basePower <= 55) return 100;
      if (basePower <= 65) return 120;
      if (basePower <= 75) return 140;
      if (basePower <= 85) return 160;
      if (basePower <= 95) return 175;
      if (basePower <= 100) return 180;
      if (basePower <= 110) return 185;
      if (basePower <= 120) return 190;
      if (basePower <= 130) return 195;
      return 200;
    }

    private static int SpecieIntroGen(EntryFields entry) {
      // Use explicit gen field if present
      if (TryGetInt(entry, "gen", out var explicitGen) && explicitGen > 0) return explicitGen;
      // Forme-based detection for alternate formes that share dex num with base species
      if (TryGetString(entry, "forme", out var forme) && !string.IsNullOrEmpty(forme)) {
        if (forme.Contains("Mega") || forme == "Primal") return 6;
        if (forme.Contains("Alola")) return 7;
        if (forme.Contains("Galar") || forme.Contains("Gmax")) return 8;
        if (forme.Contains("Hisui")) return 8;
        if (forme.Contains("Paldea")) return 9;
      }
      TryGetInt(entry, "num", out var num);
      if (num >= 906) return 9;
      if (num >= 810) return 8;
      if (num >= 722) return 7;
      if (num >= 650) return 6;
      if (num >= 494) return 5;
      if (num >= 387) return 4;
      if (num >= 252) return 3;
      if (num >= 152) return 2;
      if (num >= 1) return 1;
      return 9; // special forms
    }

    // ----------------------------------------------------------------
    // Abilities
    // ----------------------------------------------------------------

    private static RawData GetResolvedAbilities(int gen) {
      return _rawAbilities[gen] ??= ResolveGen(ParseGenBuckets(GetAbilitiesDoc().RootElement), gen);
    }

    private static IDataTable<IAbility> BuildAbilitiesTable(int gen) {
      if (gen < 3) {
        // No abilities in gen 1-2
        return new DataTable<IAbility>(new Dictionary<string, IAbility>());
      }
      var raw = GetResolvedAbilities(gen);
      var map = new Dictionary<string, IAbility>();
      foreach (var kvp in raw) {
        var id = kvp.Key;
        var entry = kvp.Value;

        // Skip isNonstandard "Past" for gen 9; keep for historical gens
        if (gen == MaxGen && TryGetString(entry, "isNonstandard", out var ns) && ns == "Past") continue;

        TryGetInt(entry, "num", out var num);
        var introGen = AbilityIntroGen(num);
        if (introGen < 0) continue; // skip special/CAP
        if (introGen > gen) continue; // not yet introduced

        TryGetString(entry, "name", out var name);

        var ability = new Ability {
          Id = id,
          Name = name,
          Kind = DataKinds.Ability,
        };
        map[id] = ability;
      }
      return new DataTable<IAbility>(map);
    }

    // ----------------------------------------------------------------
    // Items
    // ----------------------------------------------------------------

    private static RawData GetResolvedItems(int gen) {
      return _rawItems[gen] ??= ResolveGen(ParseGenBuckets(GetItemsDoc().RootElement), gen);
    }

    private static IDataTable<IItem> BuildItemsTable(int gen) {
      if (gen < 2) {
        // No held items in gen 1
        return new DataTable<IItem>(new Dictionary<string, IItem>());
      }
      var raw = GetResolvedItems(gen);
      var map = new Dictionary<string, IItem>();
      foreach (var kvp in raw) {
        var id = kvp.Key;
        var entry = kvp.Value;

        // Skip isNonstandard "Future"
        if (TryGetString(entry, "isNonstandard", out var ns) && ns == "Future") continue;

        var introGen = ItemIntroGen(entry);
        if (introGen > gen) continue;

        TryGetString(entry, "name", out var name);
        var isBerry = TryGetBool(entry, "isBerry");

        // MegaStone
        Dictionary<string, string>? megaStone = null;
        if (entry.TryGetValue("megaStone", out var msEl) && msEl.ValueKind == JsonValueKind.Object) {
          megaStone = new Dictionary<string, string>();
          foreach (var p in msEl.EnumerateObject()) {
            if (p.Value.ValueKind == JsonValueKind.String)
              megaStone[p.Name] = p.Value.GetString()!;
          }
        }

        // NaturalGift
        NaturalGiftData? naturalGift = null;
        if (entry.TryGetValue("naturalGift", out var ngEl) && ngEl.ValueKind == JsonValueKind.Object) {
          int ngBp = 0;
          string ngType = "";
          foreach (var p in ngEl.EnumerateObject()) {
            if (p.Name == "basePower" && p.Value.ValueKind == JsonValueKind.Number)
              ngBp = p.Value.GetInt32();
            else if (p.Name == "type" && p.Value.ValueKind == JsonValueKind.String)
              ngType = p.Value.GetString()!;
          }
          naturalGift = new NaturalGiftData { BasePower = ngBp, Type = ngType };
        }

        var item = new Item {
          Id = id,
          Name = name,
          Kind = DataKinds.Item,
          IsBerry = isBerry,
          MegaStone = megaStone,
          NaturalGift = naturalGift,
        };
        map[id] = item;
      }
      return new DataTable<IItem>(map);
    }

    // ----------------------------------------------------------------
    // Moves
    // ----------------------------------------------------------------

    private static RawData GetResolvedMoves(int gen) {
      return _rawMoves[gen] ??= ResolveGen(ParseGenBuckets(GetMovesDoc().RootElement), gen);
    }

    private static IDataTable<IMove> BuildMovesTable(int gen) {
      var raw = GetResolvedMoves(gen);
      var map = new Dictionary<string, IMove>();
      foreach (var kvp in raw) {
        var id = kvp.Key;
        var entry = kvp.Value;

        TryGetInt(entry, "num", out var num);
        if (num <= 0) continue;

        // Skip nonstandard CAP moves
        if (TryGetString(entry, "isNonstandard", out var ns) && ns == "CAP") continue;

        var introGen = MoveIntroGen(entry);
        if (introGen < 0 || introGen > gen) continue;

        TryGetString(entry, "name", out var name);
        TryGetInt(entry, "basePower", out var basePower);
        TryGetString(entry, "type", out var type);
        TryGetString(entry, "category", out var category);
        TryGetString(entry, "target", out var target);
        TryGetInt(entry, "priority", out var priority);

        // Flags
        var flags = new MoveFlags();
        if (entry.TryGetValue("flags", out var flagsEl) && flagsEl.ValueKind == JsonValueKind.Object) {
          foreach (var fp in flagsEl.EnumerateObject()) {
            bool flagVal = fp.Value.ValueKind == JsonValueKind.Number
              ? fp.Value.GetInt32() == 1
              : fp.Value.ValueKind == JsonValueKind.True;
            switch (fp.Name) {
              case "contact": flags.Contact = flagVal; break;
              case "bite": flags.Bite = flagVal; break;
              case "sound": flags.Sound = flagVal; break;
              case "punch": flags.Punch = flagVal; break;
              case "bullet": flags.Bullet = flagVal; break;
              case "pulse": flags.Pulse = flagVal; break;
              case "slicing": flags.Slicing = flagVal; break;
              case "wind": flags.Wind = flagVal; break;
            }
          }
        }

        // Recoil
        int[]? recoil = null;
        if (entry.TryGetValue("recoil", out var recoilEl) && recoilEl.ValueKind == JsonValueKind.Array) {
          var arr = new List<int>();
          foreach (var e in recoilEl.EnumerateArray()) arr.Add(e.GetInt32());
          recoil = arr.ToArray();
        }

        // Drain
        int[]? drain = null;
        if (entry.TryGetValue("drain", out var drainEl) && drainEl.ValueKind == JsonValueKind.Array) {
          var arr = new List<int>();
          foreach (var e in drainEl.EnumerateArray()) arr.Add(e.GetInt32());
          drain = arr.ToArray();
        }

        // Self boosts
        SelfOrSecondaryEffect? self = null;
        if (entry.TryGetValue("self", out var selfEl) && selfEl.ValueKind == JsonValueKind.Object) {
          if (selfEl.TryGetProperty("boosts", out var boostsEl) && boostsEl.ValueKind == JsonValueKind.Object) {
            var boosts = new StatsTableInput();
            foreach (var bp in boostsEl.EnumerateObject()) {
              int bv = bp.Value.GetInt32();
              switch (bp.Name) {
                case "hp": boosts.Hp = bv; break;
                case "atk": boosts.Atk = bv; break;
                case "def": boosts.Def = bv; break;
                case "spa": boosts.Spa = bv; break;
                case "spd": boosts.Spd = bv; break;
                case "spe": boosts.Spe = bv; break;
              }
            }
            self = new SelfOrSecondaryEffect { Boosts = boosts };
          }
        }

        // OverrideOffensiveStat / OverrideDefensiveStat
        StatId? overrideOffStat = null;
        if (TryGetString(entry, "overrideOffensiveStat", out var oos)) overrideOffStat = ParseStatId(oos);
        StatId? overrideDefStat = null;
        if (TryGetString(entry, "overrideDefensiveStat", out var ods)) overrideDefStat = ParseStatId(ods);

        // OverrideOffensivePokemon / OverrideDefensivePokemon
        string? overrideOffPoke = null;
        if (TryGetString(entry, "overrideOffensivePokemon", out var oop) && !string.IsNullOrEmpty(oop))
          overrideOffPoke = oop;
        string? overrideDefPoke = null;
        if (TryGetString(entry, "overrideDefensivePokemon", out var odp) && !string.IsNullOrEmpty(odp))
          overrideDefPoke = odp;

        bool isZ = IsTruthy(entry, "isZ");
        bool isMax = IsTruthy(entry, "isMax");

        // ZMove
        ZMoveData? zMove = null;
        if (entry.TryGetValue("zMove", out var zMoveEl) && zMoveEl.ValueKind == JsonValueKind.Object) {
          if (zMoveEl.TryGetProperty("basePower", out var zbp) && zbp.ValueKind == JsonValueKind.Number) {
            zMove = new ZMoveData { BasePower = zbp.GetInt32() };
          }
        }
        // For gen 7: damaging moves without explicit zMove.basePower get auto-computed Z-power
        // (Z-moves were a gen 7 mechanic; the JSON doesn't include these for regular moves)
        if (zMove == null && gen == 7 && basePower > 0 &&
            !string.IsNullOrEmpty(category) && category != MoveCategories.Status) {
          zMove = new ZMoveData { BasePower = ComputeZPower(basePower) };
        }

        // MaxMove
        MaxMoveData? maxMove = null;
        if (entry.TryGetValue("maxMove", out var maxMoveEl) && maxMoveEl.ValueKind == JsonValueKind.Object) {
          if (maxMoveEl.TryGetProperty("basePower", out var mbp) && mbp.ValueKind == JsonValueKind.Number) {
            maxMove = new MaxMoveData { BasePower = mbp.GetInt32() };
          }
        }

        // Multihit: int or int[]
        object? multihit = null;
        if (entry.TryGetValue("multihit", out var multihitEl)) {
          if (multihitEl.ValueKind == JsonValueKind.Number) {
            multihit = multihitEl.GetInt32();
          } else if (multihitEl.ValueKind == JsonValueKind.Array) {
            var arr = new List<int>();
            foreach (var e in multihitEl.EnumerateArray()) arr.Add(e.GetInt32());
            multihit = arr.ToArray();
          }
        }

        // Secondaries: non-null object -> (object)true
        object? secondaries = null;
        if (entry.TryGetValue("secondary", out var secEl) && secEl.ValueKind == JsonValueKind.Object) {
          secondaries = true;
        }

        var move = new MoveData {
          Id = id,
          Name = name,
          Kind = DataKinds.Move,
          BasePower = basePower,
          Type = type,
          // In gen 1-3, category is determined by type for damaging moves (not stored).
          // Set null so Move.cs computes Physical/Special from type. Keep "Status" as-is.
          Category = (gen <= 3 && category != MoveCategories.Status) ? null
                   : string.IsNullOrEmpty(category) ? null : category,
          Flags = flags,
          Target = string.IsNullOrEmpty(target) ? null : target,
          Recoil = recoil,
          HasCrashDamage = TryGetBool(entry, "hasCrashDamage"),
          MindBlownRecoil = TryGetBool(entry, "mindBlownRecoil"),
          StruggleRecoil = TryGetBool(entry, "struggleRecoil"),
          WillCrit = TryGetBool(entry, "willCrit"),
          Drain = drain,
          Priority = priority,
          Self = self,
          IgnoreDefensive = TryGetBool(entry, "ignoreDefensive"),
          OverrideOffensiveStat = overrideOffStat,
          OverrideDefensiveStat = overrideDefStat,
          OverrideOffensivePokemon = overrideOffPoke,
          OverrideDefensivePokemon = overrideDefPoke,
          BreaksProtect = TryGetBool(entry, "breaksProtect"),
          IsZ = isZ,
          ZMove = zMove,
          IsMax = isMax,
          MaxMove = maxMove,
          Multihit = multihit,
          Multiaccuracy = TryGetBool(entry, "multiaccuracy"),
          Secondaries = secondaries,
        };
        map[id] = move;
      }
      return new DataTable<IMove>(map);
    }

    // ----------------------------------------------------------------
    // Species
    // ----------------------------------------------------------------

    private static RawData GetResolvedSpecies(int gen) {
      return _rawSpecies[gen] ??= ResolveGen(ParseGenBuckets(GetSpeciesDoc().RootElement), gen);
    }

    private static IDataTable<ISpecie> BuildSpeciesTable(int gen) {
      var raw = GetResolvedSpecies(gen);
      var map = new Dictionary<string, ISpecie>();
      foreach (var kvp in raw) {
        var id = kvp.Key;
        var entry = kvp.Value;

        var introGen = SpecieIntroGen(entry);
        if (introGen > gen) continue;

        TryGetString(entry, "name", out var name);
        TryGetInt(entry, "num", out var num);

        // Types
        string[] types = Array.Empty<string>();
        if (entry.TryGetValue("types", out var typesEl) && typesEl.ValueKind == JsonValueKind.Array) {
          var list = new List<string>();
          foreach (var t in typesEl.EnumerateArray()) {
            if (t.ValueKind == JsonValueKind.String) list.Add(t.GetString()!);
          }
          types = list.ToArray();
        }

        // BaseStats
        var baseStats = new StatsTable();
        if (entry.TryGetValue("baseStats", out var bsEl) && bsEl.ValueKind == JsonValueKind.Object) {
          foreach (var sp in bsEl.EnumerateObject()) {
            int sv = sp.Value.GetInt32();
            switch (sp.Name) {
              case "hp": baseStats.Hp = sv; break;
              case "atk": baseStats.Atk = sv; break;
              case "def": baseStats.Def = sv; break;
              case "spa": baseStats.Spa = sv; break;
              case "spd": baseStats.Spd = sv; break;
              case "spe": baseStats.Spe = sv; break;
            }
          }
          // Gen 1-2: Spc = Spa
          if (gen <= 2) baseStats.Spc = baseStats.Spa;
        }

        // WeightKg
        double weightKg = 0;
        if (entry.TryGetValue("weightkg", out var wEl) && wEl.ValueKind == JsonValueKind.Number)
          weightKg = wEl.GetDouble();

        // Nfe: has evos
        bool nfe = entry.ContainsKey("evos");

        // Gender (optional string)
        string? gender = null;
        if (TryGetString(entry, "gender", out var gstr) && !string.IsNullOrEmpty(gstr))
          gender = gstr;

        // OtherFormes
        string[]? otherFormes = null;
        if (entry.TryGetValue("otherFormes", out var ofEl) && ofEl.ValueKind == JsonValueKind.Array) {
          var list = new List<string>();
          foreach (var e in ofEl.EnumerateArray()) {
            if (e.ValueKind == JsonValueKind.String) list.Add(e.GetString()!);
          }
          otherFormes = list.ToArray();
        }

        // BaseSpecies
        string? baseSpecies = null;
        if (TryGetString(entry, "baseSpecies", out var bs) && !string.IsNullOrEmpty(bs))
          baseSpecies = bs;

        // Abilities: gen >= 3 and gen >= 5 for hidden ability slot
        Dictionary<int, string>? abilities = null;
        if (gen >= 3 && entry.TryGetValue("abilities", out var ablEl) && ablEl.ValueKind == JsonValueKind.Object) {
          abilities = new Dictionary<int, string>();
          foreach (var ap in ablEl.EnumerateObject()) {
            if (ap.Value.ValueKind != JsonValueKind.String) continue;
            string abilityName = ap.Value.GetString()!;
            if (ap.Name == "0") abilities[0] = abilityName;
            else if (ap.Name == "1") abilities[1] = abilityName;
            else if (ap.Name == "H" && gen >= 5) abilities[2] = abilityName;
          }
          if (abilities.Count == 0) abilities = null;
        }

        var specie = new Specie {
          Id = id,
          Name = name,
          Kind = DataKinds.Species,
          Types = types,
          BaseStats = baseStats,
          WeightKg = weightKg,
          Nfe = nfe,
          Gender = gender,
          OtherFormes = otherFormes,
          BaseSpecies = baseSpecies,
          Abilities = abilities,
        };
        map[id] = specie;
      }
      return new DataTable<ISpecie>(map);
    }

    // ----------------------------------------------------------------
    // Types
    // ----------------------------------------------------------------

    private static RawData GetResolvedTypes(int gen) {
      return _rawTypes[gen] ??= ResolveGen(ParseGenBuckets(GetTypesDoc().RootElement), gen);
    }

    private static IDataTable<IType> BuildTypesTable(int gen) {
      var raw = GetResolvedTypes(gen);

      // Build effectiveness maps:
      // For each attacking type, build effectiveness[defType] = decode(defType.damageTaken[Capitalize(attackType)])
      // First pass: collect all valid type ids and their damageTaken maps (exclude nonstandard)
      var validTypes = new Dictionary<string, Dictionary<string, int>>();
      foreach (var kvp in raw) {
        var id = kvp.Key;
        var entry = kvp.Value;
        // Skip "Future" nonstandard types
        if (TryGetString(entry, "isNonstandard", out var ns) && ns == "Future") continue;
        if (!entry.TryGetValue("damageTaken", out var dtEl) || dtEl.ValueKind != JsonValueKind.Object) continue;
        var damageTaken = new Dictionary<string, int>();
        foreach (var p in dtEl.EnumerateObject()) {
          if (p.Value.ValueKind == JsonValueKind.Number)
            damageTaken[p.Name] = p.Value.GetInt32();
        }
        validTypes[id] = damageTaken;
      }

      // Add "???" type for gen 1-2 (neutral 1x against all types)
      if (gen <= 2) {
        var neutralDt = new Dictionary<string, int>();
        foreach (var typeId in validTypes.Keys) {
          neutralDt[Capitalize(typeId)] = 0; // 0 = 1x
        }
        validTypes["???"] = neutralDt;
      }

      // Build a name map: id -> Name (for "???" keep as-is, else Capitalize)
      // validTypes keys are lowercase ids, except "???"
      var idToName = new Dictionary<string, string>();
      foreach (var id in validTypes.Keys) {
        idToName[id] = id == "???" ? "???" : Capitalize(id);
      }

      // Second pass: build the attacking-perspective effectiveness for each type
      // attackType.Effectiveness[defTypeName] = decode(defType.damageTaken[attackTypeName])
      // Keys in the effectiveness dictionary use Title Case names (matching how defender.Types are stored)
      var map = new Dictionary<string, IType>();
      foreach (var attackId in validTypes.Keys) {
        var attackName = idToName[attackId]; // e.g. "Normal", "Fire", "???"
        var effectiveness = new Dictionary<string, double>();

        foreach (var kvp in validTypes) {
          var defId = kvp.Key;
          var defName = idToName[defId];   // e.g. "Ghost", "Rock"
          var defDt = kvp.Value;
          // defDt keys are Title Case attack type names (e.g. "Normal", "Bug")
          if (attackId != "???" && defDt.TryGetValue(attackName, out var encoded)) {
            effectiveness[defName] = DecodeDamageTaken(encoded);
          } else {
            // "???" attacker or type not in damageTaken -> neutral
            effectiveness[defName] = 1.0;
          }
        }

        var typeData = new TypeData {
          Id = attackId,
          Name = attackName,
          Kind = DataKinds.Type,
          Effectiveness = effectiveness,
        };
        map[typeData.Id] = typeData;
      }

      return new DataTable<IType>(map);
    }

    // ----------------------------------------------------------------
    // Natures
    // ----------------------------------------------------------------

    private static IDataTable<INature> BuildNaturesTable() {
      var doc = GetNaturesDoc();
      var root = doc.RootElement;

      // natures.json only has gen 9, flat structure: { "9": { "id": { fields } } }
      JsonElement gen9El;
      if (!root.TryGetProperty("9", out gen9El)) {
        return new DataTable<INature>(new Dictionary<string, INature>());
      }

      var map = new Dictionary<string, INature>();
      foreach (var entryProp in gen9El.EnumerateObject()) {
        var id = entryProp.Name;
        var entry = entryProp.Value;

        string name = "";
        StatId? plus = null;
        StatId? minus = null;

        foreach (var fp in entry.EnumerateObject()) {
          switch (fp.Name) {
            case "name":
              if (fp.Value.ValueKind == JsonValueKind.String) name = fp.Value.GetString()!;
              break;
            case "plus":
              if (fp.Value.ValueKind == JsonValueKind.String) plus = ParseStatId(fp.Value.GetString()!);
              break;
            case "minus":
              if (fp.Value.ValueKind == JsonValueKind.String) minus = ParseStatId(fp.Value.GetString()!);
              break;
          }
        }

        var nature = new Nature {
          Id = id,
          Name = name,
          Kind = DataKinds.Nature,
          Plus = plus,
          Minus = minus,
        };
        map[id] = nature;
      }
      return new DataTable<INature>(map);
    }
  }
}
