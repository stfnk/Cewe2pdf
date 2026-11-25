using System;
using System.Globalization;
using System.IO;
using System.Text;
using Cewe2pdf;

class CultureTests {
    static void Equal(double actual, double expected) {
        if (Math.Abs(actual - expected) > 0.0001) throw new Exception($"Expected {expected}, got {actual}");
    }
    static void Main() {
        var original = CultureInfo.CurrentCulture;
        var originalUI = CultureInfo.CurrentUICulture;
        try {
            foreach (string culture in new[] { "de-DE", "en-US", "fr-FR", "tr-TR" }) {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
                // Reproduce the original failure: decimal font sizes cannot be parsed as int.
                try { Convert.ToInt32("11.62"); throw new Exception("Expected original FormatException"); }
                catch (FormatException) { }
                Equal(McfParser.ParseNumber("11.62"), 11.62);
                Equal(McfParser.ParseNumber(" -1.25e2 "), -125);
                foreach (string invalid in new[] { "11,62", "1,234.56", "NaN", "Infinity", "1e999", "" }) {
                    try { McfParser.ParseNumber(invalid); throw new Exception("Accepted invalid value: " + invalid); }
                    catch (FormatException) { }
                }
                string xml = """
                <fotobook>
                  <pagenumbering margin="11.62" verticalMargin="2.5" fontsize="11.62" />
                  <page type="normalpage" pagenr="1">
                    <bundlesize width="500.5" height="300.25" />
                    <area areatype="textarea">
                      <position left="11.62" top="-2.5" width="100" height="50" rotation="1.25" />
                      <text><textFormat font="Calibri,11.62,0" foregroundColor="#ff000000" />
                        <![CDATA[<html><body style="font-size:11.62pt;font-weight:400"><p><span style="font-size:12.5pt;font-weight:700">Test</span></p></body></html>]]>
                      </text>
                    </area>
                    <area areatype="imagearea">
                      <image filename="safecontainer:/test.jpg"><cutout left="-1.25" top="2.5" scale="1.25" /></image>
                      <decoration><border width="1.25" color="#ff000000" /></decoration>
                      <position left="11.62" top="2.5" width="100.5" height="50.25" rotation="-1.25" />
                    </area>
                  </page>
                  <page type="normalpage" pagenr="2" />
                </fotobook>
                """;
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
                var parser = new McfParser(stream);
                var page = parser.nextPage();
                Equal(page.bundleSize.X, 200.2);
                Equal(Page.pageNoMargin.X, 4.648);
                Equal(Page.pageNoFontSize, 11.62 * 0.4 * 3.26);
                var text = (TextArea)page.areas[0];
                Equal(text.fontsize, 15);
                Equal(text.textElements[0].size, 16);
                if (!text.textElements[0].bold) throw new Exception("Font weight lost");
                Equal(text.rect.X, 4.648);
                Equal(text.rotation, 1.25);
                var image = (ImageArea)page.areas[1];
                Equal(image.borderWidth, 0.5);
                Equal(image.cutout.X, -0.5);
                Equal(image.scale, 0.5); // Preserve existing scaling semantics.
                if (parser.nextPage() != null) throw new Exception("Unexpected extra page");
                // Reset configuration to exercise parsing separately in every culture.
                typeof(Config).GetField("imgScale", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).SetValue(null, -1f);
                typeof(Config).GetField("toPage", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).SetValue(null, -1);
                Config.setMissingFromOptions(new[] { "img_scale=1.25", "to_page=2" });
                Equal(Config.ImgScale, 1.25);
                Equal(Config.ToPage, 2);
                if (Config.toPascalCase("IMG_SCALE") != "ImgScale") throw new Exception("Culture-sensitive option name");
                Console.WriteLine("PASS " + culture);
            }
        } finally {
            CultureInfo.CurrentCulture = original;
            CultureInfo.CurrentUICulture = originalUI;
        }
    }
}
