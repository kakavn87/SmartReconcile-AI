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
