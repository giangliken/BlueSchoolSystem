function showConnectionModal() {
    const modalEl = document.getElementById('connectionModal');
    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    modal.show();
}
function hideConnectionModal() {
    const modalEl = document.getElementById('connectionModal');
    const modal = bootstrap.Modal.getInstance(modalEl);
    if (modal) {
        modal.hide();
    }
}

// Hàm check kết nối Internet + server
async function checkServerConnection() {
    // Check mạng trước
    if (!navigator.onLine) {
        console.error("Thiết bị không có kết nối Internet.");
        showConnectionModal();
        return;
    }
    // Nếu có mạng, mới thử fetch server
    try {
        const response = await fetch('/api/healthcheck', { method: 'GET' });
        if (!response.ok) throw new Error("Bad response từ server");
        hideConnectionModal();
    } catch (error) {
        console.error("Không thể kết nối đến máy chủ:", error);
        showConnectionModal();
    }
}

// Lắng nghe event online/offline của trình duyệt
window.addEventListener('offline', showConnectionModal);
window.addEventListener('online', checkServerConnection);

// Kiểm tra khi trang load và mỗi 10s sau đó
checkServerConnection();
setInterval(checkServerConnection, 10000);
