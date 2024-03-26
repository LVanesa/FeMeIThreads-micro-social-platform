using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;
using Threads.Models;

namespace Threads.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Post> Posts { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<GroupUser> GroupUsers { get; set; }
        public DbSet<FollowRequest> FollowRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // definirea relatiei many-to-many dintre User si Group

            base.OnModelCreating(modelBuilder);

            // ApplicationUser configuration
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.Comments)
                .WithOne(c => c.User)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.Posts)
                .WithOne(p => p.User)
                .OnDelete(DeleteBehavior.Cascade);

            // GroupUser configuration
            modelBuilder.Entity<GroupUser>()
                .HasKey(ab => new { ab.Id, ab.UserId, ab.GroupId });

            modelBuilder.Entity<GroupUser>()
                .HasOne(ab => ab.Group)
                .WithMany(ab => ab.GroupUsers)
                .HasForeignKey(ab => ab.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GroupUser>()
                .HasOne(ab => ab.User)
                .WithMany(ab => ab.GroupUsers)
                .HasForeignKey(ab => ab.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure the many-to-many relationship between users and follow requests
            modelBuilder.Entity<FollowRequest>()
                .HasKey(fr => new {fr.Id, fr.SenderId, fr.ReceiverId });

            modelBuilder.Entity<FollowRequest>()
              .HasOne(fr => fr.Sender)
              .WithMany()
              .HasForeignKey(fr => fr.SenderId)
              .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FollowRequest>()
                .HasOne(fr => fr.Receiver)
                .WithMany()
                .HasForeignKey(fr => fr.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict); 
        }
    }
}