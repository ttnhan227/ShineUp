(function (userId) {
    'use strict';

    const currentUserId = userId || 'anonymous';
    const chatStorageKey = `chatbot-history-${currentUserId}`;

    document.addEventListener('DOMContentLoaded', () => {
        const chatToggleBtn = document.getElementById("chatbot-toggle");
        const chatBox = document.getElementById("chatbot-box");
        const chatMessages = document.getElementById("chat-messages");
        const chatLoading = document.getElementById("chat-loading");
        const userInput = document.getElementById("user-input");
        const sendBtn = document.getElementById("send-btn");

        // Gắn sự kiện
        chatToggleBtn?.addEventListener("click", toggleChatbot);
        sendBtn?.addEventListener("click", sendMessage);
        userInput?.addEventListener("keypress", e => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendMessage();
            }
        });

        // Tải lịch sử chat khi mở
        loadChatHistory();

        function toggleChatbot() {
            if (chatBox.style.display === 'none' || chatBox.style.display === '') {
                chatBox.style.display = 'flex';
                loadChatHistory();
                userInput.focus();
            } else {
                chatBox.style.display = 'none';
            }
        }

        async function sendMessage() {
            const message = userInput.value.trim();
            if (!message) return;

            appendMessage(message, 'user');
            saveMessage(message, 'user');
            userInput.value = '';
            userInput.disabled = true;
            sendBtn.disabled = true;
            chatLoading.style.display = 'block';

            try {
                const response = await fetch('/ChatBot/Ask', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({message: message})
                });

                if (!response.ok) throw new Error('Lỗi kết nối tới chatbot.');

                const data = await response.json();
                const reply = data.reply || 'Không có phản hồi.';
                appendMessage(reply, 'bot');
                saveMessage(reply, 'bot');
            } catch (error) {
                console.error(error);
                appendMessage('❌ Không thể kết nối đến chatbot.', 'bot');
                saveMessage('❌ Không thể kết nối đến chatbot.', 'bot');
            } finally {
                chatLoading.style.display = 'none';
                userInput.disabled = false;
                sendBtn.disabled = false;
                userInput.focus();
            }
        }

        function appendMessage(text, type) {
            const msgDiv = document.createElement('div');
            msgDiv.className = type === 'user' ? 'user-message' : 'bot-message';
            msgDiv.textContent = text;
            chatMessages.appendChild(msgDiv);
            chatMessages.scrollTop = chatMessages.scrollHeight;
        }

        function saveMessage(text, type) {
            try {
                const history = JSON.parse(sessionStorage.getItem(chatStorageKey)) || [];
                history.push({text, type, timestamp: new Date().toISOString()});

                // Giới hạn tối đa 100 tin nhắn
                if (history.length > 100) {
                    history.splice(0, history.length - 100);
                }

                sessionStorage.setItem(chatStorageKey, JSON.stringify(history));
            } catch (e) {
                console.error('Không thể lưu vào sessionStorage:', e);
            }
        }

        function loadChatHistory() {
            chatMessages.innerHTML = '';

            try {
                const history = JSON.parse(sessionStorage.getItem(chatStorageKey)) || [];
                if (history.length === 0) {
                    appendMessage('👋 Chào bạn! Tôi có thể giúp gì?', 'bot');
                    return;
                }

                for (const msg of history) {
                    appendMessage(msg.text, msg.type);
                }
            } catch (e) {
                console.error('Không thể tải từ sessionStorage:', e);
                appendMessage('👋 Chào bạn! Tôi có thể giúp gì?', 'bot');
            }
        }

        // Hàm global để xóa lịch sử trong tab hiện tại
        window.clearCurrentChat = function () {
            if (confirm('Bạn có chắc muốn xóa toàn bộ lịch sử chat trong tab này?')) {
                sessionStorage.removeItem(chatStorageKey);
                chatMessages.innerHTML = '';
                appendMessage('👋 Chào bạn! Tôi có thể giúp gì?', 'bot');
            }
        };
    });

})(window.ChatBotUserId);
