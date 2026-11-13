// Geometry matches LegoTrackAndWorldBuilder's first two waypoints and entry ring.
const t = Math.PI * 2 / 36;
const dx = Math.sin(t) * (165 + Math.sin(t * 2) * 38);
const dz = Math.cos(t) * (135 + Math.cos(t * 3) * 28) - 163;
const length = Math.hypot(dx, dz);
const forward = { x: dx / length, z: dz / length };
const ENTRY = { x: -forward.x * 5.5 - forward.z * 14, y: 0, z: 163 - forward.z * 5.5 + forward.x * 14 };
const CAPACITY = 4;
const RADIUS = 2.75;

function insideEntry(state) {
    const p = state && state.pos;
    return state && state.isOnFoot === true && p &&
        [p.x, p.y, p.z].every(Number.isFinite) &&
        Math.hypot(p.x - ENTRY.x, p.z - ENTRY.z) <= RADIUS && Math.abs(p.y - ENTRY.y) <= 3;
}

class RaceLobby {
    constructor(broadcast, reject) {
        this.broadcast = broadcast;
        this.reject = reject;
        this.slots = [];
        this.active = new Set();
        this.raceId = 0;
    }

    snapshot() {
        return { type: "race_lobby", slots: this.slots.map(({ playerId, playerName, slotIndex }) =>
            ({ playerId, playerName, slotIndex })), raceActive: this.active.size > 0, raceId: this.raceId };
    }

    publish() { this.broadcast(this.snapshot()); }

    join(client) {
        if (this.active.size) return this.reject(client, "Cuoc dua dang dien ra. Cho luot tiep theo.");
        if (!insideEntry(client.lastState)) return this.reject(client, "Ban phai dung trong vong tron de xac nhan.");
        if (this.slots.some(slot => slot.client === client)) return this.publish();
        if (this.slots.length >= CAPACITY) return this.reject(client, "Da du 4 slot. Hay cho luot tiep theo.");
        const slotIndex = Array.from({ length: CAPACITY }, (_, i) => i)
            .find(i => !this.slots.some(slot => slot.slotIndex === i));
        this.slots.push({ client, playerId: client.playerId, playerName: client.playerName, slotIndex });
        this.publish();
    }

    stateChanged(client) {
        if (!this.active.size && !insideEntry(client.lastState)) this.leave(client);
    }

    start(client) {
        if (this.active.size) return this.reject(client, "Cuoc dua da bat dau.");
        // Revalidate every slot immediately before committing the roster.
        this.slots = this.slots.filter(slot => insideEntry(slot.client.lastState));
        this.publish();
        if (!this.slots.some(slot => slot.client === client))
            return this.reject(client, "Hay vao vong tron va xac nhan slot truoc.");
        this.raceId++;
        this.active = new Set(this.slots.map(slot => slot.client));
        this.broadcast({ ...this.snapshot(), type: "start_race" });
        this.publish();
    }

    leave(client) {
        const before = this.slots.length;
        this.slots = this.slots.filter(slot => slot.client !== client);
        const wasRacing = this.active.delete(client);
        if (wasRacing && !this.active.size) this.slots = [];
        if (before !== this.slots.length || wasRacing) this.publish();
    }

    finish(client, raceId) {
        if (raceId !== this.raceId || !this.active.delete(client)) return;
        if (!this.active.size) this.slots = [];
        this.publish();
    }
}

module.exports = { RaceLobby, ENTRY, CAPACITY, RADIUS, insideEntry };
