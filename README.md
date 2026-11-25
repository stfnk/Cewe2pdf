# cewe2pdf

Lokales Windows-Werkzeug zum Export alter und neuer CEWE-Fotobücher als PDF. Unterstützt `.mcf` mit externem Bilderordner und `.mcfx` mit eingebetteten Bildern. Die Quelldateien werden nicht verändert.

## Benutzung

Die portable Windows-Version vollständig entpacken und die Projektdatei auf `Fotobuch-starten.cmd` ziehen. Ohne Ausgabepfad entsteht neben dem Projekt `Buch-converted.pdf`. Ein bereits vorhandenes gleichnamiges PDF wird dabei überschrieben; für Vergleiche einen anderen Ausgabepfad angeben.

```powershell
.\Cewe2pdf.exe 'C:\Fotobuecher\Buch.mcf' 'C:\Export\Buch.pdf' '-program_path=C:\Program Files\dm\dm-Fotowelt'
.\Cewe2pdf.exe '\\NAS\Freigabe\Buch.mcfx' 'C:\Export\Buch.pdf' '-program_path=C:\Program Files\CEWE\CEWE Fotowelt'
```

Bei alten MCF-Projekten muss der im Projekt angegebene Bilderordner neben der Datei erhalten bleiben. MCFX-Dateien auf UNC-Freigaben werden über eine private temporäre Kopie verarbeitet und anschließend entfernt. SQLite wird ausschließlich lesend geöffnet. Die EXE benötigt keine separat installierte .NET-Laufzeit; eine CEWE-Installation liefert Schriften und Gestaltungselemente.

Optionen: `-to_page=2` für einen kurzen Probelauf, `-img_scale=1` für die Standard-Bildauflösung, `-verbose` für ausführliche Ausgabe, `-h` für die Hilfe. Die PDF beginnt mit einem einzelnen Vordercover, zeigt danach die Innendoppelseiten und endet mit einzelnem Rückcover plus vollständigem Umschlag.

### Seitenfolge wie ein Buch

Standardreihenfolge: **Vordercover einzeln → Innenseiten als Doppelseiten → Rückcover einzeln → kompletter Umschlag**. Die letzte breite Seite enthält Rückcover, Rücken und Vordercover. Die Wiederholung der Cover am Ende ist beabsichtigt. Cover und Innenseiten behalten ihre Projektmaße; ein Hardcover-Deckel kann etwas größer sein als die Papierseite.

Der Rücken wird aus den einzelnen Coverseiten ausgeschlossen. Die Breite wird aus Umschlag- und Innenseitenmaßen bei gleichem Deckelüberstand in Breite und Höhe ermittelt; die schmalere Rücken-Textfläche ist dafür kein verlässliches Maß. Fehlen passende Maße oder sind sie unplausibel, bleibt der vollständige Umschlag erhalten und das Protokoll nennt die fehlende Zuordnung. Es werden keine Seiten abgeschnitten oder leere Cover erfunden.

Die Ausgabe erfolgt direkt mit passenden PDF-Seitengrößen und Vektor-Clipping. Klar außerhalb liegende Textobjekte werden auf Einzelcovern nicht ausgegeben. Innenseiten bleiben in ihrer Reihenfolge erhalten. `-to_page=` zählt weiterhin die gerenderten Quell-Doppelseiten; enthält ein Probelauf den Umschlag, kommen das einzelne Rückcover und der vollständige Umschlag zusätzlich ans Ende.

### Cliparts und Hintergründe

Installierte CEWE-/dm-Ressourcen und der lokale Ressourcencache werden nach den exakten Design-IDs durchsucht. Fehlt eine Ressource, lädt das Werkzeug das Original **während der Konvertierung automatisch** vom offiziellen CEWE-Dienst nach:

```powershell
.\Cewe2pdf.exe 'C:\Buch.mcf' 'C:\Buch.pdf' '-resource_path=C:\CEWE-Ressourcen'
```

Mit `--offline` wird ausschließlich lokal gesucht. Standardcache: `%LOCALAPPDATA%\Cewe2pdf\resources`; `-resource_path=` wählt einen anderen Ordner. Bereits geladene Dateien werden wiederverwendet. Downloads umfassen nur die referenzierten Design-IDs, keine Buchdateien oder privaten Fotos. Quelle und SHA-256 werden je Ressourcenpaket im Cache dokumentiert. CEWE-Grafiken werden nicht im Quellcode-Repository mitgeliefert. `--download-resources` schaltet Downloads ausdrücklich wieder ein, falls sie in der Konfiguration deaktiviert wurden.

Cliparts aus SVG und CEWE-CLP werden mit transparentem Hintergrund gerendert. Position, Drehung, Farbeinstellungen, Alpha und unterstützte Spiegelungen bleiben erhalten.

Das **CEWE-Rückenlogo ist standardmäßig ausgeschaltet**, auch wenn es im Projekt angelegt ist. Projektdatei und Konfigurationsdatei aktivieren es nicht; nur ein ausdrücklicher Kommandozeilenschalter:

```powershell
.\Cewe2pdf.exe 'C:\Buch.mcf' 'C:\Buch-mit-Logo.pdf' --spine-logo
```

Das Protokoll `cewe2pdf.log` wird im aktuellen Arbeitsordner geschrieben. Gleichzeitige Konvertierungen brauchen unterschiedliche Arbeitsordner. Die Startdatei startet jeweils einen einzelnen Lauf.

## Unterstützte Formatunterschiede

- MCF-Bilderverzeichnis aus `fotobook/@imagedir`, relative Dateipfade und `safecontainer:/`-Verweise.
- Alte Positionen, Rotation, Bildausschnitte und Maßstab direkt als Attribute; neue `position`- und `cutout`-Elemente.
- Alte Bildrahmenattribute (`borderenabled`, `sizeborder`, `colorborder`), direkte border-Elemente und neue `decoration/border`-Elemente; deaktivierte Einträge haben Vorrang. Außen-/Innenlage, Abstand und alter offset werden berücksichtigt.
- Konvexe runde Bildecken je Ecke, auch gedreht, mit entsprechend geformten Rahmen und Schattenkonturen.
- Schriftkonturen aus outline-Farbe und -Breite; Vordergrundtext bleibt Unicode-Text mit eingebetteten Fonts.
- Alte dezimale ARGB-Farben, RGB-/ARGB-Hexfarben, transparente Textflächen und numerische Qt-Ausrichtung.
- Leere Bildplatzhalter ohne beschädigten PDF-Grafikzustand.
- Keine zusätzlichen automatischen Seitenzahlen, wenn diese im CEWE-Projekt ausgeschaltet sind.
- EXIF-Ausrichtung vor dem Schließen des Bildstreams angewendet; abgetrennte Pixelkopie vermeidet verlorene Orientierung und doppelte Drehungen.
- Gebietsschemaunabhängige Dezimalzahlen, Unicode-Schrifteinbettung, direkte HTML-Absatztexte und verschachtelte Textläufe.
- CEWE-Ressourcen der angegebenen Installation, ergänzend installierte dm-Fotowelt und lokale CEWE-Downloadressourcen.

## Build und Tests

Windows und .NET SDK 10 erforderlich:

```powershell
.\build.ps1
# Bei einer portablen SDK-Installation:
.\build.ps1 -Dotnet 'C:\SDK\dotnet.exe'
```

Der Build führt CultureTests, IntegrationTests, LegacyTests DecorationTests, ShadowTests und ShapeTests und BookTests aus und erstellt danach `windows-x64`. Die Tests erzeugen ausschließlich künstliche Projekte. Geprüft werden Gebietsschemata, Texte, Schriften, alte Geometrie, Bilddateien und EXIF-Drehungen sowie Clipart-Transparenz, Umfärbung, CLP-Decodierung, Cache-Suche und der optionale Logo-Schalter.

Zusätzlicher Textvergleich für eigene Projekte:

```powershell
dotnet run --project tests/IntegrationTests.csproj -c Release -- --audit 'C:\Buch.mcf' 'C:\Export\Buch.pdf'
```

### Schatten

Bild-, Clipart- und Textschatten werden aus der sichtbaren Alpha-Kontur erzeugt; transparente Aussparungen bleiben erhalten. Unterstützt sind alte `shadowEnabled`/`shadowDistance`-Attribute und neue `decoration/shadow`- bzw. direkte `shadow`-Elemente. Winkel, Abstand, Intensität, Ausdehnung und Unschärfe werden berücksichtigt. Deaktivierte Schatten werden nicht erfunden. Der Winkel bleibt auch bei gedrehten Objekten seitenbezogen. Vordergrundtext bleibt durchsuchbar und die Fonts bleiben eingebettet.

Die CEWE-Darstellung ist nicht öffentlich spezifiziert: Ausdehnung und Unschärfe sind Näherungen. Textschatten verwenden GDI-Schriftmetriken; bei komplexem Umbruch oder nur privat geladenen CEWE-Fonts kann ihre Kontur etwas vom PDF-Text abweichen. Die Rasterkontur ist auf 1536 Pixel je Achse begrenzt; Fotos und Vordergrundtext behalten ihre bisherige Auflösung. Andere Bildmasken und Eckenformen außer convex bleiben unimplementiert. Ein exakter CEWE-Schattenvergleich benötigt ein kontrolliertes Original samt Editorvorschau.

## Grenzen

Der Renderer bildet nur einen Teil des CEWE-Formats nach. Weitere Bildmasken/Eckenformen und einzelne CSS-/Layout-Eigenschaften können abweichen oder fehlen. Nicht mehr verfügbare Design-IDs werden ausdrücklich protokolliert. Eine fertige PDF ist keine automatische Druckfreigabe; für maßgetreue Druckausgaben mit der CEWE-Vorschau vergleichen.

Ein Komplettlauf am 11.11.2026 hat 18 vorhandene ältere Projektstände verarbeitet. Seitenzahl, Zahl gerenderter Fotos, extrahierte Textinhalte, unveränderte Quellen und SHA-256 der NAS-Kopien wurden separat geprüft. Persönliche Bücher, Fotos, PDFs, Pfade und Zugangsdaten gehören nicht in dieses Repository.

## Herkunft und Lizenz

Basis: stfnk/Cewe2pdf, Commit `79cee89c4c7d02d6f31d2e47b391bd6b1f598555`, ergänzt um lokale Korrekturen. Anwendung und lokale Änderungen ab v0.4.2-shadowfix1: MIT; Copyrights von Stefan Kreller und Robert Brendel bleiben erhalten. Die PDF-Bibliothek ist jetzt **iTextSharp.LGPLv2.Core 3.8.8 (LGPL-2.0-only)** statt iTextSharp 5 (AGPL). Historische AGPL-Releases werden dadurch nicht rückwirkend umgestellt.

Die portable Ausgabe enthält eine EXE und austauschbare DLLs; immer den gesamten Ordner entpacken. Die LGPL-DLL wird nicht als Einzeldatei eingebettet, getrimmt oder statisch verlinkt. Mitgeliefert werden passende Bibliotheksquellen und Hinweise zum Austausch/Neubau. Änderungen zur eigenen Verwendung und Reverse Engineering zum Debuggen solcher Änderungen sind erlaubt. Keine Lizenz wird als pauschale Freigabe für CEWE-Grafiken oder Schriftarten dargestellt.

SvgToPng bleibt ein unabhängiges MS-PL-Kommando (`input.svg output.png width height`), SVG.NET 3.4.7: MS-PL, ExCSS 4.2.3: MIT. Die PDF-Anwendung kommuniziert nur über Standarddateien. `CEWE2PDF_SVG_TOOL` wählt einen kompatiblen unabhängigen Renderer. Die Windows-EXE ist nicht signiert.

Details: [Lizenzprüfung](LICENSE-REVIEW.md), [Quellcode und DLL-Austausch](SOURCE-CODE.md), [SPDX-Bestand](SBOM.spdx.json), [Drittanbieterhinweise](THIRD-PARTY-NOTICES.md), [Autoren](AUTHORS.md). Öffentliche CLP-Formatreferenz: https://github.com/bash0/cewe2pdf/blob/master/CLP%20file%20format.txt.
