# Prêt d'exploitation — rétention et purge

> **Document** : `docs/73-retention-operations-readiness.md`
> **Établi le** : 2026-09-03
> **Verdict** : **`TECHNICALLY_READY_FOR_SCHEDULING`** — la cadence reste une décision externe.

---

## 1. Ce qui est livré

| Élément | Emplacement | État |
|:---|:---|:---|
| Moteur d'éligibilité (pur) | `Application/Securite/MoteurRetentionService.cs` | Livré — aucune dépendance temporelle directe |
| Exécution | `Infrastructure/Securite/GestionnaireRetentionService.cs` | Livré |
| Horloge injectée | `IHorloge` | Respectée |
| Gel légal | `ActiverGelLegalAsync` / `DesactiverGelLegalAsync` | Livré |
| Purge des brouillons expirés | `PurgerBrouillonsExpiresAsync` | Livré |
| Anonymisation des dossiers échus | `AnonymiserDossiersExpiresAsync` | Livré |
| Couverture de test | `SecCert06RetentionTests` — 9 tests | Verte |

Le gel légal est respecté par construction : la sélection exclut `EstSousGelLegal`.

---

## 2. G-02 — ce qui manque, et pourquoi ce n'est pas un oubli technique

`GestionnaireRetentionService` est enregistré en `AddScoped` et **n'a aucun appelant en production** :
ni service de fond, ni tâche planifiée, ni commande d'administration. La purge ne s'exécute donc
jamais d'elle-même.

**Ce n'est pas corrigible sans décider.** Programmer une purge suppose d'en fixer la périodicité, et
le CDC n'en fixe aucune. Inventer « une fois par nuit » serait un choix métier déguisé en correctif
technique — sur des suppressions **irréversibles**.

---

## 3. Ce qui est techniquement prêt

| Propriété | État | Remarque |
|:---|:---|:---|
| Déclenchement explicite | **Prêt** | Le service est résoluble et appelable |
| Idempotence | **Prête** | Une seconde exécution ne retrouve rien à traiter |
| Sûreté au premier passage | **Prête** | Le gel légal et les seuils bornent la sélection |
| Horloge testable | **Prête** | `IHorloge` injectée — les invariants sont vérifiables |
| Journalisation | **Présente** | `ILogger` injecté et utilisé |
| Trace d'audit de la purge | **À définir** | Le contrat d'audit d'une purge n'est pas spécifié |
| Mode simulation (dry-run) | **Absent** | Voir § 4 |
| Verrouillage multi-instance | **Absent** | Voir § 4 |

---

## 4. Réserves à lever avant tout déclenchement automatique

**Aucun mode simulation.** Il n'existe pas de moyen de demander « que supprimerais-tu ? » sans
supprimer. Sur une opération irréversible, c'est le premier filet qu'on souhaite avoir. À ajouter
avant la première exécution réelle.

**Aucun verrou multi-instance.** Deux instances déclenchées simultanément exécuteraient la purge en
parallèle. Sans volumétrie ni décision de répartition de charge (`docs/58 § 6`), le point reste
ouvert.

**Aucune trace d'audit dédiée.** Le service journalise, mais rien ne garantit qu'une purge laisse
une entrée dans le journal d'audit métier — alors qu'elle supprime des données réglementées.

---

## 5. Décision attendue

| Question | Détenteur |
|:---|:---|
| Périodicité de la purge | **Métier + DSI** |
| Mode de déclenchement — service de fond, tâche planifiée, action d'administration | DSI |
| Contrat d'audit d'une purge | Métier |
| Fenêtre d'exécution autorisée | Exploitation |

**`SCHEDULE_EXTERNAL_DECISION_REQUIRED`** — le code est prêt à être déclenché ; personne n'a encore
dit quand.
