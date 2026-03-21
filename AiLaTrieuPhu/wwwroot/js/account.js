document.addEventListener("DOMContentLoaded", function () {
    const loginForm = document.getElementById('loginForm');
    const loginBtn = document.querySelector('.btn-confirm');

    if (loginForm) {
        loginForm.addEventListener('submit', function (e) {
            const username = this.querySelector('input[name="username"]').value.trim();
            const password = this.querySelector('input[name="password"]').value.trim();

            if (!username || !password) {
                e.preventDefault();
                alert("Bạn không thể vào ghế nóng nếu chưa nhập tên và mật khẩu!");
                return;
            }

            // Hiệu ứng đổi màu nút khi nhấn (giống lúc chọn đáp án)
            loginBtn.style.background = "#ffcc00";
            loginBtn.style.color = "#000";
            loginBtn.innerText = "ĐANG KIỂM TRA...";

            // Ở đây bạn có thể thêm Audio nếu có file nhạc:
            // let audio = new Audio('/sounds/final_answer.mp3');
            // audio.play();
        });
    }

    // Hiệu ứng focus input
    const inputs = document.querySelectorAll('.input-group input');
    inputs.forEach(input => {
        input.addEventListener('focus', () => {
            input.parentElement.style.transform = "scale(1.05)";
        });
        input.addEventListener('blur', () => {
            input.parentElement.style.transform = "scale(1)";
        });
    });
});