

using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;

#nullable disable

namespace FocusEV.OCPP.Database
{
    public partial class OCPPCoreContext : DbContext
    {
        private IConfiguration _configuration;

        public OCPPCoreContext(IConfiguration config) : base()
        {
            _configuration = config;
        }

        public OCPPCoreContext(DbContextOptions<OCPPCoreContext> options)
            : base(options)
        {
        }



        public virtual DbSet<MainBanner> MainBanners { get; set; }
        public virtual DbSet<ChargePoint> ChargePoints { get; set; }
        public virtual DbSet<ChargeTag> ChargeTags { get; set; }
        public virtual DbSet<ConnectorStatus> ConnectorStatuses { get; set; }
        public virtual DbSet<ConnectorStatusView> ConnectorStatusViews { get; set; }
        public virtual DbSet<MessageLog> MessageLogs { get; set; }
        public virtual DbSet<Transaction> Transactions { get; set; }
        public virtual DbSet<Owner> Owners { get; set; }
        public virtual DbSet<ChargeStation> ChargeStations { get; set; }
        public virtual DbSet<Permission> Permissions { get; set; }
        public virtual DbSet<Department> Departments { get; set; }
        public virtual DbSet<Account> Accounts { get; set; }
        public virtual DbSet<Menu> Menus { get; set; }
        public virtual DbSet<RoleCustomer> RoleCustomers { get; set; }
        public virtual DbSet<Customer> Customers { get; set; }
        public virtual DbSet<MenuChild> MenuChilds { get; set; }
        public virtual DbSet<Unitprice> Unitprices { get; set; }
        public virtual DbSet<UserApp> UserApps { get; set; }
        public virtual DbSet<DepositHistory> DepositHistorys { get; set; }
        public virtual DbSet<WalletTransaction> WalletTransactions { get; set; }
        public virtual DbSet<TransactionVirtual> TransactionVirtuals { get; set; }
        public virtual DbSet<StaticQRIPNLog> StaticQRIPNLogs { get; set; }
        public virtual DbSet<VnpIPNLog> VnpIPNLogs { get; set; }
        public virtual DbSet<QRTransaction> QRTransactions { get; set; }
        public virtual DbSet<ResponseLog> ResponseLogs { get; set; }
        public virtual DbSet<TransactionVirtualQR> TransactionVirtualQRs { get; set; }
        public virtual DbSet<TransactionLogs> TransactionLogs { get; set; }
        public virtual DbSet<LogStopTransaction> LogStopTransactions { get; set; }

   

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string sqlConnString = _configuration.GetConnectionString("SqlServer");
                string liteConnString = _configuration.GetConnectionString("SQLite");
                if (!string.IsNullOrWhiteSpace(sqlConnString))
                {
                    //optionsBuilder.UseSqlServer(sqlConnString);
                    optionsBuilder.UseSqlServer(sqlConnString, builder =>
                    {
                        builder.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                    });
                    base.OnConfiguring(optionsBuilder);

                }
                else if (!string.IsNullOrWhiteSpace(liteConnString))
                {
                    optionsBuilder.UseSqlite(liteConnString);
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChargePoint>(entity =>
            {
                entity.ToTable("ChargePoint");

                entity.HasIndex(e => e.ChargePointId, "ChargePoint_Identifier")
                    .IsUnique();

                entity.Property(e => e.ChargePointId).HasMaxLength(100);

                entity.Property(e => e.Comment).HasMaxLength(200);

                entity.Property(e => e.Name).HasMaxLength(100);

                entity.Property(e => e.Username).HasMaxLength(50);

                entity.Property(e => e.Password).HasMaxLength(50);

                entity.Property(e => e.ClientCertThumb).HasMaxLength(100);
            });
            modelBuilder.Entity<MainBanner>(entity =>
            {
                entity.HasKey(e => e.BannerId); // Đặt khóa chính
                entity.ToTable("MainBanner");   // Đặt tên bảng

            
            });

            modelBuilder.Entity<ChargeTag>(entity =>
            {
                entity.HasKey(e => e.TagId)
                    .HasName("PK_ChargeKeys");

                entity.Property(e => e.TagId).HasMaxLength(50);

                entity.Property(e => e.ParentTagId).HasMaxLength(50);

                entity.Property(e => e.TagName).HasMaxLength(200);
            });

            modelBuilder.Entity<ConnectorStatus>(entity =>
            {
                entity.HasKey(e => new { e.ChargePointId, e.ConnectorId });

                entity.ToTable("ConnectorStatus");

                entity.Property(e => e.ChargePointId).HasMaxLength(100);

                entity.Property(e => e.ConnectorName).HasMaxLength(100);

                entity.Property(e => e.LastStatus).HasMaxLength(100);
            });

            modelBuilder.Entity<ConnectorStatusView>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("ConnectorStatusView");

                entity.Property(e => e.ChargePointId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ConnectorName).HasMaxLength(100);

                entity.Property(e => e.LastStatus).HasMaxLength(100);

                entity.Property(e => e.StartResult).HasMaxLength(100);

                entity.Property(e => e.StartTagId).HasMaxLength(50);

                entity.Property(e => e.StopReason).HasMaxLength(100);

                entity.Property(e => e.StopTagId).HasMaxLength(50);
            });

            modelBuilder.Entity<MessageLog>(entity =>
            {
                entity.HasKey(e => e.LogId);

                entity.ToTable("MessageLog");

                entity.HasIndex(e => e.LogTime, "IX_MessageLog_ChargePointId");

                entity.Property(e => e.ChargePointId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ErrorCode).HasMaxLength(100);

                entity.Property(e => e.Message)
                    .IsRequired()
                    .HasMaxLength(100);
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.Property(e => e.Uid).HasMaxLength(50);

                entity.Property(e => e.ChargePointId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.StartResult).HasMaxLength(100);

                entity.Property(e => e.StartTagId).HasMaxLength(50);

                entity.Property(e => e.StopReason).HasMaxLength(100);

                entity.Property(e => e.StopTagId).HasMaxLength(50);

                entity.HasOne(d => d.ChargePoint)
                    .WithMany(p => p.Transactions)
                    .HasForeignKey(d => d.ChargePointId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Transactions_ChargePoint");
            });

            modelBuilder.Entity<Owner>(entity =>
            {
                entity.HasKey(e => e.OwnerId);

                entity.ToTable("Owner");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.Phone).HasMaxLength(50);
                entity.Property(e => e.Email).HasMaxLength(200);

            });

            modelBuilder.Entity<ChargeStation>(entity =>
            {
                entity.HasKey(e => e.ChargeStationId);

                entity.ToTable("ChargeStation");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.Name).HasMaxLength(500);
            });
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasKey(e => e.PermissionId);

                entity.ToTable("Permission");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Name).HasMaxLength(500);
            });
            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(e => e.DepartmentId);

                entity.ToTable("Department");

                entity.Property(e => e.DepartmentName)
                    .IsRequired()
                    .HasMaxLength(200);
            });
            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasKey(e => e.AccountId);

                entity.ToTable("Account");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.UserName).HasMaxLength(500);
                entity.Property(e => e.Password).HasMaxLength(500);
                entity.Property(e => e.Code).HasMaxLength(500);
            });
            modelBuilder.Entity<Menu>(entity =>
            {
                entity.HasKey(e => e.MenuId);

                entity.ToTable("Menu");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(500);

            });
            modelBuilder.Entity<RoleCustomer>(entity =>
            {
                entity.HasKey(e => e.RoleCustomerID);

                entity.ToTable("RoleCustomer");

                entity.Property(e => e.RoleCustomerName)
                    .IsRequired()
                    .HasMaxLength(200);

            });
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.CustomerId);

                entity.ToTable("Customer");

                entity.Property(e => e.FirtName)
                    .IsRequired()
                    .HasMaxLength(200);

            });
            modelBuilder.Entity<MenuChild>(entity =>
            {
                entity.HasKey(e => e.MenuchildId);

                entity.ToTable("MenuChild");

                entity.Property(e => e.MenuChildName)
                    .IsRequired()
                    .HasMaxLength(500);

            });

            modelBuilder.Entity<Unitprice>(entity =>
            {
                entity.HasKey(e => e.UnitpriceId);

                entity.ToTable("Unitprice");

                entity.Property(e => e.Price)
                    .IsRequired();

            });
            modelBuilder.Entity<UserApp>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.ToTable("UserApp");

                entity.Property(e => e.UserName)
                    .IsRequired();
                entity.Property(e => e.Password)
                  .IsRequired();

            });
            modelBuilder.Entity<DepositHistory>(entity =>
            {
                entity.HasKey(e => e.DepositHistoryId);

                entity.ToTable("DepositHistory");

                entity.Property(e => e.UserAppId)
                    .IsRequired();

            });
            modelBuilder.Entity<WalletTransaction>(entity =>
            {
                entity.HasKey(e => e.WalletTransactionId);

                entity.ToTable("WalletTransaction");

                entity.Property(e => e.TransactionId)
                    .IsRequired();
                entity.Property(e => e.UserAppId)
                 .IsRequired();

            });
            modelBuilder.Entity<TransactionVirtual>(entity =>
            {
                entity.HasKey(e => e.TransactionsVirtualID);

                entity.ToTable("TransactionVirtual");

                entity.Property(e => e.TransactionId)
                    .IsRequired();

            });
            modelBuilder.Entity<StaticQRIPNLog>(entity =>
            {
                entity.HasKey(e => e.IpnId);

                entity.ToTable("StaticQRIPNLog");

            });
            modelBuilder.Entity<VnpIPNLog>(entity =>
            {
                entity.HasKey(e => e.IpnId);

                entity.ToTable("VnpIPNLog");

            });
            modelBuilder.Entity<QRTransaction>(entity =>
            {
                entity.HasKey(e => e.QrTransactionId);

                entity.ToTable("QRTransaction");

            });
            modelBuilder.Entity<ResponseLog>(entity =>
            {
                entity.HasKey(e => e.ResponseLogId);

                entity.ToTable("ResponseLog");

            });
            modelBuilder.Entity<TransactionVirtualQR>(entity =>
            {
                entity.HasKey(e => e.TransactionQRId);

                entity.ToTable("TransactionVirtualQR");

            });
            modelBuilder.Entity<TransactionLogs>(entity =>
            {
                entity.HasKey(e => e.TransactionLogId);

                entity.ToTable("TransactionLogs");

            });
            modelBuilder.Entity<LogStopTransaction>(entity =>
            {
                entity.HasKey(e => e.LogstopId);

                entity.ToTable("LogStopTransaction");

            });
            OnModelCreatingPartial(modelBuilder);
           

        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
