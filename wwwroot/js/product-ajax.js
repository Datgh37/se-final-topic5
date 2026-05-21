/**
 * Product AJAX Filtering, Pagination & View Mode Switcher
 */

$(document).ready(function () {
    const container = '#product-list-container';
    const paginationSelector = '.product__pagination a';

    let currentViewMode = 'grid';

    // 1. Global Preloader Safeguard – ẩn preloader khi có AJAX để tránh đơ màn hình
    $(document).ajaxStart(function () {
        $('#preloder').hide();
        $('.loader').hide();
    });

    // 2. Handle Pagination Clicks (AJAX)
    $(document).on('click', paginationSelector, function (e) {
        e.preventDefault();
        const url = $(this).attr('href');
        if (!url || url === '#' || url === '') return;
        loadProductList(url);
        window.history.pushState({ path: url }, '', url);
    });

    // 3. Handle Filter/Sort Form Submissions (AJAX)
    $(document).on('submit', '.price-filter-form, .filter__sort form', function (e) {
        e.preventDefault();
        const $form = $(this);
        const action = $form.attr('action') || '/Products';
        const formData = $form.serialize();
        const fullUrl = action + (action.includes('?') ? '&' : '?') + formData;
        loadProductList(fullUrl);
        window.history.pushState({ path: fullUrl }, '', fullUrl);
    });

    // 4. Main AJAX Loader – chuyển đổi URL sang /Products/Filter để lấy partial view
    function loadProductList(url) {
        // Chuyển đổi mọi dạng URL /Products... sang /Products/Filter...
        let ajaxUrl = url
            .replace(/\/Products\/Index(\?|$|#)/, '/Products/Filter$1')
            .replace(/\/Products(\?|$|#)/, '/Products/Filter$1');

        // Nếu URL không chứa /Filter sau khi replace, thêm vào
        if (!/\/Products\/Filter/.test(ajaxUrl)) {
            ajaxUrl = '/Products/Filter' + (ajaxUrl.includes('?') ? ajaxUrl.substring(ajaxUrl.indexOf('?')) : '');
        }

        $(container).css('opacity', '0.5');

        $.ajax({
            url: ajaxUrl,
            type: 'GET',
            success: function (result) {
                $(container).html(result).css('opacity', '1');
                applyCurrentViewMode();
                initializePlugins();
            },
            error: function () {
                $(container).css('opacity', '1');
                alert('Có lỗi xảy ra khi tải danh sách sản phẩm.');
            }
        });
    }

    // 5. View Mode Switcher (Grid / List)
    $(document).on('click', '.view-mode-btn', function () {
        const $btn = $(this);
        if ($btn.hasClass('active')) return;

        const mode = $btn.data('mode');
        const $partial = $('#product-list-partial');

        $partial.addClass('view-switching');
        setTimeout(function () {
            currentViewMode = mode;
            applyCurrentViewMode();
            $partial.removeClass('view-switching');
        }, 300);
    });

    function applyCurrentViewMode() {
        const $partial = $('#product-list-partial');
        $partial.removeClass('grid-view list-view').addClass(currentViewMode + '-view');
        $('.view-mode-btn').removeClass('active');
        $('.view-mode-btn[data-mode="' + currentViewMode + '"]').addClass('active');
    }

    // 6. Reinitialize plugins after AJAX load
    function initializePlugins() {
        $('.set-bg').each(function () {
            var bg = $(this).data('setbg');
            $(this).css('background-image', 'url(' + bg + ')');
        });
        if ($.fn.niceSelect) {
            $('select').niceSelect();
        }
    }

    // 7. Browser Back/Forward Navigation
    window.onpopstate = function (e) {
        if (e.state && e.state.path) {
            loadProductList(e.state.path);
        }
    };

    // Initial setup
    applyCurrentViewMode();
    initializePlugins();
});
