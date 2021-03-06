using System;
using System.Numerics;

namespace NanoPlugin
{
  public class NanoAmount
  {
    public static BigInteger max = BigInteger.Parse("340282366920938463463374607431768211455");

    public static NanoAmount MAX_VALUE = new NanoAmount(max);

    private BigInteger rawValue;

    /**
     * Creates a NanoAmount from a given {@code raw} value.
     *
     * @param rawValue the raw value
     */
    public NanoAmount(int rawValue)
    {
      if (rawValue < 0)
        throw new ArgumentException("Raw value cannot be negative.");
    }

    /**
     * Creates a NanoAmount from a given {@code raw} value.
     *
     * @param rawValue the raw value
     */
    public NanoAmount(string rawValue)
    {
      if (NanoUtils.ValidateRaw(rawValue))
      {
        this.rawValue = BigInteger.Parse(rawValue);
      }
    }

    /**
     * Creates a NanoAmount from a given {@code raw} value.
     *
     * @param rawValue the raw value
     */
    public NanoAmount(BigInteger rawValue)
    {
      if (rawValue == null)
        throw new ArgumentException("Raw value cannot be null.");
      if (rawValue < BigInteger.Zero || rawValue > max)
        throw new ArgumentException("Raw value is outside the possible range.");
      this.rawValue = rawValue;
    }

    /**
     * Output the raw value as a string.
     */
    public override string ToString()
    {
      return rawValue.ToString();
    }

    /**
     * Returns the value of this amount in the {@code raw} unit.
     *
     * @return the value, in raw units
     */
    public BigInteger getAsRaw()
    {
      return rawValue;
    }

    /**
     * Returns the value of this amount in the standard base unit ({@link NanoUnit#BASE_UNIT}).
     *
     * @return the value, in the base unit
     */
    public string getAsNano()
    {
      return NanoUtils.RawToNano(ToString());
    }

    public override int GetHashCode()
    {
      return rawValue.GetHashCode();
    }

    public static NanoAmount operator +(NanoAmount a, NanoAmount b) => new NanoAmount(a.rawValue + b.rawValue);
    public static NanoAmount operator -(NanoAmount a, NanoAmount b) => new NanoAmount(a.rawValue - b.rawValue);
    public static bool operator <(NanoAmount a, NanoAmount b) => a.rawValue < b.rawValue;
    public static bool operator >(NanoAmount a, NanoAmount b) => a.rawValue > b.rawValue;
    public static bool operator <=(NanoAmount a, NanoAmount b) => a.rawValue <= b.rawValue;
    public static bool operator >=(NanoAmount a, NanoAmount b) => a.rawValue >= b.rawValue;

    public override bool Equals(Object obj)
    {
      if ((obj == null) || !this.GetType().Equals(obj.GetType()))
      {
        return false;
      }
      else
      {
        NanoAmount p = (NanoAmount)obj;
        return rawValue.Equals (p.rawValue);
      }
    }
  }
}