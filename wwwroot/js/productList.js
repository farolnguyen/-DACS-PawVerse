// Xử lý sự kiện khi tài liệu đã tải xô
document.addEventListener('DOMContentLoaded', function() {
    // Xử lý click vào danh mục sản phẩm
    document.querySelectorAll('.tool-sidebar a[data-category-id]').forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            const categoryId = this.getAttribute('data-category-id') || '';
            
            // Tạo URL mới với categoryId
            const url = new URL(window.location);
            url.search = categoryId ? `?categoryId=${categoryId}` : '';
            
            // Chuyển hướng đến URL mới
            window.location.href = url.toString();
        });
    });

    // Xử lý click vào danh sách yêu thích
    const wishlistFilter = document.getElementById('wishlistFilter');
    if (wishlistFilter) {
        wishlistFilter.addEventListener('click', function(e) {
            e.preventDefault();
            
            // Kiểm tra đăng nhập
            const isAuthenticated = document.body.getAttribute('data-is-authenticated') === 'true';
            if (!isAuthenticated) {
                window.location.href = '/Identity/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
                return;
            }
            
            // Tạo URL mới với tham số showWishlist
            const url = new URL(window.location);
            url.search = '?showWishlist=true';
            
            // Chuyển hướng đến URL mới
            window.location.href = url.toString();
        });
        
        // Kiểm tra nếu đang ở chế độ xem danh sách yêu thích
        const urlParams = new URLSearchParams(window.location.search);
        if (urlParams.get('showWishlist') === 'true') {
            // Kích hoạt trạng thái active cho nút danh sách yêu thích
            document.querySelectorAll('.tool-sidebar a').forEach(a => a.classList.remove('active-cat'));
            wishlistFilter.classList.add('active-cat');
            document.getElementById('categoryTitle').textContent = 'Sản phẩm yêu thích';
            
            // Gọi hàm filterProducts
            filterProducts(null, null, null, null, true);
        }
    }
    
    // Hàm lọc sản phẩm
    function filterProducts(categoryId, brand, priceRange, sortBy, showWishlist) {
        // Hiển thị loading
        const productGrid = document.getElementById('productGrid');
        if (!productGrid) return;
        
        productGrid.innerHTML = `
            <div class="text-center py-5">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
            </div>`;
        
        // Tạo URL với các tham số
        const url = new URL(window.location.href.split('?')[0]);
        const params = new URLSearchParams();
        
        if (categoryId) params.append('categoryId', categoryId);
        if (brand) params.append('brand', brand);
        if (priceRange) params.append('priceRange', priceRange);
        if (sortBy) params.append('sortBy', sortBy);
        if (showWishlist) params.append('showWishlist', 'true');
        
        // Gửi yêu cầu AJAX
        $.ajax({
            url: url.pathname,
            type: 'GET',
            data: params.toString(),
            success: function(response) {
                // Cập nhật nội dung
                const parser = new DOMParser();
                const doc = parser.parseFromString(response, 'text/html');
                const newContent = $(doc).find('#productGrid').html();
                
                if (newContent) {
                    productGrid.innerHTML = newContent;
                } else {
                    productGrid.innerHTML = '<p class="text-muted">Bạn chưa có sản phẩm nào trong danh sách yêu thích.</p>';
                }
            },
            error: function(xhr, status, error) {
                console.error('Lỗi khi tải danh sách sản phẩm:', error);
                productGrid.innerHTML = '<p class="text-danger">Đã xảy ra lỗi khi tải danh sách sản phẩm. Vui lòng thử lại sau.</p>';
            }
        });
    }
});
