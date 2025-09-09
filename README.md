# Truck Mod Importer – Version 3.0

**Truck Mod Importer** ist ein Windows-Tool zur komfortablen Verwaltung von Modlisten für **Euro Truck Simulator 2 (ETS2)** und **American Truck Simulator (ATS)**.  
Es unterstützt das Erstellen, Bearbeiten, Teilen und Importieren von Modlisten, inklusive Zusatzinformationen und automatischer Dateiverwaltung.

---

## 🚀 Features

- **Modlisten-Verwaltung**
  - Erstellen, Laden und Löschen von Modlisten pro Spiel (ETS2 / ATS).
  - Automatisches Speichern aller Änderungen in `.json`, `.txt` und optional `.note`.

- **Grid-Ansicht**
  - Anzeige aller Mods mit Spalten:
    - **Nr.** (Reihenfolge)
    - **Package** (interner Paketname)
    - **Mod** (frei editierbarer Anzeigename)
    - **Info** (benutzerdefinierte Notizen)
    - **Links** (Download-Links, pro Mod)
  - Alle Änderungen werden automatisch in die zugehörigen JSON-Dateien gespeichert.

- **Per-Liste Links**
  - Für jede Modliste wird zusätzlich eine `<ModlistName>.link` erstellt.
  - Enthält alle Download-Links dieser Modliste.
  - Beim Teilen/Importieren werden Links automatisch mit der globalen `links.json` synchronisiert.
  - Keine doppelten Einträge – bestehende Links werden erkannt und übersprungen.

- **Import / Export**
  - **Weitergeben (Export):**  
    Erzeugt eine ZIP-Datei mit allen relevanten Dateien der Modliste (`.txt`, `.json`, `.note`, `.link`).
  - **Importieren:**  
    Entpackt eine ZIP und fügt die Modliste inkl. Links automatisch ins Tool ein.

- **Profile-Verwaltung**
  - Buttons: **Klonen**, **Umbenennen**, **Entfernen** direkt im Header neben der Profilauswahl.
  - Klonen erstellt automatisch `<Profilname> - Klon` als Vorschlag.
  - Namenseingabe mit Validierung (max. 20 Zeichen).
  - Direkte Bearbeitung von Profilen (`profile.sii` wird im Hintergrund entschlüsselt).

- **Sprach- und Theme-Unterstützung**
  - Umschaltbar zwischen **Deutsch** und **Englisch**.
  - Hell- und Dunkelmodus (Buttons und Header passen sich automatisch an).

- **Stabile JSON-Persistenz**
  - Änderungen an Mod- und Info-Spalten bleiben erhalten.
  - Automatische Erstellung aller benötigten Dateien beim ersten Öffnen einer Modliste.

---

## 🆕 Neuerungen in Version 3.0

- 🔗 **Links pro Modliste:**  
  Synchronisation von `<ModlistName>.link` ↔ `links.json`.
- 📦 **Import/Export verbessert:**  
  Alle Dateien (`.txt`, `.json`, `.note`, `.link`) werden mitgepackt / mitgeladen.
- 👥 **Profil-Buttons im Header:**  
  Klonen, Umbenennen, Entfernen von Profilen direkt aus der Oberfläche.
- 🌐 **Mehrsprachigkeit (DE/EN):**  
  Alle Buttons, Menüs und Texte sind zweisprachig.
- 🎨 **Dark/Light Mode für Header-Buttons:**  
  Einheitliches Styling (auch Löschen-Button).
- 🛠️ Zahlreiche Bugfixes und Performance-Verbesserungen.

---

## 📥 Installation

1. Lade das aktuelle Setup (`TruckModImporter_Setup_3.0.exe`) von den Releases herunter.
2. Starte das Setup – es installiert das Tool inkl. Lizenzinformationen.
3. Nach der Installation kannst du das Tool direkt aus dem Startmenü öffnen.

---

## 📄 Lizenz

Dieses Projekt steht unter der **Mozilla Public License 2.0 (MPL-2.0)**.  
Den vollständigen Lizenztext findest du in der Datei [LICENSE.txt](LICENSE.txt) und im Installer.

---

## 💡 Hinweise

- Standardpfade:
  - ETS2: `Dokumente\Euro Truck Simulator 2\modlists`
  - ATS:  `Dokumente\American Truck Simulator\modlists`
- Workshop-Mods werden aktuell nicht automatisch erkannt.
- Backup deiner Modlisten vor größeren Änderungen empfohlen.
