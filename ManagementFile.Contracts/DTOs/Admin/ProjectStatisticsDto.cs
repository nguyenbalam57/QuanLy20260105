using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.Admin
{
    /// <summary>
    /// Project statistics DTO
    /// </summary>
    public class ProjectStatisticsDto
    {
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int CompletedProjects { get; set; }
        public int OverdueProjects { get; set; }
        public int NewProjects { get; set; }
        public Dictionary<string, int> ProjectsByStatus { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> ProjectsByPriority { get; set; } = new Dictionary<string, int>();
        public decimal AverageCompletionPercentage { get; set; }
        public decimal TotalEstimatedBudget { get; set; }
        public decimal TotalActualBudget { get; set; }
        public decimal BudgetVariance { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }
}
