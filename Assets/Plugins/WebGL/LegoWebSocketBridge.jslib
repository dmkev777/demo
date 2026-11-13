mergeInto(LibraryManager.library, {
    $LegoWSState: {
        socket: null,
        queue: [],
        status: 0 // 0 = disconnected, 1 = connecting, 2 = connected, 3 = error
    },

    LegoWS_Connect: function (urlPtr) {
        var url = UTF8ToString(urlPtr);
        if (LegoWSState.socket !== null) {
            try { LegoWSState.socket.close(); } catch (e) {}
            LegoWSState.socket = null;
        }
        LegoWSState.queue = [];
        LegoWSState.status = 1;

        try {
            var ws = new WebSocket(url);
            LegoWSState.socket = ws;

            ws.onopen = function () {
                LegoWSState.status = 2;
            };
            ws.onmessage = function (evt) {
                if (typeof evt.data === "string") {
                    if (LegoWSState.queue.length > 256) {
                        LegoWSState.queue.shift();
                    }
                    LegoWSState.queue.push(evt.data);
                }
            };
            ws.onerror = function () {
                LegoWSState.status = 3;
            };
            ws.onclose = function () {
                LegoWSState.status = 0;
            };
        } catch (err) {
            LegoWSState.status = 3;
        }
    },

    LegoWS_Send: function (msgPtr) {
        if (LegoWSState.socket !== null && LegoWSState.status === 2) {
            var msg = UTF8ToString(msgPtr);
            try {
                LegoWSState.socket.send(msg);
            } catch (e) {}
        }
    },

    LegoWS_GetStatus: function () {
        return LegoWSState.status;
    },

    LegoWS_HasMessage: function () {
        return LegoWSState.queue.length > 0 ? 1 : 0;
    },

    LegoWS_PopMessage: function () {
        if (LegoWSState.queue.length === 0) {
            return 0;
        }
        var str = LegoWSState.queue.shift();
        var len = lengthBytesUTF8(str) + 1;
        var buf = _malloc(len);
        stringToUTF8(str, buf, len);
        return buf;
    },

    LegoWS_Close: function () {
        if (LegoWSState.socket !== null) {
            try { LegoWSState.socket.close(); } catch (e) {}
            LegoWSState.socket = null;
        }
        LegoWSState.status = 0;
        LegoWSState.queue = [];
    }
});
