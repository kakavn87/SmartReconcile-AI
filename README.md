# SmartReconcile AI

> **Automatisierte Auftrags-, Zahlungs- und Rechnungsabstimmung für deutsche E-Commerce-Unternehmen.**

**Entwickler:** Hai Linh Nguyen  
**Stack:** Angular 22 · TypeScript · NgRx SignalStore · RxJS · Tailwind CSS 4 · ASP.NET Core 8 · EF Core · PostgreSQL · OpenAI  
**Deployment:** Render.com

## 1. Projektziel

SmartReconcile AI ist eine Full-Stack-Demo für die automatische Abstimmung von Bestellungen, Zahlungen und Rechnungen. Das System erkennt typische Buchhaltungs- und Zahlungsprobleme automatisch und stellt sie in einem modernen deutschen Business-Dashboard dar.

Das Projekt ist bewusst produktionsnah aufgebaut: Clean Architecture im Backend, PostgreSQL als persistenter Server-State, Angular 22 mit SignalStore für den UI-State, automatische Abstimmung, AI-Fallback, Tests und CI/CD.

## 2. Gesamtarchitektur

```
Angular 22
Pages → SignalStore → API Services → HttpClient
                         │
                         │ HTTPS / REST JSON
                         ▼
ASP.NET Core 8 API
Controllers → Application → Domain
                  │
            Infrastructure
             │     │      │
           EF Core AI    Worker
             │
             ▼
       PostgreSQL / Neon
```

**Grundprinzip:** Das Backend ist die Single Source of Truth für Geschäftsdaten. Angular hält den aktuell geladenen Zustand für die UI. Nach F5 werden die benötigten Daten erneut vom Backend geladen.

# 3. Backend

## Technologie

- .NET 8
- ASP.NET Core Web API
- Clean Architecture
- Entity Framework Core
- PostgreSQL / Npgsql
- BackgroundService + PeriodicTimer
- OpenAI Responses API
- CsvHelper
- Swagger / OpenAPI
- xUnit
- Docker

## Struktur

```
src/
├── SmartReconcile.Domain/
│   ├── Entities/
│   └── Enums/
├── SmartReconcile.Application/
│   ├── Abstractions/
│   ├── DTOs/
│   └── Services/
├── SmartReconcile.Infrastructure/
│   ├── Persistence/
│   ├── Repositories/
│   ├── Services/
│   └── Workers/
├── SmartReconcile.API/
│   └── Controllers/
└── SmartReconcile.Tests/
```

### Domain

Enthält Geschäftsobjekte und Enums. Die Domain kennt weder HTTP noch EF Core noch PostgreSQL.

### Application

Orchestriert die Use Cases über Interfaces, DTOs und Services.

### Infrastructure

Implementiert PostgreSQL, EF Core, Repositories, AI, Seed-Daten, CSV-Export und Background Worker.

### API

Stellt REST-Endpunkte bereit.

Wichtige Endpunkte:

```
GET  /api/orders
GET  /api/payments
GET  /api/invoices
GET  /api/discrepancies
GET  /api/dashboard
POST /api/reconciliation/run
POST /api/seed/generate
GET  /api/export/csv
```

# 4. Reconciliation Engine

Der automatische Abgleich prüft vier Regeln.

1. **MissingPayment:** Bestellung länger als 48 Stunden `Pending` und keine erfolgreiche Zahlung.
2. **OrphanedPayment:** Zahlung erfolgreich, aber Bestellung bleibt `Pending` oder `OnHold`.
3. **AmountMismatch:** Bestellsumme und Zahlungsbetrag unterscheiden sich um mehr als 0,01 €.
4. **DuplicateInvoice:** doppelte Rechnungsnummer oder gleicher Lieferant + Betrag am selben Tag.

Offene Fälle werden dedupliziert, damit der Worker nicht bei jedem Lauf denselben Fall erneut anlegt.

# 5. Background Worker

Der Worker läuft als ASP.NET Core `BackgroundService` mit `PeriodicTimer` und führt standardmäßig alle 15 Minuten einen Abgleich aus.

```
Application start
      ↓
Background Worker
      ↓
PeriodicTimer
      ↓
ReconciliationService
      ↓
Orders + Payments + Invoices
      ↓
Rules
      ↓
Discrepancies
```

Der Worker blockiert keine HTTP-Anfragen.

# 6. AI-Analyse

Rohdaten von Rechnungen können analysiert und als verständliche deutsche Erklärung zurückgegeben werden.

Wenn `OpenAI__ApiKey` fehlt, wird ein Mock/Fallback verwendet. Die Demo darf dadurch niemals abstürzen.

# 7. Seed-Daten

`POST /api/seed/generate` erzeugt realistische Demo-Daten:

- 20 Bestellungen
- 19 Zahlungen
- 23 Rechnungen
- 5 bewusst eingebaute Fehler

Danach kann `POST /api/reconciliation/run` die Abweichungen erzeugen.

# 8. Dashboard

Das Backend liefert unter anderem:

- Gesamtumsatz
- offene Abweichungen
- betroffener Betrag
- Anzahl Bestellungen
- Anzahl Zahlungen
- Anzahl Rechnungen

# 9. Frontend

## Technologie

- Angular 22
- Standalone Components
- Signals
- NgRx SignalStore
- RxJS
- Tailwind CSS 4
- Angular Router
- HttpClient
- OnPush Change Detection
- zoneless Angular
- TypeScript

## Struktur

```
frontend/src/app/
├── core/
│   ├── api/
│   ├── models/
│   └── stores/
├── layout/
│   └── app-shell.component.ts
├── features/
│   ├── dashboard/
│   ├── orders/
│   ├── payments/
│   ├── invoices/
│   ├── discrepancies/
│   ├── reconciliation/
│   └── settings/
├── app.config.ts
├── app.routes.ts
└── app.component.ts
```

### API Layer

Die API-Services kapseln `HttpClient`. Komponenten rufen nicht direkt das Backend auf.

### Models

TypeScript-Interfaces und Enums bilden die API-Verträge ab.

### SignalStores

Feature-State wird zentral verwaltet. Aktuelle Stores:

- DashboardStore
- OrdersStore
- PaymentsStore
- InvoicesStore
- DiscrepancyStore
- ReconciliationStore

# 10. Warum NgRx SignalStore?

Dieses Projekt verwendet **NgRx SignalStore** statt des klassischen NgRx Store mit Actions, Reducers und Effects.

SignalStore verbindet:

- Angular Signals
- Feature-State
- Methoden
- RxJS
- API-Flows

Beispiel:

```ts
store.load();
store.items();
store.loading();
store.error();
```

Die Komponente kennt dadurch nicht die technische HTTP-Implementierung.

# 11. Signals vs. SignalStore vs. Backend

### Signals

Für lokalen UI-State:

- Filter
- Suche
- Sortierung
- Modal
- Auswahl

### SignalStore

Für Feature/Application-State:

- Orders
- Payments
- Invoices
- Discrepancies
- Dashboard
- Loading/Error

### Backend

Für Business-Daten:

- Bestellungen
- Zahlungen
- Rechnungen
- Verträge
- IBAN
- Berechtigungen
- Reconciliation-Ergebnisse

Das Backend bleibt immer die Quelle der Wahrheit.

# 12. Beispiel Datenfluss

```
OrdersPage
   ↓
OrdersStore.load()
   ↓
OrderApiService
   ↓
HttpClient
   ↓
ASP.NET API
   ↓
PostgreSQL
   ↓
JSON
   ↓
SignalStore
   ↓
Angular Signals
   ↓
Template
```

# 13. F5 und Server-State

SignalStore ist In-Memory-State. Bei F5 wird Angular neu gestartet und der Store neu erzeugt.

Das ist gewollt:

```
F5
 ↓
Angular startet
 ↓
Store leer
 ↓
GET Backend
 ↓
aktueller Serverzustand
```

Geschäftsdaten werden nicht als Ersatz für die Datenbank in `localStorage` gespeichert.

# 14. Vertrag / IBAN / Abrechnung

Wird eine Abrechnung geöffnet, lädt die Anwendung den aktuellen Vertrag vom Backend. Dadurch wird beispielsweise eine vorher geänderte IBAN ohne kompletten Browser-Refresh angezeigt.

```
Abrechnung öffnen
      ↓
GET Vertrag / Abrechnung
      ↓
Backend
      ↓
aktuelle Daten
      ↓
SignalStore
      ↓
UI
```

# 15. UI / Design

Das Frontend ist als modernes Finance-/SaaS-Dashboard gestaltet:

- klare Typografie
- kompakte Navigation
- KPI-Karten
- dezente Borders
- Status-Badges
- Tabellen
- Hover- und Focus-Zustände
- responsive Layouts
- deutsche Fachbegriffe
- klare visuelle Hierarchie

# 16. Lokale Entwicklung

### Backend

Voraussetzungen: .NET 8 SDK und PostgreSQL/Neon.

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/SmartReconcile.API
```

Swagger:

```
http://localhost:8080/swagger
```

### Frontend

Voraussetzungen: Node.js 22 und npm.

```bash
cd frontend
npm install
npm start
```

Angular:

```
http://localhost:4200
```

# 17. Docker

Backend:

```
.NET SDK → restore → build → publish → ASP.NET Runtime
```

Frontend:

```
Node 22 → npm install → ng build → Nginx
```

# 18. Render Deployment

### Backend

- Docker Web Service
- Frankfurt
- ASP.NET Core 8
- Render `PORT`
- Neon PostgreSQL
- Auto Deploy von `main`

Live API: https://smartreconcile-ai-api.onrender.com

Swagger: https://smartreconcile-ai-api.onrender.com/swagger

### Frontend

- Render Static Site
- Node 22 Build
- Angular 22
- Auto Deploy von `main`

Live Frontend: https://smartreconcile-ai-frontend.onrender.com

# 19. CI/CD

GitHub Actions prüft:

### Backend

```bash
dotnet restore
dotnet build
dotnet test
```

### Frontend

```bash
npm install
npm run build
```

# 20. Demo-Ablauf

1. Backend starten
2. `POST /api/seed/generate`
3. `POST /api/reconciliation/run`
4. Dashboard öffnen
5. Abweichungen prüfen
6. Fall lösen oder ignorieren
7. CSV exportieren
8. optional AI-Analyse testen

# 21. Senior-Developer-Fokus

Das Projekt demonstriert bewusst:

**Backend:** Clean Architecture, SOLID, DI, REST, EF Core, PostgreSQL, Repository Pattern, Background Processing, DTOs, Testing, Docker und CI/CD.

**Frontend:** Angular 22, Standalone Components, Signals, NgRx SignalStore, RxJS, OnPush, Feature Architecture, Routing, responsive UI und Server-State-Management.

**Cloud:** Render, Neon PostgreSQL, Docker und GitHub Actions.

# 22. Entwickler

**Hai Linh Nguyen**  
Senior Full-Stack Developer

Schwerpunkte:

- PHP / Symfony
- Angular / TypeScript
- ASP.NET Core / .NET
- REST APIs
- Clean Architecture
- PostgreSQL
- AI Integration
- Cloud Deployment

SmartReconcile AI verbindet einen realistischen Finance-Use-Case mit moderner Full-Stack-Architektur und dient als technische Portfolio-Anwendung.


## Integrations & SaaS Architecture

SmartReconcile is designed as a multi-tenant SaaS. The current demo uses generated data, while the integration architecture is prepared for real e-commerce and payment platforms.

```
Tenant
  │
  ├── Connection
  │      │
  │      └── Shopify / Shopware / WooCommerce / Amazon / Stripe...
  │
  └── WebhookEvents
           │
           ▼
     Provider Adapter
           │
           ▼
 Orders / Payments / Invoices
           │
           ▼
 Reconciliation
           │
           ▼
       AI Analysis
```

### Architecture explained

- **Tenant** – represents one customer/company in the SaaS. Tenant isolation is the foundation for supporting multiple companies securely in one application.
- **Connection** – represents a configured connection between a tenant and an external platform, for example a Shopify store, Shopware shop, WooCommerce installation or Stripe account.
- **WebhookEvents** – stores incoming external events and provides idempotent processing. Duplicate deliveries can be detected without creating duplicate business records.
- **Provider Adapter** – translates provider-specific payloads and authentication rules into SmartReconcile's common domain model. This keeps Shopify-specific logic separate from the reconciliation engine.
- **Orders / Payments / Invoices** – the normalized business data used by SmartReconcile. Different providers can feed the same internal model.
- **Reconciliation** – applies business rules to identify missing payments, orphaned payments, amount mismatches and duplicate invoices.
- **AI Analysis** – explains detected discrepancies in simple German and can analyze invoice/document information to help accounting understand the cause and recommended next action.

The long-term integration strategy is to provide a dedicated connector for each platform. A Shopify app, for example, can be installed by a merchant and configured to send relevant order and payment events securely to SmartReconcile. The same integration pattern can then be reused for Shopware, WooCommerce, Amazon, Stripe and other providers.

The demo data remains available for presentations and development. Production tenants will receive their data through integrations, webhooks and synchronization processes rather than manual test-data entry.

## 23. Financial Control Tower

SmartReconcile is evolving from a reconciliation demo into a **Financial Control Tower für E-Commerce-Unternehmen**. The product does not replace the shop, ERP or accounting system. It provides a control layer across them.

### Financial Case

A discrepancy becomes a first-class financial case instead of remaining only a table row.

```
Financial Case
  │
  ├── Discrepancy
  ├── Order
  ├── Payment
  └── Invoice
       │
       ▼
  Investigation
       │
       ├── Root Cause
       ├── Affected Amount
       └── Resolution
```

Cases support:

- Open
- Investigating
- Resolved
- Ignored

Each case has a severity level and links to the underlying financial records.

### Financial Graph

The graph stores relationships between normalized financial records.

```
Order #10042
    │
    └── Payment #P991
```

The same graph model is designed to later connect payouts and bank transactions:

```
Order
  ↓
Payment
  ↓
Payout
  ↓
Bank Transaction
```

This creates a traceable financial chain across multiple providers.

### System Health

Integration connections are exposed as system-health signals. The Control Tower reports:

- Healthy
- Warning
- Disconnected

A connection without recent activity is surfaced instead of silently appearing operational.

### Financial Integrity Score

The Control Tower calculates a 0–100 score from collection coverage, affected open-case amount, case volume and critical cases.

The score is a control signal, not an accounting number. It is designed to answer:

> **Wie sicher sind die Finanzdaten meines Unternehmens aktuell?**

### Control Tower API

```
GET  /api/control-tower
GET  /api/control-tower/cases/{id}
POST /api/control-tower/cases/{id}/investigate
POST /api/control-tower/cases/{id}/resolve
POST /api/control-tower/cases/{id}/ignore
GET  /api/control-tower/graph/{nodeType}/{entityId}
```

The next connector target is Shopify. Shopify-specific OAuth, HMAC verification, encrypted access tokens and durable webhook processing will be implemented on top of this control layer rather than inside the core reconciliation engine.



## 24. Financial Control Tower Frontend

The Angular dashboard now uses the Control Tower API as the primary financial overview.

The German dashboard shows:

- **Finanzielle Integrität** — 0–100 control score
- **Umsatz** — expected cash
- **Zahlungseingang** — successfully collected payments
- **Cash Gap** — difference between expected and actual collection
- **Finanzfälle** — open and high-risk cases requiring attention
- **Systemstatus** — health of connected integrations

The frontend uses Angular Signals and NgRx SignalStore. The server remains authoritative and the dashboard loads its state from:

```
GET /api/control-tower
```

Case actions are reflected in the SignalStore without introducing a second source of truth.

Shopify is intentionally not part of this phase. The existing reconciliation and integration foundation remains the source layer for the Control Tower.


### Current implementation phase

The current milestone focuses on making the existing reconciliation product itself strong and coherent before adding external commerce connectors. Shopify remains deliberately deferred until the Control Tower, dashboard and core workflows are stable.


## Financial Case Detail & Audit Timeline

Phase 1 der Financial-Control-Tower-Weiterentwicklung ist implementiert:

- `CaseAuditEvent` mit den Ereignistypen `Detected`, `Investigated`, `ActionExecuted`, `Resolved` und `Ignored`
- persistente Audit-Timeline in PostgreSQL über `CaseAuditEvents`
- `FinancialCase.CaseNumber` für eine stabile fachliche Fallkennung
- detaillierter Case-Endpunkt `GET /api/control-tower/cases/{id}`
- `FinancialCaseDetailDto` mit:
  - Stammdaten des Finanzfalls
  - Audit-Timeline, neueste Ereignisse zuerst
  - `Order`, `Payment` und `Invoice` als GraphNodes
  - explizitem Status `Fehlt`, wenn eine verknüpfte Entität nicht vorhanden ist
  - Root-Cause-Analyse und empfohlener Maßnahme
- `POST /api/control-tower/cases/{id}/investigate` startet die KI-Ursachenanalyse und protokolliert das Ergebnis
- `POST /api/control-tower/cases/{id}/auto-fix` führt unterstützte automatische Korrekturen aus und protokolliert die Maßnahme
- `POST /api/control-tower/cases/{id}/resolve` protokolliert die Lösung inklusive optionaler Benutzer-Notiz
- `POST /api/control-tower/cases/{id}/ignore` protokolliert das Ignorieren des Falls
- bestehender Demo-Fallback für KI bleibt erhalten, sodass die API auch ohne OpenAI API Key stabil funktioniert

Die Frontend-Implementierung des Case Detail View folgt in Phase 2. Shopify bleibt weiterhin bewusst zurückgestellt, bis der Financial Control Tower vollständig stabil ist.


## Financial Case Detail View

Phase 2 des Financial Control Towers ist implementiert:

- Route `/control-tower/cases/:id`
- eigenständiger Finanzfall-Detailbildschirm mit deutscher Finanz-/Buchhaltungsterminologie
- Visual Entity Chain `Order → Payment → Invoice`
- Zustände für vorhandene, abweichende und fehlende Entitäten (`Fehlt`)
- Root-Cause- und KI-Untersuchungsbereich
- Aktion `Automatisch korrigieren (Action)`
- Lösung `Als gelöst markieren (Resolve)` mit Buchhalter-Notiz
- Aktion `Ignorieren`
- vertikale Audit Timeline mit Akteur und Zeitstempel
- dedizierter Angular SignalStore für Laden, KI-Untersuchung, Auto-Fix, Resolve und Ignore
- Dashboard-Finanzfälle führen direkt zur Detailansicht

Die bestehende Shopify-Integration bleibt bewusst zurückgestellt.


## 25. DATEV EXTF Buchungsstapel & Accounting Settings — Phase 1

Phase 1 des DATEV-Ausbaus ist implementiert:

### Accounting Settings

Die Buchhaltungseinstellungen werden tenantbezogen gespeichert:

- Beraternummer — Standard 1001
- Mandantennummer — Standard 10001
- SKR03 / SKR04
- Erlöskonto 19 % — 8400 / 4400
- Erlöskonto 7 % — 8300 / 4300
- Zahlungs-Verrechnungskonto — 1360 / 1460
- Zahlungsgebühren — 4970 / 6855

API:

GET /api/settings/accounting
PUT /api/settings/accounting

Der aktuelle Demo-Kontext verwendet den ersten aktiven Tenant. Falls noch kein Tenant existiert, wird ein Demo-Tenant mit den Standard-Buchhaltungseinstellungen angelegt.

### DATEV EXTF

Der neue Export ist verfügbar über:

GET /api/export/datev?from=2026-01-01&to=2026-01-31&onlyReconciled=true

Der Export erzeugt einen technischen DATEV-Buchungsstapel mit:

- EXTF Header Version 700
- Formatkategorie 21
- Buchungsstapel Formatversion 13
- Berater- und Mandantennummer
- Wirtschaftsjahresbeginn und Exportperiode
- SKR03/SKR04-Kennung
- vollständiger DATEV-Buchungssatzstruktur
- Semikolon als Feldtrenner
- Dezimalkomma
- CR/LF-Zeilenenden
- Windows-1252
- TTMM als Belegdatum
- Umsatzbuchungen auf konfigurierte Erlöskonten
- Zahlungsgebühren auf das konfigurierte Gebührenkonto
- Zahlungs-Verrechnung über das konfigurierte Clearingkonto

Standard-Erlöskonten wie 8400, 8300, 4400 und 4300 werden als DATEV-Automatikkonten exportiert; das BU-Feld bleibt deshalb leer.

### Reconciliation Filter

Mit onlyReconciled=true werden nur Buchungen exportiert, wenn:

- eine erfolgreiche Zahlung vorhanden ist,
- Bestell- und Zahlungsbetrag bis auf 0,01 € übereinstimmen,
- kein offener Finanzfall für Bestellung oder Zahlung existiert.

### Datenmodell

Für den DATEV-Export wurden außerdem:

- Umsatzsteuersatz 7 % / 19 % an der Bestellung
- Zahlungsgebühr am Payment

als persistente Geschäftsdaten ergänzt.

### Tests

Der Backend-Testumfang enthält DATEV-Tests für:

- EXTF Header
- SKR03
- SKR04
- 7-%-Erlöskonto
- Zahlungsgebühren
- onlyReconciled
- ungültige Datumsbereiche

Die technische DATEV-Struktur orientiert sich an der aktuellen DATEV-Formatbeschreibung. Vor einer produktiven Steuerberatung-/DATEV-Freigabe müssen die erzeugten Musterdateien zusätzlich mit dem DATEV-Prüfprogramm und in einem DATEV-Testbestand validiert werden.
