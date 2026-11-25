# v0.4.4-book1 — 2026-11-18

- Buchansicht als Standard: einzelnes Vordercover, Innendoppelseiten, einzelnes Rückcover, vollständiger Umschlag mit Rücken als letzte breite Seite.
- Cover-Panels mit echten Seitengrößen und Clipping; klar außerhalb liegende, auch gedrehte Rücken-/Rückseitentexte werden auf dem Vordercover nicht mit ausgegeben.
- Rückenbreite aus Cover-/Innenseitengeometrie statt aus der eingerückten Rücken-Textfläche. Plausibilitätsprüfung und datenerhaltender Rückfall bei fehlenden Maßen.
- Wiederholte Cover-Ausgabe verändert die Position rechter Bildhintergründe nicht mehr. Fehlende Folgeseiten am Dateiende lösen beim Zusammenfassen des Umschlags keinen Indexfehler aus.
- BookTests ergänzen die sechs bisherigen Testgruppen; keine neue Laufzeitabhängigkeit, LGPL-/MIT-Stand unverändert.

# v0.4.3-shapes1 — 2026-11-18

- Konvexe abgerundete Bildecken als Vektor-Clipping; jede Ecke separat, Radius begrenzt auf Bildmaße. Rahmen und Schatten verwenden denselben Formpfad.
- Schriftkonturen mit Farbe/Breite und transparenter bzw. deaktivierter Kontur; PDF-Text bleibt durchsuchbar und eingebettet. Textschatten berücksichtigen Konturen.
- Alte Rahmenattribute borderenabled/sizeborder/colorborder unterstützt. Explizit deaktivierte border-Elemente haben Vorrang. Rahmenlage, gap und alter offset werden berücksichtigt.
- ShapeTests für vier Kulturen, alte/neue XML-Darstellung, Deaktivierung, Vektorpfade, Alpha-Silhouetten und extrahierbaren Unicode-Text.
- Die LGPL-Bibliothek und MIT-Anwendungslizenz bleiben erhalten; keine neue Laufzeitabhängigkeit.

# v0.4.2-shadowfix1 — 2026-11-18

- Schatten für Bilder, transparente Cliparts und Texte: altes und neues XML, Intensität, Abstand, Winkel, Ausdehnung und weiche Kontur. Seitenbezogene Richtung bleibt bei Drehungen erhalten. GDI-Textschatten und CEWE-Parameterabbildung sind dokumentierte Näherungen.
- AGPL-PDF-Abhängigkeit durch iTextSharp.LGPLv2.Core 3.8.8 (LGPL-2.0-only) ersetzt. Neues Anwendungsrelease unter MIT, separat austauschbare DLL und passende Bibliotheksquellen. Historische AGPL-Releases bleiben unverändert.
- PDF-Texttests lesen mit unabhängigem PdfPig (nur Testabhängigkeit, Apache-2.0); auch Tests enthalten keine iTextSharp-5-Abhängigkeit mehr.
- Prüfungen für Unicode und Font-Einbettung erhalten; transparente PNG-Fotos behalten ihre Alpha-Kontur. Isolierte Zwischenordner je Testprojekt verhindern vermischte NuGet-Graphen.

# Änderungen

## v0.4.1-decorfix2 – 11.11.2026

- Eigenständiges SVG-/PNG-Werkzeug statt direkter MS-PL-Verknüpfung im AGPL-PDF-Programm.
- AGPL-Kennzeichnung, Autoren/KI-Hinweis, SPDX-SBOM und vollständige Drittanbieterhinweise.
- Technischer Lizenzprüfbericht mit offenen CEWE-/Schriftrechten.


## v0.4.1-decorfix1 – 11.11.2026

- Alte und neue Clipartbereiche einschließlich CEWE-CLP und SVG unterstützt; transparente PNG-Ausgabe, Farben, Alpha und Spiegelungen.
- Zusätzlicher Ressourcencache und dynamischer Download fehlender Originaldateien nach Design-ID vom offiziellen CEWE-Dienst; `--offline` deaktiviert den Netzzugriff.
- CEWE-Rückenlogo standardmäßig ausgelassen; ausschließlich mit `--spine-logo` aktiviert.
- SVG.NET und ExCSS samt Lizenzen und künstlichen Dekorationstests ergänzt.

## v0.4.1-legacyfix3 – 11.11.2026

EXIF-Ausrichtung vor dem Schließen des Bildstreams anwenden. GDI+ verliert ansonsten die verzögert gelesenen EXIF-Eigenschaften auch auf Image.Clone(); Fotos erschienen seitlich oder auf dem Kopf. Abgetrennte Pixelkopie und künstlicher JPEG-Regressionstest sichern die Korrektur.

## v0.4.1-legacyfix2 – 11.11.2026

- MCF-Dateien mit separatem Bilderordner zusätzlich zu MCFX unterstützt.
- Alte Koordinaten, Ausschnitte, Maßstäbe, Rahmen, Farben und Qt-Ausrichtung gelesen.
- Pixelbasierte Ausschnittverschiebung alter image-Attribute mit dem Bildmaßstab in Seitenkoordinaten umgerechnet; verhindert schmale Streifen und weiße Bildflächen.
- CEWEs ausgeschaltete automatische Seitennummerierung (`position=0`) respektiert. Keine zusätzlichen Ziffern neben gestalteten Seitenzahlen.
- Fehlenden RestoreState-Aufruf bei leeren Bildplatzhaltern im Release-Build korrigiert; zuvor Abbruch bzw. fehlende PDF-Seiten.
- UNC-MCFX-Zugriff über temporäre lokale Momentaufnahme; Quellen nur lesend.
- Parametrisierte SQLite-Abfragen und Dateinamen mit Apostrophen.
- Weitere lokal installierte CEWE-Hintergründe berücksichtigt.
- Fehlerhafte Eingaben liefern einen Fehlerstatus; Quelldatei kann nicht als Ausgabe überschrieben werden.
- Regressionstests für Legacy-Format und leere Platzhalter ergänzt.

## v0.4.1-textfix2 – 06.11.2026

Direkte Absatztexte, verschachtelte Textläufe, Schriftauflösung, Unicode-Einbettung und transparente Textflächen korrigiert. Gebietsschemaunabhängige Zahlenverarbeitung aus dem vorherigen Culture-Fix erhalten.
