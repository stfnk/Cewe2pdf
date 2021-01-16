using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Xml;

namespace Cewe2pdf {

    class Area {
        public RectangleF rect;
        public float rotation;
        public bool border;
        public float borderWidth;
        public string borderColor;
        public virtual string toString() {
            return "[Area] rect: " + rect.ToString() + "; rotation: " + rotation.ToString("F2");
        }
    };

    class ImageArea : Area {
        public string path;
        public Vector2 cutout;
        public float scale;
        public override string toString() {
            return "[ImageArea] rect: " + rect.ToString() + "; rotation: " + rotation.ToString("F2") + "; path: " + path;
        }
    };

    class ImageBackgroundArea : ImageArea {
        public enum ImageBackgroundType { Undefined, Left, Right, Bundle }
        public ImageBackgroundType type;
        public override string toString() {
            return "[ImageBackgroundArea] rect: " + rect.ToString() + "; rotation: " + rotation.ToString("F2") + "; path: " + path;
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
    };

    class mcfParser {

        const float SCALE = 0.4f;       // overall scale applied to pdf... does not affect resolution of images!

        const float FONT = 3.26f;       // match to photobook font size
        private string _safeContainerPath; // path to the image folder
        private XmlDocument _xmlDoc = new XmlDocument();
        private XmlNode _fotobook;      // the <fotobook> tag from .mcf file
        private XmlNode _stats;         // the <statistics> tag rfom .mcf file

        private List<XmlNode> _pages;   // all <page> nodes in .mcf
        private int _pageIterator = 0;  // keep track of current page

        public mcfParser(string pFilePath) {
            // load xml into memory
            try {
                _xmlDoc.Load(pFilePath);
                Log.Message("Loaded '" + pFilePath + "'.");
            } catch (Exception e) {
                Log.Error("Loading .mcf File: '" + pFilePath + "' failed with message: " + e.Message);
                return;
            }

            // remove .mcf from path, add folder suffix
            _safeContainerPath = pFilePath.Substring(0, pFilePath.Length - 4) + "_mcf-Dateien\\";

            // check if this path actually exists
            if (!System.IO.Directory.Exists(_safeContainerPath)) {
                Log.Error("Image folder not found. Expected at: '" + _safeContainerPath + "'"); // TODO: Log.Message some hints what to do?
                return;
            }

            // get the root xml node 'fotobook'
            _fotobook = _xmlDoc.SelectSingleNode("fotobook");

            if (_fotobook == null) {
                Log.Error("Parsing '" + pFilePath + "' failed. No <fotobook> tag found.");
                return;
            }

            // log some photobook info from .mcf
            string loginfo = "";
            loginfo += "\n<fotobook>";
            loginfo += "\n\tart_id=" + getAttributeStr(_fotobook, "art_id", "null");
            loginfo += "\n\tproductname=" + getAttributeStr(_fotobook, "productname", "null");

            XmlNode project = _fotobook.SelectSingleNode("project");
            loginfo += "\n\t<project>";
            loginfo += "\n\t\tcreatedWithHPSVersion=" + getAttributeStr(project, "createdWithHPSVersion", "null");

            XmlNode artcfg = _fotobook.SelectSingleNode("articleConfig");
            loginfo += "\n\t<articleConfig>";
            loginfo += "\n\t\tnormalpages=" + getAttributeStr(artcfg, "normalpages", "null");
            loginfo += "\n\t\ttotalpages=" + getAttributeStr(artcfg, "totalpages", "null");

            Log.Info("mcf content:"+loginfo);

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
                        float margin = getAttributeF(node, "margin");
                        float verticalMargin = getAttributeF(node, "verticalMargin");
                        Page.pageNoMargin = new Vector2(margin, verticalMargin);
                        Page.pageNoColor = getAttributeStr(node, "textcolor");
                        Page.pageNoFontSize = getAttributeF(node, "fontsize") * FONT;
                        Page.pageNoFont = getAttributeStr(node, "fontfamily");
                        break;

                    default:
                        // these are not needed
                        Log.Warning("Unhandled Node in <fotobook> '" + node.Name + "'.");
                        break;
                }
            }
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
                            if (!isDouble) page.backgroundLeft = getAttributeStr(node, "background");
                            else page.backgroundRight = getAttributeStr(node, "background");
                            break;

                        // area is the root class of all content objects
                        // NOTE: currently supports <imagearea> & <textarea>
                        case "area":
                            // get the type of current area
                            string type = node.Attributes.GetNamedItem("areatype").Value;

                            // trick area system...
                            if (type == "spinetextarea") {
                                type = "textarea";
                                page.spineSize = getAttributeF(node.SelectSingleNode("position"), "height");
                            }

                            Area newArea;

                            switch (type) {

                                case "imagearea": {
                                        // imagearea? image subnode exists!
                                        XmlNode image = node.SelectSingleNode("image");

                                        // the image file name stored in .mcf file (in format: "safecontainer:/imageName.jpg)
                                        string filename = getAttributeStr(image, "filename");

                                        // replace 'safecontainer:/' with actual path, in case filename does not exist,
                                        // store "NULL", will render as magenta outline and print error.
                                        string filePath = filename != "" ? filename.Replace("safecontainer:/", _safeContainerPath) : "NULL";

                                        // get & store cutout information
                                        XmlNode cutout = image.SelectSingleNode("cutout");
                                        Vector2 cutoutLeftTop = new Vector2(getAttributeF(cutout, "left"), getAttributeF(cutout, "top"));
                                        float scale = getAttributeF(cutout, "scale", 1.0f);

                                        // construct new area
                                        newArea = new ImageArea() {
                                            path = filePath,
                                            cutout = cutoutLeftTop,
                                            scale = scale,
                                        };

                                        // get & store border settings
                                        XmlNode border = node.SelectSingleNode("decoration/border");
                                        if (border != null) {
                                            newArea.border = true;
                                            newArea.borderWidth = getAttributeF(border, "width");
                                            newArea.borderColor = getAttributeStr(border, "color");
                                        }

                                        break;
                                    }

                                case "imagebackgroundarea": {
                                        // handle backgroundimages literally just like normal images.
                                        // TODO: de-duplicate this code as much as possible

                                        XmlNode imgbg = node.SelectSingleNode("imagebackground");

                                        // the image file
                                        string filename = getAttributeStr(imgbg, "filename");

                                        // replace 'safecontainer:/' with actual path, in case filename does not exist,
                                        // store "NULL", will render as magenta outline and print error.
                                        string filePath = filename != "" ? filename.Replace("safecontainer:/", _safeContainerPath) : "NULL";

                                        // get & store cutout information
                                        XmlNode cutout = imgbg.SelectSingleNode("cutout");
                                        Vector2 cutoutLeftTop = new Vector2(getAttributeF(cutout, "left"), getAttributeF(cutout, "top"));
                                        float scale = getAttributeF(cutout, "scale", 1.0f);

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
                                            path = filePath,
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
                                        int fontSize = (int)(Convert.ToInt32(fontInfo[1]) * SCALE * FONT); // somewhat matches the result in photobook

                                        // text color
                                        string color = getAttributeStr(textFormat, "foregroundColor");

                                        // text box background color
                                        string bgColor = getAttributeStr(textFormat, "backgroundColor");

                                        // by default align left top
                                        string alignLabel = "ALIGNLEFT";
                                        string valignLabel = "ALIGNVTOP";

                                        // NOTE: <align> sometimes holds two comma-separated values (Horizontal and Vertical alignment)
                                        // for now only handles second (horizontal).
                                        string[] align = getAttributeStr(textFormat, "Alignment").Split(",");
                                        alignLabel = align.Last();
                                        if (align.Length > 1)
                                            valignLabel = align.First();

                                        string str = extractTextFromHTML(text.InnerText, ref color);

                                        // construct new area
                                        newArea = new TextArea() {
                                            textElements = extractTextFromHTMLv2(text.InnerText),
                                            text = str,
                                            fontsize = fontSize,
                                            color = color,
                                            font = fontInfo[0],
                                            align = alignLabel,
                                            valign = valignLabel,
                                            backgroundcolor = bgColor,
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
                            XmlNode position = node.SelectSingleNode("position");

                            // apply position information to current area
                            newArea.rect = new RectangleF() {
                                X = getAttributeF(position, "left"),
                                Y = getAttributeF(position, "top"),
                                Width = getAttributeF(position, "width"),
                                Height = getAttributeF(position, "height")
                            };
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

                if (page.type == Page.Type.Fullcover) {
                    // check if next page is spine, otherwise this was the back side of the cover
                    XmlNode nextPage = _pages[_pageIterator + 1];
                    Page.Type nextType = Page.convert(nextPage.Attributes.GetNamedItem("type").Value);
                    if (nextType == Page.Type.Spine) xmlPage = nextPage;
                    else xmlPage = null; // cover page is done, proceed
                } else if (page.type == Page.Type.Spine) {
                    // check if next page is Fullcover (should be anyway)
                    XmlNode nextPage = _pages[_pageIterator + 1];
                    Page.Type nextType = Page.convert(nextPage.Attributes.GetNamedItem("type").Value);
                    if (nextType == Page.Type.Fullcover) xmlPage = nextPage;
                    else xmlPage = null;
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
                    XmlNode nextPage = _pages[_pageIterator + 1];
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

            // return the newly constructed page
            return page;
        }

        string extractTextFromHTML(string html, ref string? color) {

            // the text to return
            string res = "";

            // text is stored in html inside the .mcf
            // html basically is xml so... parse it as xml
            XmlDocument doc = new XmlDocument();
            doc?.LoadXml(html);

            // get the node that contains span objects for each line of text
            XmlNode node = doc?.SelectSingleNode("html/body/table/tr/td");

            if (node == null) {
                Log.Error("Text node not found. Stopping text parsing.");
                return "";
            }

            // extract text from each <span> and store in single string with newline character
            string styleInfo = "";
            foreach (XmlNode p in node.ChildNodes) {
                XmlNode span = p.SelectSingleNode("span");
                res += span?.InnerText + "\n"; // if span exists... add text + newline
                styleInfo = getAttributeStr(span, "style");
            }

            if (color != null && styleInfo.Contains("color:")) {
                string[] t = styleInfo.Split("color:");
                string colorhex = t.Last().Split(";").First();
                color = colorhex.Insert(1, "ff");
            }

            // return all lines
            return res;
        }

        private List<TextElement> extractTextFromHTMLv2(string html) {

            List<TextElement> ret = new List<TextElement>();

            // text is stored in html inside the .mcf
            // html basically is xml so... parse it as xml
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(html);

            // the body object contains everything we need
            XmlNode body = doc.SelectSingleNode("html/body");

            if (body == null) {
                Log.Error("Extracting text from html failed. HTML string:\n\n" + html + "\n\n");
                return ret;
            }

            // unless specified different these are the main font settings
            string fontFamily = "Calibri";
            int fontSize = 48;
            int fontWeight = 400;
            string fontStyle = "normal";
            string color = "#ffffffff";
            string textDecoration = "";

            string bodyStyle = getAttributeStr(body, "style");
            if (String.IsNullOrWhiteSpace(bodyStyle)) Log.Error("Body style for given html text was null.");
            else parseBodyStyle(bodyStyle, ref fontFamily, ref fontSize, ref fontWeight, ref fontStyle, ref color, ref textDecoration);

            // now loop through all <p> elements in <td> (new lines)
            XmlNode td = body.SelectSingleNode("table/tr/td");

            if (td == null) {
                Log.Error("table/tr/td was null. HTML string:\n\n" + html + "\n\n");
                return ret;
            }

            foreach (XmlNode p in td.ChildNodes) {
                if (p.Name != "p") continue;

                string align = getAttributeStr(p, "align", "Center");

                int i = 0;

                foreach (XmlNode span in p.ChildNodes) {
                    string style = getAttributeStr(span, "style"); // get the span specific style

                    // these might change per span
                    string fontFamilySpan = fontFamily;
                    int fontSizeSpan = fontSize;
                    int fontWeightSpan = fontWeight;
                    string fontStyleSpan = fontStyle;
                    //string colorSpan = color; // color is not specified per item, but repeats last settings.
                    string textDecorationSpan = textDecoration;

                    if (String.IsNullOrWhiteSpace(style)) Log.Warning("No style for span: '" + span.InnerText + "'.");
                    else parseBodyStyle(style, ref fontFamilySpan, ref fontSizeSpan, ref fontWeightSpan, ref fontStyleSpan, ref color, ref textDecorationSpan);

                    i++; // increment to check for last span element

                    // construct a new TextElement
                    TextElement text = new TextElement() {
                        text = span.InnerText,
                        bold = fontWeightSpan > 400,
                        italic = fontStyleSpan == "italic",
                        underlined = textDecorationSpan == "underline",
                        newline = i == p.ChildNodes.Count,
                        color = color,
                        family = fontFamilySpan,
                        size = fontSizeSpan,
                        align = align,
                    };

                    // add to return list
                    ret.Add(text);
                }
            }

            return ret;
        }

        void parseBodyStyle(string bodyStyle, ref string fontFamily, ref int fontSize, ref int fontWeight, ref string fontStyle, ref string color, ref string textDecoration) {
            string[] styles = bodyStyle.Split(";");
            foreach (string style in styles) {
                string curr = style;
                if (style.StartsWith(" "))
                    curr = style.TrimStart();

                if (curr.StartsWith("font-family:")) {
                    fontFamily = curr.Replace("font-family:", "").Replace("'", "");
                } else
                if (curr.StartsWith("font-size:")) {
                    fontSize = (int)(Convert.ToDouble(curr.Replace("font-size:", "").Replace("pt", "")) * SCALE * FONT);
                } else
                if (curr.StartsWith("font-weight:")) {
                    fontWeight = Convert.ToInt32(curr.Replace("font-weight:", ""));
                } else
                if (curr.StartsWith("font-style:")) {
                    fontStyle = curr.Replace("font-style:", "");
                } else
                if (curr.StartsWith("color:")) {
                    color = curr.Replace("color:", "").Insert(1, "ff");
                } else
                if (curr.StartsWith("text-decoration:")) {
                    textDecoration = curr.Replace("text-decoration:", "").TrimStart();
                } else {
                    if (!String.IsNullOrWhiteSpace(curr))
                        Log.Warning("Unhandled html/body/style property: '" + curr + "'.");
                }
            }
        }

        float getAttributeF(XmlNode node, string name, float or = 0.0f) {
            if (node == null) return or; // return default if node is null
            XmlNode attr = node.Attributes?.GetNamedItem(name);
            if (attr == null) return or; // return default if attribute does not exist
            string value = attr.Value;
            if (String.IsNullOrWhiteSpace(value)) return or; // return default if attribute was empty
            // convert attribute string to float, including pdf scale
            return (float)Convert.ToDouble(value.Replace(".", ",")) * SCALE;
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
