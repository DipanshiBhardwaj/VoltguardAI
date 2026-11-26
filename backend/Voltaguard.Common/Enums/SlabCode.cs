namespace Voltaguard.Common.Enums;

/// <summary>
/// Domestic tariff category a meter is billed under. Stored as a string in the
/// database so the value stays readable and a new category can be added without
/// renumbering. Values are spaced so one can be inserted later if needed.
/// </summary>
public enum SlabCode
{
    DomesticLifeline = 5,
    DomesticStandard = 10
}
