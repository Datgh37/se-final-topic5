/**
 * Cart Module Logic
 * Handles: Add to Cart, Update Quantity, Delete Item, and Notifications
 * 
 * Covers Use Cases: UC-CART-01, UC-CART-02, UC-CART-03
 * Covers Test Cases: TC_CART_01 → TC_CART_09
 */

// ============================================================
// 1. Global Toast Notification
// ============================================================
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

// ============================================================
// 2. Custom Confirm Modal (UC-CART-03: Xóa sản phẩm)
// ============================================================
function showConfirm(message, onConfirm, onCancel) {
    if ($('#custom-confirm-modal').length === 0) {
        $('body').append(`
            <div class="custom-modal-overlay" id="custom-confirm-modal">
                <div class="custom-modal">
                    <div class="custom-modal-icon">
                        <i class="fa fa-exclamation-triangle"></i>
                    </div>
                    <h3 class="custom-modal-title">Xác nhận</h3>
                    <p class="custom-modal-message"></p>
                    <div class="modal-btns">
                        <button class="modal-btn btn-cancel">
                            <i class="fa fa-times mr-1"></i> Hủy bỏ
                        </button>
                        <button class="modal-btn btn-confirm">
                            <i class="fa fa-check mr-1"></i> Đồng ý
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

// ============================================================
// 3. Add to Cart Logic (UC-CART-01)
//    TC_CART_01: Thêm thành công
//    TC_CART_02: Cộng dồn nếu đã tồn tại
//    TC_CART_03: Chặn nếu vượt tồn kho
// ============================================================
function addToCart(productId, quantity = 1) {
    $.ajax({
        url: "/Cart/AddToCart",
        type: "POST",
        data: { productId, quantity },
        success: function(response) {
            if (response.success) {
                reloadCartPreview();
                showToast(response.message, 'success');
                // TC_CART_09: Phát tín hiệu cho các tab khác biết giỏ hàng đã thay đổi
                notifyCartChanged();
            } else {
                showToast(response.message, 'error');
            }
        },
        error: function() {
            showToast("Lỗi kết nối máy chủ", "error");
        }
    });
}

// ============================================================
// 4. Update Cart Quantity (UC-CART-02)
//    TC_CART_04: Cập nhật hợp lệ → tính lại Total
//    TC_CART_05: Nhập sai → reset về 1
//    TC_CART_06: Vượt tồn kho → set max
// ============================================================
function updateCartQuantity(cartItemId, quantity, row) {
    $.ajax({
        url: "/Cart/UpdateQuantity",
        type: "POST",
        data: { cartItemId, quantity },
        success: function(response) {
            if (response.success) {
                // TC_CART_04: Cập nhật thành công
                if (row && row.length) {
                    row.find(".qty-input").val(response.quantity);
                    row.find(".subtotal").text(response.subtotal + " ₫");
                }
                
                updateGlobalTotals(response.grandTotal, response.totalItems);
                reloadCartPreview();
            } else {
                showToast(response.message, 'error');
                
                if (row && row.length) {
                    // TC_CART_05: Server trả resetQty → reset input về 1
                    if (response.resetQty) {
                        row.find(".qty-input").val(response.resetQty);
                        // Re-sync server với giá trị reset
                        updateCartQuantity(cartItemId, response.resetQty, row);
                    }
                    // TC_CART_06: Server trả maxStock → set input về max
                    else if (response.maxStock) {
                        row.find(".qty-input").val(response.maxStock);
                        // Re-sync server với giá trị max
                        updateCartQuantity(cartItemId, response.maxStock, row);
                    }
                }
            }
        },
        error: function() {
            showToast("Không thể cập nhật số lượng", "error");
        }
    });
}

// ============================================================
// 5. Delete Cart Item (UC-CART-03)
//    TC_CART_07: Xóa 1 sản phẩm → trừ tiền, giảm badge
//    TC_CART_08: Xóa SP cuối → hiện "Giỏ hàng trống"
// ============================================================
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
                            // TC_CART_08: Nếu không còn sản phẩm → reload trang hiện trạng thái trống
                            if ($(".cart-row").length === 0) location.reload(); 
                        });
                    }
                    updateGlobalTotals(response.grandTotal, response.totalItems);
                    reloadCartPreview();
                    notifyCartChanged(); // TC_CART_09: phát tín hiệu cho tab khác
                    if (!silent) {
                        showToast("Đã xóa sản phẩm khỏi giỏ hàng", "success");
                    }
                }
            }
        });
    };

    if (silent) {
        performDelete();
    } else {
        showConfirm("Bạn có chắc chắn muốn xóa sản phẩm này?", performDelete, onCancel);
    }
}

// ============================================================
// 6. Reload Cart Preview HTML (TC_CART_09: Badge AJAX)
// ============================================================
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

// ============================================================
// TC_CART_09: Cross-Tab Cart Sync
// Khi tab khác thêm/xóa sản phẩm, tab hiện tại tự reload badge
// Cơ chế: localStorage event (broadcast cross-tab) + visibilitychange
// ============================================================
function notifyCartChanged() {
    // Ghi timestamp vào localStorage để các tab khác nhận được sự kiện 'storage'
    localStorage.setItem('cart_updated', Date.now().toString());
}

// Lắng nghe tín hiệu từ tab khác (cross-tab sync)
window.addEventListener('storage', function(e) {
    if (e.key === 'cart_updated') {
        reloadCartPreview();
    }
});

// Khi người dùng quay lại tab này (từ tab khác) → reload badge ngay
document.addEventListener('visibilitychange', function() {
    if (document.visibilityState === 'visible') {
        reloadCartPreview();
    }
});

// ============================================================
// 7. Update Totals in Cart Index Page
// ============================================================
function updateGlobalTotals(grandTotal, totalItems) {
    // Update main checkout box
    $("#grand-total").text(grandTotal + " ₫");
    $("#subtotal-display").text(grandTotal + " ₫");
    
    // Fallback selectors
    $(".shoping__checkout ul li:contains('Tạm tính') span").text(grandTotal + " ₫");
    $(".shoping__checkout ul li:contains('Tổng cộng') span").text(grandTotal + " ₫");
    
    // Update cart icon badge
    $(".cart-count").text(totalItems);
    
    // Update hamburger total price text dynamically
    $(".header__cart__price span").text(grandTotal + " ₫");
}

// ============================================================
// 8. Validate Quantity Input (TC_CART_05: số âm, 0, chữ cái)
// ============================================================
function validateQtyInput(value) {
    const parsed = parseInt(value);
    if (isNaN(parsed) || parsed <= 0) {
        return { valid: false, value: 1 };
    }
    return { valid: true, value: parsed };
}

// ============================================================
// 9. Event Listeners
// ============================================================
$(document).ready(function() {
    // Flag to track unsaved quantity changes in the cart
    let isCartDirty = false;
    // Flag to track if the last update had errors (prevent checkout)
    let hasCartError = false;

    // Sync hamburger total price text on initial load
    const initialTotal = $(".cart-dropdown__total strong").first().text();
    if (initialTotal) {
        $(".header__cart__price span").text(initialTotal);
    }

    // ----------------------------------------------------------
    // 9.1 Add to Cart (UC-CART-01)
    // ----------------------------------------------------------
    $(document).on("click", ".add-to-cart-btn, .add-to-cart", function(e) {
        e.preventDefault();
        const $btn = $(this);
        
        if ($btn.hasClass('btn-disabled') || $btn.is(':disabled')) {
            showToast("Sản phẩm hiện đã hết hàng", "error");
            return false;
        }

        const productId = $btn.data("product-id");
        const quantity = parseInt($("#productQuantity").val()) || 1;
        
        if (quantity <= 0) {
            showToast("Số lượng không hợp lệ", "error");
            return false;
        }
        
        addToCart(productId, quantity);
    });

    // ----------------------------------------------------------
    // 9.2 Cart Page: +/- Button Quantity Controls (UC-CART-02)
    // ----------------------------------------------------------
    $(document).on("click", ".pro-qty-cart .qtybtn", function(e) {
        e.preventDefault();
        e.stopPropagation();

        const $button = $(this);
        const $row = $button.closest(".cart-row");
        const $input = $row.find(".qty-input");
        
        let currentVal = parseInt($input.val()) || 0;
        let newVal = currentVal;

        if ($button.hasClass('inc')) {
            newVal = currentVal + 1;
        } else {
            newVal = currentVal - 1;
        }

        $input.val(newVal);
        isCartDirty = true; // Mark cart as having unsaved changes

        // Cập nhật thành tiền dòng sản phẩm ở Client-side để giao diện phản hồi tức thì
        const priceText = $row.find(".shoping__cart__price").text();
        const unitPrice = parseFloat(priceText.replace(/[^0-9]/g, ''));
        const rowSubtotal = unitPrice * newVal;
        $row.find(".subtotal").text(rowSubtotal.toLocaleString('vi-VN') + " ₫");
    });

    // Chỉ cho phép nhập số (0-9) và tối đa một dấu trừ ở đầu
    $(document).on("input", ".pro-qty-cart .qty-input", function() {
        this.value = this.value.replace(/(?!^-)[^0-9]/g, '');
    });

    // ----------------------------------------------------------
    // 9.3 Cart Page: Direct Input (TC_CART_04, TC_CART_05)
    //     Khách hàng gõ trực tiếp số lượng mới vào ô input
    // ----------------------------------------------------------
    $(document).on("change blur", ".pro-qty-cart .qty-input", function() {
        const $input = $(this);
        const $row = $input.closest(".cart-row");
        
        let qty = parseInt($input.val());
        if (isNaN(qty)) {
            qty = 0;
        }

        $input.val($input.val()); // Keep what the user typed (e.g. empty or negative number)
        isCartDirty = true; // Mark cart as having unsaved changes

        // Cập nhật thành tiền dòng sản phẩm ở Client-side
        const priceText = $row.find(".shoping__cart__price").text();
        const unitPrice = parseFloat(priceText.replace(/[^0-9]/g, ''));
        const rowSubtotal = unitPrice * qty;
        $row.find(".subtotal").text(rowSubtotal.toLocaleString('vi-VN') + " ₫");
    });

    // ----------------------------------------------------------
    // 9.4 Cart Page: "CẬP NHẬT GIỎ HÀNG" Button (TC_CART_04)
    //     Thực hiện đồng bộ dữ liệu giỏ hàng lên database thông qua AJAX
    // ----------------------------------------------------------
    $(document).on("click", "#btn-update-cart", function(e) {
        e.preventDefault();
        hasCartError = false; // Reset error flag
        let hasError = false;
        let updateCount = 0;
        const totalRows = $(".cart-row").length;
        
        if (totalRows === 0) {
            isCartDirty = false;
            return;
        }

        $(".cart-row").each(function() {
            const $row = $(this);
            const $input = $row.find(".qty-input");
            const cartItemId = $row.data("cart-item-id");
            
            // Client-side validation: Chỉ chuyển đổi về số nguyên thô, không tự tiện chặn số âm/không hợp lệ
            let qty = parseInt($input.val());
            if (isNaN(qty)) {
                qty = 1;
            }

            // Gọi AJAX cập nhật lên Server để thực thi các nghiệp vụ kiểm thử
            $.ajax({
                url: "/Cart/UpdateQuantity",
                type: "POST",
                data: { cartItemId, quantity: qty },
                success: function(response) {
                    updateCount++;
                    
                    if (response.success) {
                        // ========================================================
                        // [TEST CASE: TC_CART_04]
                        // Nghiệp vụ: Cập nhật số lượng sản phẩm hợp lệ thành công.
                        // Hoạt động: Cập nhật lại số lượng và cột Thành tiền (subtotal)
                        //            của sản phẩm đó trên giao diện từ Server trả về.
                        // ========================================================
                        $row.find(".qty-input").val(response.quantity);
                        $row.find(".subtotal").text(response.subtotal + " ₫");
                        
                        // Khi đã đồng bộ xong dòng cuối cùng
                        if (updateCount === totalRows) {
                            isCartDirty = false; // Tắt trạng thái chưa lưu
                            updateGlobalTotals(response.grandTotal, response.totalItems);
                            reloadCartPreview();
                            
                            // Nếu tất cả cập nhật thành công không có lỗi
                            if (!hasError) {
                                showToast("Giỏ hàng đã được cập nhật thành công", "success");
                            }
                        }
                    } else {
                        // ========================================================
                        // [TEST CASE: TC_CART_05 & TC_CART_06]
                        // Nghiệp vụ: Phát hiện lỗi kiểm thực từ Server và khôi phục giao diện.
                        // ========================================================
                        showToast(response.message, 'error');
                        hasError = true;
                        hasCartError = true;

                        // [TC_CART_05] Server phát hiện số lượng âm/không hợp lệ -> trả về resetQty = 1
                        if (response.resetQty) {
                            $input.val(response.resetQty);
                            updateCartQuantity(cartItemId, response.resetQty, $row); // Re-sync Server
                        } 
                        // [TC_CART_06] Server phát hiện số lượng vượt quá tồn kho thực tế -> trả về maxStock
                        else if (response.maxStock) {
                            $input.val(response.maxStock);
                            updateCartQuantity(cartItemId, response.maxStock, $row); // Re-sync Server
                        }

                        // Khi dòng cuối cùng hoàn tất xử lý (kể cả khi thất bại), đồng bộ lại toàn bộ giỏ hàng
                        if (updateCount === totalRows) {
                            isCartDirty = false;
                            // Đã loại bỏ location.reload() để UI mượt mà, updateCartQuantity phía trên sẽ tự lo việc hiển thị Total lại
                        }
                    }
                },
                error: function() {
                    updateCount++;
                    showToast("Không thể kết nối máy chủ để cập nhật sản phẩm", "error");
                }
            });
        });
    });

    // ----------------------------------------------------------
    // 9.5 Cart Page: Delete Button (UC-CART-03)
    // ----------------------------------------------------------
    $(document).on("click", ".delete-cart-item", function(e) {
        e.preventDefault();
        deleteCartItem($(this).data("cart-item-id"), $(this).closest(".cart-row"));
    });

    // ----------------------------------------------------------
    // 9.6 Preview Dropdown Quantity Controls
    // ----------------------------------------------------------
    $(document).on("click", ".cart-preview-dec", function(e) {
        e.preventDefault();
        const item = $(this).closest(".cart-dropdown__item");
        const cartItemId = item.data("cart-item-id");
        const countSpan = item.find(".cart-preview-count");
        let qty = parseInt(countSpan.text()) - 1;

        if (qty <= 0) {
            deleteCartItem(cartItemId, null, true);
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
            showToast(`Vượt quá số lượng tồn kho (${maxStock})`, "error");
            return;
        }

        countSpan.text(qty);
        updateCartQuantity(cartItemId, qty, $());
    });

    // ----------------------------------------------------------
    // 9.7 Safe Checkout: Auto-sync if there are unsaved changes
    // ----------------------------------------------------------
    $(document).on("click", "#btn-checkout", function(e) {
        if (isCartDirty) {
            e.preventDefault();
            e.stopPropagation();
            
            const checkoutUrl = $(this).attr("href");
            showToast("Đang tự động đồng bộ giỏ hàng của bạn...", "info");
            
            // Tự động kích hoạt sự kiện click của nút "CẬP NHẬT GIỎ HÀNG"
            $("#btn-update-cart").click();
            
            // Đợi AJAX đồng bộ hoàn tất (dirty flag về false) rồi mới chuyển hướng
            const checkInterval = setInterval(function() {
                if (!isCartDirty) {
                    clearInterval(checkInterval);
                    if (!hasCartError) {
                        window.location.href = checkoutUrl;
                    } else {
                        showToast("Vui lòng kiểm tra lại giỏ hàng trước khi thanh toán", "error");
                    }
                }
            }, 100);
        }
    });
});
