using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrawlToolBlazor
{
    public class Agency
    {
        public string Name { get; set; }
        public List<string> Services { get; set; } = new List<string>();
        public List<string> Industries { get; set; } = new List<string>();
        public List<string> ClientTypes { get; set; } = new List<string>();
        public List<string> Clients { get; set; } = new List<string>();
        public List<Portfolio> Portfolios { get; set; } = new List<Portfolio>();
        public List<string> Addresses { get; set; } = new List<string>();
        public List<(string Url, int Type)> ThumbnailImages { get; set; } = new List<(string, int)>();
        public string LogoUrl { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public int PositionId { get; set; }
        public string AgencyName { get; set; }
        public string AgencyEmail { get; set; }
        public string PhoneNumber { get; set; }
        public string OfficialWebsite { get; set; }
        public string BusinessRegistrationNumber { get; set; }
        public int? YearFounded { get; set; }
        public decimal? AverageHourlyRate { get; set; }
        public int? TeamSizeId { get; set; }
        public int? BudgetId { get; set; }
        public string Facebook { get; set; }
        public string LinkedIn { get; set; }
        public string Twitter { get; set; }
        public string Instagram { get; set; }
        public string YouTube { get; set; }
        public string WhatsApp { get; set; }
        public string Description { get; set; }
        public int Status { get; set; }
        public bool IsVerified { get; set; }
        public string Position { get; set; }
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
