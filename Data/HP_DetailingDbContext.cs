using HP_Detailing.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HP_Detailing.Data
{
    public class HP_DetailingDbContext : IdentityDbContext<AppUser>
    {
        public HP_DetailingDbContext(DbContextOptions<HP_DetailingDbContext> options) : base(options)
        {
        }

        public DbSet<Staff> Staff => Set<Staff>();
        public DbSet<Position> Positions => Set<Position>();
        public DbSet<StaffProfile> StaffProfiles => Set<StaffProfile>();
        public DbSet<LaborContract> LaborContracts => Set<LaborContract>();
        public DbSet<Payroll> Payrolls => Set<Payroll>();

        public DbSet<StockImport> StockImports => Set<StockImport>();
        public DbSet<StockImportItem> StockImportItems => Set<StockImportItem>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<ServiceMaterialQuota> ServiceMaterialQuotas => Set<ServiceMaterialQuota>();

        public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
        public DbSet<Service> Services => Set<Service>();
        public DbSet<Ticket> Tickets => Set<Ticket>();
        public DbSet<TicketService> TicketServices => Set<TicketService>();
        public DbSet<Material> Materials => Set<Material>();
        public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();
        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InvoiceService> InvoiceServices => Set<InvoiceService>();
        public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
        public DbSet<MaterialUsage> MaterialUsages => Set<MaterialUsage>();
        public DbSet<Car> Cars => Set<Car>();
        public DbSet<TicketMaterialUsage> TicketMaterialUsages => Set<TicketMaterialUsage>();
        public DbSet<AuditSession> AuditSessions => Set<AuditSession>();
        public DbSet<AuditSessionItem> AuditSessionItems => Set<AuditSessionItem>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Car>()
                .HasKey(c => c.Plate);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Car)
                .WithMany()
                .HasForeignKey(t => t.Plate)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.AssignedStaff)
                .WithMany()
                .HasForeignKey(t => t.AssignedStaffId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TicketService>()
                .HasIndex(x => new { x.TicketId, x.ServiceId })
                .IsUnique(false);

            modelBuilder.Entity<WarehouseStock>()
                .HasIndex(x => x.MaterialId);

            modelBuilder.Entity<MaterialUsage>()
                .HasIndex(x => x.TicketServiceId);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.PaymentMethod)
                .WithMany()
                .HasForeignKey(i => i.PaymentMethodId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Decimal Precision
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetColumnType("decimal(18,2)");
            }
        }
    }
}

