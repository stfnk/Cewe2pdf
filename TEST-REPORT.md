# Tests v0.4.4-book1 — 18.11.2026

Basis: e0b2dfe. Windows x64, .NET SDK 10.0.403, portable .NET 10.0.14. Alle sieben Testgruppen bestanden: CultureTests, IntegrationTests, LegacyTests, DecorationTests, ShadowTests, ShapeTests und BookTests. Keine neue Laufzeitabhängigkeit.

## Neue Buchansicht

- Vier Prozesskulturen: de-DE, en-US, fr-FR, tr-TR.
- Synthetischer Umschlag mit getrennten Rücken-/Vorder-/Rückcoverobjekten, rechter Bildhintergrund, Innendoppelseite und rotierter Rückenbeschriftung.
- Vier resultierende PDF-Seiten in der Reihenfolge Vordercover, Innendoppelseite, Rückcover, kompletter Umschlag. Gemischte Größen unabhängig geprüft: 210×200, 400×190, 210×200, 440×200 PDF-Punkte.
- Schriftinhalt jeder Seite unabhängig mit PdfPig geprüft: keine versteckten Rückcover-/Rückentexte auf der Vorderseite; der vollständige Umschlag am Ende enthält alle drei Texte.
- Einzelcover schließen den echten Rücken aus. Seine Breite wird nicht mit der schmaleren Textfläche verwechselt. Plausibilitätsprüfung, Null-Rücken und rotierte Begrenzungsrechtecke getestet.
- Wiederholtes Rendern der Cover verschiebt rechte Bildhintergründe nicht mehr. Projekte ohne Cover erhalten keine erfundenen Seiten; Cover ohne Innenmaße bleiben als vollständiger Umschlag erhalten.
- Die synthetische Test-PDF wurde mit Poppler gerendert und visuell geprüft.

Der erste synthetische Rückentext war größer als sein schmaler Textrahmen; die Fixture wurde passend verkleinert. Danach bestanden Inhalts- und Geometrieprüfungen. Die ursprünglichen Originalbücher wurden dadurch nicht verändert.

## Grenzen und Gesamtexport

Die Maße der Deckel werden aus Cover-/Innenseitenmaßen mit gleichmäßigem Überstand abgeleitet. Sondergeometrien werden bei unplausiblen Werten nicht geraten, sondern als vollständiger Umschlag erhalten und protokolliert. Nicht implementierte CEWE-Eigenschaften aus dem Tag-Prüfbericht bleiben offen. Schatten/GDI-Textschatten sind weiterhin dokumentierte Näherungen.

Der Benutzer hat zusätzlich den Neuaufbau und Austausch aller vorhandenen NAS-Fotobuch-PDFs beauftragt. Dafür gibt es einen separaten Exportbericht mit Backup, Seiten-/Text-/Fontprüfung und SHA-256 je Datei. Persönliche Bücher, PDFs und dieser private Exportbericht sind nicht Teil des Softwarequellcodes.
