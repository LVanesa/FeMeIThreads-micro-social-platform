using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Threads.Data;

namespace Threads.Models
{   
    //PASUL 4 - useri si roluri
    public class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(serviceProvider.GetRequiredService
                <DbContextOptions<ApplicationDbContext>>()))
            { 
                if (context.Roles.Any())
                {
                    return;    
                }

                // CREAREA ROLURILOR 
                context.Roles.AddRange(
                    new IdentityRole
                    {
                        Id = "2c5e174e-3b0e-446f-86af483d56fd7210",
                        Name = "Admin",
                        NormalizedName = "Admin".ToUpper()
                    },
                   
                    new IdentityRole
                    {
                        Id = "2c5e174e-3b0e-446f-86af483d56fd7212",
                        Name = "User",
                        NormalizedName = "User".ToUpper()
                    }
                );

                var hasher = new PasswordHasher<ApplicationUser>();

                // CREAREA USERILOR  
                context.Users.AddRange(
                    new ApplicationUser
                    {
                        Id = "8e445865-a24d-4543-a6c6-9443d048cdb0",
                        // primary key 
                        UserName = "admin@test.com",
                        EmailConfirmed = true,
                        NormalizedEmail = "ADMIN@TEST.COM",
                        Email = "admin@test.com",
                        NormalizedUserName = "ADMIN@TEST.COM",
                        PasswordHash = hasher.HashPassword(null, "Admin1!")
                    },
                    new ApplicationUser
                    {
                        Id = "8e445865-a24d-4543-a6c6-9443d048cdb2",
                        // primary key 
                        UserName = "user@test.com",
                        EmailConfirmed = true,
                        NormalizedEmail = "USER@TEST.COM",
                        Email = "user@test.com",
                        NormalizedUserName = "USER@TEST.COM",
                        PasswordHash = hasher.HashPassword(null, "User1!")
                    }
                );

                // ASOCIEREA USER-ROLE 
                context.UserRoles.AddRange(
                    new IdentityUserRole<string>
                    {
                        RoleId = "2c5e174e-3b0e-446f-86af483d56fd7210",
                        UserId = "8e445865-a24d-4543-a6c6-9443d048cdb0"
                    },
                    new IdentityUserRole<string>
                    {
                        RoleId = "2c5e174e-3b0e-446f-86af483d56fd7212",
                        UserId = "8e445865-a24d-4543-a6c6-9443d048cdb2"
                    }
                );

                context.SaveChanges();
            }
        }
    }
}
