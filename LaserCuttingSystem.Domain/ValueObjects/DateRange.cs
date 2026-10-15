using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.ValueObjects
{
    public record DateRange
    {
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }

        public DateRange(DateTime startDate, DateTime endDate)
        {
            if (startDate >= endDate)
                throw new ArgumentException("Start date must be before end date");

            StartDate = startDate;
            EndDate = endDate;
        }

        public int DaysCount => (EndDate - StartDate).Days;

        public bool IsCurrentlyActive => DateTime.UtcNow >= StartDate && DateTime.UtcNow <= EndDate;

        public bool Contains(DateTime date) => date >= StartDate && date <= EndDate;

        public TimeSpan Duration => EndDate - StartDate;

        public static DateRange CreateMonthly(DateTime startDate) =>
            new(startDate, startDate.AddMonths(1));

        public static DateRange CreateYearly(DateTime startDate) =>
            new(startDate, startDate.AddYears(1));
    }
}
