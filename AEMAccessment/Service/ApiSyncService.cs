using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AEMAccessment.Data;
using AEMAccessment.DTOs;
using AEMAccessment.IService;
using AEMAccessment.Models;
using Microsoft.EntityFrameworkCore;

namespace AEMAccessment.Service
{
    public class ApiSyncService : IApiSyncService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<ApiSyncService> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public ApiSyncService(
            IHttpClientFactory httpClientFactory,
            ApplicationDbContext db,
            IConfiguration config,
            ILogger<ApiSyncService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _db = db;
            _config = config;
            _logger = logger;
        }


        public async Task<string?> LoginAsync()
        {
            var client = _httpClientFactory.CreateClient("AemApi");

            var loginPayload = new LoginRequest
            {
                Username = _config["AemApi:username"] ?? "user@aemenersol.com",
                Password = _config["AemApi:password"] ?? "Test@123"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(loginPayload),
                Encoding.UTF8,
                "application/json");

            try
            {
                var response = await client.PostAsync("api/Account/Login", content);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return json.Replace("\"","");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed.");
                throw;
            }
        }


        public async Task SyncPlatformWellActualAsync()
        {
            await SyncFromEndpointAsync("api/PlatformWell/GetPlatformWellActual");
        }


        public async Task SyncPlatformWellDummyAsync()
        {
            await SyncFromEndpointAsync("api/PlatformWell/GetPlatformWellDummy");
        }


        private async Task SyncFromEndpointAsync(string endpoint)
        {
            var token = await LoginAsync();
            if (string.IsNullOrEmpty(token))
                throw new InvalidOperationException("Unable to obtain bearer token.");

            var client = _httpClientFactory.CreateClient("AemApi");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call {Endpoint}", endpoint);
                throw;
            }

            var json = await response.Content.ReadAsStringAsync();

            var platforms = ParsePlatforms(json);

            if (platforms == null || platforms.Count == 0)
            {
                return;
            }

            await UpsertPlatformsAndWellsAsync(platforms);

        }


        private async Task UpsertPlatformsAndWellsAsync(List<PlatformDto> platformDtos)
        {
            foreach (var dto in platformDtos)
            {
                var existing = await _db.Platforms.FindAsync(dto.Id);

                if (existing == null)
                {
                    var newPlatform = new Platform
                    {
                        Id = dto.Id,
                        UniqueName = dto.UniqueName,
                        Latitude = dto.Latitude,
                        Longitude = dto.Longitude,
                        CreatedAt = dto.CreatedAt ?? DateTime.UtcNow,
                        UpdatedAt = dto.UpdatedAt ?? DateTime.UtcNow
                    };
                    _db.Platforms.Add(newPlatform);
                }
                else
                {
                    if (dto.UniqueName != null) existing.UniqueName = dto.UniqueName;
                    if (dto.Latitude != null) existing.Latitude = dto.Latitude;
                    if (dto.Longitude != null) existing.Longitude = dto.Longitude;
                    existing.UpdatedAt = dto.UpdatedAt ?? DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();

                if (dto.Well == null || dto.Well.Count == 0)
                    continue;

                foreach (var wellDto in dto.Well)
                {
                    var existingWell = await _db.Wells.FindAsync(wellDto.Id);

                    if (existingWell == null)
                    {
                        var newWell = new Well
                        {
                            Id = wellDto.Id,
                            PlatformId = wellDto.PlatformId,
                            UniqueName = wellDto.UniqueName,
                            Latitude = wellDto.Latitude,
                            Longitude = wellDto.Longitude,
                            CreatedAt = wellDto.CreatedAt ?? DateTime.UtcNow,
                            UpdatedAt = wellDto.UpdatedAt ?? DateTime.UtcNow
                        };
                        _db.Wells.Add(newWell);
                    }
                    else
                    {
                        if (wellDto.UniqueName != null) existingWell.UniqueName = wellDto.UniqueName;
                        if (wellDto.Latitude != null) existingWell.Latitude = wellDto.Latitude;
                        if (wellDto.Longitude != null) existingWell.Longitude = wellDto.Longitude;
                        existingWell.PlatformId = wellDto.PlatformId;
                        existingWell.UpdatedAt = wellDto.UpdatedAt ?? DateTime.UtcNow;
                    }
                }

                await _db.SaveChangesAsync();
            }
        }

        private List<PlatformDto> ParsePlatforms(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Array)
                    return JsonSerializer.Deserialize<List<PlatformDto>>(json, _jsonOptions)
                           ?? new List<PlatformDto>();

                foreach (var key in new[] { "data", "result", "value", "items", "platforms" })
                {
                    if (root.TryGetProperty(key, out var prop) &&
                        prop.ValueKind == JsonValueKind.Array)
                    {
                        return JsonSerializer.Deserialize<List<PlatformDto>>(
                                   prop.GetRawText(), _jsonOptions)
                               ?? new List<PlatformDto>();
                    }
                }

                return new List<PlatformDto>();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex ,"Parse Error ");
                return new List<PlatformDto>();
            }
        }

        private static string? FindTokenInDocument(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in element.EnumerateObject())
                {
                    if (prop.Name.Equals("token", StringComparison.OrdinalIgnoreCase) &&
                        prop.Value.ValueKind == JsonValueKind.String)
                        return prop.Value.GetString();

                    var nested = FindTokenInDocument(prop.Value);
                    if (nested != null) return nested;
                }
            }
            return null;
        }
    }
}