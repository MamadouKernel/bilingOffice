# Fiche de tests — Plateforme YABA (recette manuelle)

> Parcours de test manuels pour valider l'application de bout en bout. Complète les ~1 900 tests
> automatisés (Domain 253 · Application 1001 · Infrastructure 192 · Web 519). Cochez ✅/❌ et notez
> l'anomalie. Environnement : Development, `https://localhost:7097` (voir `docs/104` pour le lancement).

**Comptes** : `client.demo@cit-demo.ci` (client) · `m.konate@cotedivoireterminal.com` (agent/admin CIT).
Mot de passe = `CIT_TEST_USERS_PASSWORD`. 2FA (TOTP) à activer au préalable.

Légende résultat : **P** = Passé · **É** = Échec · **N/A**.

---

## 0. Environnement & lancement
| # | Étape | Résultat attendu | P/É |
|---|---|---|---|
| 0.1 | `dotnet run --launch-profile https` | Démarre, logs « Amorçage démo … terminé » | ☐ |
| 0.2 | Ouvrir `https://localhost:7097` | Page d'accueil publique s'affiche | ☐ |
| 0.3 | `/ready` | JSON `status: Healthy` (statuts seuls) | ☐ |

## 1. Authentification & 2FA
| # | Étape | Résultat attendu | P/É |
|---|---|---|---|
| 1.1 | Connexion avec mauvais mot de passe | Refus, message générique (pas d'indice sur l'existence du compte) | ☐ |
| 1.2 | 5 échecs consécutifs | Compte verrouillé (temporisation) | ☐ |
| 1.3 | Connexion valide (1ʳᵉ fois) | Redirection vers activation 2FA (QR affiché **localement**) | ☐ |
| 1.4 | Saisir un mauvais code 2FA | Refus | ☐ |
| 1.5 | Saisir le bon code TOTP | Accès accordé, arrivée sur le tableau de bord du rôle | ☐ |
| 1.6 | Utiliser un **code de secours** | Connexion OK, code consommé (non réutilisable) | ☐ |
| 1.7 | Déconnexion | Session fermée, retour accueil ; re-accès à une page protégée → login | ☐ |

## 2. Espace Client (`client.demo@cit-demo.ci`)
| # | Étape | Résultat attendu | P/É |
|---|---|---|---|
| 2.1 | Tableau de bord client | Indicateurs + accès « Mes demandes » | ☐ |
| 2.2 | « Mes demandes » | Les 4 dossiers démo visibles (nouveau / en cours / attente complément / clôturé) | ☐ |
| 2.3 | Ouvrir le dossier en **attente de complément** | « Mes actions requises » signale l'attente | ☐ |
| 2.4 | Soumettre une **nouvelle demande** d'import | Formulaire validé, ticket créé, apparaît dans la liste | ☐ |
| 2.5 | Enregistrer un **brouillon** puis y revenir | Brouillon repris tel quel | ☐ |
| 2.6 | Assistant YABA : « qu'attend-on de moi ? » | Réponse fondée sur les vraies données du dossier | ☐ |
| 2.7 | Assistant en anglais (bascule EN) | Réponse en anglais | ☐ |
| 2.8 | Préférences de notification | In-App + Email seulement (pas de SMS/WhatsApp) | ☐ |
| 2.9 | Centre de notifications | Notifications listées, marquage lu/non-lu | ☐ |
| 2.10 | Export de la liste (xlsx / pdf) | Fichier téléchargé, contenu cohérent | ☐ |

## 3. Espace Agent / Supervision (`m.konate@…`)
| # | Étape | Résultat attendu | P/É |
|---|---|---|---|
| 3.1 | File de traitement | Tickets de la file affichés selon habilitation | ☐ |
| 3.2 | Prendre en charge un ticket « nouveau » | Statut → En cours, agent assigné | ☐ |
| 3.3 | Mettre en attente client puis répondre | Transitions et messagerie du ticket correctes | ☐ |
| 3.4 | Supervision — vue par file | Compteurs (non affectés / en cours / attente), SLA proche/dépassement, ancienneté | ☐ |
| 3.5 | Supervision — vue par opérateur | Charge par opérateur, disponibilité | ☐ |
| 3.6 | Temps réel : agir sur un ticket dans un 2ᵉ onglet | La supervision se **rafraîchit** sans recharger | ☐ |
| 3.7 | Réaffecter / transférer un ticket | Réaffectation tracée, périmètre respecté | ☐ |
| 3.8 | Modifier la priorité | Ordre dans la file mis à jour | ☐ |

## 4. Administration (`m.konate@…`)
| # | Étape | Résultat attendu | P/É |
|---|---|---|---|
| 4.1 | Comptes agents : lister | Agents + permissions affichés | ☐ |
| 4.2 | Attribuer / retirer une habilitation | Effet immédiat sur l'accès de l'agent | ☐ |
| 4.3 | Référentiels (files, priorités, SLA) | Création/modif prises en compte | ☐ |
| 4.4 | Modèles / règles de notification | Édition possible ; interpolation des jetons | ☐ |
| 4.5 | Journal d'audit | Actions sensibles tracées (auteur, horodatage) | ☐ |
| 4.6 | Archives | Tickets archivés consultables (lecture seule), filtres module/date | ☐ |

## 5. Cloisonnement & sécurité (tests négatifs)
| # | Étape | Résultat attendu | P/É |
|---|---|---|---|
| 5.1 | En client, accéder à l'URL d'un dossier d'une **autre** société | Accès refusé (jamais de fuite de données) | ☐ |
| 5.2 | En client, accéder à une page **agent/admin** | Accès refusé / redirection | ☐ |
| 5.3 | Accéder à une page protégée **sans être connecté** | Redirection vers la connexion | ☐ |
| 5.4 | Appeler `POST /api/v1/...` sans jeton | 401 (pas de page HTML, pas de fuite) | ☐ |
| 5.5 | Tenter d'ouvrir `…/appsettings.json` | 404 | ☐ |
| 5.6 | Agent : voir un ticket hors de son périmètre de file | Refusé | ☐ |

## 6. Transverse (i18n, thème, résilience)
| # | Étape | Résultat attendu | P/É |
|---|---|---|---|
| 6.1 | Basculer FR ⇆ EN sur plusieurs écrans | Traductions cohérentes, aucune clé brute affichée | ☐ |
| 6.2 | Basculer thème clair ⇆ sombre | Rendu correct, contrastes lisibles | ☐ |
| 6.3 | Vue mobile (redimensionner) | Mise en page responsive utilisable | ☐ |
| 6.4 | Naviguer vers une URL inexistante | Page 404 propre (pas de stacktrace) | ☐ |
| 6.5 | E-mail (si SMTP activé) : déclencher une notification | E-mail reçu, **habillage HTML de marque** + bouton lien | ☐ |

## 7. Modules métier spécifiques (si applicables au périmètre testé)
| # | Module | Parcours clé | P/É |
|---|---|---|---|
| 7.1 | Billing Import | Soumission (unicité BL), circuit files, clôture | ☐ |
| 7.2 | Billing Export | Soumission booking (unicité), branches IES | ☐ |
| 7.3 | Customer Complaint | Nouvelle réclamation, instruction, réponse | ☐ |
| 7.4 | Avoir & Exonération | Demande, instruction par type, décision | ☐ |
| 7.5 | Enrôlement transporteurs | Enrôlement chauffeur/camion (société), champs exigés par type | ☐ |
| 7.6 | Guichet / Rendez-vous | Prise de RDV, continuité guichet | ☐ |
| 7.7 | Simulation proforma | Simulation tarifaire, cloisonnement | ☐ |

---

## Suivi d'anomalies
| # test | Sévérité (Bloquant/Majeur/Mineur) | Description | Statut |
|---|---|---|---|
|  |  |  |  |

> Rappel : les scénarios de recette automatisés correspondants existent (`RecSce01→14`, E2E `Parcours*`,
> suites `Securite/`). Cette fiche sert à la validation **manuelle** avant démo/livraison.
