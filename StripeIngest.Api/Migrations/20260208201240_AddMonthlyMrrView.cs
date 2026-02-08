using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StripeIngest.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyMrrView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER VIEW MonthlyMrrReport AS
                SELECT 
                    FORMAT(Timestamp, 'yyyy-MM') as Month,
                    SUM(CASE WHEN ChangeType = 'new' THEN MRRDelta ELSE 0 END) as NewMRR,
                    SUM(CASE WHEN ChangeType = 'upgrade' THEN MRRDelta ELSE 0 END) as ExpansionMRR,
                    SUM(CASE WHEN ChangeType = 'downgrade' THEN MRRDelta ELSE 0 END) as ContractionMRR,
                    SUM(CASE WHEN ChangeType = 'churn' THEN MRRDelta ELSE 0 END) as ChurnedMRR,
                    SUM(MRRDelta) as NetMRRChange
                FROM SubscriptionHistory
                GROUP BY FORMAT(Timestamp, 'yyyy-MM')
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW MonthlyMrrReport");
        }
    }
}
