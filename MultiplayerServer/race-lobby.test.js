const test = require("node:test");
const assert = require("node:assert/strict");
const { RaceLobby, ENTRY, RADIUS, insideEntry } = require("./race-lobby");

function setup() {
    const packets = [], errors = [];
    return { packets, errors, lobby: new RaceLobby(packet => packets.push(packet), (client, message) => errors.push({ client, message })) };
}
const player = id => ({ playerId: id, playerName: id, lastState: { isOnFoot: true, pos: { ...ENTRY } } });

test("only an on-foot player inside the visible circle may confirm", () => {
    const { lobby, errors } = setup();
    const p = player("A");
    p.lastState.pos.x += RADIUS + 0.01;
    lobby.join(p);
    assert.equal(lobby.slots.length, 0);
    p.lastState.pos = { ...ENTRY, y: 4 };
    lobby.join(p);
    p.lastState = { isOnFoot: false, pos: { ...ENTRY } };
    lobby.join(p);
    assert.equal(errors.length, 3);
    assert.equal(insideEntry({ isOnFoot: true, pos: { ...ENTRY, x: NaN } }), false);
    p.lastState = { isOnFoot: true, pos: { ...ENTRY } };
    lobby.join(p);
    assert.equal(lobby.slots.length, 1);
});

test("four distinct slots, idempotent confirm, full rejection, cancellation and reuse", () => {
    const { lobby, errors } = setup();
    const players = Array.from({ length: 5 }, (_, i) => player(String(i)));
    players.forEach(p => lobby.join(p));
    assert.equal(lobby.slots.length, 4);
    assert.equal(new Set(lobby.slots.map(s => s.slotIndex)).size, 4);
    assert.equal(errors.length, 1);
    lobby.join(players[0]);
    assert.equal(lobby.slots.length, 4);
    lobby.leave(players[1]);
    lobby.join(players[4]);
    assert.equal(lobby.slots.find(s => s.playerId === "4").slotIndex, 1);
});

test("start includes only confirmed players and preserves assigned grid positions", () => {
    const { lobby, packets, errors } = setup();
    const a = player("A"), b = player("B"), spectator = player("S");
    lobby.start(spectator);
    assert.equal(errors.length, 1);
    lobby.join(a); lobby.join(b); lobby.start(a);
    const start = packets.find(p => p.type === "start_race");
    assert.deepEqual(start.slots, [
        { playerId: "A", playerName: "A", slotIndex: 0 },
        { playerId: "B", playerName: "B", slotIndex: 1 }
    ]);
    lobby.start(b); lobby.join(spectator);
    assert.equal(packets.filter(p => p.type === "start_race").length, 1);
    assert.equal(errors.length, 3);
});

test("walking out or disconnecting releases a slot; start revalidates position", () => {
    const { lobby, packets } = setup();
    const a = player("A"), b = player("B");
    lobby.join(a); lobby.join(b);
    a.lastState.pos.x += 20;
    lobby.stateChanged(a);
    assert.equal(lobby.slots.length, 1);
    b.lastState.pos.z += 20;
    lobby.start(b);
    assert.equal(lobby.slots.length, 0);
    assert.equal(packets.some(p => p.type === "start_race"), false);
});

test("race completes only after every participant finishes or leaves, then accepts next race", () => {
    const { lobby } = setup();
    const a = player("A"), b = player("B");
    lobby.join(a); lobby.join(b); lobby.start(a);
    lobby.finish(a, 99);
    assert.equal(lobby.active.size, 2);
    lobby.finish(a, 1);
    assert.equal(lobby.snapshot().raceActive, true);
    lobby.leave(b);
    assert.equal(lobby.snapshot().raceActive, false);
    assert.equal(lobby.slots.length, 0);
    lobby.join(a); lobby.start(a);
    assert.equal(lobby.raceId, 2);
});
