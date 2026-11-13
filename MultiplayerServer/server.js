/**
 * LEGO 2K Drive & Minecraft City — Zero-Dependency WebSocket Room Server
 * Chay ngay bang lenh: node MultiplayerServer/server.js (Khong can npm install!)
 * Ho tro ca Unity Editor/PC (ws://) va Unity WebGL tren trinh duyet (ws:// hoac wss:// khi deploy len Render/Railway).
 */

const http = require("http");
const crypto = require("crypto");
const fs = require("fs");
const path = require("path");

const PORT = process.env.PORT || 8080;
const PUBLIC_DIR = path.join(__dirname, "public");

// Map: roomCode -> Map(socketId -> { socket, playerId, playerName, lastJoinPacket, lastStatePacket })
const rooms = new Map();
let nextSocketId = 1;

const MIME_TYPES = {
    ".html": "text/html; charset=utf-8",
    ".js": "application/javascript; charset=utf-8",
    ".wasm": "application/wasm",
    ".data": "application/octet-stream",
    ".json": "application/json; charset=utf-8",
    ".png": "image/png",
    ".jpg": "image/jpeg",
    ".ico": "image/x-icon",
    ".css": "text/css; charset=utf-8"
};

const server = http.createServer((req, res) => {
    // Serve Unity WebGL build if index.html exists in MultiplayerServer/public/
    let reqPath = (req.url || "/").split("?")[0];
    if (reqPath === "/") reqPath = "/index.html";

    const safePath = path.normalize(reqPath).replace(/^(\.\.[\/\\])+/, "");
    const fullPath = path.join(PUBLIC_DIR, safePath);

    if (fs.existsSync(fullPath) && fs.statSync(fullPath).isFile()) {
        const headers = {};
        let ext = path.extname(fullPath).toLowerCase();

        // Support Unity WebGL .gz and .br compressed builds
        if (ext === ".gz") {
            headers["Content-Encoding"] = "gzip";
            ext = path.extname(fullPath.slice(0, -3)).toLowerCase();
        } else if (ext === ".br") {
            headers["Content-Encoding"] = "br";
            ext = path.extname(fullPath.slice(0, -3)).toLowerCase();
        }

        headers["Content-Type"] = MIME_TYPES[ext] || "application/octet-stream";
        res.writeHead(200, headers);
        fs.createReadStream(fullPath).pipe(res);
        return;
    }

    res.writeHead(200, { "Content-Type": "text/plain; charset=utf-8" });
    let info = "=== LEGO 2K DRIVE & MINECRAFT CITY MULTIPLAYER SERVER ===\n";
    info += `Status: ONLINE (Port ${PORT})\n`;
    info += `Active Rooms: ${rooms.size}\n`;
    for (const [roomName, clients] of rooms.entries()) {
        info += ` - Room [${roomName}]: ${clients.size} player(s)\n`;
    }
    info += "\nTip: Build Unity WebGL into 'MultiplayerServer/public' to play directly at this URL!\n";
    res.end(info);
});

server.on("upgrade", (req, socket) => {
    const key = req.headers["sec-websocket-key"];
    if (!key) {
        socket.destroy();
        return;
    }

    const acceptKey = crypto
        .createHash("sha1")
        .update(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11", "binary")
        .digest("base64");

    const headers = [
        "HTTP/1.1 101 Switching Protocols",
        "Upgrade: websocket",
        "Connection: Upgrade",
        `Sec-WebSocket-Accept: ${acceptKey}`,
        "\r\n"
    ];
    socket.write(headers.join("\r\n"));
    socket.setNoDelay(true);

    const client = {
        id: nextSocketId++,
        socket,
        room: null,
        playerId: null,
        playerName: "Player",
        lastJoinJson: null,
        lastStateJson: null,
        buffer: Buffer.alloc(0)
    };

    socket.on("data", (chunk) => {
        client.buffer = Buffer.concat([client.buffer, chunk]);
        while (true) {
            const frame = tryParseFrame(client.buffer);
            if (!frame) break;
            client.buffer = client.buffer.slice(frame.totalLength);

            if (frame.opcode === 0x8) {
                // Close frame
                handleDisconnect(client);
                socket.end();
                return;
            } else if (frame.opcode === 0x9) {
                // Ping -> Pong
                sendFrame(socket, 0xA, frame.payload);
            } else if (frame.opcode === 0x1) {
                // Text frame (JSON packet)
                const text = frame.payload.toString("utf8");
                handleIncomingMessage(client, text);
            }
        }
    });

    socket.on("close", () => handleDisconnect(client));
    socket.on("error", () => handleDisconnect(client));
});

function tryParseFrame(buf) {
    if (buf.length < 2) return null;
    const firstByte = buf[0];
    const secondByte = buf[1];
    const opcode = firstByte & 0x0f;
    const isMasked = (secondByte & 0x80) !== 0;
    let payloadLen = secondByte & 0x7f;
    let offset = 2;

    if (payloadLen === 126) {
        if (buf.length < 4) return null;
        payloadLen = buf.readUInt16BE(2);
        offset = 4;
    } else if (payloadLen === 127) {
        if (buf.length < 10) return null;
        payloadLen = Number(buf.readBigUInt64BE(2));
        offset = 10;
    }

    const maskLen = isMasked ? 4 : 0;
    if (buf.length < offset + maskLen + payloadLen) return null;

    let maskKey = null;
    if (isMasked) {
        maskKey = buf.slice(offset, offset + 4);
        offset += 4;
    }

    const payload = Buffer.from(buf.slice(offset, offset + payloadLen));
    if (isMasked && maskKey) {
        for (let i = 0; i < payload.length; i++) {
            payload[i] ^= maskKey[i & 3];
        }
    }

    return {
        opcode,
        payload,
        totalLength: offset + payloadLen
    };
}

function sendFrame(socket, opcode, payloadBuf) {
    if (!socket || socket.destroyed) return;
    const len = payloadBuf.length;
    let header;
    if (len < 126) {
        header = Buffer.alloc(2);
        header[0] = 0x80 | opcode;
        header[1] = len;
    } else if (len < 65536) {
        header = Buffer.alloc(4);
        header[0] = 0x80 | opcode;
        header[1] = 126;
        header.writeUInt16BE(len, 2);
    } else {
        header = Buffer.alloc(10);
        header[0] = 0x80 | opcode;
        header[1] = 127;
        header.writeBigUInt64BE(BigInt(len), 2);
    }
    try {
        socket.write(Buffer.concat([header, payloadBuf]));
    } catch (e) {}
}

function sendText(socket, text) {
    sendFrame(socket, 0x1, Buffer.from(text, "utf8"));
}

function handleIncomingMessage(client, rawJson) {
    let pkt;
    try {
        pkt = JSON.parse(rawJson);
    } catch (e) {
        return;
    }
    if (!pkt || !pkt.type) return;

    const roomCode = (pkt.room || "PHONG1").toUpperCase();

    if (pkt.playerId) client.playerId = pkt.playerId;
    if (pkt.playerName) client.playerName = pkt.playerName;

    // Ensure client is registered in the target room
    if (client.room !== roomCode) {
        removeClientFromCurrentRoom(client);
        client.room = roomCode;
        if (!rooms.has(roomCode)) {
            rooms.set(roomCode, new Map());
        }
        rooms.get(roomCode).set(client.id, client);
        console.log(`[JOIN] ${client.playerName} (${client.playerId}) joined room [${roomCode}]`);

        // Send existing room players' cached join & state packets to the newly joined player
        const roomMap = rooms.get(roomCode);
        for (const [otherId, otherClient] of roomMap.entries()) {
            if (otherId !== client.id) {
                if (otherClient.lastJoinJson) sendText(client.socket, otherClient.lastJoinJson);
                if (otherClient.lastStateJson) sendText(client.socket, otherClient.lastStateJson);
            }
        }
    }

    if (pkt.type === "join" || pkt.type === "loadout") {
        client.lastJoinJson = rawJson;
    } else if (pkt.type === "state") {
        client.lastStateJson = rawJson;
    }

    // Broadcast packet to all other players in the same room
    const roomMap = rooms.get(roomCode);
    if (roomMap) {
        for (const [otherId, otherClient] of roomMap.entries()) {
            if (otherId !== client.id) {
                sendText(otherClient.socket, rawJson);
            }
        }
    }
}

function removeClientFromCurrentRoom(client) {
    if (!client.room) return;
    const roomMap = rooms.get(client.room);
    if (roomMap) {
        roomMap.delete(client.id);
        if (client.playerId) {
            const leaveMsg = JSON.stringify({
                type: "leave",
                room: client.room,
                playerId: client.playerId,
                playerName: client.playerName
            });
            for (const otherClient of roomMap.values()) {
                sendText(otherClient.socket, leaveMsg);
            }
        }
        if (roomMap.size === 0) {
            rooms.delete(client.room);
        }
    }
    client.room = null;
}

function handleDisconnect(client) {
    if (client.room) {
        console.log(`[LEAVE] ${client.playerName} (${client.playerId}) left room [${client.room}]`);
        removeClientFromCurrentRoom(client);
    }
}

server.listen(PORT, () => {
    console.log("==============================================================");
    console.log(` LEGO 2K Drive & Minecraft City Multiplayer Server is RUNNING!`);
    console.log(` WebSocket URL: ws://localhost:${PORT}`);
    console.log("==============================================================");
});

