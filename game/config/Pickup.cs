
namespace Game.Config;

public interface IPickup {
  float Duration { get; init; }
}

public interface IPickup<T> : IPickup {
  T GetPickup();
}
