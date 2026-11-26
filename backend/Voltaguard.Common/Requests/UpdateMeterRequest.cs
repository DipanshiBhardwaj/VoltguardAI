using System.ComponentModel.DataAnnotations;
using Voltaguard.Common.Enums;

namespace Voltaguard.Common.Requests;

/// <summary>Changes the serial number and tariff category of an existing meter.</summary>
public sealed class UpdateMeterRequest
{
    [Required]
    [MaxLength(100)]
    public string SerialNumber { get; init; } = string.Empty;

    [Required]
    public SlabCode SlabCode { get; init; }
}
