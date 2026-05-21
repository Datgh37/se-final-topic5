// ===== REMOVE VIETNAMESE ACCENTS =====
function removeVietnameseTones(str) {
    return str
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .replace(/đ/g, "d")
        .replace(/Đ/g, "D")
        .toLowerCase();
}

const input = document.getElementById("provinceInput");
const dropdown = document.getElementById("provinceDropdown");
const options = document.querySelectorAll(".option");

// ===== OPEN DROPDOWN =====
if (input) {
    input.addEventListener("focus", () => {
        dropdown.classList.add("show");
    });
}

// ===== SEARCH FILTER =====
if (input) {
    input.addEventListener("input", () => {
        const keyword = removeVietnameseTones(input.value);

        options.forEach(option => {
            const text = removeVietnameseTones(option.innerText);

            if (text.includes(keyword)) {
                option.style.display = "block";
            } else {
                option.style.display = "none";
            }
        });
    });
}

// ===== CLICK SELECT =====
options.forEach(option => {
    option.addEventListener("click", () => {
        if (input) {
            input.value = option.innerText;
            dropdown.classList.remove("show");
            // Clear error when selected
            input.classList.remove("has-error");
            const errSpan = document.getElementById("error-provinceInput");
            if (errSpan) {
                errSpan.textContent = "";
                errSpan.style.display = "none";
            }
        }
    });
});

// ===== CLICK OUTSIDE CLOSE =====
document.addEventListener("click", (e) => {
    if (!e.target.closest(".custom-select")) {
        if (dropdown) dropdown.classList.remove("show");
    }
});

// ===== FORM VALIDATION & DOUBLE SUBMIT PREVENTION =====
document.addEventListener("DOMContentLoaded", function () {
    const form = document.querySelector("form");
    const btnSubmit = document.getElementById("btn-submit-order");

    const fields = {
        fullName: {
            input: document.getElementById("Checkout_FullName"),
            span: document.getElementById("error-Checkout_FullName"),
            validate: (val) => {
                if (!val.trim()) return "Vui lòng nhập họ và tên";
                return null;
            }
        },
        phone: {
            input: document.getElementById("Checkout_Phone"),
            span: document.getElementById("error-Checkout_Phone"),
            validate: (val) => {
                if (!val.trim()) return "Vui lòng nhập số điện thoại";
                const regex = /^[0-9]{10,11}$/;
                if (!regex.test(val.trim())) return "Số điện thoại không hợp lệ (phải là số từ 10 đến 11 ký tự)";
                return null;
            }
        },
        email: {
            input: document.getElementById("Checkout_Email"),
            span: document.getElementById("error-Checkout_Email"),
            validate: (val) => {
                if (!val.trim()) return "Vui lòng nhập địa chỉ email";
                const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
                if (!regex.test(val.trim())) return "Địa chỉ email không hợp lệ";
                return null;
            }
        },
        province: {
            input: document.getElementById("provinceInput"),
            span: document.getElementById("error-provinceInput"),
            validate: (val) => {
                if (!val.trim()) return "Vui lòng chọn tỉnh / thành phố";
                return null;
            }
        },
        address: {
            input: document.getElementById("Checkout_Address"),
            span: document.getElementById("error-Checkout_Address"),
            validate: (val) => {
                if (!val.trim()) return "Vui lòng nhập địa chỉ nhận hàng";
                return null;
            }
        }
    };

    function clearErrors() {
        Object.values(fields).forEach(f => {
            if (f.input) f.input.classList.remove("has-error");
            if (f.span) {
                f.span.textContent = "";
                f.span.style.display = "none";
            }
        });
    }

    if (form) {
        form.addEventListener("submit", function (e) {
            clearErrors();
            let hasError = false;
            let firstErrorElement = null;

            Object.entries(fields).forEach(([key, f]) => {
                if (!f.input) return;
                const errorMsg = f.validate(f.input.value);
                if (errorMsg) {
                    hasError = true;
                    f.input.classList.add("has-error");
                    if (f.span) {
                        f.span.textContent = errorMsg;
                        f.span.style.display = "block";
                    }
                    if (!firstErrorElement) {
                        firstErrorElement = f.input;
                    }
                }
            });

            if (hasError) {
                e.preventDefault();
                if (firstErrorElement) {
                    firstErrorElement.focus();
                    firstErrorElement.scrollIntoView({ behavior: "smooth", block: "center" });
                }
            } else {
                // Prevent Double Submit
                if (btnSubmit) {
                    btnSubmit.disabled = true;
                    btnSubmit.innerText = "ĐANG XỬ LÝ ĐẶT HÀNG...";
                    btnSubmit.style.opacity = "0.7";
                    btnSubmit.style.cursor = "not-allowed";
                }
            }
        });

        // Clear error class when user types/interacts
        Object.values(fields).forEach(f => {
            if (f.input) {
                f.input.addEventListener("input", () => {
                    f.input.classList.remove("has-error");
                    if (f.span) {
                        f.span.textContent = "";
                        f.span.style.display = "none";
                    }
                });
            }
        });
    }
});