using System.Linq;
using Lusamine.DamageCalc.Data;
using NUnit.Framework;

namespace Lusamine.DamageCalc.Tests {
  public sealed class DataTests {
    // ── Abilities ──────────────────────────────────────────────────────────

    [Test]
    public void Generations_Abilities() {
      // Gen 1-2: no abilities
      Assert.That(TestHelper.Gen(1).Abilities.Count(), Is.EqualTo(0));
      Assert.That(TestHelper.Gen(2).Abilities.Count(), Is.EqualTo(0));

      // Gen 3: basic abilities present
      var gen3 = TestHelper.Gen(3);
      Assert.That(gen3.Abilities.Count(), Is.GreaterThan(0));

      var overgrow = gen3.Abilities.Get("overgrow");
      Assert.That(overgrow, Is.Not.Null);
      Assert.That(overgrow!.Name, Is.EqualTo("Overgrow"));
      Assert.That(overgrow.Kind, Is.EqualTo(DataKinds.Ability));

      Assert.That(gen3.Abilities.Get("blaze"), Is.Not.Null);
      Assert.That(gen3.Abilities.Get("torrent"), Is.Not.Null);

      // Beast Boost (gen 7): absent from gen 6, present from gen 7
      Assert.That(TestHelper.Gen(6).Abilities.Get("beastboost"), Is.Null);

      var beastBoost = TestHelper.Gen(7).Abilities.Get("beastboost");
      Assert.That(beastBoost, Is.Not.Null);
      Assert.That(beastBoost!.Name, Is.EqualTo("Beast Boost"));

      Assert.That(TestHelper.Gen(9).Abilities.Get("beastboost"), Is.Not.Null);

      // Contrary (gen 5): absent from gen 4, present from gen 5
      Assert.That(TestHelper.Gen(4).Abilities.Get("contrary"), Is.Null);
      Assert.That(TestHelper.Gen(5).Abilities.Get("contrary"), Is.Not.Null);

      // Each gen's count is non-decreasing
      int prevCount = 0;
      foreach (var g in new[] { 3, 4, 5, 6, 7, 8, 9 }) {
        int count = TestHelper.Gen(g).Abilities.Count();
        Assert.That(count, Is.GreaterThanOrEqualTo(prevCount), $"Gen {g} ability count < gen {g - 1}");
        prevCount = count;
      }
    }

    // ── Items ──────────────────────────────────────────────────────────────

    [Test]
    public void Generations_Items() {
      // Gen 1: no held items
      Assert.That(TestHelper.Gen(1).Items.Count(), Is.EqualTo(0));

      // Gen 2: Leftovers introduced
      var gen2 = TestHelper.Gen(2);
      var leftovers = gen2.Items.Get("leftovers");
      Assert.That(leftovers, Is.Not.Null);
      Assert.That(leftovers!.Name, Is.EqualTo("Leftovers"));
      Assert.That(leftovers.Kind, Is.EqualTo(DataKinds.Item));

      // Gen 3: Choice Band introduced
      Assert.That(TestHelper.Gen(3).Items.Get("choiceband"), Is.Not.Null);

      // Aerodactylite (gen 6 mega stone): absent from gen 5, present from gen 6
      Assert.That(TestHelper.Gen(5).Items.Get("aerodactylite"), Is.Null);

      var aerodactylite = TestHelper.Gen(6).Items.Get("aerodactylite");
      Assert.That(aerodactylite, Is.Not.Null);
      Assert.That(aerodactylite!.MegaStone, Is.Not.Null);
      Assert.That(aerodactylite.MegaStone!["Aerodactyl"], Is.EqualTo("Aerodactyl-Mega"));

      // Berries: isBerry + naturalGift
      var aguav = TestHelper.Gen(3).Items.Get("aguavberry");
      Assert.That(aguav, Is.Not.Null);
      Assert.That(aguav!.IsBerry, Is.True);
      Assert.That(aguav.NaturalGift, Is.Not.Null);
      Assert.That(aguav.NaturalGift!.Type, Is.EqualTo("Dragon"));
      Assert.That(aguav.NaturalGift.BasePower, Is.EqualTo(60));

      // Sitrus Berry
      var sitrus = TestHelper.Gen(3).Items.Get("sitrusberry");
      Assert.That(sitrus, Is.Not.Null);
      Assert.That(sitrus!.IsBerry, Is.True);

      // Z-crystals are gen 7: absent from gen 6
      Assert.That(TestHelper.Gen(6).Items.Get("normaliumz"), Is.Null);
      Assert.That(TestHelper.Gen(7).Items.Get("normaliumz"), Is.Not.Null);
    }

    // ── Moves ──────────────────────────────────────────────────────────────

    [Test]
    public void Generations_Moves() {
      // Draco Meteor: 140 BP in gen 4-5, 130 BP in gen 6+
      Assert.That(TestHelper.Gen(4).Moves.Get("dracometeor")!.BasePower, Is.EqualTo(140));
      Assert.That(TestHelper.Gen(5).Moves.Get("dracometeor")!.BasePower, Is.EqualTo(140));
      Assert.That(TestHelper.Gen(6).Moves.Get("dracometeor")!.BasePower, Is.EqualTo(130));
      Assert.That(TestHelper.Gen(9).Moves.Get("dracometeor")!.BasePower, Is.EqualTo(130));

      // Surf: 95 BP in gen 1-5, 90 BP in gen 6+
      Assert.That(TestHelper.Gen(1).Moves.Get("surf")!.BasePower, Is.EqualTo(95));
      Assert.That(TestHelper.Gen(5).Moves.Get("surf")!.BasePower, Is.EqualTo(95));
      Assert.That(TestHelper.Gen(6).Moves.Get("surf")!.BasePower, Is.EqualTo(90));

      // Bite: Normal type in gen 1, Dark in gen 2+
      Assert.That(TestHelper.Gen(1).Moves.Get("bite")!.Type, Is.EqualTo("Normal"));
      Assert.That(TestHelper.Gen(2).Moves.Get("bite")!.Type, Is.EqualTo("Dark"));
      Assert.That(TestHelper.Gen(9).Moves.Get("bite")!.Type, Is.EqualTo("Dark"));

      // Shadow Ball (gen 2): absent from gen 1, present from gen 2
      Assert.That(TestHelper.Gen(1).Moves.Get("shadowball"), Is.Null);
      Assert.That(TestHelper.Gen(2).Moves.Get("shadowball"), Is.Not.Null);

      // Body Press (gen 8): overrideOffensiveStat = Def
      var bodyPress = TestHelper.Gen(8).Moves.Get("bodypress");
      Assert.That(bodyPress, Is.Not.Null);
      Assert.That(bodyPress!.OverrideOffensiveStat, Is.EqualTo(StatId.Def));

      // Foul Play: overrideOffensivePokemon = "target"
      var foulPlay = TestHelper.Gen(9).Moves.Get("foulplay");
      Assert.That(foulPlay!.OverrideOffensivePokemon, Is.EqualTo("target"));

      // Drain move: Absorb [1, 2]
      var absorb = TestHelper.Gen(1).Moves.Get("absorb");
      Assert.That(absorb!.Drain, Is.Not.Null);
      Assert.That(absorb.Drain![0], Is.EqualTo(1));
      Assert.That(absorb.Drain[1], Is.EqualTo(2));

      // Recoil move: Brave Bird
      Assert.That(TestHelper.Gen(9).Moves.Get("bravebird")!.Recoil, Is.Not.Null);

      // Contact flag: Tackle yes, Flamethrower no
      Assert.That(TestHelper.Gen(9).Moves.Get("tackle")!.Flags.Contact, Is.True);
      Assert.That(TestHelper.Gen(9).Moves.Get("flamethrower")!.Flags.Contact, Is.False);

      // Bite flag: bite=true
      Assert.That(TestHelper.Gen(9).Moves.Get("bite")!.Flags.Bite, Is.True);

      // Bullet Seed: multihit
      var bulletSeed = TestHelper.Gen(4).Moves.Get("bulletseed");
      Assert.That(bulletSeed!.Multihit, Is.Not.Null);

      // Z-move: Acid Armor has zMove with no basePower (effect-only), Bone Rush has basePower
      var boneRush = TestHelper.Gen(7).Moves.Get("bonerush");
      Assert.That(boneRush!.ZMove, Is.Not.Null);
      Assert.That(boneRush.ZMove!.BasePower, Is.GreaterThan(0));

      // willCrit: Frost Breath
      Assert.That(TestHelper.Gen(9).Moves.Get("frostbreath")!.WillCrit, Is.True);

      // hasCrashDamage: High Jump Kick
      Assert.That(TestHelper.Gen(9).Moves.Get("highjumpkick")!.HasCrashDamage, Is.True);

      // breakProtect: Feint
      Assert.That(TestHelper.Gen(9).Moves.Get("feint")!.BreaksProtect, Is.True);

      // Kind constant
      Assert.That(TestHelper.Gen(1).Moves.Get("tackle")!.Kind, Is.EqualTo(DataKinds.Move));
    }

    // ── Species ────────────────────────────────────────────────────────────

    [Test]
    public void Generations_Species() {
      // Bulbasaur: gen 1+, grass/poison, nfe
      var bulbasaur1 = TestHelper.Gen(1).Species.Get("bulbasaur");
      Assert.That(bulbasaur1, Is.Not.Null);
      Assert.That(bulbasaur1!.Types, Is.EqualTo(new[] { "Grass", "Poison" }));
      Assert.That(bulbasaur1.BaseStats.Hp, Is.EqualTo(45));
      Assert.That(bulbasaur1.BaseStats.Atk, Is.EqualTo(49));
      Assert.That(bulbasaur1.WeightKg, Is.EqualTo(6.9));
      Assert.That(bulbasaur1.Nfe, Is.True);
      Assert.That(bulbasaur1.Kind, Is.EqualTo(DataKinds.Species));

      // Venusaur: not nfe (no evolutions)
      Assert.That(TestHelper.Gen(1).Species.Get("venusaur")!.Nfe, Is.False);

      // Pikachu stat changes: Def=30 in gen 1-5, Def=40 in gen 6+
      Assert.That(TestHelper.Gen(1).Species.Get("pikachu")!.BaseStats.Def, Is.EqualTo(30));
      Assert.That(TestHelper.Gen(5).Species.Get("pikachu")!.BaseStats.Def, Is.EqualTo(30));
      Assert.That(TestHelper.Gen(6).Species.Get("pikachu")!.BaseStats.Def, Is.EqualTo(40));

      // Clefable: Normal type in gen 1-5, Fairy type in gen 6+
      Assert.That(TestHelper.Gen(5).Species.Get("clefable")!.Types, Is.EqualTo(new[] { "Normal" }));
      Assert.That(TestHelper.Gen(6).Species.Get("clefable")!.Types, Is.EqualTo(new[] { "Fairy" }));

      // Magnemite: Electric in gen 1, Electric/Steel in gen 2+
      Assert.That(TestHelper.Gen(1).Species.Get("magnemite")!.Types, Is.EqualTo(new[] { "Electric" }));
      Assert.That(TestHelper.Gen(2).Species.Get("magnemite")!.Types, Is.EqualTo(new[] { "Electric", "Steel" }));

      // Sylveon (gen 6): absent from gen 5, present from gen 6
      Assert.That(TestHelper.Gen(5).Species.Get("sylveon"), Is.Null);
      Assert.That(TestHelper.Gen(6).Species.Get("sylveon"), Is.Not.Null);

      // Venusaur-Mega (gen 6 forme): absent from gen 5, present from gen 6
      Assert.That(TestHelper.Gen(5).Species.Get("venusaurmega"), Is.Null);
      var venusMega = TestHelper.Gen(6).Species.Get("venusaurmega");
      Assert.That(venusMega, Is.Not.Null);
      Assert.That(venusMega!.BaseSpecies, Is.EqualTo("Venusaur"));

      // Gen 1: no abilities on species
      Assert.That(bulbasaur1.Abilities, Is.Null);

      // Gen 3+: abilities on species
      var bulbasaur3 = TestHelper.Gen(3).Species.Get("bulbasaur");
      Assert.That(bulbasaur3!.Abilities, Is.Not.Null);
      Assert.That(bulbasaur3.Abilities![0], Is.EqualTo("Overgrow"));

      // Hidden ability only from gen 5
      Assert.That(TestHelper.Gen(4).Species.Get("bulbasaur")!.Abilities!.ContainsKey(2), Is.False);
      Assert.That(TestHelper.Gen(5).Species.Get("bulbasaur")!.Abilities!.ContainsKey(2), Is.True);
      Assert.That(TestHelper.Gen(5).Species.Get("bulbasaur")!.Abilities![2], Is.EqualTo("Chlorophyll"));
    }

    // ── Types ──────────────────────────────────────────────────────────────

    [Test]
    public void Generations_Types() {
      // Gen 1: no Dark, Steel, or Fairy
      Assert.That(TestHelper.Gen(1).Types.Get("dark"), Is.Null);
      Assert.That(TestHelper.Gen(1).Types.Get("steel"), Is.Null);
      Assert.That(TestHelper.Gen(1).Types.Get("fairy"), Is.Null);

      // Gen 2: Dark and Steel added; still no Fairy
      Assert.That(TestHelper.Gen(2).Types.Get("dark"), Is.Not.Null);
      Assert.That(TestHelper.Gen(2).Types.Get("steel"), Is.Not.Null);
      Assert.That(TestHelper.Gen(2).Types.Get("fairy"), Is.Null);

      // Gen 6: Fairy added
      var fairy = TestHelper.Gen(6).Types.Get("fairy");
      Assert.That(fairy, Is.Not.Null);
      Assert.That(fairy!.Name, Is.EqualTo("Fairy"));
      Assert.That(fairy.Kind, Is.EqualTo(DataKinds.Type));

      var gen9 = TestHelper.Gen(9);

      // Fire > Grass = 2x (super effective)
      Assert.That(gen9.Types.Get("fire")!.Effectiveness["Grass"], Is.EqualTo(2.0));
      // Water > Fire = 2x
      Assert.That(gen9.Types.Get("water")!.Effectiveness["Fire"], Is.EqualTo(2.0));
      // Grass > Water = 2x
      Assert.That(gen9.Types.Get("grass")!.Effectiveness["Water"], Is.EqualTo(2.0));

      // Normal > Ghost = 0x (immune)
      Assert.That(gen9.Types.Get("normal")!.Effectiveness["Ghost"], Is.EqualTo(0.0));
      // Electric > Ground = 0x (immune)
      Assert.That(gen9.Types.Get("electric")!.Effectiveness["Ground"], Is.EqualTo(0.0));
      // Ghost > Normal = 0x (immune)
      Assert.That(gen9.Types.Get("ghost")!.Effectiveness["Normal"], Is.EqualTo(0.0));

      // Fire > Fire = 0.5x (resist)
      Assert.That(gen9.Types.Get("fire")!.Effectiveness["Fire"], Is.EqualTo(0.5));
      // Water > Water = 0.5x
      Assert.That(gen9.Types.Get("water")!.Effectiveness["Water"], Is.EqualTo(0.5));

      // Ground > Fire = 2x
      Assert.That(gen9.Types.Get("ground")!.Effectiveness["Fire"], Is.EqualTo(2.0));

      // Fairy > Dragon = 2x (gen 6+)
      Assert.That(TestHelper.Gen(6).Types.Get("fairy")!.Effectiveness["Dragon"], Is.EqualTo(2.0));

      // Steel > Fairy = 2x (gen 6+)
      Assert.That(TestHelper.Gen(6).Types.Get("steel")!.Effectiveness["Fairy"], Is.EqualTo(2.0));
    }

    // ── Natures ────────────────────────────────────────────────────────────

    [Test]
    public void Generations_Natures() {
      // 25 natures, same across all gens
      foreach (var g in new[] { 1, 3, 5, 9 }) {
        Assert.That(TestHelper.Gen(g).Natures.Count(), Is.EqualTo(25), $"Gen {g} nature count");
      }

      var natures = TestHelper.Gen(9).Natures;

      // Adamant: +Atk / -Spa
      var adamant = natures.Get("adamant");
      Assert.That(adamant, Is.Not.Null);
      Assert.That(adamant!.Name, Is.EqualTo("Adamant"));
      Assert.That(adamant.Plus, Is.EqualTo(StatId.Atk));
      Assert.That(adamant.Minus, Is.EqualTo(StatId.Spa));
      Assert.That(adamant.Kind, Is.EqualTo(DataKinds.Nature));

      // Timid: +Spe / -Atk
      var timid = natures.Get("timid");
      Assert.That(timid!.Plus, Is.EqualTo(StatId.Spe));
      Assert.That(timid.Minus, Is.EqualTo(StatId.Atk));

      // Modest: +Spa / -Atk
      var modest = natures.Get("modest");
      Assert.That(modest!.Plus, Is.EqualTo(StatId.Spa));
      Assert.That(modest.Minus, Is.EqualTo(StatId.Atk));

      // Bold: +Def / -Atk
      var bold = natures.Get("bold");
      Assert.That(bold!.Plus, Is.EqualTo(StatId.Def));
      Assert.That(bold.Minus, Is.EqualTo(StatId.Atk));

      // Neutral natures: Plus and Minus are equal (or both null)
      foreach (var id in new[] { "bashful", "docile", "hardy", "quirky", "serious" }) {
        var neutral = natures.Get(id);
        Assert.That(neutral, Is.Not.Null, $"Nature '{id}' not found");
        Assert.That(neutral!.Plus == neutral.Minus, Is.True, $"Nature '{id}' should be neutral");
      }
    }

    // ── Adaptable (existing integration test) ──────────────────────────────

    [Test]
    public void Adaptable_Usage() {
      var gen = TestHelper.Gen(5);
      var result = Calc.Calculate(
        gen,
        new Pokemon(gen, "Gengar", new State.Pokemon {
          Item = "Choice Specs",
          Nature = "Timid",
          Evs = new Data.StatsTableInput { Spa = 252 },
          Boosts = new Data.StatsTableInput { Spa = 1 },
        }),
        new Pokemon(gen, "Chansey", new State.Pokemon {
          Item = "Eviolite",
          Nature = "Calm",
          Evs = new Data.StatsTableInput { Hp = 252, Spd = 252 },
        }),
        new Move(gen, "Focus Blast")
      );

      Assert.That(result.Range(), Is.EqualTo((274, 324)));
    }
  }
}
