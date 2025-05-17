using ASE.API.Features.Dealers.Models;
using ASE.API.Features.FinanceSubmissions.Models;
using ASE.API.Features.AnomalyDetection.Models;
using ASE.API.Features.MasterTemplates.Models;
using Microsoft.EntityFrameworkCore;

namespace ASE.API.Common.Data;

public class FinanceDbContext : DbContext
{
    public FinanceDbContext(DbContextOptions<FinanceDbContext> options) : base(options)
    {
    }

    public DbSet<Dealer> Dealers => Set<Dealer>();
    public DbSet<MasterTemplate> MasterTemplates => Set<MasterTemplate>();
    public DbSet<FinanceSubmission> FinanceSubmissions => Set<FinanceSubmission>();
    public DbSet<MasterTemplateSheet> MasterTemplateSheets => Set<MasterTemplateSheet>();
    public DbSet<MasterTemplateTable> MasterTemplateTables => Set<MasterTemplateTable>();
    public DbSet<MasterTemplateCell> MasterTemplateCells => Set<MasterTemplateCell>();
    public DbSet<DataAnomaly> DataAnomalies => Set<DataAnomaly>();
    public DbSet<FinanceSubmissionCell> SubmissionData => Set<FinanceSubmissionCell>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure relationships for Dealer and FinanceSubmission
        modelBuilder.Entity<FinanceSubmission>()
            .HasOne(fs => fs.Dealer)
            .WithMany(d => d.Submissions)
            .HasForeignKey(fs => fs.DealerId);
            
        // Configure relationships for MasterTemplateSheet
        modelBuilder.Entity<MasterTemplateSheet>()
            .HasOne(mts => mts.MasterTemplate)
            .WithMany(mt => mt.Sheets)
            .HasForeignKey(mts => mts.MasterTemplateId);

        // Configure relationships for MasterTemplateTable
        modelBuilder.Entity<MasterTemplateTable>()
            .HasOne(mtt => mtt.MasterTemplateSheet)
            .WithMany(mts => mts.Tables)
            .HasForeignKey(mtt => mtt.MasterTemplateSheetId);

        // Configure relationships for MasterTemplateCell
        modelBuilder.Entity<MasterTemplateCell>()
            .HasOne(mtc => mtc.MasterTemplateTable)
            .WithMany(mtt => mtt.Cells)
            .HasForeignKey(mtc => mtc.MasterTemplateTableId);
            
        // Configure relationships for SubmissionData
        modelBuilder.Entity<FinanceSubmissionCell>()
            .HasOne(sd => sd.FinanceSubmission)
            .WithMany(fs => fs.Cells)
            .HasForeignKey(sd => sd.FinanceSubmissionId);
    }
} 