using System;

namespace DamageCalc.Data {
  public sealed class Generation : IGeneration {
    public int Num { get; }
    public IAbilities Abilities { get; }
    public IItems Items { get; }
    public IMoves Moves { get; }
    public ISpecies Species { get; }
    public ITypes Types { get; }
    public INatures Natures { get; }

    public Generation(
      int num,
      IAbilities abilities,
      IItems items,
      IMoves moves,
      ISpecies species,
      ITypes types,
      INatures natures
    ) {
      Num = num;
      Abilities = abilities;
      Items = items;
      Moves = moves;
      Species = species;
      Types = types;
      Natures = natures;
    }
  }

  public sealed class Generations : IGenerations {
    private readonly Func<int, IGeneration> _factory;

    public Generations(Func<int, IGeneration> factory) {
      _factory = factory;
    }

    public IGeneration Get(int gen) {
      return _factory(gen);
    }
  }
}
