using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion; 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wms_android.shared.Models;

namespace wms_android.shared.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Parcel> Parcels { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Drivers> Drivers { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<Dispatch> Dispatches { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<UserBranch> UserBranches { get; set; }
        public DbSet<ContractCustomer> ContractCustomers { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<BranchDeposit> BranchDeposits { get; set; }
        public DbSet<ParcelDeposit> ParcelDeposits { get; set; }
        public DbSet<CODCollection> CODCollections { get; set; }
        public DbSet<DailyExpense> DailyExpenses { get; set; }
        public DbSet<ChequeDeposit> ChequeDeposits { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure DateTime properties
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties()
                    .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
                {
                    if (property.Name == nameof(Parcel.CreatedAt) || property.Name == nameof(Parcel.DispatchedAt))
                    {
                        property.SetColumnType("timestamp with time zone");
                    }
                    else
                    {
                        property.SetColumnType("timestamp without time zone");
                        
                        var converter = new ValueConverter<DateTime, DateTime>(
                            v => v.Kind == DateTimeKind.Utc ? v.ToLocalTime() : DateTime.SpecifyKind(v, DateTimeKind.Local),
                            v => DateTime.SpecifyKind(v, DateTimeKind.Local)
                        );

                        property.SetValueConverter(converter);
                    }
                }
            }

            // Configure Parcel entity
            modelBuilder.Entity<Parcel>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.CreatedAt)
                    .HasColumnType("timestamp with time zone");
                entity.Property(e => e.DispatchedAt)
                    .HasColumnType("timestamp with time zone");

                // Configure relationship with User
                entity.HasOne(p => p.CreatedBy)
                      .WithMany()
                      .HasForeignKey(p => p.CreatedById)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);
            });

            // Configure Shipment entity
            modelBuilder.Entity<Shipment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.HasIndex(e => e.WaybillNumber)
                      .IsUnique();

                entity.Property(e => e.CreatedAt)
                      .HasColumnType("timestamp with time zone");

                entity.HasOne(s => s.Creator)
                      .WithMany() 
                      .HasForeignKey(s => s.CreatedById)
                      .OnDelete(DeleteBehavior.SetNull) 
                      .IsRequired(false);

                entity.HasMany(s => s.Parcels)
                      .WithOne(p => p.Shipment)
                      .HasForeignKey(p => p.ShipmentId)
                      .OnDelete(DeleteBehavior.Cascade); 
            });

            // Configure Vehicle entity
            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.VehicleRegistrationNumber).IsRequired();
                entity.Property(e => e.BodyType).IsRequired();
            });

            // Configure Drivers entity
            modelBuilder.Entity<Drivers>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).IsRequired();
                entity.Property(e => e.LastName).IsRequired();
                entity.Property(e => e.Initials).IsRequired();
                entity.Property(e => e.LicenseNumber).IsRequired();
            });

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired();
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasColumnType("timestamp without time zone");

                entity.HasOne(u => u.Role)
                    .WithMany()
                    .HasForeignKey(u => u.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Configure Branch relationship
                entity.HasOne(u => u.Branch)
                    .WithMany()
                    .HasForeignKey(u => u.BranchId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
            });

            // Configure Dispatch entity
            modelBuilder.Entity<Dispatch>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.SourceBranch).IsRequired();
                entity.Property(e => e.VehicleNumber).IsRequired();
                entity.Property(e => e.Driver).IsRequired();
                entity.Property(e => e.DispatchTime)
                    .HasColumnType("timestamp with time zone");
                entity.Property(e => e.Status).IsRequired();

                // Configure relationship with Parcels
                entity.HasMany(d => d.Parcels)
                    .WithMany()
                    .UsingEntity(j => j.ToTable("DispatchParcels"));
                
                // Ignore the Destination property since it doesn't exist in the database
                entity.Ignore(e => e.Destination);
            });

            // Seed Vehicle data
            modelBuilder.Entity<Vehicle>().HasData(
                new Vehicle { Id = Guid.NewGuid(), VehicleRegistrationNumber = "KDL 085M", BodyType = "Truck" },
                new Vehicle { Id = Guid.NewGuid(), VehicleRegistrationNumber = "KCY 067A", BodyType = "Truck" },
                new Vehicle { Id = Guid.NewGuid(), VehicleRegistrationNumber = "KCZ 595L", BodyType = "Truck" },
                new Vehicle { Id = Guid.NewGuid(), VehicleRegistrationNumber = "KDB 387Q", BodyType = "Truck" },
                new Vehicle { Id = Guid.NewGuid(), VehicleRegistrationNumber = "KDE 228S", BodyType = "Truck" },
                new Vehicle { Id = Guid.NewGuid(), VehicleRegistrationNumber = "KAY 215H", BodyType = "Van" },
                new Vehicle { Id = Guid.NewGuid(), VehicleRegistrationNumber = "KBF 462A", BodyType = "Van" }
            );

            // Seed Drivers data
            modelBuilder.Entity<Drivers>().HasData(
                new Drivers { Id = Guid.NewGuid(), FirstName = "John", LastName = "Njuguna", Initials = "JN", LicenseNumber = "DL123456" },
                new Drivers { Id = Guid.NewGuid(), FirstName = "Robert", LastName = "Njuguna", Initials = "RN", LicenseNumber = "DL123456" },
                new Drivers { Id = Guid.NewGuid(), FirstName = "John", LastName = "Mwai", Initials = "JM", LicenseNumber = "DL123456" },
                new Drivers { Id = Guid.NewGuid(), FirstName = "David", LastName = "Mwangi", Initials = "DM", LicenseNumber = "DL123456" },
                new Drivers { Id = Guid.NewGuid(), FirstName = "David", LastName = "Kibet", Initials = "DK", LicenseNumber = "DL123456" },
                new Drivers { Id = Guid.NewGuid(), FirstName = "Erastus", LastName = "Kagwa", Initials = "EK", LicenseNumber = "DL123456" },
                new Drivers { Id = Guid.NewGuid(), FirstName = "Julius", LastName = "Kamula", Initials = "JK", LicenseNumber = "DL123456" },
                new Drivers { Id = Guid.NewGuid(), FirstName = "Stephen", LastName = "Kimuyu", Initials = "SK", LicenseNumber = "DL123456" },
                new Drivers { Id = Guid.NewGuid(), FirstName = "Charles", LastName = "Maina", Initials = "CM", LicenseNumber = "DL123456" },
                new Drivers { Id = Guid.NewGuid(), FirstName = "James", LastName = "Gichohi", Initials = "JG", LicenseNumber = "DL123456" }
            );

            // Seed Roles data
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles");
                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.Name).HasColumnName("Name");
                entity.Property(e => e.Description).HasColumnName("Description");
            });

            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin", Description = "Administrator" },
                new Role { Id = 2, Name = "Manager", Description = "Manager" },
                new Role { Id = 3, Name = "Clerk", Description = "Regular User" },
                new Role { Id = 4, Name = "Client", Description = "Client User" }
            );

            // Seed Branches data
            modelBuilder.Entity<Branch>().HasData(
                new Branch { Id = 1, Name = "Mombasa", Address = "Mombasa Address", Phone = "111", Email = "mombasa@email.com" },
                new Branch { Id = 2, Name = "Nairobi", Address = "Nairobi Address", Phone = "222", Email = "nairobi@email.com" },
                new Branch { Id = 3, Name = "Kisumu", Address = "Kisumu Address", Phone = "333", Email = "kisumu@email.com" },
                new Branch { Id = 4, Name = "Eldoret", Address = "Eldoret Address", Phone = "444", Email = "eldoret@email.com" },
                new Branch { Id = 5, Name = "Nakuru", Address = "Nakuru Address", Phone = "555", Email = "nakuru@email.com" },
                new Branch { Id = 6, Name = "Kericho", Address = "Kericho Address", Phone = "666", Email = "kericho@email.com" },
                new Branch { Id = 7, Name = "Kakamega", Address = "Kakamega Address", Phone = "777", Email = "kakamega@email.com" },
                new Branch { Id = 8, Name = "Kapsabet", Address = "Kapsabet Address", Phone = "888", Email = "kapsabet@email.com" },
                new Branch { Id = 9, Name = "Kitale", Address = "Kitale Address", Phone = "999", Email = "kitale@email.com" }
            );

            // Seed Users data
            var createdAtDate = DateTime.SpecifyKind(new DateTime(2023, 1, 1, 0, 0, 0), DateTimeKind.Local);
            byte[] passwordHash, passwordSalt;
            CreatePasswordHash("clerk123", out passwordHash, out passwordSalt);

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    FirstName = "Admin",
                    LastName = "User",
                    Email = "admin@example.com",
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    RoleId = 1,
                    CreatedAt = createdAtDate.ToLocalTime()
                },
                new User
                {
                    Id = 2,
                    Username = "manager",
                    FirstName = "Manager",
                    LastName = "Person",
                    Email = "manager@example.com",
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    RoleId = 2,
                    CreatedAt = createdAtDate.ToLocalTime()
                },
                new User
                {
                    Id = 3,
                    Username = "clerk1",
                    FirstName = "Clerk",
                    LastName = "One",
                    Email = "clerk1@example.com",
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    RoleId = 3,
                    CreatedAt = createdAtDate.ToLocalTime()
                },
                new User
                {
                    Id = 4,
                    Username = "clerk2",
                    FirstName = "Clerk",
                    LastName = "Two",
                    Email = "clerk2@example.com",
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    RoleId = 3,
                    CreatedAt = createdAtDate.ToLocalTime()
                },
                new User
                {
                    Id = 5,
                    Username = "client",
                    FirstName = "Client",
                    LastName = "User",
                    Email = "client@example.com",
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    RoleId = 4,
                    CreatedAt = createdAtDate.ToLocalTime()
                },
                new User
                {
                    Id = 6,
                    Username = "client2",
                    FirstName = "Client",
                    LastName = "UserTwo",
                    Email = "client2@example.com",
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    RoleId = 4,
                    CreatedAt = createdAtDate.ToLocalTime()
                }
            );
            
            modelBuilder.Entity<UserBranch>().HasData(
                new UserBranch { UserId = 1, BranchId = 2 }, // Admin -> Nairobi
                new UserBranch { UserId = 2, BranchId = 1 }, // Manager -> Mombasa
                new UserBranch { UserId = 3, BranchId = 1 }, // Clerk1 -> Mombasa
                new UserBranch { UserId = 4, BranchId = 2 }  // Clerk2 -> Nairobi
            );
            
            modelBuilder.Entity<UserBranch>()
                .HasKey(ub => new { ub.UserId, ub.BranchId });

            // Configure ContractCustomer entity
            modelBuilder.Entity<ContractCustomer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.CompanyName).HasMaxLength(200);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.ContactPerson).HasMaxLength(100);
                entity.Property(e => e.ContractNumber).HasMaxLength(50);
                entity.Property(e => e.PaymentTerms).HasMaxLength(100);
                entity.Property(e => e.TaxRate).HasColumnType("decimal(5,2)");
                entity.Property(e => e.CreatedAt).HasColumnType("timestamp without time zone");
                entity.Property(e => e.UpdatedAt).HasColumnType("timestamp without time zone");

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Invoice entity
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Subtotal).HasColumnType("decimal(12,2)");
                entity.Property(e => e.TaxAmount).HasColumnType("decimal(12,2)");
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(12,2)");
                entity.Property(e => e.PaidAmount).HasColumnType("decimal(12,2)").HasDefaultValue(0);
                entity.Property(e => e.Notes).HasMaxLength(1000);
                entity.Property(e => e.CreatedAt).HasColumnType("timestamp without time zone");
                entity.Property(e => e.UpdatedAt).HasColumnType("timestamp without time zone");
                entity.Property(e => e.BillingPeriodStart).HasColumnType("timestamp without time zone");
                entity.Property(e => e.BillingPeriodEnd).HasColumnType("timestamp without time zone");

                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.Items)
                    .WithOne(i => i.Invoice)
                    .HasForeignKey(i => i.InvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Create unique index for invoice number
                entity.HasIndex(e => e.InvoiceNumber)
                    .IsUnique()
                    .HasDatabaseName("IX_Invoices_InvoiceNumber_Unique");
            });

            // Configure InvoiceItem entity
            modelBuilder.Entity<InvoiceItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ParcelId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.WaybillNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Destination).HasMaxLength(200);
                entity.Property(e => e.ParcelCreatedAt).HasColumnType("timestamp with time zone");
                entity.Property(e => e.TotalPrice).HasColumnType("decimal(10,2)");
                entity.Property(e => e.CreatedAt).HasColumnType("timestamp without time zone");

                entity.HasOne(e => e.Invoice)
                    .WithMany(i => i.Items)
                    .HasForeignKey(e => e.InvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Branch entity to map to its PascalCase column names
            modelBuilder.Entity<Branch>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.Name).HasColumnName("Name");
                entity.Property(e => e.Address).HasColumnName("Address");
                entity.Property(e => e.Phone).HasColumnName("Phone");
                entity.Property(e => e.Email).HasColumnName("Email");
            });

            // Configure UserBranch entity to map to its PascalCase column names
            modelBuilder.Entity<UserBranch>(entity =>
            {
                entity.Property(e => e.UserId).HasColumnName("UserId");
                entity.Property(e => e.BranchId).HasColumnName("BranchId");
            });

            // Configure BranchDeposit entity
            BranchDeposit.ConfigureEntity(modelBuilder);

            // Configure ParcelDeposit entity
            modelBuilder.Entity<ParcelDeposit>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                
                entity.Property(e => e.DepositedAmount)
                    .HasColumnType("decimal(18,2)")
                    .HasDefaultValue(0);
                
                entity.Property(e => e.Expenses)
                    .HasColumnType("decimal(18,2)")
                    .HasDefaultValue(0);
                
                entity.Property(e => e.Notes)
                    .HasMaxLength(1000);
                
                entity.Property(e => e.CreatedAt)
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");
                
                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                // Configure relationships
                entity.HasOne(pd => pd.Parcel)
                    .WithMany()
                    .HasForeignKey(pd => pd.ParcelId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pd => pd.CreatedBy)
                    .WithMany()
                    .HasForeignKey(pd => pd.CreatedById)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);

                entity.HasOne(pd => pd.UpdatedBy)
                    .WithMany()
                    .HasForeignKey(pd => pd.UpdatedById)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);

                // Create unique index to prevent duplicate deposits for the same parcel
                entity.HasIndex(pd => pd.ParcelId)
                    .IsUnique()
                    .HasDatabaseName("IX_ParcelDeposits_ParcelId_Unique");
            });

            // Configure CODCollection entity
            modelBuilder.Entity<CODCollection>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                
                entity.Property(e => e.DispatchCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DriverName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.VehicleNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.BranchName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.TotalCODAmount).HasColumnType("decimal(12,2)");
                entity.Property(e => e.DepositedAmount).HasColumnType("decimal(12,2)").HasDefaultValue(0);
                entity.Property(e => e.Shortfall).HasColumnType("decimal(12,2)").HasDefaultValue(0);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("collected");
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasColumnType("timestamp without time zone");
                entity.Property(e => e.UpdatedAt).HasColumnType("timestamp without time zone");

                // Configure relationships
                entity.HasOne(cc => cc.Dispatch)
                    .WithMany()
                    .HasForeignKey(cc => cc.DispatchId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(cc => cc.Branch)
                    .WithMany()
                    .HasForeignKey(cc => cc.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(cc => cc.CreatedBy)
                    .WithMany()
                    .HasForeignKey(cc => cc.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(cc => cc.UpdatedBy)
                    .WithMany()
                    .HasForeignKey(cc => cc.UpdatedById)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);

                // Create unique index for dispatch code
                entity.HasIndex(cc => cc.DispatchCode)
                    .IsUnique()
                    .HasDatabaseName("IX_CODCollections_DispatchCode_Unique");
            });

            // Configure DailyExpense entity
            modelBuilder.Entity<DailyExpense>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                
                entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Amount).HasColumnType("decimal(12,2)");
                entity.Property(e => e.Vendor).HasMaxLength(100);
                entity.Property(e => e.ReceiptNumber).HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("pending");
                entity.Property(e => e.ApprovalNotes).HasMaxLength(500);
                entity.Property(e => e.RejectionReason).HasMaxLength(500);
                entity.Property(e => e.BranchName).HasMaxLength(100);
                entity.Property(e => e.ClerkName).HasMaxLength(100);
                entity.Property(e => e.CreatedByName).HasMaxLength(100);
                entity.Property(e => e.ApprovedByName).HasMaxLength(100);
                entity.Property(e => e.CreatedAt).HasColumnType("timestamp without time zone");
                entity.Property(e => e.UpdatedAt).HasColumnType("timestamp without time zone");
                entity.Property(e => e.ApprovedAt).HasColumnType("timestamp without time zone");

                // Configure relationships
                entity.HasOne(de => de.Branch)
                    .WithMany()
                    .HasForeignKey(de => de.BranchId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);

                entity.HasOne(de => de.Clerk)
                    .WithMany()
                    .HasForeignKey(de => de.ClerkId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);

                entity.HasOne(de => de.CreatedBy)
                    .WithMany()
                    .HasForeignKey(de => de.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(de => de.ApprovedBy)
                    .WithMany()
                    .HasForeignKey(de => de.ApprovedById)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
            });

            // Configure ChequeDeposit entity
            modelBuilder.Entity<ChequeDeposit>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                
                entity.Property(e => e.ChequeNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DrawerName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.BankName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Amount).HasColumnType("decimal(12,2)");
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("deposited");
                entity.Property(e => e.RelatedInvoiceNumber).HasMaxLength(50);
                entity.Property(e => e.CustomerName).HasMaxLength(100);
                entity.Property(e => e.BranchName).HasMaxLength(100);
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.BounceReason).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasColumnType("timestamp without time zone");
                entity.Property(e => e.UpdatedAt).HasColumnType("timestamp without time zone");
                entity.Property(e => e.ClearanceDate).HasColumnType("timestamp without time zone");

                // Configure relationships
                entity.HasOne(cd => cd.RelatedInvoice)
                    .WithMany()
                    .HasForeignKey(cd => cd.RelatedInvoiceId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);

                entity.HasOne(cd => cd.ContractCustomer)
                    .WithMany()
                    .HasForeignKey(cd => cd.ContractCustomerId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);

                entity.HasOne(cd => cd.Branch)
                    .WithMany()
                    .HasForeignKey(cd => cd.BranchId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);

                entity.HasOne(cd => cd.CreatedBy)
                    .WithMany()
                    .HasForeignKey(cd => cd.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(cd => cd.UpdatedBy)
                    .WithMany()
                    .HasForeignKey(cd => cd.UpdatedById)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);

                // Create unique index for cheque number
                entity.HasIndex(cd => cd.ChequeNumber)
                    .IsUnique()
                    .HasDatabaseName("IX_ChequeDeposits_ChequeNumber_Unique");
            });
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }
    }
}