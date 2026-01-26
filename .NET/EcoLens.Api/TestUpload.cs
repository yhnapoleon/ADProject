using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EcoLens.Api;

/// <summary>
/// Simple test program to upload utility bill image
/// </summary>
public class TestUpload
{
    private static readonly string BaseUrl = "http://localhost:5133";
    
    public static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: TestUpload <file_path>");
            return;
        }
        
        var filePath = args[0];
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Error: File not found: {filePath}");
            return;
        }
        
        Console.WriteLine($"\n========================================");
        Console.WriteLine($"  Upload Utility Bill Test");
        Console.WriteLine($"========================================");
        Console.WriteLine($"\nFile: {filePath}");
        Console.WriteLine($"Size: {new FileInfo(filePath).Length / 1024.0:F2} KB");
        
        // Step 1: Login
        Console.WriteLine($"\n=== Step 1: Login ===");
        var timestamp = DateTime.Now.ToString("HHmmss");
        var email = $"test{timestamp}@example.com";
        
        using var httpClient = new HttpClient();
        
        // Register
        try
        {
            var registerBody = new
            {
                username = $"testuser{timestamp}",
                email = email,
                password = "Test123!"
            };
            var registerJson = JsonSerializer.Serialize(registerBody);
            var registerContent = new StringContent(registerJson, Encoding.UTF8, "application/json");
            var registerResponse = await httpClient.PostAsync($"{BaseUrl}/api/auth/register", registerContent);
            Console.WriteLine("✓ Registered new user");
        }
        catch
        {
            Console.WriteLine("⚠ Registration failed (user may exist)");
        }
        
        // Login
        var loginBody = new
        {
            email = email,
            password = "Test123!"
        };
        var loginJson = JsonSerializer.Serialize(loginBody);
        var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");
        var loginResponse = await httpClient.PostAsync($"{BaseUrl}/api/auth/login", loginContent);
        
        if (!loginResponse.IsSuccessStatusCode)
        {
            Console.WriteLine($"✗ Login failed: {loginResponse.StatusCode}");
            return;
        }
        
        var loginResult = await loginResponse.Content.ReadAsStringAsync();
        var loginData = JsonSerializer.Deserialize<JsonElement>(loginResult);
        var token = loginData.GetProperty("token").GetString();
        
        Console.WriteLine("✓ Login successful");
        
        // Step 2: Upload File
        Console.WriteLine($"\n=== Step 2: Upload Bill File ===");
        
        try
        {
            var fileName = Path.GetFileName(filePath);
            var fileBytes = await File.ReadAllBytesAsync(filePath);
            
            using var multipartContent = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            multipartContent.Add(fileContent, "file", fileName);
            
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            Console.WriteLine("Uploading file...");
            var uploadResponse = await httpClient.PostAsync($"{BaseUrl}/api/UtilityBill/upload", multipartContent);
            
            if (uploadResponse.IsSuccessStatusCode)
            {
                var responseContent = await uploadResponse.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                Console.WriteLine("✓ Upload successful!");
                Console.WriteLine($"\n=== Extracted Data ===");
                Console.WriteLine($"Bill ID: {responseData.GetProperty("id").GetInt32()}");
                Console.WriteLine($"Bill Type: {responseData.GetProperty("billTypeName").GetString()}");
                Console.WriteLine($"Period: {responseData.GetProperty("billPeriodStart").GetString()} to {responseData.GetProperty("billPeriodEnd").GetString()}");
                Console.WriteLine($"Input Method: {responseData.GetProperty("inputMethodName").GetString()}");
                
                if (responseData.TryGetProperty("ocrConfidence", out var ocrConf))
                {
                    Console.WriteLine($"OCR Confidence: {ocrConf.GetDecimal() * 100:F2}%");
                }
                
                Console.WriteLine($"\n=== Usage Data ===");
                if (responseData.TryGetProperty("electricityUsage", out var elec) && elec.ValueKind != JsonValueKind.Null)
                {
                    Console.WriteLine($"Electricity: {elec.GetDecimal()} kWh");
                }
                if (responseData.TryGetProperty("waterUsage", out var water) && water.ValueKind != JsonValueKind.Null)
                {
                    Console.WriteLine($"Water: {water.GetDecimal()} m³");
                }
                if (responseData.TryGetProperty("gasUsage", out var gas) && gas.ValueKind != JsonValueKind.Null)
                {
                    Console.WriteLine($"Gas: {gas.GetDecimal()}");
                }
                
                Console.WriteLine($"\n=== Carbon Emissions ===");
                Console.WriteLine($"Electricity Carbon: {responseData.GetProperty("electricityCarbonEmission").GetDecimal()} kg CO2");
                Console.WriteLine($"Water Carbon: {responseData.GetProperty("waterCarbonEmission").GetDecimal()} kg CO2");
                Console.WriteLine($"Gas Carbon: {responseData.GetProperty("gasCarbonEmission").GetDecimal()} kg CO2");
                Console.WriteLine($"Total Carbon: {responseData.GetProperty("totalCarbonEmission").GetDecimal()} kg CO2");
                
                Console.WriteLine($"\n=== Test Complete ===");
            }
            else
            {
                var errorContent = await uploadResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"✗ Upload failed: {uploadResponse.StatusCode}");
                Console.WriteLine($"Response: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Upload failed: {ex.Message}");
        }
    }
}
