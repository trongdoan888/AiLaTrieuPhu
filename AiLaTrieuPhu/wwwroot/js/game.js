document.addEventListener("DOMContentLoaded", function () {
    // --- 1. KHỞI TẠO BIẾN TOÀN CỤC ---
    let timeLeft = 60;
    let timeSpent = 0;
    let countdown;

    const timerElement = document.getElementById("timer");
    const timeUsedInput = document.getElementById("timeUsed");
    const correctAnswerInput = document.getElementById("correctAnswer");
    const correctAnswer = correctAnswerInput ? correctAnswerInput.value : "";

    // Hàm đếm ngược
    function startTimer() {
        countdown = setInterval(function () {
            timeLeft--;
            timeSpent++;
            if (timerElement) timerElement.innerText = timeLeft;

            if (timeLeft <= 10 && timerElement) {
                timerElement.classList.add("timer-warning");
            }

            if (timeLeft <= 0) {
                clearInterval(countdown);
                Swal.fire({
                    title: 'Hết giờ!',
                    text: 'Bạn đã suy nghĩ quá lâu. Trò chơi kết thúc!',
                    icon: 'error',
                    confirmButtonColor: '#ffcc00',
                    background: '#051024',
                    color: '#fff'
                }).then(() => {
                    window.location.href = "/Game/EndGame?reason=timeout";
                });
            }
        }, 1000);
    }

    startTimer();

    // --- 2. LOGIC XỬ LÝ KHI CHỌN ĐÁP ÁN ---
    const answerForm = document.getElementById("answerForm");
    const answerButtons = document.querySelectorAll(".answer-btn");

    answerButtons.forEach(btn => {
        btn.addEventListener("click", function (e) {
            e.preventDefault();

            if (this.classList.contains("clicked")) return;
            answerButtons.forEach(b => b.classList.add("clicked"));

            clearInterval(countdown);

            if (timeUsedInput) timeUsedInput.value = timeSpent;

            this.style.background = "#ff9900";
            this.style.color = "#000";

            let hiddenAnswer = document.getElementById("hiddenAnswer");
            if (!hiddenAnswer) {
                hiddenAnswer = document.createElement("input");
                hiddenAnswer.type = "hidden";
                hiddenAnswer.name = "answer";
                hiddenAnswer.id = "hiddenAnswer";
                if (answerForm) answerForm.appendChild(hiddenAnswer);
            }
            hiddenAnswer.value = this.value;

            setTimeout(() => {
                if (answerForm) answerForm.submit();
            }, 2000);
        });
    });

    // --- Hàm gọi Server để lưu trạng thái đã dùng trợ giúp ---
    function markLifelineUsedOnServer(type) {
        fetch(`/Game/UseLifeline?type=${type}`, { method: 'POST' });
    }

    // --- 3. LOGIC CÁC QUYỀN TRỢ GIÚP ---

    // 3.1. Trợ giúp 50/50
    const btn5050 = document.getElementById("btn5050");
    if (btn5050) {
        btn5050.addEventListener("click", function () {
            if (this.classList.contains("used")) return;
            this.classList.add("used");

            // Báo cho Server biết để lần sau load lại không bị mất
            markLifelineUsedOnServer("5050");

            const btns = document.querySelectorAll('.answer-btn');
            let incorrectBtns = [];

            btns.forEach(btn => {
                if (btn.value !== correctAnswer) {
                    incorrectBtns.push(btn);
                }
            });

            incorrectBtns.sort(() => 0.5 - Math.random());
            if (incorrectBtns.length >= 2) {
                incorrectBtns[0].style.visibility = "hidden";
                incorrectBtns[1].style.visibility = "hidden";
            }

            Swal.fire({
                title: 'Trợ giúp 50/50',
                text: 'Máy tính đã loại bỏ 2 phương án sai!',
                icon: 'success',
                timer: 2000,
                showConfirmButton: false,
                background: '#051024',
                color: '#fff'
            });
        });
    }

    // 3.2. Trợ giúp Hỏi ý kiến khán giả
    const btnAudience = document.getElementById("btnAudience");
    if (btnAudience) {
        btnAudience.addEventListener("click", function () {
            if (this.classList.contains("used")) return;
            this.classList.add("used");
            markLifelineUsedOnServer("Audience");

            clearInterval(countdown); // Tạm dừng thời gian

            let correctPercent = Math.floor(Math.random() * 31) + 50;
            let remain = 100 - correctPercent;
            let p2 = Math.floor(Math.random() * remain);
            remain -= p2;
            let p3 = Math.floor(Math.random() * remain);
            let p4 = remain - p3;

            let percents = [p2, p3, p4];
            let resultText = "<div style='text-align: left; font-size: 18px;'>";
            const options = ["A", "B", "C", "D"];
            let incorrectIndex = 0;

            options.forEach(opt => {
                if (opt === correctAnswer) {
                    resultText += `<b>Đáp án ${opt}:</b> <span style='color:#ffcc00'>${correctPercent}%</span><br>`;
                } else {
                    resultText += `<b>Đáp án ${opt}:</b> <span style='color:#00d4ff'>${percents[incorrectIndex]}%</span><br>`;
                    incorrectIndex++;
                }
            });
            resultText += "</div>";

            Swal.fire({
                title: 'Khán giả bình chọn',
                html: resultText,
                icon: 'info',
                confirmButtonColor: '#ffcc00',
                background: '#051024',
                color: '#fff'
            }).then(() => {
                startTimer(); // Tiếp tục đồng hồ sau khi xem xong
            });
        });
    }

    // 3.3. Trợ giúp Gọi điện thoại cho người thân
    const btnCall = document.getElementById("btnCall");
    if (btnCall) {
        btnCall.addEventListener("click", function () {
            if (this.classList.contains("used")) return;
            this.classList.add("used");
            markLifelineUsedOnServer("Call");

            clearInterval(countdown);

            Swal.fire({
                title: 'Đang kết nối...',
                text: 'Đang gọi điện thoại cho người thân, vui lòng chờ!',
                icon: 'info',
                showConfirmButton: false,
                background: '#051024',
                color: '#fff',
                timer: 2000
            }).then(() => {
                Swal.fire({
                    title: 'Người thân nói:',
                    text: `"Chào bạn, câu này mình khá chắc chắn. Đáp án đúng là ${correctAnswer} nhé!"`,
                    icon: 'success',
                    confirmButtonColor: '#ffcc00',
                    background: '#051024',
                    color: '#fff'
                }).then(() => {
                    startTimer();
                });
            });
        });
    }

    // --- 4. LOGIC XÁC NHẬN DỪNG CHƠI BẰNG SWEETALERT2 ---
    const btnQuitGame = document.getElementById("btnQuitGame");
    if (btnQuitGame) {
        btnQuitGame.addEventListener("click", function () {
            clearInterval(countdown);

            Swal.fire({
                title: 'Dừng cuộc chơi?',
                text: "Bạn có chắc chắn muốn dừng lại và bảo toàn số tiền hiện tại không?",
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#ffcc00',
                cancelButtonColor: '#ff4d4d',
                confirmButtonText: '<b style="color: black;">Đồng ý, dừng lại</b>',
                cancelButtonText: '<b>Hủy, chơi tiếp</b>',
                background: '#051024',
                color: '#ffffff',
                backdrop: `rgba(0,0,0,0.8)`
            }).then((result) => {
                if (result.isConfirmed) {
                    window.location.href = "/Game/EndGame?reason=quit";
                } else {
                    startTimer();
                }
            });
        });
    }
});