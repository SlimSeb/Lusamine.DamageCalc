using System;

namespace Lusamine.DamageCalc.Data {
  public sealed class Generation : IGeneration {
    public int Num { get; }
    public IDataTable<IAbility> Abilities { get; }
    public IDataTable<IItem> Items { get; }
    public IDataTable<IMove> Moves { get; }
    public IDataTable<ISpecie> Species { get; }
    public IDataTable<IType> Types { get; }
    public IDataTable<INature> Natures { get; }

    public Generation(
      int num,
      IDataTable<IAbility> abilities,
      IDataTable<IItem> items,
      IDataTable<IMove> moves,
      IDataTable<ISpecie> species,
      IDataTable<IType> types,
      IDataTable<INature> natures
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
