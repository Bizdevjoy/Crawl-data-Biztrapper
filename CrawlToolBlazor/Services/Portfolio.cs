using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrawlToolBlazor
{
    public class Portfolio
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public decimal? Cost { get; set; }
        public string Description { get; set; }
        public int? Timeline { get; set; }
        public string Year { get; set; }
        public string ImageUrl { get; set; }
    }

}
