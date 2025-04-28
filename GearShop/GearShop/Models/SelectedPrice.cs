namespace GearShop.Models
{
    public class SelectedPrice
    {
        public int Choice { get; set; }
        public decimal PriceMin { get; set; }
        public decimal PriceMax { get; set; }
        public string? Name { get; set; }

        public SelectedPrice()
        {
            
        }
        public List<SelectedPrice> getAllFilter()
        {
            List<SelectedPrice> selectedPrices = new List<SelectedPrice>();
            selectedPrices.Add(new SelectedPrice { Choice = 0, PriceMax = 0, PriceMin = 0, Name = "tất cả giá" });
            selectedPrices.Add(new SelectedPrice { Choice = 1, PriceMax = 1000000, PriceMin = 0, Name = "Dưới 1 triệu" });
            selectedPrices.Add(new SelectedPrice { Choice = 2, PriceMax = 3000000, PriceMin = 1000000, Name = "Từ 1 triệu đến 3 triệu" });
            selectedPrices.Add(new SelectedPrice { Choice = 3, PriceMax = 5000000, PriceMin = 3000000, Name = "Từ 3 triệu đến 5 triệu" });
            selectedPrices.Add(new SelectedPrice { Choice = 4, PriceMax = 10000000, PriceMin = 5000000, Name = "Từ 5 triệu đến 10 triệu" });
            selectedPrices.Add(new SelectedPrice { Choice = 5, PriceMax = 20000000, PriceMin = 10000000, Name = "Từ 10 triệu đến 20 triệu" });
            selectedPrices.Add(new SelectedPrice { Choice = 6, PriceMax = 30000000, PriceMin = 20000000, Name = "Từ 20 triệu đến 30 triệu" });
            selectedPrices.Add(new SelectedPrice { Choice = 7, PriceMax = 40000000, PriceMin = 30000000 ,Name="Từ 30 triệu đến 40 triệu"});
            selectedPrices.Add(new SelectedPrice { Choice = 8, PriceMax = 0, PriceMin = 40000000,Name="Trên 40 triệu" });
            return selectedPrices;
        }
    }
}
