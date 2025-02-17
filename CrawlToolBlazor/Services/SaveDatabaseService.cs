using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using MySql.Data.MySqlClient;

namespace CrawlToolBlazor.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void TestConnection()
        {
            Console.WriteLine($"Connecting to database: {_connectionString}");
        }

        public void SaveAllAgenciesData(List<Agency> agencies)
        {
            foreach (var agency in agencies)
            {
                try
                {
                    using (var connection = new MySqlConnection(_connectionString))
                    {
                        connection.Open();

                        // Kiểm tra nếu trường name trống
                        if (string.IsNullOrEmpty(agency.Name))
                        {
                            Console.WriteLine("Skipping agency due to missing name.");
                            continue; // Bỏ qua agency hiện tại
                        }

                        // Kiểm tra nếu trường description trống (nếu cần)
                        if (string.IsNullOrEmpty(agency.Description))
                        {
                            Console.WriteLine($"Skipping agency: {agency.Name} due to missing description.");
                            continue; // Bỏ qua agency hiện tại
                        }

                        // Kiểm tra tên agency có trùng không
                        if (IsDuplicateAgencyName(agency.Name, connection))
                        {
                            Console.WriteLine($"Skip agency: {agency.Name} already exists in database.");
                            continue; // Bỏ qua agency hiện tại
                        }

                        // 1. Kiểm tra tính hợp lệ của địa chỉ
                        var validationErrors = ValidateAddressesWithDetails(agency.Addresses, connection);
                        if (validationErrors.Any())
                        {
                            Console.WriteLine($"Skip agency: {agency.Name} because of invalid address.");
                            foreach (var error in validationErrors)
                            {
                                Console.WriteLine($" - Errors: {error}");
                            }
                            continue; // Bỏ qua profile này và chuyển sang profile tiếp theo
                        }

                        // 2. Lưu thông tin cơ bản và các thông tin khác nếu địa chỉ hợp lệ
                        long agencyId = SaveAgencyInfo(agency, connection);
                        SaveAddresses(agencyId, agency.Addresses, connection);
                        SaveServices(agencyId, agency.Services, connection);
                        SaveIndustries(agencyId, agency.Industries, connection);
                        SaveClientTypes(agencyId, agency.ClientTypes, connection);
                        SaveClients(agencyId, agency.Clients, connection);
                        SavePortfolios(agencyId, agency.Portfolios, connection);
                        SaveImages(agencyId, agency.LogoUrl, agency.ThumbnailImages, connection);

                        Console.WriteLine($"Successfully saved agency: {agency.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing agency: {agency.Name}. Details: {ex.Message}");
                }
            }
        }

        private bool IsDuplicateAgencyName(string agencyName, MySqlConnection connection)
        {
            string query = "SELECT COUNT(*) FROM agencies WHERE name = @name;";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@name", agencyName);
                long count = Convert.ToInt64(cmd.ExecuteScalar());
                return count > 0;
            }
        }



        private readonly Dictionary<string, string> _serviceToSlugMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    { "Mobile App Development", "mobile-development" },
    { "UI/UX Design", "website-interface" },
    { "IT Services", "it-services" },
    { "Digital Marketing", "marketing" },
    { "Branding", "branding-creative" },
    { "Software Testing", "software-testing" },
    { "Software Development", "software-development" },
    { "AI Development", "artificial-intelligent" },
    { "Cloud Consulting", "cloud-services" },
    { "Cybersecurity", "cybersecurity" },
    { "Managed IT Services", "managed-it-services" },
    { "Offshore Software Development", "offshore-development-center" },
    { "Outsourcing Software Development", "it-outsourcing" },
    { "VR/AR", "vr-ar" },
    { "Big Data Analytics", "big-data-analytics" },
    { "Staff Augmentation", "staff-augmentation" },
    { "Web Development", "software-app" },
    { "IoT", "internet-of-things" },
    { "Blockchain", "blockchain-development" },
    { "eCommerce Development", "software-app" }
};


        private readonly Dictionary<string, string> _industryMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    { "eCommerce", "E-commerce and Digital Platform" },
    { "Education", "Education" },
    { "Consumer Goods", "Consumer Goods" },
    { "Corporate Services", "Corporate Services" },
    { "Delivery & Takeaway", "Delivery and Takeaway" },
    { "Finance", "Financial" },
    { "Health Care", "Healthcare" },
    { "Travel", "Tourism" },
    { "Insurance", "Insurance" },
    { "Food and Beverage", "F&B" },
    { "Crypto", "Blockchain and Cryptocurrency" },
    { "Automotive", "Robotics and Automation" }
};
        private readonly Dictionary<string, int> _projectTypeMapping = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
{
    { "Web design", 1 },
    { "Web development", 2 },
    { "Mobile app development", 3 },
    { "Outsourcing", 4 },
    { "Digital Marketing", 5 },
};

        private long SaveAgencyInfo(Agency agency, MySqlConnection connection)
        {
            string query = @"
                INSERT INTO agencies 
                (name, email, phone, position_id, agency_name, official_website, year_founded, average_hourly_rate, 
                 team_size_id, budget_id, facebook, linkedin, twitter, instagram, youtube, whatsapp, 
                 description, status, is_verified, created_at, updated_at)
                 VALUES (@name, @email, @phone, @position_id, @agency_name, @official_website, @year_founded, 
                 @average_hourly_rate, @team_size_id, @budget_id, @facebook, @linkedin, @twitter, @instagram, 
                 @youtube, @whatsapp, @description, @status, @is_verified, NOW(), NOW());
                 SELECT LAST_INSERT_ID();";

            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@name", agency.Name);
                cmd.Parameters.AddWithValue("@email", agency.Email ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@phone", agency.Phone ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@position_id", agency.PositionId);
                cmd.Parameters.AddWithValue("@agency_name", agency.Name);
                cmd.Parameters.AddWithValue("@official_website", agency.OfficialWebsite ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@year_founded", agency.YearFounded);
                cmd.Parameters.AddWithValue("@average_hourly_rate", agency.AverageHourlyRate);
                cmd.Parameters.AddWithValue("@team_size_id", agency.TeamSizeId);
                cmd.Parameters.AddWithValue("@budget_id", agency.BudgetId);
                cmd.Parameters.AddWithValue("@facebook", agency.Facebook ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@linkedin", agency.LinkedIn ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@twitter", agency.Twitter ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@instagram", agency.Instagram ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@youtube", agency.YouTube ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@whatsapp", agency.WhatsApp ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@description", agency.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@status", 3);
                cmd.Parameters.AddWithValue("@is_verified", agency.IsVerified);

                return Convert.ToInt64(cmd.ExecuteScalar());
            }
        }

        private void SaveServices(long agencyId, List<string> services, MySqlConnection connection)
        {
            foreach (var service in services)
            {
                try
                {
                    var categoryId = GetCategoryId(service, connection);
                    if (categoryId == null)
                    {
                        Console.WriteLine($"Skipped service '{service}' because no matching category was found.");
                        continue; // Skip the current service if no category_id is found
                    }

                    string query = "INSERT INTO agency_category (agency_id, category_id) VALUES (@agency_id, @category_id);";
                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@agency_id", agencyId);
                        cmd.Parameters.AddWithValue("@category_id", categoryId);
                        cmd.ExecuteNonQuery();
                        Console.WriteLine($"Successfully saved service '{service}' with category_id: {categoryId}");
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but continue processing other services
                    Console.WriteLine($"Error saving service '{service}': {ex.Message}");
                }
            }
        }



        private void SaveIndustries(long agencyId, List<string> industries, MySqlConnection connection)
        {
            foreach (var industry in industries)
            {
                var industryId = GetIndustryId(industry, connection);
                if (industryId == null) continue;

                string query = "INSERT INTO agency_industry (agency_id, industry_id) VALUES (@agency_id, @industry_id);";
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@agency_id", agencyId);
                    cmd.Parameters.AddWithValue("@industry_id", industryId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void SaveClientTypes(long agencyId, List<string> clientTypes, MySqlConnection connection)
        {
            foreach (var clientType in clientTypes)
            {
                var clientTypeId = GetClientTypeId(clientType, connection);
                if (clientTypeId == null) continue;

                string query = "INSERT INTO agency_client_type (agency_id, client_type_id) VALUES (@agency_id, @client_type_id);";
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@agency_id", agencyId);
                    cmd.Parameters.AddWithValue("@client_type_id", clientTypeId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void SaveClients(long agencyId, List<string> clients, MySqlConnection connection)
        {
            foreach (var client in clients)
            {
                string query = "INSERT INTO featured_clients (agency_id, name, status, created_at, updated_at) VALUES (@agency_id, @name, 3, NOW(), NOW());";
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@agency_id", agencyId);
                    cmd.Parameters.AddWithValue("@name", client);
                    cmd.ExecuteNonQuery();
                }
            }
        }   

        private void SavePortfolios(long agencyId, List<Portfolio> portfolios, MySqlConnection connection)
        {
            foreach (var portfolio in portfolios)
            {
                string query = @"
                    INSERT INTO agency_projects (agency_id, project_name,description, project_type_id, value, total_time, image, created_at, updated_at) 
                    VALUES (@agency_id, @name,@description, @type, @cost, @timeline, @image_url, NOW(), NOW());";
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@agency_id", agencyId);
                    cmd.Parameters.AddWithValue("@name", portfolio.Name);
                    cmd.Parameters.AddWithValue("@description", portfolio.Description);
                    cmd.Parameters.AddWithValue("@type", GetProjectTypeId(portfolio.Type, connection));
                    cmd.Parameters.AddWithValue("@cost", portfolio.Cost ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@timeline", portfolio.Timeline ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@image_url", portfolio.ImageUrl);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void SaveImages(long agencyId, string logoUrl, List<(string Url, int Type)> thumbnails, MySqlConnection connection)
        {
            if (!string.IsNullOrEmpty(logoUrl))
            {
                string query = "INSERT INTO agency_medias (agency_id, type, url, created_at, updated_at) VALUES (@agency_id, 1, @url, NOW(), NOW());";
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@agency_id", agencyId);
                    cmd.Parameters.AddWithValue("@url", logoUrl);
                    cmd.ExecuteNonQuery();
                }
            }

            foreach (var thumbnail in thumbnails)
            {
                string query = "INSERT INTO agency_medias (agency_id, type, url, created_at, updated_at) VALUES (@agency_id, @type, @url, NOW(), NOW());";
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@agency_id", agencyId);
                    cmd.Parameters.AddWithValue("@type", thumbnail.Type);
                    cmd.Parameters.AddWithValue("@url", thumbnail.Url);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private List<string> ValidateAddressesWithDetails(List<string> addresses, MySqlConnection connection)
        {
            var errors = new List<string>();

            // Dictionary để ánh xạ quốc gia
            var countryMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Vietnam", "Viet Nam" },
        { "United States", "USA" },
        // Thêm các ánh xạ quốc gia khác tại đây
    };

            foreach (var address in addresses)
            {
                if (string.IsNullOrWhiteSpace(address))
                {
                    errors.Add("Địa chỉ trống hoặc không hợp lệ.");
                    continue;
                }

                // 1. Tách quốc gia từ địa chỉ
                string country = ExtractCountryFromAddress(address);

                // 2. Kiểm tra quốc gia trong dictionary trước
                if (countryMapping.TryGetValue(country, out var mappedCountry))
                {
                    country = mappedCountry; // Cập nhật quốc gia theo ánh xạ
                }

                // 3. Kiểm tra xem country_id có tồn tại hay không
                long? countryId = GetCountryId(country, connection);

                if (!countryId.HasValue)
                {
                    errors.Add($"Không tìm thấy country_id cho địa chỉ: {address}");
                }
            }

            return errors;
        }

        private bool SaveAddresses(long agencyId, List<string> addresses, MySqlConnection connection)
        {
            // Dictionary để ánh xạ quốc gia
            var countryMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Vietnam", "Viet Nam" },
        { "United States", "USA" },
        // Thêm các ánh xạ quốc gia khác tại đây
    };

            var notFoundCountries = new List<string>();

            try
            {
                foreach (var address in addresses)
                {
                    if (string.IsNullOrWhiteSpace(address)) continue; // Bỏ qua địa chỉ rỗng hoặc null

                    // 1. Tách quốc gia từ địa chỉ
                    string country = ExtractCountryFromAddress(address);

                    // 2. Kiểm tra quốc gia trong dictionary trước
                    if (countryMapping.TryGetValue(country, out var mappedCountry))
                    {
                        country = mappedCountry; // Cập nhật quốc gia theo ánh xạ
                    }

                    // 3. Lấy country_id từ cơ sở dữ liệu
                    long? countryId = GetCountryId(country, connection);

                    // Nếu không tìm thấy country_id, thêm vào danh sách lỗi
                    if (!countryId.HasValue)
                    {
                        notFoundCountries.Add(country);
                        continue;
                    }

                    // 4. Chèn địa chỉ vào bảng addresses cùng với country_id
                    string query = @"
                INSERT INTO addresses (agency_id, street_name, status, created_at, updated_at, country_id) 
                VALUES (@agency_id, @address_line, 3, NOW(), NOW(), @country_id);";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@agency_id", agencyId);
                        cmd.Parameters.AddWithValue("@address_line", address);
                        cmd.Parameters.AddWithValue("@country_id", countryId.Value);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Ghi log nếu có quốc gia không tìm thấy
                if (notFoundCountries.Any())
                {
                    LogNotFoundCountries(notFoundCountries);
                }

                return true; // Địa chỉ được xử lý thành công
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi lưu địa chỉ: {ex.Message}");
                return false; // Trả về false nếu có lỗi xảy ra
            }
        }


        private void LogNotFoundCountries(List<string> countries)
        {
            // Ghi log quốc gia không tìm thấy vào file
            string logFilePath = "C:\\Users\\Admin\\Dropbox\\PC\\Desktop\\not_found_countries.log"; // Đường dẫn file log
            using (var writer = new StreamWriter(logFilePath, append: true))
            {
                writer.WriteLine($"Log Date: {DateTime.Now}");
                foreach (var country in countries.Distinct()) // Chỉ ghi lại các quốc gia duy nhất
                {
                    writer.WriteLine($"Country not found: {country}");
                }
                writer.WriteLine("---------------------------------------------------");
            }

            Console.WriteLine($"Logged {countries.Count} not found countries to {logFilePath}");
        }

        private string ExtractCountryFromAddress(string address)
        {
            // Regex để lấy phần sau dấu phẩy cuối cùng (quốc gia)
            var match = Regex.Match(address, @"[^,]+$");
            return match.Success ? match.Value.Trim() : string.Empty;
        }

        private long? GetCountryId(string countryName, MySqlConnection connection)
        {
            if (string.IsNullOrWhiteSpace(countryName)) return null;

            string query = "SELECT id FROM countries WHERE name = @country_name LIMIT 1;";

            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@country_name", countryName);

                var result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt64(result); // Trả về ID nếu tìm thấy
                }
            }

            return null; // Không tìm thấy
        }




        private long? GetCategoryId(string service, MySqlConnection connection)
        {
            // 1. Check in the slug map
            if (_serviceToSlugMap.TryGetValue(service, out var slug))
            {
                // Check in the database if the slug already exists
                string query = @"SELECT id FROM categories WHERE slug = @slug LIMIT 1;";
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@slug", slug);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        return Convert.ToInt64(result); // Return ID if it exists
                }
            }

            // 2. If not found in the slug map, check directly in the `categories` table by service name
            string queryByName = @"SELECT id FROM categories WHERE slug = @service_name LIMIT 1;";
            using (var cmd = new MySqlCommand(queryByName, connection))
            {
                cmd.Parameters.AddWithValue("@service_name", service);
                var result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    Console.WriteLine($"Category '{service}' found in the categories table.");
                    return Convert.ToInt64(result); // Return ID if found
                }
            }

            // If not found in both slug map and categories table
            Console.WriteLine($"Category '{service}' not found in slug map or categories table.");
            return null;
        }

        private long? GetIndustryId(string industry, MySqlConnection connection)
        {
            // Giải mã ký tự HTML trước khi xử lý
            industry = DecodeHtmlString(industry);

            // 1. Kiểm tra trong mapping
            if (_industryMapping.TryGetValue(industry, out var mappedIndustry))
            {
                mappedIndustry = DecodeHtmlString(mappedIndustry); // Giải mã ký tự trong mapping nếu cần
                string query = @"SELECT industry_id FROM industry_translations WHERE LOWER(name) = @name LIMIT 1;";
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@name", mappedIndustry.ToLower());
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        return Convert.ToInt64(result); // Trả về ID nếu tồn tại
                }
            }

            // 2. Kiểm tra trực tiếp trong database
            string checkQuery = @"SELECT industry_id FROM industry_translations WHERE LOWER(name) = @name LIMIT 1;";
            using (var cmd = new MySqlCommand(checkQuery, connection))
            {
                cmd.Parameters.AddWithValue("@name", industry.ToLower());
                var result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                    return Convert.ToInt64(result); // Trả về ID nếu đã tồn tại
            }

            // 3. Thêm mới nếu không tồn tại
            string insertIndustry = "INSERT INTO industries (status, created_at, updated_at) VALUES (3, NOW(), NOW());";
            using (var cmd = new MySqlCommand(insertIndustry, connection))
            {
                cmd.ExecuteNonQuery();
            }

            // Lấy ID mới chèn bằng LAST_INSERT_ID()
            long industryId = Convert.ToInt64(new MySqlCommand("SELECT LAST_INSERT_ID();", connection).ExecuteScalar());

            // Thêm vào bảng industry_translations
            string insertTranslation = @"
        INSERT INTO industry_translations (industry_id, locale, name, description) 
        VALUES (@industry_id, 'en', @name, @name);";
            using (var cmd = new MySqlCommand(insertTranslation, connection))
            {
                cmd.Parameters.AddWithValue("@industry_id", industryId);
                cmd.Parameters.AddWithValue("@name", industry);
                cmd.ExecuteNonQuery();
            }

            return industryId;
        }

        // Hàm decode ký tự HTML
        private string DecodeHtmlString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            return HttpUtility.HtmlDecode(input);
        }




        private long? GetClientTypeId(string clientType, MySqlConnection connection)
        {
            var clientTypeMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Startups", "Start- up" },
        { "Small Businesses", "Small Businesses" },
        { "Medium Businesses", "Medium Businesses" },
        { "Enterprise / Corporate", "Enterprise/ Corporate" }
    };

            if (clientTypeMapping.TryGetValue(clientType, out var mappedType))
            {
                string query = @"SELECT client_type_id FROM client_type_translations WHERE LOWER(name) = @name LIMIT 1;";
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@name", mappedType.ToLower());
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                        return Convert.ToInt64(result);
                }
            }

            // Fallback for unmatched client types
            return null;
        }

        private int? GetProjectTypeId(string projectType, MySqlConnection connection)
        {
            // 1. Kiểm tra trong _projectTypeMapping
            if (_projectTypeMapping.TryGetValue(projectType, out var typeId))
            {
                return typeId; // Trả về ID nếu tìm thấy trong mapping
            }

            // 2. Kiểm tra trong database
            string query = @"SELECT project_type_id FROM project_type_translations WHERE LOWER(name) = @name LIMIT 1;";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@name", projectType.ToLower());
                var result = cmd.ExecuteScalar();
                if (result != null)
                    return Convert.ToInt32(result); // Trả về ID nếu tìm thấy trong database
            }

            // 3. Nếu không tồn tại, thêm mới vào bảng project_types
            string insertTypeQuery = "INSERT INTO project_types (status, created_at, updated_at) VALUES (3, NOW(), NOW());";
            using (var cmd = new MySqlCommand(insertTypeQuery, connection))
            {
                cmd.ExecuteNonQuery();
            }

            // Lấy ID mới được thêm vào từ bảng project_types bằng LAST_INSERT_ID()
            int newTypeId;
            using (var cmd = new MySqlCommand("SELECT LAST_INSERT_ID();", connection))
            {
                newTypeId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // 4. Thêm bản dịch vào bảng project_type_translations
            string insertTranslationQuery = @"
        INSERT INTO project_type_translations (project_type_id, locale, name) 
        VALUES (@project_type_id, 'en', @name);";
            using (var cmd = new MySqlCommand(insertTranslationQuery, connection))
            {
                cmd.Parameters.AddWithValue("@project_type_id", newTypeId);
                cmd.Parameters.AddWithValue("@name", projectType);
                cmd.ExecuteNonQuery();
            }

            return newTypeId; // Trả về ID mới được thêm
        }

    }
}
