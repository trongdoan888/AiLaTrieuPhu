let timeLeft = 60;
let score = 0;
let timerInterval;

function startTimer() {
    timerInterval = setInterval(() => {
        timeLeft--;
        document.getElementById("timer").innerText = timeLeft;
        if (timeLeft <= 0) endGame("Hết giờ!");
    }, 1000);
}

function use5050() {
    // Logic: Ẩn ngẫu nhiên 2 đáp án sai trong DOM
    alert("Đã sử dụng trợ giúp 50/50");
}

function endGame(msg) {
    clearInterval(timerInterval);
    alert(msg + " - Tổng điểm: " + score);
    window.location.href = "/Home/Index";
}

document.querySelectorAll('.ans').forEach(btn => {
    btn.addEventListener('click', function () {
        const choice = this.getAttribute('data-ans');
        // Gửi AJAX lên GameController để checkAnswer
        // Nếu đúng: score += 200, reset timer, load câu hỏi mới
        // Nếu sai: endGame("Sai rồi!");
    });
});

startTimer();