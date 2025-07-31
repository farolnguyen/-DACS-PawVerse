using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using PawVerse.Models.Location;
using PawVerse.Services.Interfaces;

namespace PawVerse.Services;

public class LocationService : ILocationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private const string BaseUrl = "https://provinces.open-api.vn/api";
    private const string ProvincesCacheKey = "Provinces";
    private const string DistrictsCacheKeyPrefix = "Districts_";
    private const string WardsCacheKeyPrefix = "Wards_";
    private const string ProvinceCacheKeyPrefix = "Province_";
    private const string DistrictCacheKeyPrefix = "District_";
    private const string WardCacheKeyPrefix = "Ward_";
    private const int CacheDurationHours = 24;

    public LocationService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
    }

    public async Task<List<Province>> GetProvincesAsync()
    {
        return await _cache.GetOrCreateAsync(ProvincesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(CacheDurationHours);
            return await GetAsync<List<Province>>($"{BaseUrl}/p/");
        }) ?? new List<Province>();
    }

    public async Task<List<District>> GetDistrictsByProvinceAsync(string provinceCode)
    {
        var cacheKey = $"{DistrictsCacheKeyPrefix}{provinceCode}";
        
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(CacheDurationHours);
            var province = await GetAsync<Province>($"{BaseUrl}/p/{provinceCode}?depth=2");
            return province?.Districts ?? new List<District>();
        });
    }

    public async Task<List<Ward>> GetWardsByDistrictAsync(string districtCode)
    {
        var cacheKey = $"{WardsCacheKeyPrefix}{districtCode}";
        
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(CacheDurationHours);
            var district = await GetAsync<District>($"{BaseUrl}/d/{districtCode}?depth=2");
            return district?.Wards ?? new List<Ward>();
        });
    }

    public async Task<Province> GetProvinceByCodeAsync(string code)
    {
        var cacheKey = $"{ProvinceCacheKeyPrefix}{code}";
        
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(CacheDurationHours);
            return await GetAsync<Province>($"{BaseUrl}/p/{code}");
        });
    }

    public async Task<District> GetDistrictByCodeAsync(string code)
    {
        var cacheKey = $"{DistrictCacheKeyPrefix}{code}";
        
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(CacheDurationHours);
            return await GetAsync<District>($"{BaseUrl}/d/{code}?depth=2");
        });
    }

    public async Task<Ward> GetWardByCodeAsync(string code)
    {
        var cacheKey = $"{WardCacheKeyPrefix}{code}";
        
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(CacheDurationHours);
            return await GetAsync<Ward>($"{BaseUrl}/w/{code}");
        });
    }

    private async Task<T> GetAsync<T>(string url)
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Error fetching data from {url}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();

        // Handle the API's behavior of returning an empty array `[]` for a "not found" resource.
        // This prevents a JsonException when trying to deserialize an array into a single object.
        if (content.Trim() == "[]")
        {
            return default; // Returns null for reference types.
        }

        try
        {
            return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            // Re-throw with more context for easier debugging.
            throw new JsonException($"Failed to deserialize JSON from {url}. Content: {content}", ex);
        }
    }
}
