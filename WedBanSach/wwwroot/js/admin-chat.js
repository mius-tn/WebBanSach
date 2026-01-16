"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").withAutomaticReconnect().build();

connection.onreconnected(connectionId => {
    console.log('Reconnected. Rejoining groups...');
    connection.invoke("JoinAdminGroup");
    if (typeof currentRoomId !== 'undefined' && currentRoomId > 0) {
        connection.invoke("JoinRoom", currentRoomId);
    }
});

// UI Elements
const chatBody = document.getElementById('adminChatBody');
const userSearchInput = document.getElementById('userSearchInput');

// Search Filter Logic
if (userSearchInput) {
    userSearchInput.addEventListener('input', function (e) {
        const term = e.target.value.toLowerCase().trim();
        const items = document.querySelectorAll('.user-item');

        items.forEach(item => {
            const name = item.dataset.name || '';
            const email = item.dataset.email || '';

            if (name.includes(term) || email.includes(term)) {
                item.style.display = 'flex';
            } else {
                item.style.display = 'none';
            }
        });
    });
}
const replyInput = document.getElementById('replyInput'); // Fixed ID
// const sendBtn is handled via onclick in HTML, but we can add listener if we remove onclick there
// For now, let's keep onclick OR better, use JS listener.
const sendBtn = document.querySelector('button[onclick="sendReply()"]');

connection.start().then(function () {
    console.log("Admin SignalR Connected.");
    // Join Admin Group
    connection.invoke("JoinAdminGroup");
    // Actually Hub sends to clients.Group("Admins"). We need to add this user to "Admins" group?
    // Hub.OnConnectedAsync? 
    // Assuming backend handles it or we rely on Room group. 
    // If we are in "Room_{id}", we receive User messages too.

    // Join specific room group if selected
    if (typeof currentRoomId !== 'undefined' && currentRoomId > 0) {
        connection.invoke("JoinRoom", currentRoomId);
        scrollToBottom();
    }
}).catch(function (err) {
    return console.error(err.toString());
});

// 1. Receive Message from User
connection.on("ReceiveUserMessage", function (userId, userName, avatarUrl, message, roomId, type, email) {
    console.log("New User Message:", userName, message, roomId);

    // Safely handle nulls
    userName = userName || "Khách";
    email = email || "";

    // Update Sidebar Preview
    const userItem = document.querySelector(`.user-item[data-room-id="${roomId}"]`);
    if (userItem) {
        const lastMsgDiv = userItem.querySelector('.last-message');
        if (lastMsgDiv) {
            lastMsgDiv.textContent = (type === "Image" ? "[Hình ảnh]" : message);
            lastMsgDiv.style.fontWeight = "bold"; // Highlight unread
            lastMsgDiv.style.color = "#000";
        }
        // Move to top
        const list = document.getElementById('userListContainer');
        if (list) list.prepend(userItem);
    } else {
        // Create new item if not exists
        const list = document.getElementById('userListContainer');
        if (list) {
            const newItem = document.createElement('a');
            newItem.href = `/AdminChat?roomId=${roomId}`; // Or appropriate URL generation
            newItem.className = 'user-item';
            newItem.style.display = 'flex'; // Ensure visibility
            newItem.dataset.roomId = roomId;
            newItem.dataset.name = userName.toLowerCase();
            newItem.dataset.email = email.toLowerCase();

            let avatarHtml = '';
            if (avatarUrl) {
                avatarHtml = `<img src="${avatarUrl}" class="user-avatar object-fit-cover" style="background-color: transparent;">`;
            } else {
                const char = userName.charAt(0).toUpperCase();
                avatarHtml = `<div class="user-avatar" style="background-color: #ffb3c1;">${char}</div>`;
            }

            newItem.innerHTML = `
                <button class="delete-chat-btn" onclick="deleteRoom(event, ${roomId})" title="Xóa hội thoại">
                    <i class="bi bi-trash"></i>
                </button>
                ${avatarHtml}
                <div class="user-info">
                    <div class="user-name">${userName}</div>
                    <div class="last-message" style="font-weight: bold; color: #000;">${type === "Image" ? "[Hình ảnh]" : message}</div>
                </div>
            `;
            list.prepend(newItem);
        }
    }

    // If we are in the active room, append it to chat
    if (typeof currentRoomId !== 'undefined' && Number(currentRoomId) === Number(roomId)) {
        appendMessage(message, "user", type);
    }
});

// Delete Room Logic
function deleteRoom(event, roomId) {
    event.preventDefault(); // Prevent link navigation
    event.stopPropagation(); // Stop bubbling

    if (confirm("Bạn có chắc chắn muốn xóa cuộc trò chuyện này? Hành động này không thể hoàn tác.")) {
        connection.invoke("DeleteRoom", roomId).catch(err => console.error(err));
    }
}

connection.on("ReceiveDeleteRoom", function (roomId) {
    const item = document.querySelector(`.user-item[data-room-id="${roomId}"]`);
    if (item) {
        item.remove();
    }

    // If observing this room, clear area or redirect?
    if (typeof currentRoomId !== 'undefined' && Number(currentRoomId) === Number(roomId)) {
        // Clear chat area or refresh
        window.location.href = '/AdminChat';
    }
});

// 2. Receive Admin Reply (from others)
connection.on("ReceiveAdminReply", function (message, type) {
    appendMessage(message, "admin", type);
});

// 3. Send Reply Logic
function sendReply() {
    const message = replyInput.value.trim();
    // const imageInput = document.getElementById("imageUpload"); // If implemented

    if (message && currentRoomId) {
        // Optimistic Append
        appendMessage(message, "admin", "Text");

        connection.invoke("AdminReply", currentRoomId, message, "Text").catch(err => {
            console.error(err);
            alert("Gửi thất bại!");
        });
        replyInput.value = "";
    } else if (!currentRoomId) {
        alert("Vui lòng chọn khách hàng để chat!");
    }
}

// 4. Append Message Helper
function appendMessage(msg, sender, type = "Text") {
    if (!chatBody) return;

    const msgDiv = document.createElement('div');
    msgDiv.classList.add('message', sender);

    if (type === "Image") {
        msgDiv.innerHTML = `<img src="${msg}" style="max-width: 200px; border-radius: 8px; cursor:pointer;" onclick="window.open(this.src,'_blank')">`;
    } else {
        msgDiv.textContent = msg;
    }

    const timeDiv = document.createElement('div');
    timeDiv.classList.add('message-time');
    const now = new Date();
    timeDiv.textContent = now.getHours() + ":" + String(now.getMinutes()).padStart(2, '0');

    msgDiv.appendChild(timeDiv);
    chatBody.appendChild(msgDiv);
    scrollToBottom();
}

function scrollToBottom() {
    if (chatBody) {
        chatBody.scrollTop = chatBody.scrollHeight;
    }
}

// 5. Enter Key Support
if (replyInput) {
    replyInput.addEventListener('keypress', function (e) {
        if (e.key === 'Enter') {
            sendReply();
        }
    });
}
