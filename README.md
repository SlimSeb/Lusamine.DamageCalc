# Lusamine.DamageCalc

A Pokémon damage calculator library for .NET 8, supporting generations 1–9. Ported from [@smogon/calc](https://github.com/smogon/damage-calc).

## Installation

```
dotnet add package Lusamine.DamageCalc
```

## Quick start

```csharp
using Lusamine.DamageCalc;
using Lusamine.DamageCalc.Data;

// 1. Create a generation data context
var gen = DataIndex.Create(9); // gen 9 (Scarlet/Violet)

// 2. Build attacker, defender, and move
var attacker = new Pokemon(gen, "Gengar", new State.Pokemon {
    Item    = "Life Orb",
    Nature  = "Modest",
    Evs     = new StatsTableInput { Spa = 252 },
    Boosts  = new StatsTableInput { Spa = 3 },
});

var defender = new Pokemon(gen, "Chansey", new State.Pokemon {
    Item   = "Eviolite",
    Nature = "Bold",
    Evs    = new StatsTableInput { Hp = 100, Spd = 100 },
    Boosts = new StatsTableInput { Spd = 1 },
});

var move = new Move(gen, "Sludge Bomb");

// 3. Calculate
var result = Calc.Calculate(gen, attacker, defender, move);

Console.WriteLine(result.Desc());
// +3 252+ SpA Life Orb Gengar Sludge Bomb vs. +1 100 HP / 100 SpD Eviolite Chansey:
//   204-242 (30.6 - 36.3%) -- 52.9% chance to 3HKO

Console.WriteLine(result.Range()); // (204, 242)
```

## Core concepts

### Generations (1–9)

Each calculation is scoped to a generation. Create one with `DataIndex.Create(int gen)`. The object is cheap to cache:

```csharp
var gen4 = DataIndex.Create(4);
var gen9 = DataIndex.Create(9);
```

The bundled JSON data covers all Pokémon, moves, items, abilities, types, and natures for every generation.

### Pokemon

```csharp
var p = new Pokemon(gen, "Garchomp", new State.Pokemon {
    Level   = 50,
    Nature  = "Jolly",
    Ability = "Rough Skin",
    Item    = "Choice Scarf",
    Evs     = new StatsTableInput { Atk = 252, Spe = 252, Hp = 4 },
    Ivs     = new StatsTableInput { Spa = 0 },   // dump SpA
    Boosts  = new StatsTableInput { Atk = 1 },   // +1 after a Dragon Dance
    Status  = "brn",                              // "brn" | "par" | "slp" | "frz" | "psn" | "tox"
    CurHP   = 150,                                // current HP (defaults to max)
    TeraType = "Dragon",                          // Gen 9 Terastallization
});
```

**Gen 1/2 note:** use `Ivs` with `Spc` for the Special stat, or set DVs directly—the HP DV is derived automatically.

### Move

```csharp
// Normal move
var move = new Move(gen, "Earthquake");

// Z-Move
var zMove = new Move(gen, "Wood Hammer", new State.Move { UseZ = true });

// Max Move / G-Max Move
var maxMove = new Move(gen, "Flamethrower", new State.Move { UseMax = true });

// Critical hit
var crit = new Move(gen, "Surf", new State.Move { IsCrit = true });

// Multihit override (force 5 hits for Bullet Seed)
var fiveHit = new Move(gen, "Bullet Seed", new State.Move { Hits = 5 });

// Stat overrides (e.g. Tera Blast Physical)
var teraBlast = new Move(gen, "Tera Blast", new State.Move {
    Overrides = new DamageCalc.Data.MoveData { Category = "Physical" }
});
```

### Field

```csharp
var field = new Field(new State.Field {
    GameType = GameTypes.Doubles,       // "Singles" (default) | "Doubles"
    Weather  = "Rain",                  // "Sun" | "Rain" | "Sand" | "Hail" | "Snow"
    Terrain  = "Electric",              // "Electric" | "Grassy" | "Misty" | "Psychic"

    AttackerSide = new State.Side {
        IsHelpingHand = true,
        IsTailwind    = true,
    },
    DefenderSide = new State.Side {
        IsSR          = true,           // Stealth Rock
        Spikes        = 1,              // 1–3 layers
        IsLightScreen = true,
        IsReflect     = false,
        IsSeeded      = true,           // Leech Seed
        IsFriendGuard = true,           // Doubles ally reduction
    },
});
```

### Result

```csharp
var result = Calc.Calculate(gen, attacker, defender, move, field);

// Damage range
(int min, int max) range = result.Range();          // (204, 242)

// Full description string
string desc = result.Desc();                        // default % notation
string descPx = result.FullDesc("px");             // pixel notation

// KO chance
var ko = result.Kochance();
Console.WriteLine($"{ko.chance:P1} chance to {ko.n}HKO — {ko.text}");

// Recoil / recovery
var recoil   = result.Recoil();    // (double[] recoil, string text)
var recovery = result.Recovery();  // (double[] recovery, string text)

// Access the post-calc state
result.Attacker.Boosts.Spa;   // may be modified by e.g. Meteor Beam
result.Field.DefenderSide.IsReflect; // false after Brick Break
```

## Doubles

```csharp
var field = new Field(new State.Field { GameType = GameTypes.Doubles });

// Spread moves hit all adjacent: pass field to trigger spread damage reduction
var result = Calc.Calculate(gen, attacker, defender, new Move(gen, "Earthquake"), field);
```

## Caching generations

`DataIndex.Create` parses JSON each call. Cache instances for performance:

```csharp
// Application startup
private static readonly Dictionary<int, IGeneration> Gens =
    Enumerable.Range(1, 9)
              .ToDictionary(g => g, DataIndex.Create);

// Usage
var result = Calc.Calculate(Gens[9], attacker, defender, move);
```

## Stat calculator

```csharp
using DamageCalc;
using DamageCalc.Data;

var gen = DataIndex.Create(9);

int speed = Stats.CalcStat(gen, StatId.Spe, baseSpeed: 130, iv: 31, ev: 252, level: 50, nature: "Jolly");
```

## Custom move/species data

Pass your own data tables to `DataIndex.Create` to override moves or species for custom game modes:

```csharp
// Example: give Splash 200 base power
var customMoves = JsonDataLoader.GetMovesTable(9);
// (modify the table as needed)
var gen = DataIndex.Create(9, customMoves, JsonDataLoader.GetSpeciesTable(9));
```
