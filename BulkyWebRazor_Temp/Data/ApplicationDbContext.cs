﻿using BulkyWebRazor_Temp.Models;
using Microsoft.EntityFrameworkCore;

namespace BulkyWebRazor_Temp.Data
{
    public class ApplicationDbContext :DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) :base(options)
        {
            
        }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>().HasData(
                new Category { Category_id = 1, Name = "Action", DisplayOrder = 1 },
                new Category { Category_id = 2, Name = "Fantasy", DisplayOrder = 2 },
                new Category { Category_id = 3, Name = "History", DisplayOrder = 3 }
                );
                
        }

    }
}
