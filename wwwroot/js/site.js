$(document).ready(function () {
    // 1. Tự động đóng các alert có class .auto-hide sau 5 giây
    setTimeout(function() {
        $(".alert.auto-hide").fadeOut(500, function() {
            $(this).remove();
        });
    }, 5000);
    
    // Global AJAX setup for Anti-Forgery Token
    $.ajaxSetup({
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        }
    });

    // 2. Hiện/Ẩn mật khẩu toàn cục
    $(document).on('click', '.password-toggle', function() {
        const input = $(this).siblings('input');
        const type = input.attr('type') === 'password' ? 'text' : 'password';
        input.attr('type', type);
        $(this).toggleClass('fa-eye fa-eye-slash');
    });

    // 3. Live Search Logic
    let searchTimeout = null;
    const $searchInput = $('#heroSearchInput');
    const $resultsDropdown = $('#liveSearchResults');

    if ($searchInput.length) {
        $searchInput.on('input', function() {
            const keyword = $(this).val().trim();
            
            clearTimeout(searchTimeout);
            
            if (keyword.length < 2) {
                $resultsDropdown.hide().empty();
                return;
            }

            searchTimeout = setTimeout(function() {
                $.ajax({
                    url: '/Products/LiveSearch',
                    type: 'GET',
                    data: { keyword: keyword },
                    success: function(data) {
                        if (data && data.length > 0) {
                            const isEn = $('html').attr('lang') === 'en';
                            let html = `<div class="live-search-header">${isEn ? 'Search Results' : 'Kết quả tìm kiếm'}</div>`;
                            html += '<div class="live-search-results">';
                            data.forEach(item => {
                                const finalPrice = item.discount > 0 
                                    ? (item.unitPrice * (1 - item.discount / 100)) 
                                    : item.unitPrice;
                                
                                html += `
                                    <a href="/Products/Details/${item.productId}" class="live-search-item">
                                        <img src="${item.imageUrl.replace('~/', '/')}" class="live-search-img" />
                                        <div class="live-search-info">
                                            <span class="live-search-name">${item.productName}</span>
                                            <div class="live-search-price-container">
                                                <span class="live-search-price">${finalPrice.toLocaleString('vi-VN')} ₫</span>
                                                ${item.discount > 0 ? `<span class="live-search-price-old">${item.unitPrice.toLocaleString('vi-VN')} ₫</span>` : ''}
                                            </div>
                                        </div>
                                    </a>`;
                            });
                            html += '</div>';
                            html += `
                                <div class="live-search-footer">
                                    <a href="/Products?keyword=${encodeURIComponent(keyword)}" class="live-search-view-all">${isEn ? 'View all results' : 'Xem tất cả kết quả'}</a>
                                </div>`;
                            $resultsDropdown.html(html).fadeIn(200);
                        } else {
                            $resultsDropdown.hide().empty();
                        }
                    }
                });
            }, 300); // Debounce 300ms
        });

        // Hide dropdown when clicking outside
        $(document).on('click', function(e) {
            if (!$(e.target).closest('.search-input-wrapper').length) {
                $resultsDropdown.hide();
            }
        });

        $searchInput.on('focus', function() {
            if ($resultsDropdown.children().length > 0) {
                $resultsDropdown.show();
            }
        });
    }

    // 4. Newsletter AJAX logic
    $('.footer__widget form').on('submit', function(e) {
        e.preventDefault();
        const email = $(this).find('input').val().trim();
        const isEn = $('html').attr('lang') === 'en';
        if (!email) {
            showToast(isEn ? "Please enter your email." : "Vui lòng nhập email.", "error");
            return;
        }
        // Giả lập gửi newsletter thành công vì chưa có Controller/Action cụ thể
        showToast(isEn ? "Thank you for subscribing!" : "Cảm ơn bạn đã đăng ký nhận tin!", "success");
        $(this).find('input').val('');
    });

    // 5. Toggle Wishlist Logic
    $(document).on('click', '.toggle-favorite', function(e) {
        e.preventDefault();
        const $btn = $(this);
        const productId = $btn.data('product-id');

        $.ajax({
            url: '/Products/ToggleFavorite',
            type: 'POST',
            data: { productId: productId },
            success: function(response) {
                if (response.success) {
                    showToast(response.message, 'success');
                    
                    // Cập nhật số lượng trên header
                    $('#favoriteCount, #favoriteCountMobile').text(response.totalCount);
                    
                    // Cập nhật UI icon (nếu là trang Details)
                    if ($btn.hasClass('heart-icon')) {
                        const icon = $btn.find('span');
                        if (response.isAdded) {
                            icon.removeClass('icon_heart_alt').addClass('icon_heart');
                            $btn.addClass('active');
                        } else {
                            icon.removeClass('icon_heart').addClass('icon_heart_alt');
                            $btn.removeClass('active');
                        }
                    } else {
                        // Nếu là ở Product Card
                        const icon = $btn.find('i');
                        if (response.isAdded) {
                            icon.addClass('is-favorite');
                        } else {
                            icon.removeClass('is-favorite');
                        }
                    }
                } else {
                    showToast(response.message, 'error');
                }
            },
            error: function() {
                const isEn = $('html').attr('lang') === 'en';
                showToast(isEn ? "Please login to perform this action." : "Vui lòng đăng nhập để thực hiện chức năng này.", "error");
            }
        });
    });

    // Cập nhật số lượng yêu thích khi load trang
    function updateFavCount() {
        $.get('/Products/GetFavoriteCount', function(count) {
            $('#favoriteCount, #favoriteCountMobile').text(count);
        });
    }
    updateFavCount();

    // 6. Xử lý dọn dẹp URL tìm kiếm (Clean URL)
    $(document).on('submit', '.hero-search-container', function (e) {
        const $form = $(this);
        const keyword = $('#heroSearchInput').val().trim();
        const seriesId = $form.find('select[name="seriesId"]').val();

        // Nếu cả 2 đều trống, chuyển hướng về trang Products sạch
        if (!keyword && !seriesId) {
            e.preventDefault();
            window.location.href = $form.attr('action') || '/Products';
        }
        // Nếu từ khóa trống nhưng có chọn Series, dọn dẹp để URL chỉ còn seriesId
        else if (!keyword && seriesId) {
            e.preventDefault();
            window.location.href = '/Products?seriesId=' + seriesId;
        }
    });

    // 7. Toggle Hamburger Language Dropdown on Click (Mobile Accordion style)
    $(document).on('click', '.humberger__menu__widget .header__top__right__language', function (e) {
        // If clicking a link inside the dropdown list, let it navigate
        if ($(e.target).closest('ul').length > 0) {
            return;
        }
        
        e.preventDefault();
        e.stopPropagation();
        
        const $ul = $(this).find('ul');
        const isOpen = $ul.hasClass('show-lang-dropdown');
        
        if (isOpen) {
            $ul.removeClass('show-lang-dropdown');
        } else {
            $ul.addClass('show-lang-dropdown');
        }
    });

    // Close when clicking outside
    $(document).on('click', function (e) {
        if (!$(e.target).closest('.humberger__menu__widget .header__top__right__language').length) {
            $('.humberger__menu__widget .header__top__right__language ul').removeClass('show-lang-dropdown');
        }
    });

});
