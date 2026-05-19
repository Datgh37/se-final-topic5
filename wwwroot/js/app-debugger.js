/**
 * TuNhanTamTInh E-Commerce Debugger Helper (Global)
 * Thiet ke boi Antigravity nham toi uu hoa viec giam sat va go loi (debug) phia Client.
 * Ho tro in nhat ky Console dep mat, co mau sac phan loai, tu dong inspect du lieu va bat loi API.
 */

window.AppDebugger = (function () {
    // Cau hinh che do Debug (co the tat bang cach set false khi len Production)
    let isDebugMode = true;

    // Bo mau sac Console CSS cuc dep
    const STYLES = {
        info: "background: #560bad; color: #fff; padding: 2px 6px; border-radius: 3px; font-weight: bold; font-family: monospace; font-size: 11px;",
        success: "background: #2b9348; color: #fff; padding: 2px 6px; border-radius: 3px; font-weight: bold; font-family: monospace; font-size: 11px;",
        warn: "background: #f77f00; color: #fff; padding: 2px 6px; border-radius: 3px; font-weight: bold; font-family: monospace; font-size: 11px;",
        error: "background: #d62828; color: #fff; padding: 2px 6px; border-radius: 3px; font-weight: bold; font-family: monospace; font-size: 11px;",
        apiReq: "background: #0077b6; color: #fff; padding: 2px 6px; border-radius: 3px; font-weight: bold; font-family: monospace; font-size: 11px;",
        apiRes: "background: #0096c7; color: #fff; padding: 2px 6px; border-radius: 3px; font-weight: bold; font-family: monospace; font-size: 11px;",
        chatbot: "background: #7209b7; color: #fff; padding: 2px 6px; border-radius: 3px; font-weight: bold; font-family: monospace; font-size: 11px;"
    };

    function getTimestamp() {
        const now = new Date();
        return now.toLocaleTimeString() + "." + String(now.getMilliseconds()).padStart(3, '0');
    }

    return {
        /**
         * Bat hoac Tat che do Debug nhanh chong
         * @param {boolean} enable true de bat, false de tat
         */
        setMode: function (enable) {
            isDebugMode = enable;
            console.log(`%c[DEBUGGER]%c Che do Debug da duoc ${enable ? 'BAT' : 'TAT'}.`, STYLES.info, "color: inherit;");
        },

        /**
         * 1. Ghi log thong tin chung (General Log)
         * @param {string} title Tieu de cua log
         * @param {any} data Du lieu / Object muon in ra de xem cau truc
         */
        info: function (title, data = null) {
            if (!isDebugMode) return;
            console.log(`%c[INFO] [${getTimestamp()}] ${title}%c`, STYLES.info, "");
            if (data !== null) {
                console.dir(data);
            }
        },

        /**
         * 2. Ghi log khi hanh dong thanh cong (Success Log)
         * @param {string} title Tieu de hanh dong
         * @param {any} data Chi tiet du lieu
         */
        success: function (title, data = null) {
            if (!isDebugMode) return;
            console.log(`%c[SUCCESS] [${getTimestamp()}] ${title}%c`, STYLES.success, "");
            if (data !== null) {
                console.dir(data);
            }
        },

        /**
         * 3. Ghi log canh bao (Warning Log)
         * @param {string} title Noi dung canh bao
         * @param {any} data Du lieu di kem neu co
         */
        warn: function (title, data = null) {
            if (!isDebugMode) return;
            console.warn(`%c[WARN] [${getTimestamp()}] ${title}%c`, STYLES.warn, "");
            if (data !== null) {
                console.dir(data);
            }
        },

        /**
         * 4. Ghi log LOI khi xay ra exception hoac that bai (Error & Exception Log)
         * @param {string} context Boi canh xay ra loi (VD: "Them vao gio hang")
         * @param {Error|string} error Doi tuong Error hoac chuoi thong bao loi
         */
        error: function (context, error = null) {
            if (!isDebugMode) return;
            console.error(`%c[ERROR] [${getTimestamp()}] Loi tai: ${context}%c`, STYLES.error, "");
            if (error) {
                if (error instanceof Error) {
                    console.error("Chi tiet Exception:", error.message);
                    console.error("Stack trace:", error.stack);
                } else {
                    console.error("Chi tiet loi:", error);
                }
            }
        },

        /**
         * 5. Debug cuoc goi API (AJAX/Fetch) gui di va nhan ve cuc ky chuyen nghiep
         * @param {string} url Endpoint API
         * @param {string} method Phuong thuc HTTP (GET, POST, PUT, DELETE,...)
         * @param {any} requestPayload Du lieu gui di (Query Params hoac Request Body)
         * @param {any} responseData Du lieu API phan hoi ve tu server
         * @param {number} status Ma trang thai HTTP (200, 400, 500,...)
         */
        api: function (url, method, requestPayload = null, responseData = null, status = 200) {
            if (!isDebugMode) return;
            const isSuccess = status >= 200 && status < 300;
            const style = isSuccess ? STYLES.apiRes : STYLES.error;

            console.groupCollapsed(`%c[API ${method}]%c [${status}] ${url}`, isSuccess ? STYLES.apiReq : STYLES.error, "font-weight: bold; color: inherit;");
            
            console.log("%cPayload gui di (Request Body / Params):", "color: #0077b6; font-weight: bold;");
            if (requestPayload) console.dir(requestPayload);
            else console.log("(Khong co payload)");

            console.log(`%cKet qua phan hoi (Response Data) [Status ${status}]:`, `color: ${isSuccess ? '#2b9348' : '#d62828'}; font-weight: bold;`);
            if (responseData) console.dir(responseData);
            else console.log("(Khong co du lieu phan hoi)");

            console.groupEnd();
        },

        /**
         * 6. Debug cuoc goi Chatbot (AJAX to Gemini RAG) cuc ky chi tiet va dep mat
         * @param {string} text Tin nhan cua nguoi dung gui di
         * @param {any} response Phieu tra ve tu ChatbotController (chua Reply va Products)
         * @param {number} duration Thoi gian phan hoi bang giay
         * @param {any} errorObj Doi tuong loi neu xay ra loi mang
         */
        chatbot: function (text, response, duration, errorObj = null) {
            if (!isDebugMode) return;

            if (errorObj) {
                console.groupCollapsed(`%c[🤖 CHATBOT ERROR] [${getTimestamp()}] Request: "${text}"`, STYLES.error);
                console.log("%c[1/3] Payload gui di:", "color: #0077b6; font-weight: bold;", { message: text });
                console.error(`%c[ERR] Chi tiet loi mang sau ${duration}s:`, "color: #d62828; font-weight: bold;", errorObj);
                console.groupEnd();
                return;
            }

            const hasError = response && response.error;
            const style = hasError ? STYLES.warn : STYLES.chatbot;

            console.groupCollapsed(
                `%c[🤖 CHATBOT] [${getTimestamp()}] Request: "${text}"%c (${duration}s)`, 
                style, 
                "color: #7209b7; font-weight: bold; font-family: sans-serif;"
            );

            console.log("%c[1/3] Payload gui di (Request Body):", "color: #0077b6; font-weight: bold;", { message: text });

            if (hasError) {
                console.warn("%c[2/3] Trinity AI tra ve loi nghiep vu:", "color: #f77f00; font-weight: bold;", response);
                console.log("%c[3/3] RAG Products Match:", "color: #6c757d; font-weight: bold;", "None (Do loi)");
            } else {
                console.log(`%c[2/3] Phieu tra ve (AI Reply):`, "color: #2b9348; font-weight: bold;", response.reply);
                
                if (response.products && response.products.length > 0) {
                    console.log("%c[3/3] RAG Products Match:", "color: #e63946; font-weight: bold;", response.products);
                } else {
                    console.log("%c[3/3] RAG Products Match:", "color: #6c757d; font-weight: bold;", "None (Khong trung tu khoa SP)");
                }
            }

            console.groupEnd();
        }
    };
})();

// In ra thong bao khoi tao thanh cong
AppDebugger.info("AppDebugger initialized successfully! Ready to debug globally.");
