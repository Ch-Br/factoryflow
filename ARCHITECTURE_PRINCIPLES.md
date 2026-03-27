# ARCHITECTURE_PRINCIPLES

## Architekturform

Wir bauen **einen modularen Monolithen** mit klaren Modulgrenzen.

Keine verteilten Microservices in V1.

---

## Architekturprinzipien

### Grundprinzipien
- fachliche Modularisierung vor technischer Schichtung
- hohe Testbarkeit
- geringe Koppelung
- explizite Geschäftsregeln
- Revisionssicherheit
- einfache Deploybarkeit
- robuste Änderbarkeit durch KI-Assistenten

### Stil
- Clean Architecture in pragmatischer Form
- DDD-light
- Vertical Slice Thinking für Features
- klare Trennung von:
  - Domain
  - Application
  - Infrastructure
  - UI / API

### Verbotene Muster
- God-Services
- Logik im UI
- unstrukturierte Shared-Helper-Müllhalden
- direkte Datenbanklogik in Komponenten
- implizite Geschäftsregeln in JavaScript oder Razor
- harte Abhängigkeiten zwischen Fachmodulen ohne definierte Schnittstellen

---

## Sicherheitsprinzipien

### Grundregeln
- Least Privilege
- Rollen- und bereichsbezogene Berechtigungen
- sensible Aktionen nur mit klaren Rechten
- durchgängige Nachvollziehbarkeit
- keine versteckten Rechteeskalationen

### Sicherheitsanforderungen
- Authentifizierung für alle geschützten Bereiche
- Autorisierung auf Rollen- und Ressourcenebene
- Auditierung kritischer Aktionen
- sichere Dateibehandlung
- Validierung aller API-Eingaben
- Schutz vor IDOR, CSRF, XSS, Injection
- keine sicherheitsrelevante Logik nur im Client

### Spätere KI-/Agenten-Vorbereitung
- Scope-basierte Rechte
- getrennte technische Identitäten
- explizite Audit-Markierung maschineller Aktionen

---

## UX-Prinzipien

### Allgemein
Die App muss in industriellen Umgebungen funktionieren und nicht nur in ruhigen Büro-Szenarien.

### Leitlinien
- wenige Klicks
- klare visuelle Hierarchie
- große Bedienelemente
- gute Lesbarkeit
- hoher Kontrast
- klare Zustände
- unterbrechungsrobuste Workflows
- automatisches Zwischenspeichern
- schnelle Erfassung
- Fehlervermeidung vor Fehlerbehandlung

### Modi
Die UI soll verschiedene Modi unterstützen:
- **Operator-Modus**: extrem vereinfacht
- **Supervisor-Modus**: Überblick und Steuerung
- **Backoffice-Modus**: Listen, Suche, Filter, Bearbeitung
- **Admin-Modus**: Konfiguration und Governance

### Shopfloor UX
Bei shopfloor-nahen Prozessen gilt:
- Ticket-Erfassung in unter 15 Sekunden anstreben
- Scan/Foto wichtiger als lange Texteingabe
- mobile und Tablet-Nutzung priorisieren
- robuste Zustandswiederherstellung sicherstellen

---

## Entwicklungsprinzipien

### Allgemein
- klar benannte Klassen
- kleine, fokussierte Methoden
- keine versteckte Magie
- keine Business Rules im Controller oder Razor-Markup
- sprechende Namen
- keine übermäßigen Abkürzungen
- frühe Validierung
- explizite Fehlerbehandlung

### C#
- async/await korrekt verwenden
- CancellationToken berücksichtigen
- keine unnötigen statischen Hilfsklassen
- Domainlogik nicht in EF-Entities zersplittern
- Value Objects dort einsetzen, wo sie sinnvoll sind
- MediatR nur wenn wirklich hilfreich; keine Blindabhängigkeit

### EF Core / PostgreSQL
- Migrationen sauber halten
- Indizes bewusst planen
- keine N+1-Probleme erzeugen
- Abfragen projizieren statt riesige Objektgraphen zu laden
- Optimistic Concurrency dort verwenden, wo Konflikte relevant sind
- Audit-Felder konsistent führen

### Blazor
- Komponenten klein und fokussiert halten
- keine Geschäftslogik direkt in Seiten
- State bewusst verwalten
- Lade- und Fehlerzustände explizit abbilden
- Formulare robust validieren
- Realtime-Updates kontrolliert einbinden

---

## Teststrategie

### Pflicht für geschäftskritische Logik
- Unit Tests
- Integrationstests
- ausgewählte End-to-End-Tests

### Unit Tests
Testen:
- Ticket-Erstellung
- Statusübergänge
- SLA-Berechnung
- Eskalationen
- Berechtigungen
- Routing-Regeln
- Parent/Child-Verhalten

### Integrationstests
Testen:
- API + PostgreSQL
- Repositories / DbContext-Verhalten
- SignalR-Grundflüsse
- Dateiupload-Metadatenfluss
- Hintergrundprozesse / Eskalationen

### E2E-Tests
Testen:
- Ticket anlegen
- Ticket weiterleiten
- Ticket eskalieren
- Ticket abschließen
- Supervisor sieht Live-Änderung
- mobile Erfassung wichtiger Standardfälle

### Architekturtests / Strukturtests
Testen:
- keine unzulässigen Modulreferenzen
- UI referenziert Domain nicht direkt falsch
- Infrastruktur dringt nicht in Domänenregeln ein

---

## ADR-Disziplin

Wichtige Architekturentscheidungen werden als ADR dokumentiert.

Beispiele:
- Warum modularer Monolith?
- Warum PostgreSQL?
- Warum Blazor Web App?
- Wie werden Anhänge gespeichert?
- Wie wird Auditierung umgesetzt?
- Wie werden Workflows modelliert?
- Wann wird RabbitMQ/MQTT eingeführt?

### Jede größere Entscheidung soll enthalten
- Kontext
- Entscheidung
- Begründung
- Alternativen
- Konsequenzen

---

## V1-Prinzip

Für V1 gilt immer:

**Einfacher, robuster, testbarer, produktionsnaher modularer Monolith vor komplexer Zukunftsarchitektur.**
