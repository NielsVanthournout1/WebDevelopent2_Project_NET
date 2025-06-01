using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace app.SID_in_beurzen;

public partial class SidInBeurzenContext : DbContext
{
    public SidInBeurzenContext()
    {
    }

    public SidInBeurzenContext(DbContextOptions<SidInBeurzenContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Candidate> Candidates { get; set; }

    public virtual DbSet<MigrationRecord> MigrationRecords { get; set; }

    public virtual DbSet<Program> Programs { get; set; }

    public virtual DbSet<Registration> Registrations { get; set; }

    public virtual DbSet<RoleType> RoleTypes { get; set; }

    public virtual DbSet<TradeShow> TradeShows { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseMySQL("server=localhost;port=3307;uid=root;pwd=root;database=SID_in_beurzen");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("PRIMARY");

            entity.ToTable("Accounts", "sid_in_beurzen");

            entity.HasIndex(e => e.RoleTypeId, "IDX_Accounts_RoleType");

            entity.Property(e => e.AccountId).HasColumnType("int(11)");
            entity.Property(e => e.EmailAddress)
                .HasMaxLength(100)
                .HasDefaultValueSql("'NULL'");
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .HasDefaultValueSql("'NULL'");
            entity.Property(e => e.IsActive).HasDefaultValueSql("'NULL'");
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .HasDefaultValueSql("'NULL'");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasDefaultValueSql("'NULL'");
            entity.Property(e => e.RegistrationToken).HasDefaultValueSql("'NULL'");
            entity.Property(e => e.RoleTypeId)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("int(11)");

            entity.HasOne(d => d.RoleType).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.RoleTypeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Accounts_RoleType");
        });

        modelBuilder.Entity<Candidate>(entity =>
        {
            entity.HasKey(e => e.CandidateId).HasName("PRIMARY");

            entity.ToTable("Candidate", "sid_in_beurzen");

            entity.Property(e => e.CandidateId).HasColumnType("int(11)");
            entity.Property(e => e.BirthDate)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("date");
            entity.Property(e => e.FieldOfStudy)
                .HasMaxLength(100)
                .HasDefaultValueSql("'NULL'");
            entity.Property(e => e.GivenName)
                .HasMaxLength(50)
                .HasDefaultValueSql("'NULL'");
            entity.Property(e => e.Institution)
                .HasMaxLength(100)
                .HasDefaultValueSql("'NULL'");
            entity.Property(e => e.QrcodeLink)
                .HasMaxLength(255)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("QRCodeLink");
            entity.Property(e => e.Surname)
                .HasMaxLength(50)
                .HasDefaultValueSql("'NULL'");
        });

        modelBuilder.Entity<MigrationRecord>(entity =>
        {
            entity.HasKey(e => e.MigrationKey).HasName("PRIMARY");

            entity.ToTable("MigrationRecords", "sid_in_beurzen");

            entity.Property(e => e.MigrationKey).HasMaxLength(150);
            entity.Property(e => e.VersionInfo).HasMaxLength(32);
        });

        modelBuilder.Entity<Program>(entity =>
        {
            entity.HasKey(e => e.ProgramId).HasName("PRIMARY");

            entity.ToTable("Program", "sid_in_beurzen");

            entity.Property(e => e.ProgramId).HasColumnType("int(11)");
            entity.Property(e => e.ProgramName)
                .HasMaxLength(100)
                .HasDefaultValueSql("'NULL'");
        });

        modelBuilder.Entity<Registration>(entity =>
        {
            entity.HasKey(e => e.RegistrationId).HasName("PRIMARY");

            entity.ToTable("Registration", "sid_in_beurzen");

            entity.HasIndex(e => e.CandidateId, "IDX_Registration_Candidate");

            entity.HasIndex(e => e.TradeShowId, "IDX_Registration_TradeShow");

            entity.Property(e => e.RegistrationId).HasColumnType("int(11)");
            entity.Property(e => e.CandidateId)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("int(11)");
            entity.Property(e => e.Notes)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("text");
            entity.Property(e => e.RegisteredAt)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("datetime");
            entity.Property(e => e.TradeShowId)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("int(11)");

            entity.HasOne(d => d.Candidate).WithMany(p => p.Registrations)
                .HasForeignKey(d => d.CandidateId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Registration_Candidate");

            entity.HasOne(d => d.TradeShow).WithMany(p => p.Registrations)
                .HasForeignKey(d => d.TradeShowId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Registration_TradeShow");

            entity.HasMany(d => d.Programs).WithMany(p => p.Registrations)
                .UsingEntity<Dictionary<string, object>>(
                    "RegistrationProgram",
                    r => r.HasOne<Program>().WithMany()
                        .HasForeignKey("ProgramId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .HasConstraintName("FK_RegistrationProgram_Program"),
                    l => l.HasOne<Registration>().WithMany()
                        .HasForeignKey("RegistrationId")
                        .HasConstraintName("FK_RegistrationProgram_Registration"),
                    j =>
                    {
                        j.HasKey("RegistrationId", "ProgramId").HasName("PRIMARY");
                        j.ToTable("RegistrationProgram", "sid_in_beurzen");
                        j.HasIndex(new[] { "ProgramId" }, "IDX_RegistrationProgram_Program");
                        j.IndexerProperty<int>("RegistrationId").HasColumnType("int(11)");
                        j.IndexerProperty<int>("ProgramId").HasColumnType("int(11)");
                    });
        });

        modelBuilder.Entity<RoleType>(entity =>
        {
            entity.HasKey(e => e.RoleTypeId).HasName("PRIMARY");

            entity.ToTable("RoleType", "sid_in_beurzen");

            entity.Property(e => e.RoleTypeId).HasColumnType("int(11)");
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<TradeShow>(entity =>
        {
            entity.HasKey(e => e.TradeShowId).HasName("PRIMARY");

            entity.ToTable("TradeShow", "sid_in_beurzen");

            entity.Property(e => e.TradeShowId).HasColumnType("int(11)");
            entity.Property(e => e.EventDate)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("date");
            entity.Property(e => e.Title)
                .HasMaxLength(100)
                .HasDefaultValueSql("'NULL'");
            entity.Property(e => e.Venue)
                .HasMaxLength(100)
                .HasDefaultValueSql("'NULL'");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
