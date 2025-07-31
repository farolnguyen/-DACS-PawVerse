using System.Text.Json.Serialization;

namespace PawVerse.Models.Location;

public class Province
{
    public int Code { get; set; }
    public string Name { get; set; }
    public string NameEn { get; set; }
    public string FullName { get; set; }
    public string FullNameEn { get; set; }
    public string CodeName { get; set; }
    
    // For backward compatibility
    [JsonIgnore]
    public string CodeString => Code.ToString();
    
    // Navigation property for districts
    public List<District> Districts { get; set; } = new();
}
