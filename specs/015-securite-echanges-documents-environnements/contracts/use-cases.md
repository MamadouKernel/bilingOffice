# CONTRATS D'UTILISATION & CAS D'USAGE DURCIS — FEATURE 015

## Sécurité des Échanges, Protection Documentaire & Cloisonnement d'Environnement

---

### 1. CONTRATS TECHNIQUES DE SÉCURITÉ (C15)

#### Contrat `C15-01` — Middleware Security Headers & HTTPS Enforcement (`SEC-04`)
- **Signature** : `app.UseSecurityHeaders()` & `app.UseHsts()`
- **Comportement** :
  - Injecte systématiquement les en-têtes `X-Content-Type-Options: nosniff`, `X-Frame-Options: SAMEORIGIN`, `Referrer-Policy: strict-origin-when-cross-origin`.
  - Enforce la redirection HTTPS et le header `Strict-Transport-Security`.
- **Preuve de test** : `CIT.Plateforme.Web.Tests/Securite/SecCert04TransportTests.cs`

#### Contrat `C15-02` — Isolation GED & Intégrité Cryptographique (`SEC-04`)
- **Signature** : `IStockageDocuments.EnregistrerAsync(Stream contenu, string nomFichier, CancellationToken ct)`
- **Comportement** :
  - Stocke physiquement le flux hors de l'arborescence publique avec renommage opaque.
  - Calcule l'empreinte SHA-256 pour vérifier l'intégrité à l'extraction.
- **Preuve de test** : `CIT.Plateforme.Infrastructure.Tests/Documents/StockageSecuriseTests.cs`

#### Contrat `C15-03` — Validation d'Admission & Magic Bytes (`SEC-07`)
- **Signature** : `IValidateurFichierDocument.ValiderFormatAsync(Stream contenu, string nomFichier, long tailleOctets)`
- **Comportement** :
  - Vérifie la conformité de l'extension et des Magic Bytes d'en-tête contre la liste blanche.
  - Rejette immédiatement tout exécutable masqué ou dépassement de taille.
- **Preuve de test** : `CIT.Plateforme.Application.Tests/Documents/ControleFichiersTests.cs`

#### Contrat `C15-04` — Adaptateur Antivirus Réel Defender & Détection EICAR (`SEC-07`)
- **Signature** : `ScannerAntivirusDefender : IAnalyseAntivirus`
- **Comportement** :
  - Invoque le processus réel `MpCmdRun.exe` sur le flux temporaire.
  - Interprète les codes de sortie réels (0 = Sain, 2 = Infecté, Autre = Fail-Closed).
  - Détecte avec succès la chaîne standard EICAR.
- **Preuve de test** : `CIT.Plateforme.Infrastructure.Tests/Documents/ScannerAntivirusTests.cs`

#### Contrat `C15-05` — Cloisonnement & Gardes d'Environnement (`SEC-09`)
- **Signature** : `Program.cs` / `ConfigurationSecurityGuards`
- **Comportement** :
  - Désactive `DevelopmentSeeder` et Swagger en mode `Production`.
  - Vérifie l'absence de secrets de production dans les fichiers versionnés.
- **Preuve de test** : `CIT.Plateforme.Web.Tests/Securite/SecCert09EnvironnementTests.cs`
