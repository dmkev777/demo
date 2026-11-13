const test = require("node:test");
const assert = require("node:assert/strict");
const { spawn } = require("node:child_process");
const { ENTRY } = require("./race-lobby");

test("real WebSocket clients: consent, spectator isolation, spoof rejection and disconnect cleanup", { skip: typeof WebSocket === "undefined", timeout: 15000 }, async t => {
    const child = spawn(process.execPath, [require.resolve("./server")], { env: { ...process.env, PORT: "0" }, windowsHide: true });
    const clients = [];
    t.after(() => { clients.forEach(c => c.ws.close()); child.kill(); });
    const url = await new Promise((resolve, reject) => {
        let output = "";
        const timeout = setTimeout(() => reject(new Error("server startup timed out: " + output)), 5000);
        child.once("error", reject);
        child.stdout.on("data", data => {
            output += data;
            const match = output.match(/WebSocket URL: (ws:\/\/localhost:\d+)/);
            if (match) { clearTimeout(timeout); resolve(match[1]); }
        });
    });
    async function connect(id, room = "TEST") {
        const ws = new WebSocket(url);
        const packets = [];
        ws.addEventListener("message", event => packets.push(JSON.parse(event.data)));
        await new Promise((resolve, reject) => { ws.addEventListener("open", resolve, { once: true }); ws.addEventListener("error", reject, { once: true }); });
        const client = {
            ws, packets,
            send(type, extra = {}) { ws.send(JSON.stringify({ type, playerId: id, playerName: id, room, ...extra })); },
            async wait(predicate) {
                const deadline = Date.now() + 3000;
                while (Date.now() < deadline) {
                    const index = packets.findIndex(predicate);
                    if (index >= 0) return packets.splice(index, 1)[0];
                    await new Promise(resolve => setTimeout(resolve, 10));
                }
                throw new Error(`${id}: expected packet missing: ${JSON.stringify(packets)}`);
            }
        };
        clients.push(client);
        client.send("join", { isOnFoot: true, pos: ENTRY });
        await client.wait(p => p.type === "race_lobby");
        client.send("state", { isOnFoot: true, pos: ENTRY });
        return client;
    }
    const a = await connect("A"), b = await connect("B"), spectator = await connect("S"), otherRoom = await connect("OTHER", "OTHER_ROOM");
    a.send("race_join");
    await a.wait(p => p.type === "race_lobby" && p.slots.length === 1);
    b.send("race_join");
    await b.wait(p => p.type === "race_lobby" && p.slots.length === 2);
    spectator.send("race_start_request");
    await spectator.wait(p => p.type === "race_error");
    spectator.send("start_race", { slots: [{ playerId: "S", slotIndex: 0 }] });
    spectator.send("race_lobby", { playerId: "SERVER", slots: [] });
    a.send("race_start_request");
    const startA = await a.wait(p => p.type === "start_race");
    const startB = await b.wait(p => p.type === "start_race");
    assert.equal(startA.playerId, "SERVER");
    assert.deepEqual(startA, startB);
    assert.deepEqual(startA.slots.map(p => p.playerId), ["A", "B"]);
    const spectatorRoster = await spectator.wait(p => p.type === "start_race");
    assert.equal(spectatorRoster.slots.some(p => p.playerId === "S"), false);
    assert.equal(otherRoom.packets.some(p => p.type === "start_race"), false);
    a.send("race_finish", { raceId: startA.raceId });
    b.ws.close();
    await spectator.wait(p => p.type === "race_lobby" && p.slots.length === 0 && !p.raceActive);
    spectator.send("race_join");
    const next = await spectator.wait(p => p.type === "race_lobby" && p.slots.length === 1 && p.slots[0].playerId === "S");
    assert.equal(next.slots[0].slotIndex, 0);
    spectator.send("leave");
    await a.wait(p => p.type === "race_lobby" && p.slots.length === 0 && !p.raceActive);
});
