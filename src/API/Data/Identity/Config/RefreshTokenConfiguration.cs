using Core.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Identity.Config;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(rt => rt.JwtId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(rt => rt.ReplacedByToken)
            .HasMaxLength(256);

        builder.HasIndex(rt => rt.Token);

        builder.HasIndex(rt => rt.AppUserId);

        builder.HasOne(rt => rt.AppUser)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
