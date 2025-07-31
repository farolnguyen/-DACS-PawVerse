using System.Text.Json.Serialization;

namespace PawVerse.Models.Location;

public class Ward
{
    public int Code { get; set; }
    public string Name { get; set; }
    public string NameEn { get; set; }
    public string FullName { get; set; }
    public string FullNameEn { get; set; }
    public string CodeName { get; set; }
    public int DistrictCode { get; set; }
    
    // For backward compatibility
    [JsonIgnore]
    public string CodeString => Code.ToString();
    
    [JsonIgnore]
    public string DistrictCodeString => DistrictCode.ToString();
}
