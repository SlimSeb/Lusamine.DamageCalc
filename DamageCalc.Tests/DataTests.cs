using NUnit.Framework;

namespace DamageCalc.Tests {
  public sealed class DataTests {
    [Test]
    [Ignore("Requires @pkmn/dex reference data, not available in C# test project.")]
    public void Generations_Abilities() { }

    [Test]
    [Ignore("Requires @pkmn/dex reference data, not available in C# test project.")]
    public void Generations_Items() { }

    [Test]
    [Ignore("Requires @pkmn/dex reference data, not available in C# test project.")]
    public void Generations_Moves() { }

    [Test]
    [Ignore("Requires @pkmn/dex reference data, not available in C# test project.")]
    public void Generations_Species() { }

    [Test]
    [Ignore("Requires @pkmn/dex reference data, not available in C# test project.")]
    public void Generations_Types() { }

    [Test]
    [Ignore("Requires @pkmn/dex reference data, not available in C# test project.")]
    public void Generations_Natures() { }

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
