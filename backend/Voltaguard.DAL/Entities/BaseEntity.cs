namespace Voltaguard.DAL.Entities;

/// <summary>
/// Shared columns every domain entity carries. CreatedOn and IsDeleted are set
/// centrally in the DbContext, never by hand. Soft delete means rows are hidden,
/// not physically removed.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public bool IsDeleted { get; set; }
}
