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
input.addEventListener("focus", () => {
    dropdown.classList.add("show");
});

// ===== SEARCH FILTER =====
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

// ===== CLICK SELECT =====
options.forEach(option => {
    option.addEventListener("click", () => {
        input.value = option.innerText;
        dropdown.classList.remove("show");
    });
});

// ===== CLICK OUTSIDE CLOSE =====
document.addEventListener("click", (e) => {
    if (!e.target.closest(".custom-select")) {
        dropdown.classList.remove("show");
    }
});