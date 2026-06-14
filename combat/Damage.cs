
using static Utils.Per50000;

namespace Combat;

public interface ITakeDamage {
  uint MaxHealth { get => 0; init { } }
  uint Health { get => 0; set { } }

  void TakeDamage(uint damage) {
    uint val = this.Health;
    uint x = val - damage;
    this.Health = val > damage ? x : 0;
  }
}

public interface ITakeMetalDamage : ITakeDamage {
  ushort MetalDR { get => 0; set { } }
  ushort MetalVul { get => 0; set { } }

  new void TakeDamage(uint damage) {
    uint temp = CalcDIV(damage, this.MetalDR, this.MetalVul);
    ((ITakeDamage)this).TakeDamage(temp);
  }
}

public interface ITakeNatureDamage : ITakeDamage {
  ushort NatureDR { get => 0; set { } }
  ushort NatureVul { get => 0; set { } }

  new void TakeDamage(uint damage) {
    uint temp = CalcDIV(damage, this.NatureDR, this.NatureVul);
    ((ITakeDamage)this).TakeDamage(temp);
  }
}

public interface ITakeWaterDamage : ITakeDamage {
  ushort WaterDR { get => 0; set { } }
  ushort WaterVul { get => 0; set { } }

  new void TakeDamage(uint damage) {
    uint temp = CalcDIV(damage, this.WaterDR, this.WaterVul);
    ((ITakeDamage)this).TakeDamage(temp);
  }
}

public interface ITakeIceDamage : ITakeDamage {
  ushort IceDR { get => 0; set { } }
  ushort IceVul { get => 0; set { } }

  new void TakeDamage(uint damage) {
    uint temp = CalcDIV(damage, this.IceDR, this.IceVul);
    ((ITakeDamage)this).TakeDamage(temp);
  }
}

public interface ITakeFireDamage : ITakeDamage {
  ushort FireDR { get => 0; set { } }
  ushort FireVul { get => 0; set { } }

  new void TakeDamage(uint damage) {
    uint temp = CalcDIV(damage, this.FireDR, this.FireVul);
    ((ITakeDamage)this).TakeDamage(temp);
  }
}

public interface ITakeEarthDamage : ITakeDamage {
  ushort EarthDR { get => 0; set { } }
  ushort EarthVul { get => 0; set { } }

  new void TakeDamage(uint damage) {
    uint temp = CalcDIV(damage, this.EarthDR, this.EarthVul);
    ((ITakeDamage)this).TakeDamage(temp);
  }
}

public interface ITakeDarkDamage : ITakeDamage {
  ushort DarkDR { get => 0; set { } }
  ushort DarkVul { get => 0; set { } }

  new void TakeDamage(uint damage) {
    uint temp = CalcDIV(damage, this.DarkDR, this.DarkVul);
    ((ITakeDamage)this).TakeDamage(temp);
  }
}

public interface ITakeLightDamage : ITakeDamage {
  ushort LightDR { get => 0; set { } }
  ushort LightVul { get => 0; set { } }

  new void TakeDamage(uint damage) {
    uint temp = CalcDIV(damage, this.LightDR, this.LightVul);
    ((ITakeDamage)this).TakeDamage(temp);
  }
}

public interface ITakeLifeDamage : ITakeDamage {
  ushort LifeDR { get => 0; set { } }
  ushort LifeVul { get => 0; set { } }

  new void TakeDamage(uint damage) {
    uint temp = CalcDIV(damage, this.LifeDR, this.LifeVul);
    ((ITakeDamage)this).TakeDamage(temp);
  }
}

public interface ITakeDeathDamage : ITakeDamage {
  ushort DeathDR { get => 0; set { } }
  ushort DeathVul { get => 0; set { } }

  new void TakeDamage(uint damage) {
    uint temp = CalcDIV(damage, this.DeathDR, this.DeathVul);
    ((ITakeDamage)this).TakeDamage(temp);
  }
}

public interface ITakeWindDamage : ITakeDamage {
  ushort WindDR { get => 0; set { } }
  ushort WindVul { get => 0; set { } }

  new void TakeDamage(uint damage) {
    uint temp = CalcDIV(damage, this.WindDR, this.WindVul);
    ((ITakeDamage)this).TakeDamage(temp);
  }
}

public interface ITakeShadowDamage : ITakeDamage {
  ushort ShadowDR { get => 0; set { } }
  ushort ShadowVul { get => 0; set { } }

  new void TakeDamage(uint damage) {
    uint temp = CalcDIV(damage, this.ShadowDR, this.ShadowVul);
    ((ITakeDamage)this).TakeDamage(temp);
  }
}

public interface ITakeStarDamage : ITakeDamage {
  ushort StarDR { get => 0; set { } }
  ushort StarVul { get => 0; set { } }

  new void TakeDamage(uint damage) {
    uint temp = CalcDIV(damage, this.StarDR, this.StarVul);
    ((ITakeDamage)this).TakeDamage(temp);
  }
}

public interface ITakeThunderDamage : ITakeDamage {
  ushort ThunderDR { get => 0; set { } }
  ushort ThunderVul { get => 0; set { } }

  new void TakeDamage(uint damage) {
    uint temp = CalcDIV(damage, this.ThunderDR, this.ThunderVul);
    ((ITakeDamage)this).TakeDamage(temp);
  }
}

public interface ITakeBloodDamage : ITakeDamage {
  ushort BloodDR { get => 0; set { } }
  ushort BloodVul { get => 0; set { } }

  new void TakeDamage(uint damage) {
    uint temp = CalcDIV(damage, this.BloodDR, this.BloodVul);
    ((ITakeDamage)this).TakeDamage(temp);
  }
}

public interface ITakePoisonDamage : ITakeDamage {
  ushort PoisonDR { get => 0; set { } }
  ushort PoisonVul { get => 0; set { } }

  new void TakeDamage(uint damage) {
    uint temp = CalcDIV(damage, this.PoisonDR, this.PoisonVul);
    ((ITakeDamage)this).TakeDamage(temp);
  }
}

public interface ITakeSonicDamage : ITakeDamage {
  ushort SonicDR { get => 0; set { } }
  ushort SonicVul { get => 0; set { } }

  new void TakeDamage(uint damage) {
    uint temp = CalcDIV(damage, this.SonicDR, this.SonicVul);
    ((ITakeDamage)this).TakeDamage(temp);
  }
}

public interface ITakeSpiritDamage : ITakeDamage {
  ushort SpiritDR { get => 0; set { } }
  ushort SpiritVul { get => 0; set { } }

  new void TakeDamage(uint damage) {
    uint temp = CalcDIV(damage, this.SpiritDR, this.SpiritVul);
    ((ITakeDamage)this).TakeDamage(temp);
  }
}

public interface IBasicTakeDamage : ITakeDamage,
  ITakeMetalDamage,
  ITakeNatureDamage,
  ITakeWaterDamage, ITakeIceDamage,
  ITakeFireDamage,
  ITakeEarthDamage,

  ITakeDarkDamage,
  ITakeLightDamage,

  ITakeLifeDamage,
  ITakeDeathDamage,

  ITakeWindDamage,
  ITakeShadowDamage,
  ITakeStarDamage,
  ITakeThunderDamage,

  ITakeBloodDamage,
  ITakePoisonDamage,
  ITakeSonicDamage,
  ITakeSpiritDamage {
  enum Type : byte {
    Basic, Metal, Nature, Water, Ice, Fire, Earth,
    Light, Dark,
    Life, Death,
    Wind, Shadow, Star, Thunder,
    Blood, Poison, Sonic, Spirit
  }
}

public class P : ITakeDamage,
  ITakeMetalDamage,
  ITakeNatureDamage,
  ITakeWaterDamage, ITakeIceDamage,
  ITakeFireDamage,
  ITakeEarthDamage {
  public uint MaxHealth { get; init; }
  public uint Health { get; private set; }

  public void TakeDamage(uint damage) {
    uint val = this.Health;
    uint x = val - damage;
    this.Health = val > damage ? x : 0;
  }

  private System.Collections.Generic.List<nint> values = [];

}
