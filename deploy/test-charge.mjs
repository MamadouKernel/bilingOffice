// NFR-04 / NFR-05 — Test de charge YABA : N utilisateurs simultanés, chacun connecté avec un compte
// de recette, enchaînent les pages de leur rôle pendant D secondes. Mesure p50/p95/p99 et erreurs.
//
//   node deploy/test-charge.mjs --url https://localhost:7097 --utilisateurs 50 --duree 60
//
// Connexion : point /uat/session (instance de recette/développement uniquement — il n'existe pas
// en production, où la DSI joue le test sur la préproduction).
import { writeFileSync } from "node:fs";

const arg = (nom, defaut) => { const i = process.argv.indexOf(`--${nom}`); return i > 0 ? process.argv[i + 1] : defaut; };
const URL_BASE = arg("url", "https://localhost:7097");
const UTILISATEURS = +arg("utilisateurs", 50);
const DUREE = +arg("duree", 60) * 1000;
const SEUIL_P95_MS = 3000; // NFR-04

const PARCOURS = {
  "uat.client.normal@local.test": ["/client/dashboard", "/client/demandes", "/notifications", "/avoir-exoneration/mes-demandes", "/reclamations/mes-reclamations"],
  "uat.collab.global@local.test": ["/client/dashboard", "/client/demandes", "/rendez-vous/mes-rendez-vous"],
  "uat.client.compte@local.test": ["/client/dashboard", "/client/demandes"],
  "uat.pilote.manifeste@local.test": ["/agent/dashboard", "/billing-import/manifeste"],
  "uat.facturation@local.test": ["/agent/dashboard", "/billing-import/facturation"],
  "uat.caisse@local.test": ["/agent/dashboard", "/billing-import/validation-paiement"],
  "uat.agent.gate@local.test": ["/agent/dashboard", "/enrolement/file-agent-gate"],
  "uat.superviseur@local.test": ["/supervision"],
};
const comptes = Object.keys(PARCOURS);

async function connecter(email) {
  const r = await fetch(`${URL_BASE}/uat/session/${encodeURIComponent(email)}`, { redirect: "manual" });
  const cookies = r.headers.getSetCookie().map((c) => c.split(";")[0]).join("; ");
  if (!cookies) throw new Error(`Connexion impossible pour ${email} (HTTP ${r.status})`);
  return cookies;
}

const mesures = [];
async function utilisateur(n, fin) {
  const email = comptes[n % comptes.length];
  const cookies = await connecter(email);
  const pages = PARCOURS[email];
  let k = n;
  while (Date.now() < fin) {
    const page = pages[k++ % pages.length];
    const debut = performance.now();
    let statut = 0;
    try {
      const r = await fetch(URL_BASE + page, { headers: { cookie: cookies }, redirect: "manual" });
      await r.arrayBuffer();
      statut = r.status;
    } catch { statut = -1; }
    mesures.push({ page, ms: performance.now() - debut, statut });
    await new Promise((ok) => setTimeout(ok, 500 + Math.random() * 1500)); // temps de lecture
  }
}

const centile = (valeurs, p) => { const t = [...valeurs].sort((a, b) => a - b); return t.length ? Math.round(t[Math.min(t.length - 1, Math.floor(p / 100 * t.length))]) : 0; };

console.log(`Test de charge : ${UTILISATEURS} utilisateurs simultanés, ${DUREE / 1000} s, ${URL_BASE}`);
const fin = Date.now() + DUREE;
const debut = Date.now();
await Promise.all(Array.from({ length: UTILISATEURS }, (_, n) => utilisateur(n, fin)));
const secondes = (Date.now() - debut) / 1000;

const ok = mesures.filter((m) => m.statut >= 200 && m.statut < 400);
const erreurs = mesures.length - ok.length;
const temps = ok.map((m) => m.ms);
const parPage = Object.fromEntries([...new Set(mesures.map((m) => m.page))].map((p) => {
  const t = ok.filter((m) => m.page === p).map((m) => m.ms);
  return [p, { requetes: t.length, p50: centile(t, 50), p95: centile(t, 95) }];
}));
const rapport = {
  url: URL_BASE, utilisateurs: UTILISATEURS, dureeSecondes: Math.round(secondes), requetes: mesures.length,
  debitParSeconde: +(mesures.length / secondes).toFixed(1), erreurs, tauxErreurPct: +(100 * erreurs / Math.max(1, mesures.length)).toFixed(2),
  p50: centile(temps, 50), p95: centile(temps, 95), p99: centile(temps, 99), max: Math.round(Math.max(0, ...temps)),
  statutsErreur: Object.entries(mesures.filter((m) => !(m.statut >= 200 && m.statut < 400)).reduce((a, m) => ({ ...a, [m.statut]: (a[m.statut] ?? 0) + 1 }), {})),
  parPage, verdictNfr04: centile(temps, 95) < SEUIL_P95_MS && erreurs === 0 ? "CONFORME" : "NON CONFORME",
};
writeFileSync(arg("sortie", "deploy/rapport-test-charge.json"), JSON.stringify(rapport, null, 2));
console.table(parPage);
console.log(rapport);
process.exit(rapport.verdictNfr04 === "CONFORME" ? 0 : 1);
