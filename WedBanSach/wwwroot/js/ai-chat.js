$(document).ready(function () {
    // 1. Session Storage & Audio Config (Refresh starts a clean fresh session)
    localStorage.removeItem("ai_session_id");
    let sessionId = null;
    let soundEnabled = localStorage.getItem("ai_sound_enabled") !== "false";
    let recognition = null;
    let isListening = false;

    updateSoundIcon();

    // 2. Pure Digital Beep Synthesizer using Web Audio API
    function playBeep(isDouble = false) {
        if (!soundEnabled) return;
        try {
            const ctx = new (window.AudioContext || window.webkitAudioContext)();
            
            function triggerOsc(freq, duration, delay) {
                const osc = ctx.createOscillator();
                const gain = ctx.createGain();
                osc.connect(gain);
                gain.connect(ctx.destination);
                osc.type = 'sine';
                osc.frequency.setValueAtTime(freq, ctx.currentTime + delay);
                gain.gain.setValueAtTime(0.04, ctx.currentTime + delay);
                gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + delay + duration);
                osc.start(ctx.currentTime + delay);
                osc.stop(ctx.currentTime + delay + duration);
            }

            // Normal beep or celebrating double beep
            if (isDouble) {
                triggerOsc(880, 0.08, 0); // Note A5
                triggerOsc(1109, 0.12, 0.09); // Note C#6
            } else {
                triggerOsc(987, 0.09, 0); // Note B5
            }
        } catch (e) {
            console.log("Audio play blocked by browser policy.");
        }
    }

    // Update audio toggle icon
    function updateSoundIcon() {
        const $soundIcon = $("#aiSoundToggle i");
        if (soundEnabled) {
            $soundIcon.removeClass("bi-volume-mute-fill").addClass("bi-volume-up-fill");
        } else {
            $soundIcon.removeClass("bi-volume-up-fill").addClass("bi-volume-mute-fill");
        }
    }

    // Toggle sound
    $(document).on("click", "#aiSoundToggle", function () {
        soundEnabled = !soundEnabled;
        localStorage.setItem("ai_sound_enabled", soundEnabled);
        updateSoundIcon();
        playBeep();
    });

    // 3. Mascot Emotion Controller
    const $mascot = $("#aiMascot");
    const $mascotContainer = $(".ai-mascot-container");

    function setMascotEmotion(emotion) {
        $mascot.removeClass("mascot-normal mascot-thinking mascot-happy mascot-helpful");
        $mascotContainer.removeClass("thinking");
        
        switch (emotion) {
            case "thinking":
                $mascot.addClass("mascot-thinking");
                $mascotContainer.addClass("thinking");
                break;
            case "happy":
                $mascot.addClass("mascot-happy");
                playBeep(true);
                // Return to normal after 3 seconds
                setTimeout(() => {
                    if ($mascot.hasClass("mascot-happy")) setMascotEmotion("normal");
                }, 3000);
                break;
            case "helpful":
                $mascot.addClass("mascot-helpful");
                break;
            case "normal":
            default:
                $mascot.addClass("mascot-normal");
                break;
        }
    }

    // 4. HTML5 Speech-to-Text Voice Chat
    if ('webkitSpeechRecognition' in window || 'SpeechRecognition' in window) {
        const SpeechRec = window.SpeechRecognition || window.webkitSpeechRecognition;
        recognition = new SpeechRec();
        recognition.continuous = false;
        recognition.interimResults = false;
        recognition.lang = "vi-VN";

        recognition.onstart = function () {
            isListening = true;
            $("#aiVoiceBtn").addClass("listening");
        };

        recognition.onend = function () {
            isListening = false;
            $("#aiVoiceBtn").removeClass("listening");
        };

        recognition.onerror = function (event) {
            console.log("Voice Recognition Error: ", event.error);
        };

        recognition.onresult = function (event) {
            const resultText = event.results[0][0].transcript;
            $("#aiChatInput").val(resultText);
            playBeep();
        };
    } else {
        $("#aiVoiceBtn").hide(); // Hide mic if not supported
    }

    $(document).on("click", "#aiVoiceBtn", function () {
        if (!recognition) return;
        if (isListening) {
            recognition.stop();
        } else {
            recognition.start();
        }
    });

    // 5. Toggle Chat Popup
    $(document).on("click", ".ai-mascot-container", function (e) {
        // Prevent trigger if they click the sound toggle or close btn inside, but it's separate
        $("#aiChatPopup").toggleClass("active");
        if ($("#aiChatPopup").hasClass("active")) {
            playBeep();
            scrollChatToBottom();
            // Recover history if session exists and chat body is empty
            if (sessionId && $("#aiChatBody").children().length <= 1) {
                loadHistory();
            }
        }
    });

    $(document).on("click", "#aiCloseChat", function (e) {
        e.stopPropagation(); // Stop opening the chat again
        $("#aiChatPopup").removeClass("active");
    });

    // Helper: Scroll to bottom
    function scrollChatToBottom() {
        const chatBody = document.getElementById("aiChatBody");
        if (chatBody) {
            chatBody.scrollTop = chatBody.scrollHeight;
        }
    }

    // Helper: Format Currency
    function formatVND(amount) {
        return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);
    }

    // 6. Append Message Bubbles
    function appendMessage(role, text, products = []) {
        const $chatBody = $("#aiChatBody");
        const bubbleClass = role === "user" ? "user" : "ai";
        
        let msgHtml = `<div class="ai-msg-bubble ${bubbleClass}">${text}</div>`;
        $chatBody.append(msgHtml);

        // Append product cards if available
        if (products && products.length > 0) {
            let productsHtml = `<div class="ai-product-suggestions">`;
            products.forEach(p => {
                const originalPriceHtml = p.discountPrice ? `<span class="ai-product-oldprice">${formatVND(p.price)}</span>` : '';
                const sellingPrice = p.discountPrice ? p.discountPrice : p.price;
                
                productsHtml += `
                    <div class="ai-product-card">
                        <img class="ai-product-img" src="${p.imageUrl}" alt="${p.title}">
                        <div class="ai-product-details">
                            <h5 class="ai-product-title">${p.title}</h5>
                            <p class="ai-product-author">${p.author}</p>
                            <div class="ai-product-price-box">
                                <span class="ai-product-price">${formatVND(sellingPrice)}</span>
                                ${originalPriceHtml}
                            </div>
                            <div class="ai-product-actions">
                                <a href="/chi-tiet-san-pham/${p.bookId}" class="ai-product-btn view" target="_blank">Chi tiết</a>
                                <button type="button" class="ai-product-btn buy ai-add-to-cart-btn" data-book-id="${p.bookId}">Mua ngay</button>
                            </div>
                        </div>
                    </div>
                `;
            });
            productsHtml += `</div>`;
            $chatBody.append(productsHtml);
        }

        scrollChatToBottom();
    }

    // Helper: Append typing indicator
    function appendTypingIndicator() {
        const indicatorHtml = `
            <div class="ai-msg-bubble ai typing" id="aiTypingIndicator">
                <div class="typing-dot-bubble"></div>
                <div class="typing-dot-bubble"></div>
                <div class="typing-dot-bubble"></div>
            </div>
        `;
        $("#aiChatBody").append(indicatorHtml);
        scrollChatToBottom();
    }

    function removeTypingIndicator() {
        $("#aiTypingIndicator").remove();
    }

    // 7. Send Message to API
    function sendMessage(text) {
        if (!text || text.trim() === "") return;

        appendMessage("user", text);
        $("#aiChatInput").val("");
        
        setMascotEmotion("thinking");
        appendTypingIndicator();

        $.ajax({
            url: "/api/ai/chat",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify({
                sessionId: sessionId ? parseInt(sessionId) : null,
                message: text
            }),
            success: function (res) {
                removeTypingIndicator();
                if (res.success) {
                    // Update SessionId locally
                    if (res.sessionId) {
                        sessionId = res.sessionId;
                        localStorage.setItem("ai_session_id", res.sessionId);
                    }

                    // Print AI Response
                    appendMessage("assistant", res.text, res.recommendedProducts);
                    playBeep();

                    // Intent reactions
                    if (res.intent === "policy") {
                        setMascotEmotion("helpful");
                    } else if (res.cartUpdated) {
                        // Auto cart addition triggered by AI
                        setMascotEmotion("happy");
                        updateHeaderCartCount(res.cartCount);
                        // Trigger sweetalert
                        if (typeof Swal !== "undefined") {
                            Swal.fire({
                                icon: 'success',
                                title: 'Thêm giỏ hàng thành công',
                                text: res.cartMessage,
                                timer: 2000,
                                showConfirmButton: false,
                                toast: true,
                                position: 'top-end'
                            });
                        }
                    } else {
                        setMascotEmotion("normal");
                    }
                } else {
                    appendMessage("assistant", "Rất tiếc, AI không phản hồi kịp lúc. Bạn thử nhắn lại nhé!");
                    setMascotEmotion("normal");
                }
            },
            error: function () {
                removeTypingIndicator();
                appendMessage("assistant", "Lỗi kết nối máy chủ. Vui lòng kiểm tra lại mạng!");
                setMascotEmotion("normal");
            }
        });
    }

    // Action buttons inside Layout
    $(document).on("click", "#aiSendBtn", function () {
        sendMessage($("#aiChatInput").val());
    });

    $("#aiChatInput").on("keypress", function (e) {
        if (e.which === 13) {
            sendMessage($(this).val());
        }
    });

    // 8. Suggestion Chips Handler
    $(document).on("click", ".ai-chip", function () {
        const text = $(this).text();
        sendMessage(text);
    });

    // 9. Recommended Product Cart Button click
    $(document).on("click", ".ai-add-to-cart-btn", function (e) {
        e.stopPropagation();
        const bookId = $(this).data("book-id");
        const $btn = $(this);
        $btn.prop("disabled", true).text("Đang thêm...");

        $.ajax({
            url: "/api/ai/add-to-cart",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify({
                bookId: parseInt(bookId),
                quantity: 1
            }),
            success: function (res) {
                $btn.prop("disabled", false).text("Mua ngay");
                if (res.success) {
                    setMascotEmotion("happy");
                    updateHeaderCartCount(res.cartCount);
                    if (typeof Swal !== "undefined") {
                        Swal.fire({
                            icon: 'success',
                            title: 'Tuyệt vời!',
                            text: res.message,
                            timer: 2000,
                            showConfirmButton: false,
                            toast: true,
                            position: 'top-end'
                        });
                    }
                }
            },
            error: function () {
                $btn.prop("disabled", false).text("Mua ngay");
            }
        });
    });

    // Helper: Update Header Cart Badge
    function updateHeaderCartCount(count) {
        const $badge = $(".cart-badge, #cartCount, .cart-count");
        if ($badge.length > 0) {
            $badge.text(count);
            // Flash effect
            $badge.fadeOut(100).fadeIn(100).fadeOut(100).fadeIn(100);
        }
    }

    // 10. Load Chat History from Server
    function loadHistory() {
        if (!sessionId) return;
        $.ajax({
            url: `/api/ai/history?sessionId=${sessionId}`,
            type: "GET",
            success: function (res) {
                if (res.success && res.messages.length > 0) {
                    // Empty body save for initial greetings
                    const $chatBody = $("#aiChatBody");
                    $chatBody.empty();
                    res.messages.forEach(m => {
                        appendMessage(m.role, m.text, m.recommendedProducts);
                    });
                }
            }
        });
    }
});
