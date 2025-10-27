using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SO.Core;

namespace SO.Data;

public class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(u => u.Email).HasColumnName("email").IsRequired().HasMaxLength(100);
        builder.Property(u => u.FirstName).HasColumnName("first_name").IsRequired().HasMaxLength(50);
        builder.Property(u => u.LastName).HasColumnName("last_name").IsRequired().HasMaxLength(50);
        builder.Property(u => u.College).HasColumnName("college").HasMaxLength(100);
        builder.Property(u => u.University).HasColumnName("university").HasMaxLength(100);
        builder.Property(u => u.TechStack).HasColumnName("tech_stack").HasMaxLength(255);
        builder.Property(u => u.Program).HasColumnName("program").HasMaxLength(50);
        builder.Property(u => u.Address).HasColumnName("address").HasMaxLength(255);
        builder.HasIndex(u => u.Email).IsUnique();
    }
}