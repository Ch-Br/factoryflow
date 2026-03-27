# PROJECT_MANIFEST

## Projektname

Arbeitstitel:
- FactoryFlow
- PlantDesk
- OpsTicket
- FlowForge

Interner Projektname:
**FactoryFlow**

---

## Produktvision

Wir entwickeln eine industrielle Ticket- und Workflow-Web-App, die als zentrales digitales Nervensystem für Produktionsbetriebe dient.

Die Plattform verbindet:
- Produktion
- Arbeitsvorbereitung
- Lager
- Einkauf
- Verkauf
- Konstruktion
- Qualität
- Instandhaltung
- Management

Das System ist nicht nur ein Ticketsystem, sondern eine operative Plattform zur:
- schnellen Meldung von Problemen
- transparenten Bearbeitung von Aufgaben
- abteilungsübergreifenden Zusammenarbeit
- Steuerung von Eskalationen
- Dokumentation von Entscheidungen
- Analyse von Engpässen
- Vorbereitung auf IoT- und KI-gestützte Automatisierung

---

## Produktziel

Ziel ist die Entwicklung einer robusten, erweiterbaren und produktionsnahen Web-App für industrielle Umgebungen.

Die Plattform soll:
- auf Desktop, Tablet und mobilen Industrie-Endgeräten funktionieren
- schnelle und einfache Bedienung in Werkshallen ermöglichen
- auch für Büroabteilungen leistungsfähig und übersichtlich sein
- streng auditierbar und nachvollziehbar arbeiten
- klare Zuständigkeiten, Rollen und Eskalationen unterstützen
- modular erweiterbar für IoT, Analytics und KI sein

---

## Nicht-Ziele für V1

Die folgenden Punkte gehören **nicht** in die erste Version, außer sie werden explizit priorisiert:

- echte verteilte Microservice-Landschaft
- vollständige KI-Automatisierung
- autonome Agenten mit Schreibrechten
- hochkomplexes BPMN-Framework
- extrem tiefe ERP/MES/PLM-Integrationen
- vollwertiges DMS als eigenes Produkt
- globale Multi-Tenant-SaaS-Architektur
- überladene Customizing-Oberflächen für alles

V1 soll ein **modularer Monolith** sein.

---

## Technische Leitplanken

### Ziel-Stack
- **Frontend:** Blazor Web App
- **Backend:** ASP.NET Core
- **Datenbank:** PostgreSQL
- **ORM:** Entity Framework Core
- **Realtime:** SignalR
- **Authentifizierung:** ASP.NET Core Identity oder OpenIddict-basierte Erweiterung
- **API-Dokumentation:** OpenAPI / Swagger
- **Deployment:** Docker / Coolify / Linux
- **Tests:** xUnit, FluentAssertions, ggf. Playwright
- **Logging:** Serilog
- **Observability später:** OpenTelemetry

### Dateiverarbeitung
- In V1 keine S3-Abhängigkeit
- Anhänge zunächst über lokale oder serverseitige Dateiablage mit sauberem Abstraktionslayer
- Dateispeicher muss später austauschbar sein

### Realtime
- Statusänderungen, Zuweisungen und Eskalationen sollen live sichtbar werden
- SignalR ist die bevorzugte Technologie

### Spätere Erweiterbarkeit
Architektur muss spätere Erweiterungen vorbereiten für:
- MQTT / IoT / ESP32
- Workflow-Erweiterungen
- Benachrichtigungskanäle
- Analytics
- KI-gestützte Assistenz
- externe Integrationen

---

## Fachliche Kernmodule

### Identity
- Benutzer
- Rollen
- Teams
- Abteilungen
- Standorte
- optionale Schichtmodelle
- Berechtigungen

### Tickets
- Ticket-Erfassung
- Ticket-Stammdaten
- Prioritäten
- Kategorien
- Ticket-Typen
- Kommentare
- Historie
- Beziehungen
- Parent/Child-Tickets

### Workflows
- Zustände
- Übergänge
- Regeln
- Pflichtfelder je Zustand
- Freigaben
- automatische Aktionen
- Eskalationsmechanismen

### Assignments
- Team-Zuweisung
- Besitzer
- Eskalationspfade
- Vertretungen
- schichtabhängige Zuständigkeiten

### Notifications
- interne Benachrichtigungen
- E-Mail-Versand
- Realtime-Updates
- Erinnerungen
- Eskalationshinweise

### Attachments
- Dateimetadaten
- Dateiablage-Abstraktion
- Upload/Download
- Rechteprüfung
- Verknüpfung mit Tickets

### Assets
- Maschinen
- Anlagen
- Arbeitsplätze
- Produktionslinien
- technische Objekte
- optionale Wartungshistorie

### Warehouse
- Lagerkontext
- Lagerplätze
- Materialbezug
- Scan-/QR-/Barcode-Bezüge
- lagerrelevante Tickettypen

### Procurement
- Bestellanforderungen
- beschaffungsbezogene Tickets
- Genehmigungslogik
- Lieferantenkontext

### Engineering
- Änderungsanträge
- Konstruktionsrückmeldungen
- technische Problemstellungen
- Dokument- und Bildbezüge

### Audit
- revisionssichere Ereignisprotokolle
- Änderungsverfolgung
- Nachvollziehbarkeit sicherheits- und qualitätsrelevanter Aktionen

### Reporting
- KPI-Grundlagen
- Ticket Age
- Time in Status
- SLA-Auswertungen
- Bottleneck-Analysen
- Durchlaufzeiten

### IoT (später)
- MQTT-Ingestion
- Geräteverwaltung
- Tasterevents
- Heartbeats
- OTA-Status
- Gerätezustände

---

## Zielgruppen und Nutzungskontexte

### Primäre Nutzergruppen
- Werker in der Produktion
- Schichtleiter
- Arbeitsvorbereitung
- Lagerpersonal
- Einkauf
- Konstruktion
- Qualitätssicherung
- Instandhaltung
- Management
- Administration

### Nutzungskontexte
- Werkshalle
- Besprechungsräume
- Büroarbeitsplätze
- Tablets an Linien
- Handhelds / MDE-nahe Geräte
- Service-/Support-Desk

---

## Domänenmodell – Kernobjekte

### Zentrale Entitäten
- User
- Role
- Team
- Department
- Site
- Shift
- Ticket
- TicketType
- TicketPriority
- TicketCategory
- TicketStatus
- WorkflowDefinition
- WorkflowState
- WorkflowTransition
- SLAProfile
- Assignment
- Comment
- Attachment
- AuditEntry
- Machine
- ProductionLine
- Asset
- MaterialReference
- PurchaseRequest
- EngineeringChange
- Notification

### Wichtige Regeln
- Ein Ticket kann Parent und mehrere Children haben
- Ein Ticket kann Kommentare, Anhänge und Historieneinträge besitzen
- Ein Ticket kann sich auf Maschine, Arbeitsplatz, Auftrag, Material, Kunde oder Lagerplatz beziehen
- Ein Ticket durchläuft einen nachvollziehbaren Statuslebenszyklus
- Jede kritische Statusänderung ist auditierbar
- Berechtigungen sind rollen- und kontextabhängig

---

## Ticket-Prinzipien

### Ticket-Typen müssen konfigurierbar sein
Beispiele:
- Maschinenstörung
- Qualitätsabweichung
- Materialmangel
- Werkzeugdefekt
- Einkaufsanforderung
- Reklamation
- Änderungsantrag
- Wartungsbedarf
- Sicherheitsereignis
- Büro-/IT-Anfrage

### Jeder Ticket-Typ kann definieren
- Standardpriorität
- erlaubte Zustände
- Pflichtfelder
- Standardteam
- Eskalationslogik
- SLA-Profil
- Eingabemaske / UI-Konfiguration

---

## Betriebs- und Qualitätsanforderungen

### Nicht-funktionale Anforderungen
- hohe Änderbarkeit
- klare Deploybarkeit
- gute Diagnosefähigkeit
- robuste Fehlerbehandlung
- nachvollziehbare Logs
- resiliente Background-Jobs
- performante Listen und Suchfunktionen
- gute Mehrbenutzerfähigkeit

### Performance-Ziele
- typische UI-Aktionen sollen schnell reagieren
- große Ticketlisten müssen filterbar und paginierbar sein
- Realtime-Events dürfen die Seite nicht blockieren
- Dateiuploads und Statuswechsel sollen sauber entkoppelt sein

### Datenqualität
- keine stillen Datenkorruptionen
- klare Statusübergänge
- valide Referenzen
- nachvollziehbare Änderungen

---

## Projektstruktur

Empfohlene Struktur:

/src
  /Apps
    /Web
  /BuildingBlocks
    /Application
    /Domain
    /Infrastructure
    /Presentation
  /SharedKernel
  /Modules
    /Identity
    /Tickets
    /Workflows
    /Assignments
    /Notifications
    /Attachments
    /Assets
    /Warehouse
    /Procurement
    /Engineering
    /Audit
    /Reporting
    /IoT

/tests
  /UnitTests
  /IntegrationTests
  /ArchitectureTests
  /EndToEndTests

/docs
  /adr
  /domain
  /features
  /ux
  /api

---

## Reifegrad-Roadmap

### Phase 1 – Fundament
- Benutzer, Rollen, Teams
- Ticket Core
- Kommentare
- Anhänge
- Historie
- einfache Zuweisungen
- Listen, Suche, Filter
- Audit-Basis

### Phase 2 – Workflow & SLA
- Ticket-Typen
- Statusmodelle
- Workflow-Regeln
- SLA-Profile
- Eskalationslogik
- Benachrichtigungen

### Phase 3 – Shopfloor UX
- Tablet-Optimierung
- schnelle Erfassungsmaske
- Foto-/Scan-Workflows
- Realtime-Dashboard
- robuste Unterbrechungsbehandlung

### Phase 4 – Fachmodule
- Instandhaltung / Assets
- Lager
- Einkauf
- Konstruktion
- Qualitätsfälle

### Phase 5 – Integration
- E-Mail-Inbound/Outbound
- MQTT / IoT
- ERP-/MES-nahe Schnittstellen
- Gerätezustände

### Phase 6 – Intelligence
- KPI-Dashboards
- Bottleneck-Analysen
- Wissensbasis
- KI-Vorschläge für Routing, Priorität und Dubletten

---

## Definition of Done

Ein Feature ist erst fertig, wenn:
- das fachliche Ziel klar beschrieben ist
- die Geschäftsregeln dokumentiert sind
- die Modulzuordnung passt
- der Code verständlich und wartbar ist
- Tests vorhanden sind
- Sicherheits- und Berechtigungsaspekte berücksichtigt sind
- Logging/Audit bedacht wurde
- UI-Fehlerfälle behandelt werden
- die Implementierung dokumentiert oder nachvollziehbar ist

---

## Leitsatz

Diese Plattform ist nicht nur ein Ticketsystem.

Sie ist das operative Koordinationssystem zwischen Werkshalle, Technik und kaufmännischen Bereichen – robust genug für den Alltag und offen genug für die digitale Fabrik von morgen.
