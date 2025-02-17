using HtmlAgilityPack;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium;
using System.Net.Http;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace CrawlToolBlazor.Services
{
    public class CrawlService
    {
        private readonly HttpClient _httpClient;

        public CrawlService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        }

        public List<string> GetAgencyUrls(string pageUrl)
        {
            List<string> agencyUrls = new List<string>();
            var options = new ChromeOptions();

            // Các tùy chọn
            options.AddArgument("--disable-gpu");
            options.AddArgument("--window-size=1920,1080");
            string[] userAgents = new string[]
            {
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36"
            };
            Random rand = new Random();
            string randomUserAgent = userAgents[rand.Next(userAgents.Length)];
            options.AddArgument($"user-agent={randomUserAgent}");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--enable-unsafe-swiftshader");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-cache");
            options.AddArgument("--disable-blink-features=AutomationControlled");

            IWebDriver driver = new ChromeDriver(options);
            Console.WriteLine($"Đang crawl từ URL: {pageUrl}");
            driver.Navigate().GoToUrl(pageUrl);
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

            try
            {
                var agencyList = wait.Until(drv => drv.FindElement(By.XPath("//ul[@class='agency-list']")));
                var agencyItems = agencyList.FindElements(By.XPath("./li"));

                foreach (var agencyItem in agencyItems)
                {
                    var linkElement = agencyItem.FindElement(By.XPath(".//a"));
                    string href = linkElement.GetAttribute("href");
                    agencyUrls.Add(href);
                }

                // Log kết quả
                Console.WriteLine($"Đã tìm thấy {agencyUrls.Count} URL.");
            }
            catch (NoSuchElementException)
            {
                Console.WriteLine("Không tìm thấy thẻ <ul> hoặc <li> chứa hồ sơ.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Đã xảy ra lỗi: " + ex.Message);
            }
            finally
            {
                driver.Quit();
            }

            return agencyUrls;
        }


        public async Task<List<Agency>> CrawlMultiplePagesAsync(List<string> urls)
        {
            var crawlTasks = urls.Select(url => CrawlDataAsync(url));
            var results = await Task.WhenAll(crawlTasks);
            //ClearTempDirectory();
            return results.ToList();
        }

        //public void ClearTempDirectory()
        //{
        //    try
        //    {
        //        string tempPath = Path.GetTempPath(); // Lấy đường dẫn đến thư mục Temp
        //        var tempDirectory = new DirectoryInfo(tempPath);

        //        // Xóa tất cả các thư mục con và tệp trong thư mục Temp
        //        foreach (var file in tempDirectory.GetFiles())
        //        {
        //            file.Delete(); // Xóa tệp
        //        }

        //        foreach (var dir in tempDirectory.GetDirectories())
        //        {
        //            dir.Delete(true); // Xóa thư mục và tất cả các tệp bên trong
        //        }

        //        Console.WriteLine("Đã xóa tất cả tệp và thư mục trong thư mục Temp.");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Đã xảy ra lỗi khi xóa thư mục Temp: " + ex.Message);
        //    }
        //}

        public async Task<Agency> CrawlDataAsync(string url)
        {
            var options = new ChromeOptions();
            options.AddArgument("--disable-gpu");
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--headless");
            options.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-cache");

            using (var driver = new ChromeDriver(options))
            {
                try
                {
                    driver.Navigate().GoToUrl(url);
                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
                    wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").ToString() == "complete");

                    string pageSource = driver.PageSource;
                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(pageSource);

                    var clientTypeNodes = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'clients__wrap')]//ul//li");
                    var clientTypes = clientTypeNodes?.Select(node => node.InnerText.Trim()).ToList() ?? new List<string>();

                    var agency = new Agency
                    {
                        Description = ExtractInnerText(htmlDoc, "//div[contains(@class, 'profile-overview--content') and contains(@class, 'tab-overview--description')]"),
                        Name = ExtractInnerText(htmlDoc, "//h1[contains(@class, 'title')]/span"), // Updated XPath to target <span>
                        OfficialWebsite = ExtractAttribute(htmlDoc, "//div[@class='profile-header--actions']//a[contains(@class, 'site')]", "href"),
                        YearFounded = ParseYear(ExtractInnerText(htmlDoc, "//div[text()='Year Founded']/following-sibling::div")),
                        AverageHourlyRate = ParseDecimal(ExtractInnerText(htmlDoc, "//div[contains(@class, 'overview-agency-hourly-rate')]")),
                        TeamSizeId = ParseTeamSize(ExtractInnerText(htmlDoc, "//div[text()='Number of Employees']/following-sibling::div")),
                        BudgetId = ParseBudget(ExtractInnerText(htmlDoc, "//div[text()='Minimal Budget']/following-sibling::div")),
                        Facebook = ExtractAttribute(htmlDoc, "//a[contains(@class, 'icon    -fb')]", "href"),
                        LinkedIn = ExtractAttribute(htmlDoc, "//a[contains(@class, 'icon-in')]", "href"),
                        Twitter = ExtractAttribute(htmlDoc, "//a[contains(@class, 'icon-tw')]", "href"),
                        Instagram = ExtractAttribute(htmlDoc, "//a[contains(@class, 'icon-ig')]", "href"),
                        YouTube = ExtractAttribute(htmlDoc, "//a[contains(@class, 'icon-yt')]", "href"),
                        WhatsApp = ExtractAttribute(htmlDoc, "//a[contains(@class, 'icon-wa')]", "href"),
                        Services = ExtractList(htmlDoc, "//ul[@class='services']/li/a"),
                        Industries = ExtractList(htmlDoc, "//div[contains(@class, 'industries__wrap')]//ul//li"),
                        Clients = ExtractList(htmlDoc, "//div[@id='clients']//div[@class='tab-clients--title simple']/span"),
                        ClientTypes = clientTypes,
                        Portfolios = ExtractPortfolios(htmlDoc, driver),
                        LogoUrl = ExtractAttribute(htmlDoc, "//div[contains(@class, 'profile-header--image')]/img", "src"),
                        ThumbnailImages = ExtractThumbnailImages(htmlDoc),
                        CreatedAt = DateTime.Now,
                        Addresses = ExtractAddresses(htmlDoc),
                        UpdatedAt = DateTime.Now,
                        PositionId = 1
                    };

                    // Log agency information
                    Console.WriteLine("Agency extracted:");
                    Console.WriteLine($"  Name: {agency.Name}");
                    Console.WriteLine($"  Description: {agency.Description}");
                    Console.WriteLine($"  Official Website: {agency.OfficialWebsite}");
                    Console.WriteLine($"  Year Founded: {agency.YearFounded}");
                    Console.WriteLine($"  Average Hourly Rate: {agency.AverageHourlyRate}");
                    Console.WriteLine($"  Team Size ID: {agency.TeamSizeId}");
                    Console.WriteLine($"  Budget ID: {agency.BudgetId}");
                    Console.WriteLine($"  Facebook: {agency.Facebook}");
                    Console.WriteLine($"  LinkedIn: {agency.LinkedIn}");
                    Console.WriteLine($"  Twitter: {agency.Twitter}");
                    Console.WriteLine($"  Instagram: {agency.Instagram}");
                    Console.WriteLine($"  YouTube: {agency.YouTube}");
                    Console.WriteLine($"  WhatsApp: {agency.WhatsApp}");
                    Console.WriteLine($"  Services: {string.Join(", ", agency.Services)}");
                    Console.WriteLine($"  Industries: {string.Join(", ", agency.Industries)}");
                    Console.WriteLine($"  Clients: {string.Join(", ", agency.Clients)}");
                    Console.WriteLine($"  Client Types: {string.Join(", ", agency.ClientTypes)}");
                    Console.WriteLine($"  Logo URL: {agency.LogoUrl}");
                    Console.WriteLine($"  Created At: {agency.CreatedAt}");
                    Console.WriteLine($"  Addresses: {string.Join(", ", agency.Addresses)}");

                    SaveAgencyToTemp(agency);
                    return agency;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error crawling data: {ex.Message}");
                    throw;
                }
                finally
                {
                    driver.Quit();
                }
            }
        }

        private void SaveAgencyToTemp(Agency agency)
        {
            // Chỉ định đường dẫn lưu vào ổ D
            string tempPath = @"D:\TempAgencies"; // Thay đổi đường dẫn đến ổ D
            Directory.CreateDirectory(tempPath); // Tạo thư mục nếu chưa tồn tại
            string fileName = Path.Combine(tempPath, $"agency_{Guid.NewGuid()}.json");

            try
            {
                string jsonData = JsonConvert.SerializeObject(agency, Formatting.Indented);
                File.WriteAllText(fileName, jsonData);
                Console.WriteLine($"Dữ liệu đã được lưu vào: {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi lưu dữ liệu vào tệp: {ex.Message}");
            }
        }






        #region Helper Methods
        private string ExtractInnerText(HtmlDocument doc, string xpath)
        {
            var node = doc.DocumentNode.SelectSingleNode(xpath);
            return node?.InnerText.Trim();
        }

        private string ExtractAttribute(HtmlDocument doc, string xpath, string attribute)
        {
            var node = doc.DocumentNode.SelectSingleNode(xpath);
            return node?.GetAttributeValue(attribute, null)?.Trim();
        }

        private string ExtractAttribute(HtmlNode node, string xpath, string attribute)
        {
            var selectedNode = node.SelectSingleNode(xpath);
            return selectedNode?.GetAttributeValue(attribute, null)?.Trim();
        }

        private List<string> ExtractAddresses(HtmlDocument doc)
        {
            var addresses = new List<string>();

            // XPath cho tất cả các địa chỉ trong phần "Headquarters" và "Other Locations"
            var addressNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'full-address')]//span[contains(@class, 'address')]");

            if (addressNodes != null)
            {
                addresses = addressNodes
                    .Select(node => node.InnerText.Trim())
                    .Where(address => !string.IsNullOrWhiteSpace(address))
                    .ToList();
            }

            return addresses;
        }

        private decimal? ParseCost(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            // Loại bỏ các ký tự không cần thiết như dấu $ hoặc dấu phẩy
            text = text.Replace("$", "").Replace(",", "").Trim();

            // Thử chuyển đổi sang kiểu decimal
            if (decimal.TryParse(text, out var result))
            {
                return result;
            }

            // Nếu không thể chuyển đổi, trả về null
            return null;
        }

        private int? ParseYear(string text) =>
            int.TryParse(text, out var year) ? year : null;

        private decimal? ParseDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            return decimal.TryParse(text.Replace("$", "").Replace("/hr", "").Trim(), out var result) ? result : null;
        }

        private int? ParseTeamSize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            // Tách số từ chuỗi, ví dụ "50 - 100" -> "50"
            var match = System.Text.RegularExpressions.Regex.Match(text, @"\d+");
            if (match.Success)
            {
                int size = int.Parse(match.Value);
                return size <= 10 ? 1 :
                       size <= 50 ? 2 :
                       size <= 100 ? 3 :
                       size <= 250 ? 4 : 5;
            }

            return null;
        }


        private int? ParseBudget(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            // Lấy số từ chuỗi, ví dụ "$10,000" -> "10000"
            var match = System.Text.RegularExpressions.Regex.Match(text, @"\d+(,\d+)?");
            if (match.Success)
            {
                var budget = int.Parse(match.Value.Replace(",", ""));
                return budget < 10000 ? 1 :
                       budget < 25000 ? 2 :
                       budget < 50000 ? 3 :
                       budget < 100000 ? 4 : 5;
            }

            return null;
        }


        private List<string> ExtractList(HtmlDocument doc, string xpath)
        {
            var nodes = doc.DocumentNode.SelectNodes(xpath);
            return nodes?.Select(node => node.InnerText.Trim()).ToList() ?? new List<string>();
        }

        private string ExtractInnerText(HtmlNode node, string xpath)
        {
            var targetNode = node.SelectSingleNode(xpath);
            return targetNode?.InnerText.Trim();
        }


        private List<Portfolio> ExtractPortfolios(HtmlDocument doc, IWebDriver driver)
        {
            var nodes = doc.DocumentNode.SelectNodes("//ul[@class='portfolio-list']/li");
            if (nodes == null)
            {
                Console.WriteLine("No nodes found from HtmlAgilityPack.");
                return new List<Portfolio>();
            }

            var portfolios = new List<Portfolio>();
            var portfolioElements = driver.FindElements(By.CssSelector(".portfolio-list .portfolio-item"));
            Console.WriteLine($"nodes.Count: {nodes.Count}, portfolioElements.Count: {portfolioElements.Count}");

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                string description = null;

                try
                {
                    if (i < portfolioElements.Count)
                    {
                        // Click vào phần tử để mở modal
                        var portfolioElement = portfolioElements[i];

                        // Cuộn phần tử vào vùng nhìn thấy (viewport)
                        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", portfolioElement);
                        Thread.Sleep(500); // Chờ để đảm bảo trang ổn định

                        try
                        {
                            // Nhấp bằng Actions để tránh lỗi bị chặn
                            Actions actions = new Actions(driver);
                            actions.MoveToElement(portfolioElement).Click().Perform();
                        }
                        catch (Exception clickEx)
                        {
                            // Nếu vẫn bị chặn, sử dụng JavaScript click
                            Console.WriteLine($"Fallback to JavaScript click for item {i}: {clickEx.Message}");
                            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", portfolioElement);
                        }
                        Thread.Sleep(1000); // Chờ 1 giây sau khi mở modal để nội dung tải xong
                        // Chờ modal hiển thị
                        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
                        var modal = wait.Until(d => d.FindElement(By.CssSelector(".modal-portfolio--text")));
                        if (modal == null || !modal.Displayed)
                        {
                            Console.WriteLine($"Modal for portfolio item {i} is not displayed or not loaded.");
                            continue;
                        }


                        // Kiểm tra nếu modal hiển thị
                        if (modal.Displayed)
                        {
                            description = modal.Text.Trim();

                            //description = NormalizeDescription(description);
                            //Thread.Sleep(1000); // Chờ 1 giây trước khi đóng modal
                            // Đóng modal
                            var closeButton = driver.FindElement(By.CssSelector(".edit-modal--close .close"));
                            closeButton.Click();

                            // Chờ modal đóng
                            wait.Until(d => !modal.Displayed);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error extracting description for portfolio item {i}: {ex.Message}");
                }
                var addsNodes = node.SelectNodes(".//div[contains(@class, 'overview-portfolio--adds')]/div");
                decimal? cost = null;
                int? timeline = null;
                string year = null;

                // Duyệt qua từng thẻ <div> trong phần dữ liệu bổ sung
                if (addsNodes != null)
                {
                    foreach (var addsNode in addsNodes)
                    {
                        var iconClass = addsNode.SelectSingleNode(".//svg/use")?.GetAttributeValue("xlink:href", "");

                        if (iconClass != null)
                        {
                            if (iconClass.Contains("portfolio-icon-cost"))
                            {
                                cost = ParseCost(addsNode.InnerText.Trim());
                            }
                            else if (iconClass.Contains("portfolio-icon-timeline"))
                            {
                                int extractedTimeline;
                                if (int.TryParse(ExtractNumber(addsNode.InnerText.Trim()), out extractedTimeline))
                                {
                                    timeline = extractedTimeline; // Gán giá trị nếu parse thành công
                                }
                                else
                                {
                                    timeline = null; // Gán null nếu không parse được
                                }

                            }
                            else if (iconClass.Contains("portfolio-icon-year"))
                            {
                                year = addsNode.InnerText.Trim();
                            }
                        }
                    }
                }


                var portfolio = new Portfolio
                {
                    Name = ExtractInnerText(node, ".//div[contains(@class, 'overview-portfolio--name')]"),
                    Type = ExtractInnerText(node, ".//div[contains(@class, 'overview-portfolio--hash')]"),
                    Cost = cost, // Giá trị đã được phân loại
                    Timeline = timeline, // Giá trị đã được phân loại
                    Year = year, // Giá trị đã được phân loại
                    ImageUrl = ExtractAttribute(node, ".//img", "src"),
                    Description = description
                };


                // In thông tin của Portfolio vừa tạo
                Console.WriteLine("Portfolio extracted:");
                Console.WriteLine($"  Name: {portfolio.Name}");
                Console.WriteLine($"  Type: {portfolio.Type}");
                Console.WriteLine($"  Cost: {portfolio.Cost}");
                Console.WriteLine($"  Timeline: {portfolio.Timeline}");
                Console.WriteLine($"  Year: {portfolio.Year}");
                Console.WriteLine($"  ImageUrl: {portfolio.ImageUrl}");
                Console.WriteLine($"  Description: {portfolio.Description}");

                portfolios.Add(portfolio);
            }

            Console.WriteLine($"Total portfolios extracted: {portfolios.Count}");
            return portfolios;
        }

        private string NormalizeDescription(string description)
        {
            if (string.IsNullOrEmpty(description)) return "No description available.";
            description = Regex.Replace(description, @"\s+", " ").Trim();
            return description;
        }








        private string ExtractNumber(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            var match = System.Text.RegularExpressions.Regex.Match(input, @"\d+(\.\d+)?");
            return match.Success ? match.Value : null;
        }


        private List<(string Url, int Type)> ExtractThumbnailImages(HtmlDocument doc)
        {
            var nodes = doc.DocumentNode.SelectNodes("//div[@class='profile-carousel--thumbnails']//img");
            if (nodes == null) return new List<(string, int)>();

            var thumbnails = new List<(string, int)>();
            for (int i = 0; i < nodes.Count; i++)
            {
                string url = nodes[i].GetAttributeValue("src", null);
                int type = i == 0 ? 2 : 3;
                thumbnails.Add((url, type));
            }
            return thumbnails;
        }
        #endregion
    }
}
