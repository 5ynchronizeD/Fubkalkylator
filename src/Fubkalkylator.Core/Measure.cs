namespace Fubkalkylator.Core;

/// <summary>
/// Ett längdmått som internt lagras i tum men enkelt kan läsas i mm eller cm.
/// </summary>
public readonly record struct Measure(double Inches)
{
    public double Millimeters => Inches * SawConstants.MmPerInch;
    public double Centimeters => Inches * SawConstants.CmPerInch;

    public static implicit operator Measure(double inches) => new(inches);

    public override string ToString() => $"{Inches:0.##}\"";
}
