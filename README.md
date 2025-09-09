ETS2/ATS Modlists Im-Exporter

Version: 3.0
Ein schlankes Windows-Tool zum Importieren, Exportieren und Pflegen von active_mods-Listen für Euro Truck Simulator 2 und American Truck Simulator – mit Vorschau, Notizen, Link-Verwaltung und automatischem Entschlüsseln der profile.sii.

Made by Winne (rore58) – for the DanielDoubleU community.

✨ Features

Spiel & Profil wählen: ETS2/ATS umschalten, Profile auslesen (auch bei benutzerdefinierten Pfaden).

Modlisten laden/anzeigen: .txt-Modlisten aus modlists/<ETS2|ATS> werden mit Lade-Reihenfolge (1,2,3, …) in umgekehrter Reihenfolge angezeigt.

Übernehmen in profile.sii: active_mods-Block wird präzise ersetzt, inkl. Backup-Rotation.

Export: active_mods aus profile.sii als .txt speichern.

Auto-Decrypt: Binäre profile.sii werden beim Start automatisch via tools/SII_Decrypt.exe entschlüsselt.

Notizen pro Modliste: Footer-Textfeld speichert/liest <deineListe>.note im selben Ordner.

Download-Links pro Mod: Bekannte Links aus links.json (steam/sonstige); wenn unbekannt → Google-Suche-Button pro Mod.

Dark/Light Theme & Deutsch/Englisch: Im Optionen-Dialog umschaltbar.

Ordner öffnen: Schnellzugriff auf Profil-Ordner.

Modliste löschen: Löscht ausgewählte Liste und die zugehörige .note.

Über-Dialog: App-Info direkt aus dem UI.

🖼️ UI-Überblick

Oben: Spiel, Profil, Modliste (+ Löschen-Button neben der Modliste).

Buttons: Modliste laden, Modliste übernehmen, Profilordner öffnen, Modliste exportieren, Text-Check, Donate, Backup wiederherstellen, Optionen, Über.

Mitte: Vorschau-Grid der Modliste mit Spalten #, Modname, Aktionen (z. B. Download/Google suchen).

Footer: Logo (ETS2/ATS), „Info zur Modliste“-Label, Notizfeld (speichert .note), rechte Seitenlegende.

📦 Ordnerstruktur

Im Programmverzeichnis sollten folgende Ordner existieren:

assets/               (Logos, Bilder, …)
modlists/
  ETS2/
    deineListe.txt
    links.json        (optional: Schlüssel→URL Map)
    deineListe.note   (wird automatisch angelegt, wenn du Notizen speicherst)
  ATS/
    ...
tools/
  SII_Decrypt.exe     (für Auto-Decrypt; inkl. Lizenzhinweisen)


Hinweis: links.json (pro Spiel) ist ein simples Dictionary { "Schlüssel": "URL" }.
Schlüssel sind z. B. Modname oder der volle Token aus der Modzeile. Unbekannte Mods erhalten in der UI einen Google-Suche-Button.

🛠️ Installation & Systemvoraussetzungen

Windows 10/11

.NET 8 Desktop Runtime (oder du lieferst eine self-contained Publish-Version aus)

Schreibrechte im gewählten Profil-Ordner

Start: Entpacke das Release-ZIP in einen Ordner deiner Wahl und starte TruckModImporter.exe.

▶️ Bedienung (Kurzfassung)

Spiel (ETS2/ATS) auswählen.

Profil auswählen.

Modliste auswählen → Modliste laden.

(Optional) Notizen schreiben – wird automatisch als .note gespeichert.

Modliste übernehmen → active_mods wird in profile.sii ersetzt.

Profilordner öffnen oder Modliste exportieren bei Bedarf.

Optionen: Sprache, Theme, benutzerdefinierte Profilpfade.

🧠 Wie der Import funktioniert

Deine .txt-Modliste muss einen gültigen active_mods-Block enthalten, z. B.:

active_mods: 4
active_mods[0]: "my_mod_package|My Cool Mod"
active_mods[1]: "another_pkg|Another Mod"
...


Beim Übernehmen:

Sichert profile.sii → Backup-Rotation (bis zu 5 Zeitstempel-Backups).

Erkennt/entschlüsselt ggf. binäre profile.sii (Auto-Decrypt).

Ersetzt genau den vorhandenen active_mods-Block durch den aus der geladenen .txt.

⚙️ Optionen

Sprache: Deutsch / Englisch

Theme: Hell / Dunkel

Profilpfade: Standard oder benutzerdefiniert (falls Steam-Ordner verschoben o. ä.)

Einstellungen werden automatisch gespeichert und beim Start wiederhergestellt.

💾 Export, Backups & Wiederherstellung

Export: Liest active_mods aus profile.sii und speichert als .txt (Dateiname frei wählbar).

Backups: Vor jedem Übernehmen wird profile.sii gesichert. Es werden die letzten 5 Backups behalten.

Wiederherstellen: Wähle im Dialog die gewünschte Sicherung und stelle sie zurück.

🔗 Links zu Mods

Automatische Erkennung: Steam-Workshop-IDs in der Modzeile → Workshop-URL wird erkannt.

links.json pro Spiel:

Speicherort: modlists/ETS2/links.json bzw. modlists/ATS/links.json

Format:

{
  "EAA Map - SEM EUROPA": "https://example.com/download/eaa",
  "eaa_base_1.55_v4": "https://example.com/download/eaa-base"
}


Wenn ein Link vorhanden ist → Download-Button in der Liste.

Sonst bekommst du pro Mod einen Google-Suche-Button.

🧰 Entwickeln & Bauen
Mit Visual Studio

Projekt/Solution öffnen → Build

Publish (Beispiel):

Rechtsklick auf Projekt → Veröffentlichen → Ordnerprofil einrichten

Ziel z. B. bin\Publish\win-x64

Mit dotnet
# Debug build
dotnet build

# Publish (framework-dependent)
dotnet publish -c Release -r win-x64 --self-contained false -o .\publish

# Publish (self-contained, eine größere, aber lauffertige App)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish

Wichtige csproj-Einträge (Assets/Tools/Modlists kopieren)

Stelle sicher, dass deine .csproj diese Gruppen enthält:

<ItemGroup>
  <Content Include="assets\**\*.*">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </Content>
  <Content Include="modlists\**\*.txt">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </Content>
  <Content Include="modlists\**\*.json">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </Content>
  <Content Include="tools\**\*.*">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </Content>
</ItemGroup>

❗ Troubleshooting

Keine Profile sichtbar?
→ In Optionen benutzerdefinierte Pfade setzen oder prüfen, ob die Standardpfade existieren.

Modlisten erscheinen nicht?
→ .txt in modlists/ETS2 bzw. modlists/ATS ablegen.

Übernehmen meldet Fehler / kein Effekt?
→ Prüfe, ob die .txt einen korrekten active_mods-Block enthält.
→ Schreibrechte im Profilordner vorhanden?

Auto-Decrypt greift nicht?
→ Liegt tools/SII_Decrypt.exe im richtigen Ordner? Läuft ohne Admin-Prompt?

Theme wechselt nicht überall?
→ Einmal Theme umschalten (Hell → Dunkel → Hell). Sollte dann für alle UI-Elemente greifen.

📄 Lizenz & Credits

SCS Software: Euro Truck Simulator 2 / American Truck Simulator

Trucky / truckymods.io: Für Mod-Verzeichnis/Links (nur externe Verweise/Recherche)

Steam Workshop: Für Workshop-Links

Dieses Projekt ist inoffiziell und steht nicht in Verbindung mit SCS, Steam oder Trucky.

(Lizenztext deines Projekts hier ergänzen – z. B. MIT.)

🤝 Beiträge

Issues und Pull Requests sind willkommen.
Bitte beim Einreichen von Mod-Links auf rechtliche Rahmenbedingungen achten und keine urheberrechtlich geschützten Dateien anhängen.

💬 Kontakt

Winne (rore58) – DanielDoubleU Community
