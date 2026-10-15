using LaserCuttingSystem.Business.IServices.IAdminServices;
using LaserCuttingSystem.Data.IRepositories;
using LaserCuttingSystem.Domain.DTOs.Admin.AdminDashboard;
using LaserCuttingSystem.Domain.DTOs.Admin.AdminRevenueStats;
using LaserCuttingSystem.Domain.DTOs.Admin.AdminSubscription;
using LaserCuttingSystem.Domain.DTOs.Common;
using LaserCuttingSystem.Domain.Entities.Identity;
using LaserCuttingSystem.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.Services.AdminServices
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminDashboardService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<AdminDashboardSummaryDto> GetDashboardSummaryAsync()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var activeUsers = await _userManager.Users.Where(u => u.IsActive).CountAsync();

            var totalErrorCodes = await _unitOfWork.ErrorCodes.CountAsync(ec => ec.IsActive);
            var totalMachines = await _unitOfWork.LaserMachines.CountAsync(m => m.IsActive);

            var activeSubscriptions = await _unitOfWork.UserSubscriptions
                .CountAsync(s => s.IsActive && s.Status == SubscriptionStatus.Active && s.EndDate > DateTime.UtcNow);

            var currentMonth = DateTime.UtcNow.Date.AddDays(1 - DateTime.UtcNow.Day);
            var currentYear = new DateTime(DateTime.UtcNow.Year, 1, 1);

            var monthlyPayments = await _unitOfWork.PaymentHistories
                .FindAsync(p => p.Status == PaymentStatus.Completed && p.TransactionDate >= currentMonth);

            var yearlyPayments = await _unitOfWork.PaymentHistories
                .FindAsync(p => p.Status == PaymentStatus.Completed && p.TransactionDate >= currentYear);

            return new AdminDashboardSummaryDto
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                TotalErrorCodes = totalErrorCodes,
                TotalMachines = totalMachines,
                ActiveSubscriptions = activeSubscriptions,
                MonthlyRevenue = monthlyPayments.Sum(p => p.Amount),
                YearlyRevenue = yearlyPayments.Sum(p => p.Amount),
                LastUpdated = DateTime.UtcNow
            };
        }

        public async Task<SubscriptionStatsDto> GetSubscriptionStatsAsync()
        {
            var subscriptions = await _unitOfWork.UserSubscriptions
                .FindAsync(s => s.IsActive && s.Status == SubscriptionStatus.Active,
                           s => s.Plan, s => s.PaymentHistories);

            var groupedByPlan = subscriptions
                .GroupBy(s => s.Plan)
                .Select(g => new PlanDistributionDto
                {
                    PlanName = g.Key.Name,
                    DisplayName = g.Key.DisplayName,
                    UserCount = g.Count(),
                    Revenue = g.SelectMany(s => s.PaymentHistories
                        .Where(p => p.Status == PaymentStatus.Completed))
                        .Sum(p => p.Amount),
                    Percentage = 0 // Will be updated below
                })
                .ToList();

            var totalSubscriptions = groupedByPlan.Sum(p => p.UserCount);
            var totalRevenue = groupedByPlan.Sum(p => p.Revenue);

            // ✅ YENİDEN ATA: with kullanarak yeni liste
            groupedByPlan = groupedByPlan
                .Select(plan => plan with
                {
                    Percentage = totalSubscriptions > 0
                        ? (double)plan.UserCount / totalSubscriptions * 100
                        : 0
                })
                .ToList();

            return new SubscriptionStatsDto
            {
                PlanDistributions = groupedByPlan,
                TotalSubscriptions = totalSubscriptions,
                AverageMonthlyRevenue = totalRevenue / (totalSubscriptions > 0 ? totalSubscriptions : 1),
                TotalRevenue = totalRevenue
            };
        }

        public async Task<RevenueStatsDto> GetRevenueStatsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var now = DateTime.UtcNow;
            var currentMonth = new DateTime(now.Year, now.Month, 1);
            var lastMonth = currentMonth.AddMonths(-1);
            var currentYear = new DateTime(now.Year, 1, 1);
            var lastYear = new DateTime(now.Year - 1, 1, 1);

            var currentMonthRevenue = await GetRevenueForPeriod(currentMonth, now);
            var lastMonthRevenue = await GetRevenueForPeriod(lastMonth, currentMonth);
            var currentYearRevenue = await GetRevenueForPeriod(currentYear, now);
            var lastYearRevenue = await GetRevenueForPeriod(lastYear, currentYear);

            var monthlyGrowthRate = lastMonthRevenue > 0
                ? (double)(currentMonthRevenue - lastMonthRevenue) / (double)lastMonthRevenue * 100
                : 0;

            var yearlyGrowthRate = lastYearRevenue > 0
                ? (double)(currentYearRevenue - lastYearRevenue) / (double)lastYearRevenue * 100
                : 0;

            // Monthly breakdown for the current year
            var monthlyBreakdown = new List<MonthlyRevenueDto>();
            for (int month = 1; month <= 12; month++)
            {
                var monthStart = new DateTime(now.Year, month, 1);
                var monthEnd = monthStart.AddMonths(1);

                if (monthStart > now) break;

                var monthRevenue = await GetRevenueForPeriod(monthStart, monthEnd);
                var transactionCount = await GetTransactionCountForPeriod(monthStart, monthEnd);

                monthlyBreakdown.Add(new MonthlyRevenueDto
                {
                    Year = now.Year,
                    Month = month,
                    MonthName = monthStart.ToString("MMMM"),
                    Revenue = monthRevenue,
                    TransactionCount = transactionCount
                });
            }

            return new RevenueStatsDto
            {
                CurrentMonthRevenue = currentMonthRevenue,
                LastMonthRevenue = lastMonthRevenue,
                CurrentYearRevenue = currentYearRevenue,
                LastYearRevenue = lastYearRevenue,
                MonthlyGrowthRate = monthlyGrowthRate,
                YearlyGrowthRate = yearlyGrowthRate,
                MonthlyBreakdown = monthlyBreakdown
            };
        }

        public async Task<PagedResult<RecentActivityDto>> GetRecentActivitiesAsync(SearchRequest request)
        {
            var activities = await _unitOfWork.UserActivities
                .GetQueryable()
                .Include(a => a.User)
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(a => new RecentActivityDto
                {
                    ActivityType = a.ActivityType,
                    UserEmail = a.User!.Email,
                    UserName = a.User.FullName,
                    CreatedAt = a.CreatedAt,
                    TimeAgo = GetTimeAgo(a.CreatedAt)
                })
                .ToListAsync();

            var totalCount = await _unitOfWork.UserActivities.CountAsync(a => a.IsActive);

            return new PagedResult<RecentActivityDto>
            {
                Items = activities,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize),
                HasPreviousPage = request.PageNumber > 1,
                HasNextPage = request.PageNumber < Math.Ceiling((double)totalCount / request.PageSize)
            };
        }

        private async Task<decimal> GetRevenueForPeriod(DateTime startDate, DateTime endDate)
        {
            var payments = await _unitOfWork.PaymentHistories
                .FindAsync(p => p.Status == PaymentStatus.Completed
                               && p.TransactionDate >= startDate
                               && p.TransactionDate < endDate);

            return payments.Sum(p => p.Amount);
        }

        private async Task<int> GetTransactionCountForPeriod(DateTime startDate, DateTime endDate)
        {
            return await _unitOfWork.PaymentHistories
                .CountAsync(p => p.Status == PaymentStatus.Completed
                                && p.TransactionDate >= startDate
                                && p.TransactionDate < endDate);
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;

            if (timeSpan.TotalMinutes < 1) return "Az önce";
            if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes} dakika önce";
            if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours} saat önce";
            if (timeSpan.TotalDays < 30) return $"{(int)timeSpan.TotalDays} gün önce";
            if (timeSpan.TotalDays < 365) return $"{(int)(timeSpan.TotalDays / 30)} ay önce";

            return $"{(int)(timeSpan.TotalDays / 365)} yıl önce";
        }
    }
}
