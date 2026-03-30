using InstagramClone.Common.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstagramClone.Infrastructure.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<IdentityRole>
{
    public void Configure(EntityTypeBuilder<IdentityRole> builder)
    {
        builder.HasData(
            new IdentityRole
            {
                Id = "3BA43D62-5360-4A30-A29A-D3F2BB371CC1",
                ConcurrencyStamp = "4ebeedfc-8a96-4459-80aa-94e7c2b1fa22",
                Name = RoleNames.User,
                NormalizedName = RoleNames.User.ToUpper()
            },
            new IdentityRole
            {
                Id = "0114C45B-EB0A-4D57-950C-B435F395087F",
                ConcurrencyStamp = "24e38ef9-0968-4fed-bc6f-8e6b2f41420b",
                Name = RoleNames.Administrator,
                NormalizedName = RoleNames.Administrator.ToUpper()
            }
            );
    }
}
