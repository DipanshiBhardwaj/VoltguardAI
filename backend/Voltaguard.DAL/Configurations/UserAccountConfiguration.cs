using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltaguard.Common.Constants;
using Voltaguard.DAL.Entities;

namespace Voltaguard.DAL.Configurations;

/// <summary>
/// Extra rules on the Identity user table. Identity maps the table itself
/// (AspNetUsers); this only adds the role check and the name length.
/// </summary>
public class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    // Matches the limit on RegisterRequest.FullName.
    private const int MaxFullNameLength = 150;

    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_AspNetUsers_Role",
            $"\"Role\" IN ({SqlList.Of([RoleConstants.Customer, RoleConstants.Admin])})"));

        builder.Property(u => u.FullName).HasMaxLength(MaxFullNameLength).IsRequired();
    }
}
