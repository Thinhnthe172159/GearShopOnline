using System;
using System.Collections.Generic;

namespace GearShop.Models
{
    public class RevenueStatisticsViewModel
    {
        public string Period { get; set; }
        public decimal DailyRevenue { get; set; }
        public int DailyOrderCount { get; set; }
        public List<ProductStatisticsViewModel> BestSellingProducts { get; set; }
        public List<ProductStatisticsViewModel> WorstSellingProducts { get; set; }
        public List<DailyRevenueViewModel> DailyRevenues { get; set; }
        public List<MonthlyRevenueViewModel> MonthlyRevenues { get; set; }
    }

    public class ProductStatisticsViewModel
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class DailyRevenueViewModel
    {
        public DateTime Date { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class MonthlyRevenueViewModel
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}