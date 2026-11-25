using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Globalization;
using System.Numerics;
using System.Xml;

namespace Cewe2pdf {

    class Area {
        public RectangleF rect;
        public float rotation;
        public ShadowSettings shadow = new();
        public bool border;
        public float borderWidth;
        public string borderColor;
        public string borderPosition = "outside";
        public float borderGap, borderOffset;
        public float BorderCenterOffset => borderOffset + (borderPosition switch {
            "outside" => borderWidth / 2,
            "outsideWithGap" => borderWidth / 2 + Math.Max(0, borderGap),
            "insideWithGap" => -borderWidth / 2 - Math.Max(0, borderGap),
            "inside" => -borderWidth / 2,
            _ => 0,
        });
        public virtual string toString() {
            return "[Area] rect: " + rect.ToString() + "; rotation: " + rotation.ToString("F2");
        }
    };

    class ImageArea : Area {
        public ImageCorners corners = new();
        public string filename; // the filename stored in mcfx database
        public Vector2 cutout;
        public float scale;
        public override string toString() {
            return "[ImageArea] rect: " + rect.ToString() + "; rotation: " + rotation.ToString("F2") + "; path: " + filename;
        }
    };

    class ClipartArea : Area {
        public string designId;
        public string uniqueName;
        public string mirror;
        public float opacity = 1;
        public bool spineLogo;
        public Dictionary<string,string> colors = new(StringComparer.OrdinalIgnoreCase);
    }

    class ImageBackgroundArea : ImageArea {
        public enum ImageBackgroundType { Undefined, Left, Right, Bundle }
        public ImageBackgroundType type;
        public override string toString() {
            return "[ImageBackgroundArea] rect: " + rect.ToString() + "; rotation: " + rotation.ToString("F2") + "; path: " + filename;
        }
    };

    public class TextElement {
        public string text = "";
        public bool bold = false;
        public bool italic = false;
        public bool underlined = false;
        public bool newline = false;
        public string color = "#ffffffff";
        public string family = "Calibri";
        public int size = 48;
        public string align = "Center";
    }

    class TextArea : Area {
        public List<TextElement> textElements;
        public string text;
        public int fontsize;
        public string color;
        public string font;
        public string align;
        public string valign;
        public string backgroundcolor;
        public float outlineWidth;
        public string outlineColor;
        public override string toString() {
            return "[TextArea] rect: " + rect.ToString() + "; rotation: " + rotation.ToString("F2") + "; font size: " + fontsize + "; text:\n\n" + text + "\n\n";
        }
    };

    class Page {
        public enum Type {
            Unknown,
            Fullcover,
            Spine,
            Emptypage,
            Normalpage,
        };

        static public Type convert(string typename) {
            switch (typename) {
                case "normalpage": return Type.Normalpage;
                case "fullcover": return Type.Fullcover;
                case "spine": return Type.Spine;
                case "emptypage": return Type.Emptypage;
                default: return Type.Unknown;
            }
        }

        public List<Area> areas = new List<Area>();
        public Type type = Type.Unknown;
        public Vector2 bundleSize = Vector2.Zero;
        public string backgroundLeft;
        public string backgroundRight;
        public float spineSize; // only relevant for cover. 


        public string pageNoLeft;
        public string pageNoRight;
        static public string pageNoFont;
        static public float pageNoFontSize;
        static public Vector2 pageNoMargin;
        static public string pageNoColor;
        static public bool pageNoEnabled;
    };

    class McfParser {

        const float SCALE = 0.4f;       // overall scale applied to pdf... does not affect resolution of images!
        const float FONT = 3.26f;       // match to photobook font size

        private XmlDocument _xmlDoc = new XmlDocument();
        private XmlNode _fotobook;      // the <fotobook> tag from .mcf file
        private XmlNode _stats;         // the <statistics> tag rfom .mcf file

        private List<XmlNode> _pages;   // all <page> nodes in .mcf
        public CoverLayout CoverLayout { get; private set; }
        private int _pageIterator = 0;  // keep track of current page

        public McfParser(System.IO.MemoryStream mcf) {
            Page.pageNoEnabled = false;
            // load xml into memory
            try {
                _xmlDoc.Load(mcf);
                Log.Message("Loaded .mcf from stream.");
            } catch (Exception e) {
                Log.Error("Loading .mcf from stream failed with message: " + e.Message);
                throw;
            }

            _parseXmlDocument();
            ReadCoverLayout();
        }

        private void _parseXmlDocument() {
            // get the root xml node 'fotobook'
            _fotobook = _xmlDoc.SelectSingleNode("fotobook");

            if (_fotobook == null) {
                Log.Error("xml parsing failed. No <fotobook> tag found.");
                return;
            }

            // log some photobook info from .mcf
            string loginfo = "";
            loginfo += "\n\t\t<fotobook>";
            loginfo += "\n\t\t  art_id=" + getAttributeStr(_fotobook, "art_id", "null");
            loginfo += "\n\t\t  productname=" + getAttributeStr(_fotobook, "productname", "null");

            XmlNode project = _fotobook.SelectSingleNode("project");
            loginfo += "\n\t\t  <project>";
            loginfo += "\n\t\t    createdWithHPSVersion=" + getAttributeStr(project, "createdWithHPSVersion", "null");

            XmlNode artcfg = _fotobook.SelectSingleNode("articleConfig");
            loginfo += "\n\t\t  <articleConfig>";
            loginfo += "\n\t\t    normalpages=" + getAttributeStr(artcfg, "normalpages", "null");
            loginfo += "\n\t\t    totalpages=" + getAttributeStr(artcfg, "totalpages", "null");

            Log.Info(".mcf content:" + loginfo);

            // initialize page list
            _pages = new List<XmlNode>();

            // handle all relevant nodes in <fotobook>
            foreach (XmlNode node in _fotobook.ChildNodes) {

                switch (node.Name) {
                    case "page":
                        _pages.Add(node);
                        break;

                    case "statistics":
                        // TODO: properly report to user
                        //Console.WriteLine("\nStatistics:\n\telapsed Time: " + node.Attributes.GetNamedItem("elapsedTimeNet").Value);
                        break;

                    case "pagenumbering":
                        Page.pageNoEnabled = getAttributeStr(node, "position", "0") != "0" && getAttributeStr(node, "enabled", "1") != "0";
                        float margin = getAttributeF(node, "margin");
                        float verticalMargin = getAttributeF(node, "verticalMargin");
                        Page.pageNoMargin = new Vector2(margin, verticalMargin);
                        Page.pageNoColor = getAttributeStr(node, "textcolor");
                        Page.pageNoFontSize = getAttributeF(node, "fontsize") * FONT;
                        Page.pageNoFont = getAttributeStr(node, "fontfamily");
                        break;

                    default:
                        // these are not needed/supported for now
                        Log.Warning("Unhandled Node in <fotobook> '" + node.Name + "'.");
                        break;
                }
            }
        }
        private void ReadCoverLayout() {
            var cover = _pages.FirstOrDefault(p => getAttributeStr(p,"type") == "fullcover")?.SelectSingleNode("bundlesize");
            var inside = _pages.Where(p => getAttributeStr(p,"type") is "normalpage" or "emptypage")
                .Select(p => p.SelectSingleNode("bundlesize")).FirstOrDefault(p => p != null);
            if (cover == null) return;
            if (inside != null) CoverLayout = Cewe2pdf.CoverLayout.FromSizes(getAttributeF(cover,"width"),getAttributeF(cover,"height"),getAttributeF(inside,"width"),getAttributeF(inside,"height"));
            if (CoverLayout == null) Log.Warning("Cover panel dimensions could not be determined; keeping the original full cover page.");
        }


        public Page nextPage() {
            // walk all pages found in .mcf, return null on last
            if (_pageIterator >= _pages.Count) return null; // handle out of bounds

            // the current xml page
            XmlNode xmlPage = _pages[_pageIterator];

            // the reconstructed page (still empty)
            Page page = new Page();

            // keep track which page we are currently processing, every Page object actually consists of
            // two <fotobook/page> nodes, the left and right side in photobook
            bool isDouble = false;

            // need to collect double pages here, in case of cover actually 3 pages.
            while (xmlPage != null) {

                // the current page type, later used to handle left/right and special page cases
                page.type = Page.convert(xmlPage.Attributes.GetNamedItem("type").Value);

                // store page number
                if (page.type == Page.Type.Normalpage) {
                    if (isDouble) page.pageNoRight = getAttributeStr(xmlPage, "pagenr");
                    else page.pageNoLeft = getAttributeStr(xmlPage, "pagenr");

                }

                // iterate all sub nodes this page contains
                foreach (XmlNode node in xmlPage.ChildNodes) {

                    switch (node.Name) {
                        // bundlesize is the left & right combined size of the page
                        case "bundlesize":
                            page.bundleSize = new Vector2(getAttributeF(node, "width"), getAttributeF(node, "height"));
                            break;

                        // NOTE: currently only handles background id
                        case "designElementIDs":
                            // store background for left and right individually
                            string background = getAttributeStr(node, "background");
                            if (!isDouble) page.backgroundLeft = string.IsNullOrWhiteSpace(background) ? null : background;
                            else page.backgroundRight = string.IsNullOrWhiteSpace(background) ? null : background;
                            break;

                        // area is the root class of all content objects
                        // NOTE: currently supports <imagearea> & <textarea>
                        case "area":
                            // get the type of current area
                            string type = node.Attributes.GetNamedItem("areatype").Value;

                            // trick area system...
                            if (type == "spinetextarea") {
                                type = "textarea";
                                page.spineSize = getAttributeF(node.SelectSingleNode("position") ?? node, "height");
                            }

                            Area newArea;

                            switch (type) {

                                case "spinelogoarea":
                                case "clipartarea": {
                                        bool logo = type == "spinelogoarea";
                                        if (logo && !Config.SpineLogo) { newArea = new Area(); break; }
                                        XmlNode clip = node.SelectSingleNode("clipart");
                                        XmlNode settings = clip?.SelectSingleNode("ClipartConfiguration");
                                        var art = new ClipartArea {
                                            designId = logo ? "spine-logo" : getAttributeStr(clip,"designElementId",getAttributeStr(node.SelectSingleNode("designElementIDs"),"clipart")),
                                            uniqueName = getAttributeStr(clip,"uniqueName"),
                                            mirror = getAttributeStr(settings,"mirror"),
                                            opacity = (float)ParseNumber(getAttributeStr(node,"alpha","1")),
                                            spineLogo = logo
                                        };
                                        if (settings != null) foreach(XmlNode colorNode in settings.SelectNodes("colors/color"))
                                            art.colors[getAttributeStr(colorNode,"source")] = getAttributeStr(colorNode,"target");
                                        newArea = art; break;
                                    }

                                case "imagearea": {
                                        // imagearea? image subnode exists!
                                        XmlNode image = node.SelectSingleNode("image");

                                        // the image file name stored in .mcf file (in format: "safecontainer:/imageName.jpg)
                                        string filename = getAttributeStr(image, "filename");

                                        // remove 'safecontainer:/', in case filename does not exist,
                                        // store "NULL", will render as magenta outline and print error.
                                        string fileName = filename != "" ? filename.Replace("safecontainer:/", "") : "NULL";

                                        // get & store cutout information
                                        XmlNode cutout = image.SelectSingleNode("cutout") ?? image;
                                        Vector2 cutoutLeftTop = new Vector2(getAttributeF(cutout, "left"), getAttributeF(cutout, "top"));
                                        float scale = getAttributeF(cutout, "scale", 1.0f);
                                        // Pre-cutout MCF stores translation in source-image pixels.
                                        // New cutout elements already use layout units.
                                        if (image.SelectSingleNode("cutout") == null) cutoutLeftTop *= scale / SCALE;

                                        // construct new area
                                        newArea = new ImageArea() {
                                            filename = fileName,
                                            cutout = cutoutLeftTop,
                                            scale = scale,
                                        };

                                        // get & store border settings
                                        XmlNode border = node.SelectSingleNode("decoration/border") ?? node.SelectSingleNode("border");
                                        if (border != null) {
                                            newArea.border = getAttributeStr(border, "enabled", "1") is not ("0" or "false" or "no");
                                            newArea.borderWidth = Math.Max(0, getAttributeF(border, "width"));
                                            newArea.borderColor = getAttributeStr(border, "color");
                                            newArea.borderPosition = getAttributeStr(border, "position", border.Attributes?["offset"] != null ? "centered" : "outside");
                                            newArea.borderGap = getAttributeF(border, "gap");
                                            newArea.borderOffset = getAttributeF(border, "offset");
                                        } else {
                                            newArea.border = getAttributeStr(node,"borderenabled","0") is "1" or "true" or "yes";
                                            newArea.borderWidth = Math.Max(0,getAttributeF(node,"sizeborder"));
                                            newArea.borderColor = getAttributeStr(node,"colorborder");
                                        }
                                        ((ImageArea)newArea).corners = ImageCorners.Parse(node);

                                        break;
                                    }

                                case "imagebackgroundarea": {
                                        // handle backgroundimages literally just like normal images.
                                        // TODO: de-duplicate this code as much as possible

                                        XmlNode imgbg = node.SelectSingleNode("imagebackground");

                                        // the image file
                                        string filename = getAttributeStr(imgbg, "filename");

                                        // remove 'safecontainer:/', in case filename does not exist,
                                        // store "NULL", will render as magenta outline and print error.
                                        string fileName = filename != "" ? filename.Replace("safecontainer:/", "") : "NULL";

                                        // get & store cutout information
                                        XmlNode cutout = imgbg.SelectSingleNode("cutout") ?? imgbg;
                                        Vector2 cutoutLeftTop = new Vector2(getAttributeF(cutout, "left"), getAttributeF(cutout, "top"));
                                        float scale = getAttributeF(cutout, "scale", 1.0f);
                                        if (imgbg.SelectSingleNode("cutout") == null) cutoutLeftTop *= scale / SCALE;

                                        string bgPosition = getAttributeStr(imgbg, "backgroundPosition");
                                        ImageBackgroundArea.ImageBackgroundType bgtype = ImageBackgroundArea.ImageBackgroundType.Undefined;

                                        if (bgPosition == "LEFT_OR_TOP")
                                            bgtype = ImageBackgroundArea.ImageBackgroundType.Left;
                                        else if (bgPosition == "RIGHT_OR_BOTTOM")
                                            bgtype = ImageBackgroundArea.ImageBackgroundType.Right;
                                        else if (bgPosition == "BUNDLE")
                                            bgtype = ImageBackgroundArea.ImageBackgroundType.Bundle;
                                        else
                                            Log.Error("Unhandled background image position: " + bgPosition);

                                        // construct new area
                                        newArea = new ImageBackgroundArea() {
                                            filename = fileName,
                                            cutout = cutoutLeftTop,
                                            scale = scale,
                                            type = bgtype
                                        };

                                        break;
                                    }

                                case "textarea": {
                                        // in <textarea> these exist:
                                        XmlNode text = node.SelectSingleNode("text");
                                        XmlNode textFormat = text.SelectSingleNode("textFormat");

                                        // NOTE: <font> stores several comma-separated values: Fontname,Fonstsize,...and more. Currently only handles these two
                                        string[] fontInfo = getAttributeStr(textFormat, "font").Split(",");

                                        // get the fontsize, take pdf scale into account and adjust to photobook settings
                                        int fontSize = (int)(ParseNumber(fontInfo[1]) * SCALE * FONT); // somewhat matches the result in photobook

                                        // text color
                                        string color = getAttributeStr(textFormat, "foregroundColor");

                                        // text box background color
                                        string bgColor = getAttributeStr(textFormat, "backgroundColor");
                                        // Legacy projects serialized ARGB zero as '#000000'.
                                        if (node.SelectSingleNode("position") == null && bgColor == "#000000") bgColor = "#00000000";

                                        // by default align left top
                                        string alignLabel = "ALIGNLEFT";
                                        string valignLabel = "ALIGNVTOP";

                                        // NOTE: <align> sometimes holds two comma-separated values (Horizontal and Vertical alignment)
                                        // for now only handles second (horizontal).
                                        string[] align = getAttributeStr(textFormat, "Alignment").Split(",");
                                        alignLabel = align.First();
                                        if (align.Length > 1)
                                            valignLabel = align.Last();
                                        if (int.TryParse(align[0], out int qtAlign)) {
                                            alignLabel = (qtAlign & 4) != 0 ? "ALIGNHCENTER" : (qtAlign & 2) != 0 ? "ALIGNRIGHT" : (qtAlign & 8) != 0 ? "ALIGNJUSTIFY" : "ALIGNLEFT";
                                            valignLabel = (qtAlign & 128) != 0 ? "ALIGNVCENTER" : (qtAlign & 64) != 0 ? "ALIGNVBOTTOM" : "ALIGNVTOP";
                                        }

                                        var elements = extractTextFromHTMLv2(text.InnerText, ref color, fontInfo[0], fontSize);
                                        string str = string.Concat(elements.Select(e => e.text + (e.newline ? "\n" : "")));

                                        XmlNode outline = text.SelectSingleNode("outline");
                                        bool outlineEnabled = outline != null
                                            ? getAttributeStr(outline,"enabled","1") is not ("0" or "false" or "no")
                                            : getAttributeStr(textFormat,"hasOutline","0") == "1";
                                        float outlineWidth = outlineEnabled ? Math.Max(0, getAttributeF(outline ?? textFormat, outline != null ? "width" : "outlineWidth")) : 0;
                                        string outlineColor = getAttributeStr(outline,"color",getAttributeStr(textFormat,"outlineColor"));

                                        // construct new area
                                        newArea = new TextArea() {
                                            textElements = elements,
                                            text = str,
                                            fontsize = fontSize,
                                            color = color,
                                            font = fontInfo[0],
                                            align = alignLabel,
                                            valign = valignLabel,
                                            backgroundcolor = bgColor,
                                            outlineWidth = outlineWidth,
                                            outlineColor = outlineColor,
                                        };

                                        break;
                                    }
                                default:
                                    // there are more areatypes, for now just create an empty area that wont draw anything
                                    // and inform user.
                                    newArea = new Area();
                                    Log.Warning("Unhandled area type in <page/area> '" + type + "'.");
                                    break;
                            }

                            // sanity check, cant be null really :P
                            if (newArea == null) break;

                            // all areas contain position information
                            XmlNode position = node.SelectSingleNode("position") ?? node;

                            // apply position information to current area
                            newArea.rect = new RectangleF() {
                                X = getAttributeF(position, "left"),
                                Y = getAttributeF(position, "top"),
                                Width = getAttributeF(position, "width"),
                                Height = getAttributeF(position, "height")
                            };

                            if (newArea is TextArea) {
                                // Sometime text fails if not rounded
                                newArea.rect.Width = (float)Math.Round(newArea.rect.Width);
                                newArea.rect.Height = (float)Math.Round(newArea.rect.Height);
                            }

                            newArea.shadow = ShadowSettings.Parse(node);
                            newArea.rotation = getAttributeF(position, "rotation") / SCALE; // undo scale for rotation

                            // store new page in list
                            page.areas.Add(newArea);
                            break;

                        default:
                            // inform user about unhandled node
                            Log.Warning("Unhandled Node in <page> '" + node.Name + "'.");
                            break;
                    }
                }

                // Handle all these specific page types and cases

                if (page.type == Page.Type.Fullcover || page.type == Page.Type.Spine) {
                    XmlNode nextPage = _pageIterator+1<_pages.Count ? _pages[_pageIterator+1] : null;
                    var nextType = nextPage != null ? Page.convert(getAttributeStr(nextPage,"type")) : Page.Type.Unknown;
                    xmlPage = nextType is Page.Type.Fullcover or Page.Type.Spine ? nextPage : null;
                    page.type = Page.Type.Fullcover;
                } else if (page.type == Page.Type.Emptypage) {
                    // check if next page exists... otherwise end of book.
                    if (_pageIterator + 1 < _pages.Count) {
                        XmlNode nextPage = _pages[_pageIterator + 1];
                        xmlPage = nextPage;
                        isDouble = true;
                    } else {
                        xmlPage = null;
                    }
                } else if (page.type == Page.Type.Normalpage && !isDouble) {
                    XmlNode nextPage = _pageIterator+1<_pages.Count ? _pages[_pageIterator+1] : null;
                    // next is second half of a double page...
                    xmlPage = nextPage;
                    isDouble = true;
                } else {
                    // this was second half of a double page, continue with new page
                    xmlPage = null;
                }

                // increment to handle next <page> object in list
                _pageIterator++;
            }

            if(page.type==Page.Type.Fullcover && CoverLayout!=null)page.spineSize=CoverLayout.SpineWidth;
            // return the newly constructed page
            return page;
        }

        private List<TextElement> extractTextFromHTMLv2(string html, ref string? textAreaColor, string defaultFamily, int defaultSize) {
            var doc = new XmlDocument { XmlResolver = null };
            doc.LoadXml(html);
            var body = doc.SelectSingleNode("html/body");
            var result = new List<TextElement>();
            if (body == null) throw new FormatException("CEWE text has no HTML body.");
            string baseColor = textAreaColor ?? "#ff000000";
            var initial = new TextElement { color = baseColor, family = defaultFamily, size = defaultSize };
            void Visit(XmlNode node, TextElement inherited) {
                string family = inherited.family, color = inherited.color;
                int size = inherited.size, weight = inherited.bold ? 700 : 400;
                string style = inherited.italic ? "italic" : "normal";
                string decoration = inherited.underlined ? "underline" : "";
                parseBodyStyle(getAttributeStr(node, "style"), ref family, ref size, ref weight, ref style, ref color, ref decoration);
                var current = new TextElement { family = family, size = size, color = color,
                    bold = weight > 400, italic = style == "italic", underlined = decoration.Contains("underline"),
                    align = getAttributeStr(node, "align", inherited.align) };
                if (node.NodeType == XmlNodeType.Text || node.NodeType == XmlNodeType.CDATA || node.NodeType == XmlNodeType.SignificantWhitespace) {
                    current.text = node.Value;
                    result.Add(current);
                    return;
                }
                if (node.Name == "br") { current.text = "\n"; result.Add(current); return; }
                int start = result.Count;
                foreach (XmlNode child in node.ChildNodes) Visit(child, current);
                if (node.Name == "p") {
                    if (result.Count == start) { current.text = ""; result.Add(current); }
                    result[result.Count - 1].newline = true;
                }
            }
            Visit(body, initial);
            return result;
        }

        void parseBodyStyle(string bodyStyle, ref string fontFamily, ref int fontSize, ref int fontWeight, ref string fontStyle, ref string color, ref string textDecoration) {
            string[] styles = bodyStyle.Split(";");
            foreach (string style in styles) {
                string curr = style.Trim();

                if (curr.StartsWith("font-family:")) {
                    fontFamily = curr.Replace("font-family:", "").Trim().Split(',')[0].Trim().Trim('\"', '\'');
                } else
                if (curr.StartsWith("font-size:")) {
                    fontSize = (int)(ParseNumber(curr.Replace("font-size:", "").Replace("pt", "")) * SCALE * FONT);
                } else
                if (curr.StartsWith("font-weight:")) {
                    fontWeight = int.Parse(curr.Replace("font-weight:", ""), NumberStyles.Integer, CultureInfo.InvariantCulture);
                } else
                if (curr.StartsWith("font-style:")) {
                    fontStyle = curr.Replace("font-style:", "").Trim();
                } else
                if (curr.StartsWith("color:")) {
                    string value = curr.Replace("color:", "").Trim();
                    color = value.Length == 7 ? value.Insert(1, "ff") : value;
                } else
                if (curr.StartsWith("text-decoration:")) {
                    textDecoration = curr.Replace("text-decoration:", "").TrimStart();
                } else {
                    if (!String.IsNullOrWhiteSpace(curr))
                        Log.Warning("Unhandled html/body/style property: '" + curr + "'.");
                }
            }
        }

        // XML/CSS numbers use decimal points. Do not accept thousands separators.
        internal static double ParseNumber(string value) {
            double number = double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
            if (!double.IsFinite(number) || Math.Abs(number) > float.MaxValue)
                throw new FormatException("Invalid finite CEWE number: '" + value + "'.");
            return number;
        }
        float getAttributeF(XmlNode node, string name, float or = 0.0f) {
            if (node == null) return or; // return default if node is null
            XmlNode attr = node.Attributes?.GetNamedItem(name);
            if (attr == null) return or; // return default if attribute does not exist
            string value = attr.Value;
            if (String.IsNullOrWhiteSpace(value)) return or; // return default if attribute was empty
            // convert attribute string to float, including pdf scale
            return (float)ParseNumber(value) * SCALE;
        }

        string getAttributeStr(XmlNode node, string name, string or = "") {
            if (node == null) return or; // return default if node is null
            XmlNode attr = node.Attributes?.GetNamedItem(name);
            if (attr == null) return or; // return default if attribute does not exist
            return attr.Value; // return string directly
        }

        public int pageCount() => _pages.Count / 2; // return page count, account for double pages
    }
}
