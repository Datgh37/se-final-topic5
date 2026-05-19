/**
 * Cart Module Logic
 * Handles: Add to Cart, Update Quantity, Delete Item, and Notifications
 */

// 1. Global Toast Notification
function showToast(message, type = 'info') {
    const container = document.getElementById('toast-container');
    if (!container) return;
    const toast = document.createElement('div');
    toast.className = `toast-msg toast-${type}`;
    toast.innerHTML = `<i class="fa ${type === 'success' ? 'fa-check-circle' : (type === 'error' ? 'fa-exclamation-circle' : 'fa-info-circle')} mr-2"></i> ${message}`;
    
    container.appendChild(toast);
    setTimeout(() => { 
        toast.style.opacity = '0';
        setTimeout(() => toast.remove(), 300); 
    }, 3000);
}

// 2. Custom Confirm Modal
function showConfirm(message, onConfirm, onCancel) {
    if ($('#custom-confirm-modal').length === 0) {
        $('body').append(`
            <div class="custom-modal-overlay" id="custom-confirm-modal">
                <div class="custom-modal">
                    <div class="custom-modal-icon">
                        <i class="fa fa-exclamation-triangle"></i>
                    </div>
                    <h3 class="custom-modal-title">${$('html').attr('lang') === 'en' ? 'Confirm' : 'Xác nhận'}</h3>
                    <p class="custom-modal-message"></p>
                    <div class="modal-btns">
                        <button class="modal-btn btn-cancel">
                            <i class="fa fa-times mr-1"></i> ${$('html').attr('lang') === 'en' ? 'Cancel' : 'Hủy bỏ'}
                        </button>
                        <button class="modal-btn btn-confirm">
                            <i class="fa fa-check mr-1"></i> ${$('html').attr('lang') === 'en' ? 'OK' : 'Đồng ý'}
                        </button>
                    </div>
                </div>
            </div>
        `);
    }

    const modal = $('#custom-confirm-modal');
    modal.find('.custom-modal-message').text(message);
    modal.css('display', 'flex');
    setTimeout(() => modal.find('.custom-modal').addClass('active'), 30);

    modal.find('.btn-confirm').off('click').on('click', function() {
        closeConfirmModal();
        if (onConfirm) onConfirm();
    });

    modal.find('.btn-cancel').off('click').on('click', function() {
        closeConfirmModal();
        if (onCancel) onCancel();
    });
}

function closeConfirmModal() {
    const modal = $('#custom-confirm-modal');
    modal.find('.custom-modal').removeClass('active');
    setTimeout(() => modal.css('display', 'none'), 250);
}

// 3. Add to Cart Logic
function addToCart(productId, quantity = 1) {
    $.ajax({
        url: "/Cart/AddToCart",
        type: "POST",
        data: { productId, quantity },
        success: function(response) {
            if (response.success) {
                reloadCartPreview();
                showToast(response.message, 'success');
            } else {
                showToast(response.message, 'error');
            }
        },
        error: function() {
            const isEn = $('html').attr('lang') === 'en';
            showToast(isEn ? "Server connection error" : "Lỗi kết nối máy chủ", "error");
        }
    });
}

// 4. Update Cart Quantity (Main Cart Page)
function updateCartQuantity(cartItemId, quantity, row) {
    $.ajax({
        url: "/Cart/UpdateQuantity",
        type: "POST",
        data: { cartItemId, quantity },
        success: function(response) {
            if (response.success) {
                // Update specific row elements
                if (row && row.length) {
                    row.find(".qty-input").val(response.quantity);
                    row.find(".subtotal").text(response.subtotal + " ₫");
                }
                
                // Update global totals
                updateGlobalTotals(response.grandTotal, response.totalItems);
                
                // Update header cart preview
                reloadCartPreview();
            } else if (response.message) {
                showToast(response.message, 'error');
                if (row && row.length && response.quantity) {
                    row.find(".qty-input").val(response.quantity);
                }
            }
        },
        error: function() {
            const isEn = $('html').attr('lang') === 'en';
            showToast(isEn ? "Cannot update quantity" : "Không thể cập nhật số lượng", "error");
        }
    });
}

// 5. Delete Cart Item
function deleteCartItem(cartItemId, row, silent = false, onCancel) {
    const performDelete = function() {
        $.ajax({
            url: "/Cart/DeleteCartItem",
            type: "POST",
            data: { cartItemId },
            success: function(response) {
                if (response.success) {
                    if (row) {
                        row.fadeOut(300, function() { 
                            $(this).remove(); 
                            if ($(".cart-row").length === 0) location.reload(); 
                        });
                    }
                    updateGlobalTotals(response.grandTotal, response.totalItems);
                    reloadCartPreview();
                    if (!silent) {
                        const isEn = $('html').attr('lang') === 'en';
                        showToast(isEn ? "Product removed from cart" : "Đã xóa sản phẩm khỏi giỏ hàng", "success");
                    }
                }
            }
        });
    };

    if (silent) {
        performDelete();
    } else {
        const isEn = $('html').attr('lang') === 'en';
        showConfirm(isEn ? "Are you sure you want to delete this product?" : "Bạn có chắc chắn muốn xóa sản phẩm này?", performDelete, onCancel);
    }
}

// 6. Reload Cart Preview HTML (Fix recursion)
function reloadCartPreview() {
    $.get("/Cart/GetCartPreview", function(html) {
        $(".cart-vc-content").html(html);
        const newCount = $(".cart-dropdown").data("total-count");
        if (newCount !== undefined) $(".cart-count").text(newCount);
        
        // Update hamburger total price text dynamically
        const newTotal = $(".cart-dropdown__total strong").first().text();
        if (newTotal) {
            $(".header__cart__price span").text(newTotal);
        } else {
            $(".header__cart__price span").text("0 ₫");
        }
    });
}

// 7. Update Totals in Cart Index
function updateGlobalTotals(grandTotal, totalItems) {
    // Update main checkout box
    $("#grand-total").text(grandTotal + " ₫");
    
    // Target the first <li> for subtotal (Tạm tính)
    $(".shoping__checkout ul li:contains('Tạm tính') span").text(grandTotal + " ₫");
    
    // Target the second <li> for total (Tổng cộng) - fallback if #grand-total is not enough
    $(".shoping__checkout ul li:contains('Tổng cộng') span").text(grandTotal + " ₫");
    
    // Update cart icon badge
    $(".cart-count").text(totalItems);
    
    // Update hamburger total price text dynamically
    $(".header__cart__price span").text(grandTotal + " ₫");
}

    // 8. Event Listeners
    $(document).ready(function() {
        // Sync hamburger total price text on initial load
        const initialTotal = $(".cart-dropdown__total strong").first().text();
        if (initialTotal) {
            $(".header__cart__price span").text(initialTotal);
        }

        // Add to Cart
        $(document).on("click", ".add-to-cart-btn, .add-to-cart", function(e) {
            e.preventDefault();
            const $btn = $(this);
            
            // Prevent adding to cart if out of stock
            const isEn = $('html').attr('lang') === 'en';
            if ($btn.hasClass('btn-disabled') || $btn.is(':disabled')) {
                showToast(isEn ? "Product is currently out of stock" : "Sản phẩm hiện đã hết hàng", "error");
                return false;
            }

            const productId = $btn.data("product-id");
            const quantity = parseInt($("#productQuantity").val()) || 1;
            
            if (quantity <= 0) {
                showToast(isEn ? "Invalid quantity" : "Số lượng không hợp lệ", "error");
                return false;
            }
            
            addToCart(productId, quantity);
        });

        // Main Cart Page Quantity Logic (Manual control for pro-qty-cart)
        $(document).on("click", ".pro-qty-cart .qtybtn", function(e) {
            e.preventDefault();
            e.stopPropagation();

            const $button = $(this);
            const $row = $button.closest(".cart-row");
            const $input = $row.find(".qty-input");
            const cartItemId = $row.data("cart-item-id");
            const maxStock = parseInt($button.parent().data("max-stock")) || 999;
            
            let currentVal = parseInt($input.val()) || 0;
            let newVal = currentVal;

            if ($button.hasClass('inc')) {
                newVal = currentVal + 1;
            } else {
                newVal = currentVal - 1;
            }

            // Validation
            if (newVal > maxStock) {
                const isEn = $('html').attr('lang') === 'en';
                showToast(isEn ? `Only ${maxStock} items left in stock` : `Chỉ còn ${maxStock} sản phẩm trong kho`, "error");
                $input.val(maxStock);
                updateCartQuantity(cartItemId, maxStock, $row);
                return;
            }

            if (newVal <= 0) {
                $input.val(0); 
                deleteCartItem(cartItemId, $row, false, function() {
                    // ON CANCEL: Reset to 1 and sync
                    $input.val(1);
                    updateCartQuantity(cartItemId, 1, $row);
                });
            } else {
                $input.val(newVal);
                updateCartQuantity(cartItemId, newVal, $row);
            }
        });

        // Main Cart Page Delete
        $(document).on("click", ".delete-cart-item", function(e) {
            e.preventDefault();
            deleteCartItem($(this).data("cart-item-id"), $(this).closest(".cart-row"));
        });

    // --- Preview Dropdown Quantity Controls ---
    $(document).on("click", ".cart-preview-dec", function(e) {
        e.preventDefault();
        const item = $(this).closest(".cart-dropdown__item");
        const cartItemId = item.data("cart-item-id");
        const countSpan = item.find(".cart-preview-count");
        let qty = parseInt(countSpan.text()) - 1;

        if (qty <= 0) {
            deleteCartItem(cartItemId, null, true); // Auto delete silent
        } else {
            countSpan.text(qty);
            updateCartQuantity(cartItemId, qty, $()); 
        }
    });

    $(document).on("click", ".cart-preview-inc", function(e) {
        e.preventDefault();
        const item = $(this).closest(".cart-dropdown__item");
        const maxStock = parseInt(item.data("max-stock")) || 999;
        const cartItemId = item.data("cart-item-id");
        const countSpan = item.find(".cart-preview-count");
        let qty = parseInt(countSpan.text()) + 1;

        if (qty > maxStock) {
            const isEn = $('html').attr('lang') === 'en';
            showToast(isEn ? `Exceeds stock limit (${maxStock})` : `Vượt quá số lượng tồn kho (${maxStock})`, "error");
            return;
        }

        countSpan.text(qty);
        updateCartQuantity(cartItemId, qty, $());
    });
});
