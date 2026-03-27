# ADR-0001 – Entscheidung für modularen Monolithen

## Status
Accepted

---

## Kontext

Für die industrielle Ticket- und Workflow-Plattform wurde eine Architekturentscheidung benötigt.

Mögliche Optionen:
- Microservices
- Modularer Monolith
- Klassischer Monolith

Anforderungen:
- schnelle Entwicklung
- gute Testbarkeit
- klare Domänenstruktur
- einfache Deploybarkeit
- Erweiterbarkeit für IoT und KI
- geringe Komplexität für V1

---

## Entscheidung

Wir implementieren V1 als **modularen Monolithen**.

---

## Begründung

### Vorteile
- geringere Komplexität
- einfacher Debugging-Prozess
- klare Codebasis
- schneller Entwicklungsfortschritt
- keine verteilten Systemprobleme (Netzwerk, Konsistenz)
- ideal für KI-gestützte Entwicklung (Cursor)

### Nachteile
- weniger Skalierungsmöglichkeiten als Microservices
- potenziell wachsender Codebase-Monolith

Diese Nachteile werden bewusst akzeptiert, da:
- funktionale Skalierung wichtiger ist als technische Skalierung in V1
- spätere Extraktion einzelner Module möglich ist

---

## Alternativen

### Microservices
Verworfen für V1 wegen:
- hoher Komplexität
- Deployment-Aufwand
- schwerer testbar
- unnötig für initiale Phase

### Klassischer Monolith
Verworfen wegen:
- fehlender Modularität
- schwer wartbar
- hohe Koppelung

---

## Konsequenzen

### Kurzfristig
- schnellere Entwicklung
- geringere Komplexität
- klare Struktur durch Module

### Mittelfristig
- Möglichkeit zur Extraktion einzelner Module:
  - Notifications
  - IoT
  - Reporting

### Langfristig
- schrittweise Migration zu verteilten Systemen möglich

---

## Technische Umsetzung

- Modulstruktur unter `/Modules`
- klare Trennung:
  - Domain
  - Application
  - Infrastructure
  - Presentation
- keine direkten Abhängigkeiten zwischen Modulen ohne Schnittstellen

---

## Leitsatz

„Ein modularer Monolith liefert Geschwindigkeit heute und Flexibilität für morgen.“
