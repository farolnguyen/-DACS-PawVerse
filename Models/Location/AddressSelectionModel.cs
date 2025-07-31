using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace PawVerse.Models.Location
{
    public class AddressSelectionModel
    {
        // Selected values with both old and new property names
        [Display(Name = "Tỉnh/Thành phố")]
        [Required(ErrorMessage = "Vui lòng chọn tỉnh/thành phố")]
        public string SelectedProvinceCode { get; set; }

        [Display(Name = "Quận/Huyện")]
        [Required(ErrorMessage = "Vui lòng chọn quận/huyện")]
        public string SelectedDistrictCode { get; set; }

        [Display(Name = "Phường/Xã")]
        [Required(ErrorMessage = "Vui lòng chọn phường/xã")]
        public string SelectedWardCode { get; set; }

        [Display(Name = "Địa chỉ cụ thể (Số nhà, tên đường)")]
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ cụ thể")]
        public string StreetAddress { get; set; }

        // Navigation properties for dropdown lists
        [JsonIgnore]
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Provinces { get; set; } = new();
        
        [JsonIgnore]
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Districts { get; set; } = new();
        
        [JsonIgnore]
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Wards { get; set; } = new();

        // Helper method to get full address
        public string GetFullAddress()
        {
            var parts = new List<string>();
            
            if (!string.IsNullOrEmpty(StreetAddress))
                parts.Add(StreetAddress.Trim());
                
            var ward = Wards?.FirstOrDefault(w => w.Value == SelectedWardCode);
            var district = Districts?.FirstOrDefault(d => d.Value == SelectedDistrictCode) ?? 
                         (ward != null ? null : Districts?.FirstOrDefault(d => !string.IsNullOrEmpty(d.Value)));
            var province = Provinces?.FirstOrDefault(p => p.Value == SelectedProvinceCode) ?? 
                         (district != null ? null : Provinces?.FirstOrDefault(p => !string.IsNullOrEmpty(p.Value)));

            if (ward != null)
                parts.Add(ward.Text);
                
            if (district != null)
                parts.Add(district.Text);
                
            if (province != null)
                parts.Add(province.Text);

            return string.Join(", ", parts);
        }


    }
}
