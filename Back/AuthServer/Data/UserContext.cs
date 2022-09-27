using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Data
{
    public class UserContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {

        public UserContext(DbContextOptions<UserContext> options) : base(options)
        {
            
        }
        public virtual DbSet<UserKey> UserKeys { get; set; } = null!;

        public virtual DbSet<Device> UserDevices { get; set; } = null!;

        public virtual DbSet<Token> Tokens { get; set; } = null!;

        public virtual DbSet<AuthenticationSession> AuthenticationSessions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<KeyChallenge>();
        }
    }
}