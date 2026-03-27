# FactoryFlow

FactoryFlow ist eine industrielle Ticket- und Workflow-Plattform für Produktionsbetriebe.

## Vision

FactoryFlow verbindet Werkshalle und Büro in einem System:
- Produktion
- Arbeitsvorbereitung
- Lager
- Einkauf
- Verkauf
- Konstruktion
- Qualität
- Instandhaltung

Ziel ist ein robustes, auditierbares und erweiterbares System zur:
- Problemverfolgung
- Workflow-Steuerung
- Eskalation
- Analyse
- Vorbereitung für IoT und KI

---

## Architektur

- Modularer Monolith (V1)
- ASP.NET Core + Blazor Web App
- PostgreSQL
- SignalR für Realtime
- Docker / Linux Deployment

Siehe:
- PROJECT_MANIFEST.md
- ARCHITECTURE_PRINCIPLES.md
- CURSOR_RULES.md

---

## Projektstruktur

/src
/tests
/docs

---

## Entwicklungsprinzipien

- fachlich denken vor technisch
- kleine, testbare Schritte
- klare Modulgrenzen
- keine unnötige Komplexität

---

## Einstieg

1. Repository klonen
2. Datenbank konfigurieren (PostgreSQL)
3. `dotnet run` starten
4. Swagger / UI öffnen

---

## Status

🚧 In Entwicklung – Fokus auf Fundament (Tickets, Workflow, Identity)
