using LaserCuttingSystem.Business.IServices.IAdminServices;
using LaserCuttingSystem.Domain.DTOs.Admin.AdminDashboard;
using LaserCuttingSystem.Domain.DTOs.Admin.AdminRevenueStats;
using LaserCuttingSystem.Domain.DTOs.Admin.AdminSubscription;
using LaserCuttingSystem.Domain.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaserCuttingSystem.WebApi.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/dashboard")]
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IAdminDashboardService _dashboardService;

        public AdminDashboardController(IAdminDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

       
        [HttpGet("summary")]
        public async Task<ActionResult<ApiResponse<AdminDashboardSummaryDto>>> GetDashboardSummary()
        {
            try
            {
                var summary = await _dashboardService.GetDashboardSummaryAsync();
                return Ok(ApiResponse<AdminDashboardSummaryDto>.CreateSuccess(summary));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<AdminDashboardSummaryDto>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        
        [HttpGet("subscription-stats")]
        public async Task<ActionResult<ApiResponse<SubscriptionStatsDto>>> GetSubscriptionStats()
        {
            try
            {
                var stats = await _dashboardService.GetSubscriptionStatsAsync();
                return Ok(ApiResponse<SubscriptionStatsDto>.CreateSuccess(stats));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<SubscriptionStatsDto>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        
        [HttpGet("revenue-stats")]
        public async Task<ActionResult<ApiResponse<RevenueStatsDto>>> GetRevenueStats(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var stats = await _dashboardService.GetRevenueStatsAsync(fromDate, toDate);
                return Ok(ApiResponse<RevenueStatsDto>.CreateSuccess(stats));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<RevenueStatsDto>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        
        [HttpPost("recent-activities")]
        public async Task<ActionResult<ApiResponse<PagedResult<RecentActivityDto>>>> GetRecentActivities(
            [FromBody] SearchRequest request)
        {
            try
            {
                var activities = await _dashboardService.GetRecentActivitiesAsync(request);
                return Ok(ApiResponse<PagedResult<RecentActivityDto>>.CreateSuccess(activities));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<PagedResult<RecentActivityDto>>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }
    }
}
