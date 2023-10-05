using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Tresh.Domain.Models;
using Tresh.Domain.Tokens;

namespace Tresh.Dal
{
    public class DataContext: IdentityDbContext
    {
        public DbSet<ItemData> Items { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {

        }
    }
}
