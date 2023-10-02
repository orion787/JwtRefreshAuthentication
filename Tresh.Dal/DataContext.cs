using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
