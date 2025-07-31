/**
 * PawVerse Admin v2.0
 * Custom Admin JavaScript
 */

document.addEventListener('DOMContentLoaded', function() {
    // Toggle sidebar on mobile
    const sidebarToggleMobile = document.querySelector('.paw-admin-sidebar-toggle-mobile');
    const sidebar = document.querySelector('.paw-admin-sidebar');
    const sidebarToggle = document.querySelector('.paw-admin-sidebar-toggle');
    
    if (sidebarToggleMobile) {
        sidebarToggleMobile.addEventListener('click', function(e) {
            e.preventDefault();
            sidebar.classList.toggle('show');
            document.body.classList.toggle('paw-admin-sidebar-show');
        });
    }
    
    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', function(e) {
            e.preventDefault();
            sidebar.classList.remove('show');
            document.body.classList.remove('paw-admin-sidebar-show');
        });
    }
    
    // Close sidebar when clicking outside on mobile
    document.addEventListener('click', function(e) {
        if (window.innerWidth < 1200) {
            const isClickInsideSidebar = sidebar.contains(e.target);
            const isClickOnToggle = sidebarToggleMobile && (sidebarToggleMobile === e.target || sidebarToggleMobile.contains(e.target));
            
            if (!isClickInsideSidebar && !isClickOnToggle && sidebar.classList.contains('show')) {
                sidebar.classList.remove('show');
                document.body.classList.remove('paw-admin-sidebar-show');
            }
        }
    });
    
    // Handle user dropdown
    const userDropdowns = document.querySelectorAll('.paw-admin-user-dropdown');
    
    userDropdowns.forEach(function(dropdown) {
        const button = dropdown.querySelector('.paw-admin-user-btn');
        const menu = dropdown.querySelector('.paw-admin-user-menu');
        
        if (button && menu) {
            button.addEventListener('click', function(e) {
                e.stopPropagation();
                const isOpen = menu.style.opacity === '1';
                
                // Close all other dropdowns
                document.querySelectorAll('.paw-admin-user-menu').forEach(function(m) {
                    if (m !== menu) {
                        m.style.opacity = '0';
                        m.style.visibility = 'hidden';
                        m.style.transform = 'translateY(10px)';
                    }
                });
                
                // Toggle current dropdown
                if (isOpen) {
                    menu.style.opacity = '0';
                    menu.style.visibility = 'hidden';
                    menu.style.transform = 'translateY(10px)';
                } else {
                    menu.style.opacity = '1';
                    menu.style.visibility = 'visible';
                    menu.style.transform = 'translateY(0)';
                }
            });
        }
    });
    
    // Close dropdowns when clicking outside
    document.addEventListener('click', function() {
        document.querySelectorAll('.paw-admin-user-menu').forEach(function(menu) {
            menu.style.opacity = '0';
            menu.style.visibility = 'hidden';
            menu.style.transform = 'translateY(10px)';
        });
    });
    
    // Handle active nav items
    const currentPath = window.location.pathname;
    const navLinks = document.querySelectorAll('.paw-admin-nav-link');
    
    navLinks.forEach(function(link) {
        const href = link.getAttribute('href');
        if (href && currentPath.startsWith(href) && href !== '/') {
            link.parentElement.classList.add('active');
        } else {
            link.parentElement.classList.remove('active');
        }
    });
    
    // Initialize tooltips
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function(tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
    
    // Initialize popovers
    const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    popoverTriggerList.map(function(popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });
    
    // Auto-hide alerts after 5 seconds
    const alerts = document.querySelectorAll('.alert.alert-dismissible');
    alerts.forEach(function(alert) {
        setTimeout(function() {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, 5000);
    });
    
    // Enable dropdowns on hover for desktop
    if (window.innerWidth >= 992) {
        const dropdowns = document.querySelectorAll('.dropdown-hover');
        
        dropdowns.forEach(function(dropdown) {
            dropdown.addEventListener('mouseenter', function() {
                const menu = this.querySelector('.dropdown-menu');
                if (menu) {
                    menu.classList.add('show');
                }
            });
            
            dropdown.addEventListener('mouseleave', function() {
                const menu = this.querySelector('.dropdown-menu');
                if (menu) {
                    menu.classList.remove('show');
                }
            });
        });
    }
    
    // Handle file input preview
    const fileInputs = document.querySelectorAll('.paw-admin-file-input');
    
    fileInputs.forEach(function(input) {
        input.addEventListener('change', function() {
            const preview = document.querySelector(this.dataset.preview);
            if (preview && this.files && this.files[0]) {
                const reader = new FileReader();
                
                reader.onload = function(e) {
                    if (preview.tagName === 'IMG') {
                        preview.src = e.target.result;
                    } else {
                        preview.style.backgroundImage = `url(${e.target.result})`;
                    }
                    preview.style.display = 'block';
                };
                
                reader.readAsDataURL(this.files[0]);
            }
        });
    });
    
    // Handle form validation
    const forms = document.querySelectorAll('.needs-validation');
    
    Array.from(forms).forEach(function(form) {
        form.addEventListener('submit', function(event) {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            
            form.classList.add('was-validated');
        }, false);
    });
    
    // Handle data tables
    if (typeof $ !== 'undefined' && $.fn.DataTable) {
        $('.paw-admin-datatable').DataTable({
            responsive: true,
            language: {
                url: '//cdn.datatables.net/plug-ins/1.10.25/i18n/Vietnamese.json'
            },
            dom: "<'row'<'col-sm-12 col-md-6'l><'col-sm-12 col-md-6'f>>" +
                 "<'row'<'col-sm-12'tr>>" +
                 "<'row'<'col-sm-12 col-md-5'i><'col-sm-12 col-md-7'p>>",
            pageLength: 10,
            order: [[0, 'desc']]
        });
    }
    
    // Handle select2 if available
    if (typeof $ !== 'undefined' && $.fn.select2) {
        $('.paw-admin-select2').select2({
            theme: 'bootstrap-5',
            width: '100%',
            dropdownParent: $('body')
        });
    }
    
    // Handle summernote if available
    if (typeof $ !== 'undefined' && $.fn.summernote) {
        $('.paw-admin-summernote').summernote({
            height: 300,
            minHeight: 150,
            maxHeight: 600,
            focus: true,
            lang: 'vi-VN',
            toolbar: [
                ['style', ['style']],
                ['font', ['bold', 'underline', 'clear']],
                ['fontname', ['fontname']],
                ['color', ['color']],
                ['para', ['ul', 'ol', 'paragraph']],
                ['table', ['table']],
                ['insert', ['link', 'picture', 'video']],
                ['view', ['fullscreen', 'codeview', 'help']]
            ]
        });
    }
    
    // Handle date picker if available
    if (typeof $ !== 'undefined' && $.fn.datepicker) {
        $('.paw-admin-datepicker').datepicker({
            format: 'dd/mm/yyyy',
            autoclose: true,
            todayHighlight: true,
            language: 'vi',
            orientation: 'bottom auto'
        });
    }
    
    // Handle file manager if needed
    const fileManagerButtons = document.querySelectorAll('.paw-admin-file-manager-trigger');
    
    fileManagerButtons.forEach(function(button) {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            const targetInput = document.querySelector(this.dataset.target);
            const targetPreview = document.querySelector(this.dataset.preview);
            
            // This is a placeholder for file manager integration
            // You'll need to implement the actual file manager integration
            console.log('Open file manager for input:', targetInput);
            
            // Example of how to set the value and preview when a file is selected
            // This would be replaced with actual file manager callback
            if (targetInput && targetPreview) {
                // Simulate file selection
                setTimeout(() => {
                    const fileUrl = 'https://via.placeholder.com/800x600';
                    targetInput.value = fileUrl;
                    
                    if (targetPreview.tagName === 'IMG') {
                        targetPreview.src = fileUrl;
                    } else {
                        targetPreview.style.backgroundImage = `url(${fileUrl})`;
                    }
                    targetPreview.style.display = 'block';
                }, 500);
            }
        });
    });
});

// Add a global error handler
window.addEventListener('error', function(e) {
    console.error('Global error:', e.error);
    
    // You could show a nice error message to the user here
    // For example, using Bootstrap's toast or alert component
    if (typeof bootstrap !== 'undefined') {
        const toastEl = document.createElement('div');
        toastEl.className = 'toast align-items-center text-white bg-danger border-0 position-fixed bottom-0 end-0 m-3';
        toastEl.setAttribute('role', 'alert');
        toastEl.setAttribute('aria-live', 'assertive');
        toastEl.setAttribute('aria-atomic', 'true');
        toastEl.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">
                    <i class="fas fa-exclamation-circle me-2"></i>
                    Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau.
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        `;
        
        document.body.appendChild(toastEl);
        const toast = new bootstrap.Toast(toastEl);
        toast.show();
        
        // Remove the toast after it's hidden
        toastEl.addEventListener('hidden.bs.toast', function() {
            document.body.removeChild(toastEl);
        });
    }
});
