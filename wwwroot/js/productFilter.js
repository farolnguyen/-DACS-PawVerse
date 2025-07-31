// Xử lý filter và sắp xếp sản phẩm
document.addEventListener('DOMContentLoaded', function() {
    // Lấy các phần tử filter
    const brandFilter = document.getElementById('brandFilter');
    const priceFilter = document.getElementById('priceFilter');
    const sortBy = document.getElementById('sortBy');
    const categoryLinks = document.querySelectorAll('[data-category-id]');
    const wishlistFilter = document.getElementById('wishlistFilter');
    
    // Khởi tạo giá trị filter từ URL
    const urlParams = new URLSearchParams(window.location.search);
    const currentBrand = urlParams.get('brand') || '';
    const currentPriceRange = urlParams.get('priceRange') || '';
    const currentSort = urlParams.get('sortBy') || '';
    const currentCategoryId = urlParams.get('categoryId') || '';
    const currentShowWishlist = urlParams.get('showWishlist') === 'true';
    
    // Thiết lập giá trị ban đầu cho các filter
    if (brandFilter) brandFilter.value = currentBrand;
    if (priceFilter) priceFilter.value = currentPriceRange;
    if (sortBy) sortBy.value = currentSort;
    
    // Cập nhật trạng thái active cho danh mục
    updateActiveCategory(currentCategoryId, currentShowWishlist);
    
    // Xử lý sự kiện thay đổi filter thương hiệu
    if (brandFilter) {
        brandFilter.addEventListener('change', function() {
            updateFilters({ brand: this.value });
        });
    }
    
    // Xử lý sự kiện thay đổi filter giá
    if (priceFilter) {
        priceFilter.addEventListener('change', function() {
            updateFilters({ priceRange: this.value });
        });
    }
    
    // Xử lý sự kiện sắp xếp
    if (sortBy) {
        sortBy.addEventListener('change', function() {
            updateFilters({ sortBy: this.value });
        });
    }
    
    // Xử lý sự kiện click vào danh mục
    categoryLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            const categoryId = this.getAttribute('data-category-id') || '';
            updateFilters({ 
                categoryId: categoryId,
                brand: brandFilter ? brandFilter.value : '',
                priceRange: priceFilter ? priceFilter.value : '',
                sortBy: sortBy ? sortBy.value : ''
            });
        });
    });
    
    // Xử lý sự kiện click vào danh sách yêu thích
    if (wishlistFilter) {
        wishlistFilter.addEventListener('click', function(e) {
            e.preventDefault();
            updateFilters({ showWishlist: true });
        });
    }
    
    // Xử lý sự kiện back/forward của trình duyệt
    window.addEventListener('popstate', function() {
        updateProducts();
    });
    
    // Cập nhật trạng thái active cho danh mục
    function updateActiveCategory(activeCategoryId, showWishlist) {
        categoryLinks.forEach(link => {
            const linkId = link.getAttribute('data-category-id') || '';
            
            if (showWishlist) {
                link.classList.remove('active-cat');
                if (link.id === 'wishlistFilter') {
                    link.classList.add('active-cat');
                }
            } else if (linkId === activeCategoryId) {
                link.classList.add('active-cat');
            } else {
                link.classList.remove('active-cat');
            }
        });
    }
    
    // Cập nhật filter và URL
    function updateFilters(updates) {
        const urlParams = new URLSearchParams(window.location.search);
        
        // Cập nhật các tham số
        if ('brand' in updates) {
            if (updates.brand) {
                urlParams.set('brand', updates.brand);
            } else {
                urlParams.delete('brand');
            }
        }
        
        if ('priceRange' in updates) {
            if (updates.priceRange) {
                urlParams.set('priceRange', updates.priceRange);
            } else {
                urlParams.delete('priceRange');
            }
        }
        
        if ('sortBy' in updates) {
            if (updates.sortBy) {
                urlParams.set('sortBy', updates.sortBy);
            } else {
                urlParams.delete('sortBy');
            }
        }
        
        if ('categoryId' in updates) {
            if (updates.categoryId) {
                urlParams.set('categoryId', updates.categoryId);
            } else {
                urlParams.delete('categoryId');
            }
            // Xóa showWishlist khi chọn danh mục
            urlParams.delete('showWishlist');
        }
        
        if ('showWishlist' in updates && updates.showWishlist) {
            urlParams.set('showWishlist', 'true');
            // Xóa categoryId khi chọn danh sách yêu thích
            urlParams.delete('categoryId');
        }
        
        // Cập nhật URL và tải lại sản phẩm
        const newUrl = window.location.pathname + (urlParams.toString() ? '?' + urlParams.toString() : '');
        window.history.pushState({}, '', newUrl);
        
        // Cập nhật trạng thái active cho danh mục
        updateActiveCategory(
            urlParams.get('categoryId') || '', 
            urlParams.get('showWishlist') === 'true'
        );
        
        updateProducts();
    }
    
    // Hàm tải và cập nhật sản phẩm dựa trên filter hiện tại
    function updateProducts() {
        const productGrid = document.getElementById('productGrid');
        const categoryTitle = document.getElementById('categoryTitle');
        
        if (!productGrid) return;
        
        // Hiển thị trạng thái đang tải
        productGrid.innerHTML = '<div class="text-center py-5"><div class="spinner-border" role="status"><span class="visually-hidden">Đang tải...</span></div></div>';
        
        // Tạo URL với các tham số filter
        let url = '/Product/Index' + window.location.search;
        
        // Lấy dữ liệu đã lọc
        fetch(url)
            .then(response => {
                if (!response.ok) {
                    throw new Error('Lỗi tải dữ liệu');
                }
                return response.text();
            })
            .then(html => {
                // Tạo phần tử tạm để phân tích HTML
                const temp = document.createElement('div');
                temp.innerHTML = html;
                
                // Tìm phần tử product grid trong phản hồi
                const responseProductGrid = temp.querySelector('#productGrid');
                const responseCategoryTitle = temp.querySelector('#categoryTitle');
                
                // Cập nhật nội dung trang
                if (responseProductGrid) {
                    productGrid.innerHTML = responseProductGrid.innerHTML;
                }
                
                if (responseCategoryTitle && categoryTitle) {
                    categoryTitle.textContent = responseCategoryTitle.textContent;
                }
                
                // Cập nhật lại các sự kiện cho sản phẩm
                initializeProductGrid();
            })
            .catch(error => {
                console.error('Lỗi:', error);
                productGrid.innerHTML = '<div class="alert alert-danger">Có lỗi xảy ra khi tải dữ liệu. Vui lòng thử lại sau.</div>';
            });
    }
    
    // Khởi tạo các sự kiện cho sản phẩm
    function initializeProductGrid() {
        // Thêm mã khởi tạo nếu cần
    }
    
    // Khởi tạo ban đầu
    initializeProductGrid();
});
