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
    public DbSet<DataPattern> DataPatterns => Set<DataPattern>();
    public DbSet<BusinessImpact> BusinessImpacts => Set<BusinessImpact>();
    public DbSet<TimeRange> TimeRanges => Set<TimeRange>();
    public DbSet<IndustryComparison> IndustryComparisons => Set<IndustryComparison>();
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
            
        // Configure BusinessImpact relationships
        modelBuilder.Entity<BusinessImpact>()
            .HasOne(bi => bi.DataAnomaly)
            .WithOne(da => da.BusinessImpact)
            .HasForeignKey<BusinessImpact>(bi => bi.DataAnomalyId)
            .IsRequired(false);
            
        modelBuilder.Entity<BusinessImpact>()
            .HasOne(bi => bi.DataPattern)
            .WithOne(dp => dp.BusinessImpact)
            .HasForeignKey<BusinessImpact>(bi => bi.DataPatternId)
            .IsRequired(false);
            
        // Configure TimeRange relationships
        modelBuilder.Entity<TimeRange>()
            .HasOne(tr => tr.DataAnomaly)
            .WithOne(da => da.TimeRange)
            .HasForeignKey<TimeRange>(tr => tr.DataAnomalyId)
            .IsRequired(false);
            
        modelBuilder.Entity<TimeRange>()
            .HasOne(tr => tr.DataPattern)
            .WithOne(dp => dp.TimeRange)
            .HasForeignKey<TimeRange>(tr => tr.DataPatternId)
            .IsRequired(false);
            
        // Configure IndustryComparison relationship
        modelBuilder.Entity<IndustryComparison>()
            .HasOne(ic => ic.DataPattern)
            .WithOne(dp => dp.IndustryComparison)
            .HasForeignKey<IndustryComparison>(ic => ic.DataPatternId);
    }
} 