using LaserCuttingSystem.Domain.DTOs.Admin.AdminDashboard;
using LaserCuttingSystem.Domain.DTOs.Admin.AdminRevenueStats;
using LaserCuttingSystem.Domain.DTOs.Admin.AdminSubscription;
using LaserCuttingSystem.Domain.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.IServices.IAdminServices
{
    public interface IAdminDashboardService
    {
        Task<AdminDashboardSummaryDto> GetDashboardSummaryAsync();
        Task<SubscriptionStatsDto> GetSubscriptionStatsAsync();
        Task<RevenueStatsDto> GetRevenueStatsAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<PagedResult<RecentActivityDto>> GetRecentActivitiesAsync(SearchRequest request);
    }
}
