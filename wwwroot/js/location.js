// location.js - Xử lý chọn tỉnh/huyện/xã

// Global function to initialize location selects
window.initializeLocationSelects = function(options) {
    const {
        provinceSelector,
        districtSelector,
        wardSelector,
        apiBaseUrl = '/api/locations',
        onProvinceChange = () => {},
        onDistrictChange = () => {},
        onWardChange = () => {}
    } = options;

    const $province = $(provinceSelector);
    const $district = $(districtSelector);
    const $ward = $(wardSelector);

    // Initialize Select2 for province if not already initialized
    if (!$province.hasClass('select2-hidden-accessible')) {
        $province.select2({
            theme: 'bootstrap-5',
            width: '100%',
            placeholder: $province.data('placeholder') || 'Chọn tỉnh/thành phố',
            allowClear: true
        });
    }

    // Initialize Select2 for district if not already initialized
    if (!$district.hasClass('select2-hidden-accessible')) {
        $district.select2({
            theme: 'bootstrap-5',
            width: '100%',
            placeholder: $district.data('placeholder') || 'Chọn quận/huyện',
            allowClear: true,
            disabled: !$province.val()
        });
    }

    // Initialize Select2 for ward if not already initialized
    if (!$ward.hasClass('select2-hidden-accessible')) {
        $ward.select2({
            theme: 'bootstrap-5',
            width: '100%',
            placeholder: $ward.data('placeholder') || 'Chọn phường/xã',
            allowClear: true,
            disabled: !$district.val()
        });
    }

    // Load districts when province changes
    $province.off('change').on('change', function() {
        const $this = $(this);
        const provinceCode = $this.val();
        const provinceName = $this.find('option:selected').text();
        
        if (provinceCode) {
            $this.attr('data-selected-province', provinceCode);
            $this.next('.select2-container').find('.select2-selection__rendered')
                .attr('title', provinceName)
                .text(provinceName);
        }
        
        $district.val(null).trigger('change').prop('disabled', !provinceCode);
        $ward.val(null).trigger('change').prop('disabled', true);
        
        if (provinceCode) {
            loadDistricts(provinceCode);
            onProvinceChange(provinceCode);
        }
    });

    // Load wards when district changes
    $district.off('change').on('change', function() {
        const $this = $(this);
        const districtCode = $this.val();
        const districtName = $this.find('option:selected').text();
        
        if (districtCode) {
            $this.attr('data-selected-district', districtCode);
            $this.next('.select2-container').find('.select2-selection__rendered')
                .attr('title', districtName)
                .text(districtName);
        }
        
        $ward.val(null).trigger('change').prop('disabled', !districtCode);
        
        if (districtCode) {
            loadWards(districtCode);
            onDistrictChange(districtCode);
        }
    });

    // Handle ward change
    $ward.off('change').on('change', function() {
        const $this = $(this);
        const wardCode = $this.val();
        const wardName = $this.find('option:selected').text();
        
        if (wardCode) {
            $this.attr('data-selected-ward', wardCode);
            $this.next('.select2-container').find('.select2-selection__rendered')
                .attr('title', wardName)
                .text(wardName);
        }
        
        onWardChange(wardCode);
    });

    // Function to load districts
    function loadDistricts(provinceCode) {
        if (!provinceCode) return;
        
        const $loadingOption = $('<option>', { value: '', text: 'Đang tải quận/huyện...' });
        $district.empty().append($loadingOption).trigger('change');
        $district.prop('disabled', true);
        
        $.ajax({
            url: `${apiBaseUrl}/provinces/${provinceCode}/districts`,
            method: 'GET',
            success: function(districts) {
                const $defaultOption = $('<option>', { 
                    value: '', 
                    text: $district.data('placeholder') || 'Chọn quận/huyện'
                });
                
                const $options = [];
                districts.sort((a, b) => a.name.localeCompare(b.name));
                
                districts.forEach(district => {
                    $options.push(
                        $('<option>', {
                            value: district.code.toString(),
                            text: district.name,
                            'data-province-code': district.provinceCode.toString()
                        })
                    );
                });
                
                $district.empty()
                    .append($defaultOption)
                    .append($options)
                    .prop('disabled', false);
                
                $district.select2('destroy');
                $district.select2({
                    theme: 'bootstrap-5',
                    width: '100%',
                    placeholder: $district.data('placeholder') || 'Chọn quận/huyện',
                    allowClear: true
                });
                
                const selectedDistrictCode = $district.data('selected-district');
                $district.val(selectedDistrictCode);
                $district.trigger('change');
            },
            error: function(xhr, status, error) {
                console.error('Error loading districts:', error);
                const $errorOption = $('<option>', { 
                    value: '', 
                    text: 'Lỗi khi tải quận/huyện'
                });
                $district.empty().append($errorOption).prop('disabled', false);
            }
        });
    }

    // Function to load wards
    function loadWards(districtCode) {
        if (!districtCode) return;
        
        const $loadingOption = $('<option>', { value: '', text: 'Đang tải phường/xã...' });
        $ward.empty().append($loadingOption).trigger('change');
        $ward.prop('disabled', true);
        
        $.ajax({
            url: `${apiBaseUrl}/districts/${districtCode}/wards`,
            method: 'GET',
            success: function(wards) {
                const $defaultOption = $('<option>', { 
                    value: '', 
                    text: $ward.data('placeholder') || 'Chọn phường/xã'
                });
                
                const $options = [];
                wards.sort((a, b) => a.name.localeCompare(b.name));
                
                wards.forEach(ward => {
                    $options.push(
                        $('<option>', {
                            value: ward.code.toString(),
                            text: ward.name,
                            'data-district-code': ward.districtCode.toString()
                        })
                    );
                });
                
                $ward.empty()
                    .append($defaultOption)
                    .append($options)
                    .prop('disabled', false);
                
                $ward.select2('destroy');
                $ward.select2({
                    theme: 'bootstrap-5',
                    width: '100%',
                    placeholder: $ward.data('placeholder') || 'Chọn phường/xã',
                    allowClear: true
                });
                
                const selectedWardCode = $ward.data('selected-ward');
                $ward.val(selectedWardCode).trigger('change');
            },
            error: function(xhr, status, error) {
                console.error('Error loading wards:', error);
                const $errorOption = $('<option>', { 
                    value: '', 
                    text: 'Lỗi khi tải phường/xã'
                });
                $ward.empty().append($errorOption).prop('disabled', false);
            }
        });
    }

    // Initial trigger on page load if a province is already selected
    const initialProvinceCode = $province.val();
    if (initialProvinceCode) {
        $province.trigger('change');
    }
};

// Hàm hiển thị thông báo lỗi
function showError(message) {
    // Sử dụng thư viện thông báo nếu có (như Toastr, SweetAlert2)
    if (typeof toastr !== 'undefined') {
        toastr.error(message);
    } else if (typeof Swal !== 'undefined') {
        Swal.fire('Lỗi', message, 'error');
    } else {
        alert(message);
    }
}
