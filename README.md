# Hướng Dẫn Cài Đặt và Thiết Lập Dự Án Crawl Data

## Công Cụ Cần Cài Đặt

1. **Visual Studio**
   - Tải và cài đặt [Visual Studio](https://visualstudio.microsoft.com/) (phiên bản Community là miễn phí).
   - Khi cài đặt, hãy đảm bảo chọn các workloads liên quan đến .NET desktop development.

2. **.NET SDK**
   - Cài đặt .NET SDK từ [dotnet.microsoft.com](https://dotnet.microsoft.com/download).
   - Đảm bảo rằng bạn đã cài đặt phiên bản .NET phù hợp ( bản mới nhất )

3. **NuGet Package Manager**
   - Visual Studio đi kèm với NuGet Package Manager. Đảm bảo rằng bạn có thể sử dụng nó để cài đặt các thư viện cần thiết lưu ý tương ứng với bản net sdk .

## Thiết Lập Dự Án

1. **Clone Dự Án**
   - Mở terminal hoặc Command Prompt và clone dự án từ kho lưu trữ:

2. **Mở Dự Án**
   - Mở Visual Studio và chọn "Open a project or solution".
   - Chọn file `.sln` trong thư mục dự án của bạn.

3. **Cài Đặt Thư Viện**
   - Mở **Package Manager Console** từ menu Tools > NuGet Package Manager.
   - Cài đặt các thư viện cần thiết (ví dụ: `HtmlAgilityPack` cho việc crawl data):
     ```bash
     Install-Package HtmlAgilityPack, waitHelper, microsoft.aspnetcore.components.web , microsoft.aspnetcore.components.webAssembly, mysql.data, newtonsoft.json, selenium.support, selenium.webdriver
     


4. **Chạy Dự Án**
   - Nhấn `F5` hoặc chọn "Start" để chạy ứng dụng.
   - Theo dõi console để xem kết quả từ quá trình crawl data.

## Lưu Ý
- Đảm bảo rằng bạn đã cài đặt đúng phiên bản .NET SDK và các thư viện cần thiết để tránh gặp phải các vấn đề tương thích.
