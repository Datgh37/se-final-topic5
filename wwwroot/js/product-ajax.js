/**
 * Product AJAX Filtering, Pagination & View Mode Switcher
 */

$(document).ready(function () {
    const container = '#product-list-container';
    const paginationSelector = '.product__pagination a';
    const filterFormSelector = '.price-filter-form, .filter__sort form';
    
    let currentViewMode = 'grid'; // Default to grid every time page loads

    // 1. Global Preloader Safeguard
    $(document).ajaxStart(function() {
        $("#preloder").hide(); // Force hide preloader during AJAX
        $(".loader").hide();
    });

    // 2. Handle Pagination Clicks
    $(document).on('click', paginationSelector, function (e) {
        e.preventDefault();
        const url = $(this).attr('href');
        if (!url || url === '#' || url === '') return;
        loadProductList(url);
        window.history.pushState({ path: url }, '', url);
    });

    // 3. Handle Filter/Sort Form Submissions (AJAX)
    $(document).on('submit', filterFormSelector, function (e) {
        e.preventDefault();
        const $form = $(this);
        const url = $form.attr('action');
        const formData = $form.serialize();
        const fullUrl = url + (url.includes('?') ? '&' : '?') + formData;
        loadProductList(fullUrl);
        window.history.pushState({ path: fullUrl }, '', fullUrl);
    });

    // 4. Main AJAX Loader
    function loadProductList(url) {
        let ajaxUrl = url.replace('/Products/Index', '/Products/Filter')
                           .replace('/Products?', '/Products/Filter?')
                           .replace('/Products#', '/Products/Filter#');
        
        $(container).css('opacity', '0.5');
        
        // LOG debug dau vao truoc khi goi API
        AppDebugger.info("Bat dau goi AJAX tai/loc danh sach san pham", { 
            originalUrl: url, 
            ajaxUrl: ajaxUrl 
        });
        
        $.ajax({
            url: ajaxUrl,
            type: 'GET',
            success: function (result) {
                // LOG debug thanh cong khi nhan phan hoi
                AppDebugger.api(ajaxUrl, 'GET', { originalUrl: url }, { 
                    status: "Thanh cong", 
                    bytesReceived: result.length,
                    message: "HTML partial loaded successfully"
                }, 200);

                $(container).html(result);
                $(container).css('opacity', '1');
                applyCurrentViewMode();
                initializePlugins();
            },
            error: function (xhr, status, error) {
                // LOG debug that bai va bat exception
                AppDebugger.error("AJAX loadProductList gap loi", { 
                    status: status, 
                    error: error, 
                    statusCode: xhr.status,
                    responseText: xhr.responseText
                });

                $(container).css('opacity', '1');
                alert('Có lỗi xảy ra khi tải danh sách sản phẩm.');
            }
        });
    }

    // 5. View Mode Switcher with Animation
    $(document).on('click', '.view-mode-btn', function () {
        const $btn = $(this);
        if ($btn.hasClass('active')) return;

        const mode = $btn.data('mode');
        const $partial = $('#product-list-partial');

        // Start Transition Animation
        $partial.addClass('view-switching');

        setTimeout(() => {
            currentViewMode = mode; // Save to variable instead of localStorage
            applyCurrentViewMode();
            $partial.removeClass('view-switching');
        }, 300);
    });

    function applyCurrentViewMode() {
        const mode = currentViewMode; // Use the local variable
        const $partial = $('#product-list-partial');
        
        $partial.removeClass('grid-view list-view').addClass(mode + '-view');
        $('.view-mode-btn').removeClass('active');
        $('.view-mode-btn[data-mode="' + mode + '"]').addClass('active');
    }

    // 6. Helpers
    function initializePlugins() {
        $('.set-bg').each(function () {
            var bg = $(this).data('setbg');
            $(this).css('background-image', 'url(' + bg + ')');
        });
        if ($.fn.niceSelect) {
            $('select').niceSelect();
        }
    }

    window.onpopstate = function (e) {
        if (e.state && e.state.path) {
            loadProductList(e.state.path);
        }
    };

    // Initial setup
    applyCurrentViewMode();
    initializePlugins();
});
