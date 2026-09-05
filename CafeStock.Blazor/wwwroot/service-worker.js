// Service worker mínimo para que CafeStock cumpla los requisitos de
// instalabilidad de Chrome (PWA con ventana propia en Android/escritorio).
//
// NO cachea nada a propósito: CafeStock es Blazor Server y necesita una
// conexión viva (SignalR/WebSocket) para funcionar, así que el modo offline
// no aplica. El handler `fetch` deja pasar todas las peticiones a la red tal
// cual; su única función es existir, que es lo que Chrome exige para ofrecer
// "Instalar aplicación" en vez de un simple acceso directo.

self.addEventListener('install', () => self.skipWaiting());

self.addEventListener('activate', (event) => event.waitUntil(self.clients.claim()));

self.addEventListener('fetch', () => {
    // Passthrough: sin respondWith, el navegador resuelve la petición con su
    // comportamiento de red por defecto. No se toca /_blazor ni el WebSocket.
});
