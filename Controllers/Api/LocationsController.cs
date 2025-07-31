using Microsoft.AspNetCore.Mvc;
using PawVerse.Models.Location;
using PawVerse.Services.Interfaces;

namespace PawVerse.Controllers.Api;

[Route("api/[controller]")]
[ApiController]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _locationService;
    private readonly ILogger<LocationsController> _logger;

    public LocationsController(
        ILocationService locationService,
        ILogger<LocationsController> logger)
    {
        _locationService = locationService;
        _logger = logger;
    }

    [HttpGet("provinces")]
    public async Task<IActionResult> GetProvinces()
    {
        try
        {
            var provinces = await _locationService.GetProvincesAsync();
            return Ok(provinces);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provinces");
            return StatusCode(500, "Có lỗi xảy ra khi tải danh sách tỉnh/thành phố");
        }
    }

    [HttpGet("provinces/{code}")]
    public async Task<IActionResult> GetProvince(string code)
    {
        try
        {
            var province = await _locationService.GetProvinceByCodeAsync(code);
            if (province == null)
                return NotFound();
                
            return Ok(province);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting province with code {code}");
            return StatusCode(500, "Có lỗi xảy ra khi tải thông tin tỉnh/thành phố");
        }
    }

    [HttpGet("provinces/{provinceCode}/districts")]
    public async Task<IActionResult> GetDistricts(string provinceCode)
    {
        try
        {
            var districts = await _locationService.GetDistrictsByProvinceAsync(provinceCode);
            return Ok(districts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting districts for province {provinceCode}");
            return StatusCode(500, "Có lỗi xảy ra khi tải danh sách quận/huyện");
        }
    }

    [HttpGet("districts/{districtCode}/wards")]
    public async Task<IActionResult> GetWards(string districtCode)
    {
        try
        {
            var wards = await _locationService.GetWardsByDistrictAsync(districtCode);
            return Ok(wards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting wards for district {districtCode}");
            return StatusCode(500, "Có lỗi xảy ra khi tải danh sách phường/xã");
        }
    }

    [HttpGet("districts/{code}")]
    public async Task<IActionResult> GetDistrict(string code)
    {
        try
        {
            var district = await _locationService.GetDistrictByCodeAsync(code);
            if (district == null)
                return NotFound();
                
            return Ok(district);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting district with code {code}");
            return StatusCode(500, "Có lỗi xảy ra khi tải thông tin quận/huyện");
        }
    }

    [HttpGet("wards/{code}")]
    public async Task<IActionResult> GetWard(string code)
    {
        try
        {
            var ward = await _locationService.GetWardByCodeAsync(code);
            if (ward == null)
                return NotFound();
                
            return Ok(ward);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting ward with code {code}");
            return StatusCode(500, "Có lỗi xảy ra khi tải thông tin phường/xã");
        }
    }
}
