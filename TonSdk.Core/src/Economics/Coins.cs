using System;
using System.Globalization;
using System.Numerics;

namespace TonSdk.Core.Economics;

/// <summary>
///     Represents an amount of TON cryptocurrency.
///     Immutable value type that stores value in nanotons (10^-9 TON).
/// </summary>
public readonly struct Coins : IEquatable<Coins>, IComparable<Coins>
{
    const int DefaultDecimals = 9;
    static readonly decimal DecimalMultiplier = 1_000_000_000m; // 10^9

    /// <summary>
    ///     Value in nanotons (smallest unit)
    /// </summary>
    public readonly BigInteger NanoValue;

    Coins(BigInteger nanoValue)
    {
        NanoValue = nanoValue;
    }

    #region Factory Methods

    /// <summary>
    ///     Creates Coins from nanotons (smallest unit).
    /// </summary>
    public static Coins FromNano(BigInteger nanoValue)
    {
        return new Coins(nanoValue);
    }

    /// <summary>
    ///     Creates Coins from nanotons (smallest unit).
    /// </summary>
    public static Coins FromNano(long nanoValue)
    {
        return new Coins(new BigInteger(nanoValue));
    }

    /// <summary>
    ///     Creates Coins from nanotons string.
    /// </summary>
    public static Coins FromNano(string nanoValue)
    {
        if (!BigInteger.TryParse(nanoValue, out BigInteger value))
            throw new ArgumentException($"Invalid nano value: {nanoValue}", nameof(nanoValue));

        return new Coins(value);
    }

    /// <summary>
    ///     Creates Coins from TON amount (with decimals).
    /// </summary>
    public static Coins FromCoins(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        BigInteger nanoValue = new BigInteger(amount * DecimalMultiplier);
        return new Coins(nanoValue);
    }

    /// <summary>
    ///     Creates Coins from string representation (supports both "1.5" TON and "1500000000" nano).
    /// </summary>
    public static Coins Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or empty", nameof(value));

        value = value.Replace(",", ".").Trim();

        // Try parsing as decimal (TON format)
        if (value.Contains("."))
        {
            if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal decimalValue))
                throw new ArgumentException($"Invalid coins value: {value}", nameof(value));

            return FromCoins(decimalValue);
        }

        // Parse as nanotons
        if (!BigInteger.TryParse(value, out BigInteger nanoValue))
            throw new ArgumentException($"Invalid coins value: {value}", nameof(value));

        return new Coins(nanoValue);
    }

    /// <summary>
    ///     Zero coins.
    /// </summary>
    public static Coins Zero => new(BigInteger.Zero);

    #endregion

    #region Arithmetic Operations

    /// <summary>
    ///     Adds two Coins values.
    /// </summary>
    public Coins Add(Coins other)
    {
        return new Coins(NanoValue + other.NanoValue);
    }

    /// <summary>
    ///     Subtracts two Coins values.
    /// </summary>
    public Coins Sub(Coins other)
    {
        return new Coins(NanoValue - other.NanoValue);
    }

    /// <summary>
    ///     Multiplies Coins by a decimal value.
    /// </summary>
    public Coins Mul(decimal multiplier)
    {
        BigInteger result = new BigInteger((decimal)NanoValue * multiplier);
        return new Coins(result);
    }

    /// <summary>
    ///     Divides Coins by a decimal value.
    /// </summary>
    public Coins Div(decimal divisor)
    {
        if (divisor == 0)
            throw new DivideByZeroException("Cannot divide by zero");

        BigInteger result = new BigInteger((decimal)NanoValue / divisor);
        return new Coins(result);
    }

    #endregion

    #region Conversions

    /// <summary>
    ///     Returns value in nanotons as string.
    /// </summary>
    public string ToNano()
    {
        return NanoValue.ToString();
    }

    /// <summary>
    ///     Returns value in TON (with decimal point).
    /// </summary>
    public decimal ToDecimal()
    {
        return (decimal)NanoValue / DecimalMultiplier;
    }

    /// <summary>
    ///     Returns value as BigInteger (nanotons).
    /// </summary>
    public BigInteger ToBigInt()
    {
        return NanoValue;
    }

    /// <summary>
    ///     Returns formatted TON value with appropriate decimal places (removes trailing zeros).
    /// </summary>
    public override string ToString()
    {
        decimal value = ToDecimal();
        string formatted = value.ToString($"F{DefaultDecimals}", CultureInfo.InvariantCulture);

        // Remove trailing zeros after decimal point
        if (formatted.Contains("."))
        {
            formatted = formatted.TrimEnd('0').TrimEnd('.');
        }

        return formatted;
    }

    #endregion

    #region Comparison & Equality

    public bool IsNegative() => NanoValue < 0;
    public bool IsPositive() => NanoValue > 0;
    public bool IsZero() => NanoValue == 0;

    public bool Equals(Coins other)
    {
        return NanoValue == other.NanoValue;
    }

    public override bool Equals(object? obj)
    {
        return obj is Coins other && Equals(other);
    }

    public override int GetHashCode()
    {
        return NanoValue.GetHashCode();
    }

    public int CompareTo(Coins other)
    {
        return NanoValue.CompareTo(other.NanoValue);
    }

    public static bool operator ==(Coins left, Coins right) => left.Equals(right);
    public static bool operator !=(Coins left, Coins right) => !left.Equals(right);
    public static bool operator <(Coins left, Coins right) => left.NanoValue < right.NanoValue;
    public static bool operator >(Coins left, Coins right) => left.NanoValue > right.NanoValue;
    public static bool operator <=(Coins left, Coins right) => left.NanoValue <= right.NanoValue;
    public static bool operator >=(Coins left, Coins right) => left.NanoValue >= right.NanoValue;

    public static Coins operator +(Coins left, Coins right) => left.Add(right);
    public static Coins operator -(Coins left, Coins right) => left.Sub(right);
    public static Coins operator *(Coins left, decimal right) => left.Mul(right);
    public static Coins operator /(Coins left, decimal right) => left.Div(right);

    #endregion
}
