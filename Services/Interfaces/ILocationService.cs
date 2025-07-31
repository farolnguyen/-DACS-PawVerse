using PawVerse.Models.Location;

namespace PawVerse.Services.Interfaces;

public interface ILocationService
{
    Task<List<Province>> GetProvincesAsync();
    Task<List<District>> GetDistrictsByProvinceAsync(string provinceCode);
    Task<List<Ward>> GetWardsByDistrictAsync(string districtCode);
    Task<Province> GetProvinceByCodeAsync(string code);
    Task<District> GetDistrictByCodeAsync(string code);
    Task<Ward> GetWardByCodeAsync(string code);
}
