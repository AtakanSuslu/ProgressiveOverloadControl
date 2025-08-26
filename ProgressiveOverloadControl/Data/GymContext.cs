using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection.Emit;
using System.Text;

namespace ProgressiveOverloadControl.Data
{
    public class GymContext : DbContext
    {
        public DbSet<SetLogEntity> SetLogs => Set<SetLogEntity>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(
                    "Data Source=EV-PC\\LOCALHOST;Initial Catalog=ThexchBotDb;Integrated Security=True;MultipleActiveResultSets=True;Connect Timeout=30;TrustServerCertificate=True");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SetLogEntity>(e =>
            {
                e.ToTable("SetLogs");
                e.HasKey(x => x.Id);
                e.Property(x => x.Exercise).HasMaxLength(100).IsRequired();
                e.Property(x => x.WeightKg).HasColumnType("float"); // double <-> float
                e.Property(x => x.RIR).HasColumnType("int").IsRequired(false);
                e.HasIndex(x => new { x.Date, x.Exercise });
            });
        }
    }
}
