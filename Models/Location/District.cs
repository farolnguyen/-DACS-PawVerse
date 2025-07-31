using System.Text.Json.Serialization;

namespace PawVerse.Models.Location;

public class District
{
    public int Code { get; set; }
    public string Name { get; set; }
    public string NameEn { get; set; }
    public string FullName { get; set; }
    public string FullNameEn { get; set; }
    public string CodeName { get; set; }
    public int ProvinceCode { get; set; }
    public List<Ward> Wards { get; set; } = new();
    
    // For backward compatibility
    [JsonIgnore]
    public string CodeString => Code.ToString();
    
    [JsonIgnore]
    public string ProvinceCodeString => ProvinceCode.ToString();
}
