using System.ComponentModel.DataAnnotations;
using Voltaguard.Common.Enums;

namespace Voltaguard.Common.Requests;

/// <summary>Registers a new meter asset under the signed-in user.</summary>
public sealed class CreateMeterRequest
{
    [Required]
    [MaxLength(100)]
    public string SerialNumber { get; init; } = string.Empty;

    [Required]
    public SlabCode SlabCode { get; init; }
}
