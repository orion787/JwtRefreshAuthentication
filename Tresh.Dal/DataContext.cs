using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Tresh.Domain.Models;

namespace Tresh.Dal
{
    public class DataContext: IdentityDbContext
    {
        public DbSet<ItemData> Items { get; set; }
        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {

        }
    }
}
