
using System;
using Core.Random;
using Game.Config.Pickup;

namespace Game.Config;

public interface IPickup {
  float Duration { get; }
  string TexturePath { get; }
  Godot.Shape2D Shape { get; }
}

public interface IPickup<T> : IPickup {
  void ApplyTo(T target);
}

public interface IDropable {
  ReadOnlySpan<(IPickup, uint)> DropItems { get; }

  static Game.Pickup.Scene.Pickup? GetDrop(ReadOnlySpan<(IPickup, uint)> what) {
    var len = what.Length;
    if (len is 0) return null;
    if (len is 1) {
      var (itme, weight) = what[0];
      if (itme is null or Empty || weight is 0) return null;
      else return new(itme);
    }

    ulong total = 0;
    foreach (var (_, count) in what)
      total += count;

    if (total is 0) return null;

    var rand = Core.Rng.Shared.NextUInt64(total);

    ulong c = 0;
    foreach (var (item, count) in what) {
      c += count;
      if (rand < c)
        return item is Empty ? null : new(item);
    }

    return null;
  }
}
