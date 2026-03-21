document.addEventListener("DOMContentLoaded", function () {
    // --- 1. KHỞI TẠO BIẾN TOÀN CỤC ---
    let timeLeft = 60;
    let timeSpent = 0;
    let countdown; // Phải khai báo let ở đây để quyền trợ giúp Gọi Điện có thể dừng và bật lại được

    const timerElement = document.getElementById("timer");
    const timeUsedInput = document.getElementById("timeUsed");

    // Lấy đáp án đúng từ thẻ hidden để dùng cho các quyền trợ giúp
    const correctAnswerInput = document.getElementById("correctAnswer");
    const correctAnswer = correctAnswerInput ? correctAnswerInput.value : "";

    // Hàm đếm ngược
    function startTimer() {
        countdown = setInterval(function () {
            timeLeft--;
            timeSpent++;
            if (timerElement) timerElement.innerText = timeLeft;

            // Cảnh báo đỏ khi còn dưới 10s
            if (timeLeft <= 10 && timerElement) {
                timerElement.classList.add("timer-warning");
            }

            // Hết giờ
            if (timeLeft <= 0) {
                clearInterval(countdown);
                alert("Hết giờ! Bạn đã bị loại.");
                window.location.href = "/Game/EndGame?reason=timeout";
            }
        }, 1000);
    }

    // Bắt đầu đếm ngược ngay khi load xong trang
    startTimer();


    // --- 2. LOGIC XỬ LÝ KHI CHỌN ĐÁP ÁN ---
    const answerForm = document.getElementById("answerForm");
    const answerButtons = document.querySelectorAll(".answer-btn");

    answerButtons.forEach(btn => {
        btn.addEventListener("click", function (e) {
            e.preventDefault();

            // CHẶN BẤM 2 LẦN GÂY LỖI DATABASE
            if (this.classList.contains("clicked")) return;
            answerButtons.forEach(b => b.classList.add("clicked"));

            clearInterval(countdown); // Dừng đồng hồ ngay lập tức

            // Ghi nhận tổng thời gian suy nghĩ
            if (timeUsedInput) timeUsedInput.value = timeSpent;

            // Hiệu ứng nhấp nháy nút màu cam
            this.style.background = "#ff9900";
            this.style.color = "#000";

            // Lấy đáp án được chọn đẩy vào form
            let hiddenAnswer = document.getElementById("hiddenAnswer");
            if (!hiddenAnswer) {
                hiddenAnswer = document.createElement("input");
                hiddenAnswer.type = "hidden";
                hiddenAnswer.name = "answer";
                hiddenAnswer.id = "hiddenAnswer";
                if (answerForm) answerForm.appendChild(hiddenAnswer);
            }
            hiddenAnswer.value = this.value;

            // Chờ 2 giây tạo kịch tính rồi nộp bài
            setTimeout(() => {
                if (answerForm) answerForm.submit();
            }, 2000);
        });
    });

    // --- 3. LOGIC CÁC QUYỀN TRỢ GIÚP ---

    // 3.1. Trợ giúp 50/50
    const btn5050 = document.getElementById("btn5050");
    if (btn5050) {
        btn5050.addEventListener("click", function () {
            if (this.classList.contains("used")) return; // Chặn bấm lần 2
            this.classList.add("used");

            const btns = document.querySelectorAll('.answer-btn');
            let incorrectBtns = [];

            // Tìm ra 3 nút có đáp án sai
            btns.forEach(btn => {
                if (btn.value !== correctAnswer) {
                    incorrectBtns.push(btn);
                }
            });

            // Xáo trộn mảng đáp án sai và chọn ra 2 cái để ẩn đi
            incorrectBtns.sort(() => 0.5 - Math.random());
            if (incorrectBtns.length >= 2) {
                incorrectBtns[0].style.visibility = "hidden";
                incorrectBtns[1].style.visibility = "hidden";
            }

            alert("Máy tính đã loại bỏ 2 phương án sai!");
        });
    }

    // 3.2. Trợ giúp Hỏi ý kiến khán giả
    const btnAudience = document.getElementById("btnAudience");
    if (btnAudience) {
        btnAudience.addEventListener("click", function () {
            if (this.classList.contains("used")) return;
            this.classList.add("used");

            // Thuật toán random %: Đáp án đúng từ 50% - 80%
            let correctPercent = Math.floor(Math.random() * 31) + 50;
            let remain = 100 - correctPercent;

            // Chia nốt số % còn lại cho 3 đáp án sai
            let p2 = Math.floor(Math.random() * remain);
            remain -= p2;
            let p3 = Math.floor(Math.random() * remain);
            let p4 = remain - p3;

            let percents = [p2, p3, p4];
            let resultText = "Kết quả bình chọn của khán giả trong trường quay:\n\n";

            const options = ["A", "B", "C", "D"];
            let incorrectIndex = 0;

            options.forEach(opt => {
                if (opt === correctAnswer) {
                    resultText += `Đáp án ${opt}: ${correctPercent}%\n`;
                } else {
                    resultText += `Đáp án ${opt}: ${percents[incorrectIndex]}%\n`;
                    incorrectIndex++;
                }
            });

            alert(resultText);
        });
    }

    // 3.3. Trợ giúp Gọi điện thoại cho người thân
    const btnCall = document.getElementById("btnCall");
    if (btnCall) {
        btnCall.addEventListener("click", function () {
            if (this.classList.contains("used")) return;
            this.classList.add("used");

            // Tạm dừng đồng hồ khi gọi điện
            clearInterval(countdown);

            alert("Đang kết nối tín hiệu tới người thân...");

            setTimeout(() => {
                alert(`Người thân: "Chào bạn, câu này mình khá chắc chắn. Đáp án đúng là ${correctAnswer} nhé!"`);

                // Gọi hàm startTimer() để đếm ngược tiếp tục chạy
                startTimer();
            }, 1500); // Giả lập chờ 1.5 giây mới nhấc máy
        });
    }
});