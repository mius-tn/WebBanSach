"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").build();

// DOM Elements
const chatBody = document.getElementById("chatBody");
const messageInput = document.getElementById("messageInput");
const sendButton = document.getElementById("sendButton");
// Use class selector for widget button since it has no ID in Layout
const chatButton = document.querySelector(".chat-widget-btn");
const chatPopup = document.querySelector(".chat-popup");
const closeChat = document.querySelector(".close-chat"); // Might not exist in Layout yet, but good to have
const unreadBadge = document.querySelector(".chat-badge");

// Image Upload Elements
const fileInput = document.createElement("input");
fileInput.type = "file";
fileInput.accept = "image/*";
fileInput.style.display = "none";
document.body.appendChild(fileInput);

const uploadBtn = document.getElementById("uploadButton");

// State
let isChatOpen = false;

// Initialize SignalR
connection.start().then(function () {
    console.log("SignalR Connected.");
    if (currentUserEmail) {
        connection.invoke("GetClientHistory", currentUserEmail).catch(err => console.error(err));
    }
}).catch(function (err) {
    return console.error(err.toString());
});

// Helper: Append Message
function appendMessage(message, className, type = "Text") {
    const msgDiv = document.createElement("div");
    msgDiv.classList.add("message", className);

    msgDiv.style.animation = "slideIn 0.3s ease-out";

    if (type === "Image") {
        msgDiv.innerHTML = `<img src="${message}" style="max-width: 150px; border-radius: 8px; cursor: pointer;" onclick="window.open(this.src, '_blank')">`;
    } else {
        msgDiv.textContent = message;
    }

    chatBody.appendChild(msgDiv);
    chatBody.scrollTop = chatBody.scrollHeight;
}

// Receive History
connection.on("ReceiveHistory", function (messages) {
    // Optional: Clear or Keep existing.
    // chatBody.innerHTML = ''; 
    const defaultMsg = chatBody.querySelector('.message.admin');
    // We keep default greeting if exists

    messages.forEach(msg => {
        appendMessage(msg.content, msg.role, msg.type);
    });
});

// Receive Admin Reply
connection.on("ReceiveAdminReply", function (message, type) {
    appendMessage(message, 'admin', type);
    showNotification();
});

// Receive Confirmation of Own Message
connection.on("ReceiveMessageConfirmation", function (message, type) {
    // Already appended locally, do nothing or show "sent" status
    // appendMessage(message, 'user', type);
});

// Receive Chat Reset (Deleted by Admin)
connection.on("ReceiveChatReset", function () {
    if (chatBody) {
        chatBody.innerHTML = '<div class="message admin">Cuộc trò chuyện đã được làm mới.</div>';
    }
    // Optimistically create new room on next message? No, Hub handles it.
});

// Send Message Logic
function sendMessage() {
    const message = messageInput.value.trim();
    if (!message) return;

    if (!currentUserEmail) {
        alert("Vui lòng đăng nhập để chat với Admin!");
        return;
    }

    // Optimistic Append
    appendMessage(message, 'user', "Text");
    console.log("Sending message as:", currentUserEmail, message);

    connection.invoke("SendMessage", currentUserEmail, message, "Text").catch(err => console.error(err));
    messageInput.value = "";
    scrollToBottom();
}

function scrollToBottom() {
    if (chatBody) chatBody.scrollTop = chatBody.scrollHeight;
}

function showNotification() {
    if (!isChatOpen && unreadBadge) {
        unreadBadge.style.display = 'block';
    }
}

// Event Listeners
if (sendButton) {
    sendButton.addEventListener("click", sendMessage);
}

if (messageInput) {
    messageInput.addEventListener("keydown", function (e) {
        if (e.key === "Enter") {
            e.preventDefault();
            sendMessage();
        }
    });
}

// Toggle Chat
if (chatButton) {
    chatButton.addEventListener('click', (e) => {
        // Prevent click from propagating to document
        e.stopPropagation();

        isChatOpen = !isChatOpen;
        if (chatPopup) chatPopup.style.display = isChatOpen ? 'flex' : 'none';

        if (isChatOpen) {
            if (unreadBadge) unreadBadge.style.display = 'none';
            scrollToBottom();
            setTimeout(() => {
                if (messageInput) messageInput.focus();
            }, 100);
        }
    });
}

// Close Chat when clicking outside
document.addEventListener('click', function (event) {
    if (isChatOpen && chatPopup && !chatPopup.contains(event.target) && !chatButton.contains(event.target)) {
        isChatOpen = false;
        chatPopup.style.display = 'none';
        // Optional: show badge if there were unread? No, closing means we saw it.
    }
});

// Upload Logic
if (uploadBtn) {
    uploadBtn.addEventListener("click", (e) => {
        e.preventDefault(); // Prevent form submission
        fileInput.click();
    });
}

fileInput.addEventListener("change", function () {
    const file = fileInput.files[0];
    if (file) {
        const formData = new FormData();
        formData.append("file", file);

        fetch('/api/ChatUpload/upload', {
            method: 'POST',
            body: formData
        })
            .then(response => response.json())
            .then(data => {
                if (data.url) {
                    // Optimistic Append
                    appendMessage(data.url, 'user', "Image");
                    connection.invoke("SendMessage", currentUserEmail, data.url, "Image").catch(err => console.error(err));
                }
            })
            .catch(error => console.error('Error uploading image:', error));
    }
});

// Drag and Drop Logic
if (chatPopup) {
    // Create Overlay
    const overlay = document.createElement("div");
    overlay.className = "chat-drag-overlay";
    overlay.innerHTML = `
        <i class="bi bi-cloud-arrow-up-fill text-danger fs-1 mb-2"></i>
        <h6 class="fw-bold text-danger">Thả ảnh vào đây</h6>
    `;
    chatPopup.appendChild(overlay);

    const events = ['dragenter', 'dragover', 'dragleave', 'drop'];
    events.forEach(eventName => {
        chatPopup.addEventListener(eventName, preventDefaults, false);
    });

    function preventDefaults(e) {
        e.preventDefault();
        e.stopPropagation();
    }

    chatPopup.addEventListener('dragenter', () => {
        chatPopup.classList.add('chat-drag-active');
    });

    chatPopup.addEventListener('dragleave', (e) => {
        if (!chatPopup.contains(e.relatedTarget)) {
            chatPopup.classList.remove('chat-drag-active');
        }
    });

    // Also handle leave on the overlay itself to be smoother
    overlay.addEventListener('dragleave', (e) => {
        chatPopup.classList.remove('chat-drag-active');
    });

    chatPopup.addEventListener('drop', (e) => {
        chatPopup.classList.remove('chat-drag-active');
        const dt = e.dataTransfer;
        const files = dt.files;

        if (files && files.length > 0) {
            handleFileUpload(files[0]);
        }
    });

    function handleFileUpload(file) {
        if (!file.type.startsWith('image/')) {
            alert("Vui lòng chỉ tải lên file ảnh!");
            return;
        }

        const formData = new FormData();
        formData.append("file", file);

        fetch('/api/ChatUpload/upload', {
            method: 'POST',
            body: formData
        })
            .then(response => response.json())
            .then(data => {
                if (data.url) {
                    // Optimistic Append
                    appendMessage(data.url, 'user', "Image");
                    connection.invoke("SendMessage", currentUserEmail, data.url, "Image").catch(err => console.error(err));
                }
            })
            .catch(error => console.error('Error uploading image:', error));
    }
}
