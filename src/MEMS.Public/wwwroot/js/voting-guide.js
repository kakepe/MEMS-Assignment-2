// Browser-only choices. This module has no network, telemetry or cookie operations.
const prefix = 'mems.guide.v1.';
const unavailable = "Session storage is unavailable. Allow this site's session storage and reload to use your guide.";
function key(tenant) {
    if (!Number.isSafeInteger(tenant) || tenant <= 0) throw new Error('Invalid municipality.');
    return prefix + tenant;
}
function unique(ids) {
    return [...new Set(ids.filter(id => Number.isInteger(id) && id > 0 && id <= 2147483647))];
}
export function read(tenant) {
    try {
        const storageKey = key(tenant), raw = sessionStorage.getItem(storageKey);
        if (raw === null) return { ids: [], error: null, notice: null };
        let data;
        try { data = JSON.parse(raw); } catch { data = null; }
        if (!data || data.version !== 1 || data.municipalityId !== tenant || !Array.isArray(data.ids)) {
            sessionStorage.removeItem(storageKey);
            return { ids: [], error: null, notice: 'Invalid saved guide data was cleared for this municipality.' };
        }
        const ids = unique(data.ids);
        if (JSON.stringify(data.ids) !== JSON.stringify(ids)) {
            const result = write(tenant, ids);
            if (result.error) return result;
        }
        return { ids, error: null, notice: null };
    } catch { return { ids: [], error: unavailable, notice: null }; }
}
export function write(tenant, ids) {
    try {
        const storageKey = key(tenant), clean = unique(ids);
        if (clean.length) sessionStorage.setItem(storageKey, JSON.stringify({ version: 1, municipalityId: tenant, ids: clean }));
        else sessionStorage.removeItem(storageKey);
        return { ids: clean, error: null, notice: null };
    } catch { return { ids: [], error: unavailable, notice: null }; }
}
export function stampPrintDate() {
    const now = new Date();
    for (const element of document.querySelectorAll('[data-guide-print-date]')) {
        element.dateTime = now.toISOString();
        element.textContent = now.toLocaleString(undefined, { dateStyle: 'long', timeStyle: 'short' });
    }
}
window.addEventListener('beforeprint', stampPrintDate);
export function printGuide() { stampPrintDate(); window.print(); }
