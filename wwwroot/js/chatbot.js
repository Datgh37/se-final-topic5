/* ==========================================================================
   PREMIUM AI CHATBOT SYSTEM - LOGIC
   ========================================================================== */

$(document).ready(function () {
    const $bubble = $('#chatbot-bubble');
    const $window = $('#chatbot-window');
    const $closeBtn = $('#chatbot-close');
    const $resizeBtn = $('#chatbot-resize');
    const $messagesContainer = $('#chatbot-messages');
    const $inputField = $('#chatbot-input');
    const $sendBtn = $('#chatbot-send');

    // 1. Toggle Chat Window
    $bubble.on('click', function () {
        $window.toggleClass('hidden');
        $messagesContainer.scrollTop($messagesContainer[0].scrollHeight);
        if (!$window.hasClass('hidden')) {
            $inputField.focus();
        }
    });

    $closeBtn.on('click', function () {
        $window.addClass('hidden');
    });

    // Toggle Chat Window Size (PC only)
    $resizeBtn.on('click', function (e) {
        e.stopPropagation();
        $window.toggleClass('wide');
        
        // Toggle icon
        const $icon = $(this).find('i');
        if ($window.hasClass('wide')) {
            $icon.removeClass('fa-expand').addClass('fa-compress');
        } else {
            $icon.removeClass('fa-compress').addClass('fa-expand');
        }
        
        // Scroll to bottom after resizing is finished animating
        setTimeout(function() {
            $messagesContainer.animate({ scrollTop: $messagesContainer[0].scrollHeight }, 200);
        }, 300);
    });

    // Close chat window if clicked outside on mobile
    $(document).on('click', function (e) {
        if ($(window).width() < 576) {
            if (!$window.hasClass('hidden') && 
                !$(e.target).closest('#chatbot-window').length && 
                !$(e.target).closest('#chatbot-bubble').length) {
                $window.addClass('hidden');
            }
        }
    });

    // 2. Click Quick Suggestion
    $(document).on('click', '.suggestion-btn', function () {
        const query = $(this).data('query');
        if (query) {
            // Remove suggestions container after choosing one
            $(this).closest('.chatbot-suggestions').fadeOut(300, function() {
                $(this).remove();
            });
            sendMessage(query);
        }
    });

    // 3. Send Message on Click or Enter Key
    $sendBtn.on('click', function () {
        triggerMessageSend();
    });

    $inputField.on('keypress', function (e) {
        if (e.which === 13) {
            triggerMessageSend();
        }
    });

    function triggerMessageSend() {
        const messageText = $inputField.val().trim();
        if (messageText !== "") {
            sendMessage(messageText);
        }
    }

    // 4. Send Message via AJAX
    function sendMessage(text) {
        const isEn = $('html').attr('lang') === 'en';
        
        // Render User Message
        appendMessage(text, 'user');
        $inputField.val('');
        
        // Render Typing Indicator
        showTypingIndicator();
        
        // AJAX to Backend Controller
        const startTime = performance.now();
        
        $.ajax({
            url: '/Chatbot/Ask',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ message: text }),
            success: function (response) {
                const duration = ((performance.now() - startTime) / 1000).toFixed(2);
                hideTypingIndicator();
                
                // Goi he thong Debugger toan cuc
                if (window.AppDebugger) {
                    window.AppDebugger.chatbot(text, response, duration);
                }
                
                if (response.error) {
                    appendMessage((isEn ? "An error occurred: " : "Có lỗi xảy ra: ") + response.error, 'bot');
                    return;
                }

                // Render Bot Reply
                appendMessage(response.reply, 'bot');

                // Render Products (if any)
                if (response.products && response.products.length > 0) {
                    appendProducts(response.products);
                }
            },
            error: function (xhr, status, error) {
                const duration = ((performance.now() - startTime) / 1000).toFixed(2);
                hideTypingIndicator();
                
                // Goi he thong Debugger toan cuc de bao loi mang/server
                if (window.AppDebugger) {
                    window.AppDebugger.chatbot(text, null, duration, { status, error, responseText: xhr.responseText });
                }
                
                appendMessage(isEn ? "I'm currently a bit busy, please try asking again in a few seconds! 🤖" : "Hiện tại tôi đang bận một chút, bạn hãy thử hỏi lại sau vài giây nhé! 🤖", 'bot');
            }
        });
    }

    // 5. Append Message Bubble
    function appendMessage(text, sender) {
        const parsedText = sender === 'bot' ? parseMarkdown(text) : escapeHtml(text);
        const avatarHtml = sender === 'bot' 
            ? `<div class="message-avatar"><img src="/images/Others/bot-avatar.png" alt="AI Avatar"></div>` 
            : '';
        const messageHtml = `
            <div class="chat-message ${sender}">
                ${avatarHtml}
                <div class="message-bubble">${parsedText}</div>
            </div>
        `;
        $messagesContainer.append(messageHtml);
        $messagesContainer.animate({ scrollTop: $messagesContainer[0].scrollHeight }, 300);
    }

    // 6. Append Product Cards
    function appendProducts(products) {
        const isEn = $('html').attr('lang') === 'en';
        let productsHtml = '<div class="chatbot-product-carousel">';
        
        products.forEach(p => {
            const formattedPrice = formatCurrency(p.finalPrice);
            const formattedOldPrice = formatCurrency(p.unitPrice);
            
            let priceHtml = `<span class="chatbot-price-current">${formattedPrice}</span>`;
            if (p.discount > 0) {
                priceHtml += `
                    <span class="chatbot-price-old">${formattedOldPrice}</span>
                    <span class="chatbot-discount-badge">-${p.discount * 100}%</span>
                `;
            }

            productsHtml += `
                <div class="chatbot-product-card">
                    <img src="${p.primaryImageUrl}" alt="${escapeHtml(p.productName)}" class="chatbot-product-img" onerror="this.src='/images/product-default.png'">
                    <div class="chatbot-product-info">
                        <h6 class="chatbot-product-name" title="${escapeHtml(p.productName)}">${escapeHtml(p.productName)}</h6>
                        <div class="chatbot-product-pricing">
                            ${priceHtml}
                        </div>
                    </div>
                    <div class="chatbot-product-action">
                        <a href="/Products/Details/${p.productId}" class="chatbot-view-btn">${isEn ? "View Details" : "Xem chi tiết"}</a>
                    </div>
                </div>
            `;
        });
        
        productsHtml += '</div>';
        $messagesContainer.append(productsHtml);
        $messagesContainer.animate({ scrollTop: $messagesContainer[0].scrollHeight }, 400);
    }

    // 7. Show/Hide Typing Indicator
    function showTypingIndicator() {
        const indicatorHtml = `
            <div id="typing-indicator" class="chat-message bot">
                <div class="message-avatar">
                    <img src="/images/Others/bot-avatar.png" alt="AI Avatar">
                </div>
                <div class="message-bubble typing-indicator">
                    <span class="typing-dot"></span>
                    <span class="typing-dot"></span>
                    <span class="typing-dot"></span>
                </div>
            </div>
        `;
        $messagesContainer.append(indicatorHtml);
        $messagesContainer.animate({ scrollTop: $messagesContainer[0].scrollHeight }, 300);
    }

    function hideTypingIndicator() {
        $('#typing-indicator').remove();
    }

    // 8. Help Helpers: Escape HTML & Format Currency & Parse Markdown
    function escapeHtml(string) {
        return String(string)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    function formatCurrency(value) {
        return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value);
    }

    function parseMarkdown(text) {
        let html = text;
        // Escape standard HTML first for safety
        html = escapeHtml(html);
        
        // Restore markdown elements
        // Bold: **text** -> <strong>text</strong>
        html = html.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
        
        // Bullet points: * item or - item -> <li>item</li>
        html = html.replace(/^\s*[\*\-]\s+(.*?)$/gm, '<li>$1</li>');
        
        // Wrap contiguous list items in <ul>
        html = html.replace(/(<li>.*?<\/li>)+/g, '<ul>$&</ul>');
        
        // Newlines: \n -> <br> (but not right after list tags to avoid double spacing)
        html = html.replace(/\n/g, '<br>');
        html = html.replace(/<\/ul><br>/g, '</ul>');
        html = html.replace(/<br><ul>/g, '<ul>');
        
        return html;
    }
});
