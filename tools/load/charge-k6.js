// Test de charge HTTP — plateforme YABA (k6).
//
// PORTÉE : mesure le chemin HTTP (rendu de page serveur, health, redirections). Il NE mesure PAS
// le coût mémoire des circuits Blazor Server persistants (WebSocket) à grande échelle — cela exige
// un client SignalR/Blazor dédié et une instance dimensionnée. Voir docs/102 §12.
//
// PRÉREQUIS : installer k6 (https://k6.io). Lancer une instance de l'app, puis :
//   k6 run -e BASE=https://localhost:7097 tools/load/charge-k6.js
// (insecureSkipTLSVerify est activé plus bas pour le certificat de dev.)
//
// Ajuster `stages` pour viser un palier de charge (ici montée jusqu'à 200 utilisateurs virtuels).
// Pour approcher « 10 000 connexions », il faut plusieurs générateurs de charge distribués ET une
// cible dimensionnée/scale-out — un poste unique ne génère pas 10 000 VUs de façon fiable.

import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  insecureSkipTLSVerify: true,
  stages: [
    { duration: '30s', target: 50 },   // montée
    { duration: '1m', target: 200 },   // palier
    { duration: '30s', target: 0 },    // descente
  ],
  thresholds: {
    // Objectif NFR-04 : temps de réponse < 3 s. Le p95 doit rester sous 3 s.
    http_req_duration: ['p(95)<3000'],
    http_req_failed: ['rate<0.05'],    // < 5 % d'échecs (429 inclus si le rate limiter mord)
  },
};

const BASE = __ENV.BASE || 'https://localhost:7097';

export default function () {
  // Pages publiques représentatives (pas d'authentification requise).
  const connexion = http.get(`${BASE}/comptes/connexion`);
  check(connexion, {
    'connexion 200 ou 429 (rate limiter)': (r) => r.status === 200 || r.status === 429,
  });

  const ready = http.get(`${BASE}/ready`);
  check(ready, { 'ready 200': (r) => r.status === 200 });

  sleep(1);
}
