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
        public virtual string toString() {
            return "[ImageArea] rect: " + rect.ToString() + "; rotation: " + rotation.ToString("F2") + "; path: " + path;
        }
    };

    class TextArea : Area {
        public string text;
        public int fontsize;
        public string color;
        public string font;
        public string align;
        public virtual string toString() {
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
    };

    class mcfParser {

        const float SCALE = 0.4f;

        private string _filePath;
        private XmlDocument _xmlDoc = new XmlDocument();

        private XmlNode _fotobook;      // the 'fotobook' tag from .mcf file

        // interesting data
        private List<XmlNode> _pages = new List<XmlNode>();   // all page nodes in xml
        private XmlNode _stats;         // the statistics node

        private int _pageIterator = 0;

        public mcfParser(string pFilePath) {

            // load xml into memory
            try {
                _xmlDoc.Load(pFilePath);
            } catch (Exception e) {
                Console.WriteLine("Loading mcf File: '" + pFilePath + "' failed with error: " + e.Message);
            }

            // store filepath
            _filePath = pFilePath;

            // get the root xml node 'fotobook'
            _fotobook = _xmlDoc.SelectSingleNode("fotobook");
            Debug.Assert(_fotobook != null, "parsing " + pFilePath + " failed. 'fotobook' tag not found. Is it a valid .mfc file?");

            // store all relevant nodes
            foreach (XmlNode node in _fotobook.ChildNodes) {
                switch (node.Name) {
                    case "page":
                        _pages.Add(node);
                        break;
                    case "statistics":
                        Console.WriteLine("\nStatistics:\n\telapsed Time: " + node.Attributes.GetNamedItem("elapsedTimeNet").Value);
                        break;
                    default:
                        Console.WriteLine("Found unhandled Node '" + node.Name + "' in 'fotobook'");
                        break;
                }
            }
        }

        /// <summary>
        /// iterates all pages in xml, returns 0 when reached end.
        /// </summary>
        /// <returns>current page, null after last</returns>
        public Page nextPage() {
            if (_pageIterator >= _pages.Count) return null; // avoid overflow

            Console.WriteLine("Page " + _pageIterator + "/" + _pages.Count);

            // the current xml page
            XmlNode xmlPage = _pages[_pageIterator];

            // the reconstructed page (still empty)
            Page page = new Page();

            bool isDouble = false;

            // need to collect double pages here, in case of cover actually 3 pages.
            while (xmlPage != null) {

                // the current page type
                page.type = Page.convert(xmlPage.Attributes.GetNamedItem("type").Value);

                // iterate all sub nodes this page contains
                foreach (XmlNode node in xmlPage.ChildNodes) {

                    switch (node.Name) {
                        // bundlesize is the total size of the page
                        case "bundlesize":
                            page.bundleSize = new Vector2(getAttributeF(node, "width"), getAttributeF(node, "height"));
                            break;

                        case "designElementIDs":
                            if (!isDouble)
                                page.backgroundLeft = getAttributeStr(node, "background");
                            else
                                page.backgroundRight = getAttributeStr(node, "background");
                            break;

                        // area is the root class of all content objects
                        case "area":
                            string type = node.Attributes.GetNamedItem("areatype").Value;

                            Area newArea;

                            switch (type) {
                                case "imagearea":
                                    XmlNode image = node.SelectSingleNode("image");

                                    // the image file name stored in .mcf file
                                    string filename = getAttributeStr(image, "filename");

                                    // construct path to the Images folder next to .mcf file
                                    string path = _filePath.Substring(0, _filePath.Length - 4) + "_mcf-Dateien\\";

                                    string filePath = filename != "" ? filename.Replace("safecontainer:/", path) : "NULL";

                                    // cutout information
                                    XmlNode cutout = image.SelectSingleNode("cutout");
                                    Vector2 cutoutLeftTop = new Vector2(getAttributeF(cutout, "left"), getAttributeF(cutout, "top"));
                                    float scale = getAttributeF(cutout, "scale", 1.0f);

                                    newArea = new ImageArea() {
                                        path = filePath,
                                        cutout = cutoutLeftTop,
                                        scale = scale,
                                    };

                                    // border settings
                                    XmlNode border = node.SelectSingleNode("decoration/border");
                                    if (border != null) {
                                        newArea.border = true;
                                        newArea.borderWidth = getAttributeF(border, "width");
                                        newArea.borderColor = getAttributeStr(border, "color");
                                    }
                                    break;

                                case "textarea": {
                                        XmlNode text = node.SelectSingleNode("text");
                                        XmlNode textFormat = text.SelectSingleNode("textFormat");
                                        string[] fontInfo = getAttributeStr(textFormat, "font").Split(",");

                                        int fontSize = (int)(Convert.ToInt32(fontInfo[1]) * SCALE * 3.26f); // somewhat matches the result in photobook

                                        string color = getAttributeStr(textFormat, "foregroundColor");

                                        string alignLabel = "ALIGNLEFT";
                                        string[] align = getAttributeStr(textFormat, "Alignment").Split(",");
                                        alignLabel = align.Last();

                                        newArea = new TextArea() {
                                            text = extractTextFromHTML(text.InnerText),
                                            fontsize = fontSize,
                                            color = color,
                                            font = fontInfo[0],
                                            align = alignLabel,
                                        };
                                        break;
                                    }
                                default:
                                    Console.WriteLine("Unhandled area type: '" + type + "'");
                                    newArea = new Area();
                                    break;
                            }

                            if (newArea == null) break;

                            // all areas contain position information
                            XmlNode position = node.SelectSingleNode("position");

                            // apply position information to current page
                            newArea.rect = new RectangleF() {
                                X = getAttributeF(position, "left"),
                                Y = getAttributeF(position, "top"),
                                Width = getAttributeF(position, "width"),
                                Height = getAttributeF(position, "height"),
                            };
                            newArea.rotation = getAttributeF(position, "rotation");

                            page.areas.Add(newArea);
                            break;

                        // unhandled subtype
                        default:
                            Console.WriteLine("Unhandled page node: '" + node.Name + "'");
                            break;
                    }
                }

                // For now just handle all these specific cases
                if (page.type == Page.Type.Fullcover) {
                    // check if next page is spine
                    XmlNode nextPage = _pages[_pageIterator + 1];
                    Page.Type nextType = Page.convert(nextPage.Attributes.GetNamedItem("type").Value);
                    if (nextType == Page.Type.Spine) {
                        xmlPage = nextPage;
                    } else {
                        xmlPage = null;
                    }

                } else if (page.type == Page.Type.Spine) {
                    // check if next page is spine
                    XmlNode nextPage = _pages[_pageIterator + 1];
                    Page.Type nextType = Page.convert(nextPage.Attributes.GetNamedItem("type").Value);
                    if (nextType == Page.Type.Fullcover) {
                        xmlPage = nextPage;
                    } else {
                        xmlPage = null;
                    }
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
                    // special case, second half of a double page... 
                    xmlPage = nextPage;
                    isDouble = true;
                } else {
                    xmlPage = null;
                }

                _pageIterator++;
            }

            return page;
        }

        string extractTextFromHTML(string html) {

            //string text = html.Replace("</span></p></td></tr></table></body></html>", "");

            //string[] subs = text.Split(">");

            string res = "";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(html);


            //XmlNode body = doc.SelectSingleNode("body");
            //XmlNode table = body.SelectSingleNode("table");
            //XmlNode tr = table.SelectSingleNode("tr");
            //XmlNode td = tr.SelectSingleNode("td");


            XmlNode node = doc.SelectSingleNode("html/body/table/tr/td");

            foreach (XmlNode p in node.ChildNodes) {
                XmlNode span = p.SelectSingleNode("span");
                res += span?.InnerText + "\n";
            }

            //return subs.Last();
            return res;
        }

        float getAttributeF(XmlNode node, string name, float or = 0.0f) {
            if (node == null) return or;
            XmlNode attr = node.Attributes.GetNamedItem(name);
            if (attr == null) return or;
            string value = attr.Value;
            if (value == "") return or;

            return (float)Convert.ToDouble(value.Replace(".", ",")) * SCALE;
        }

        string getAttributeStr(XmlNode node, string name, string or = "") {
            if (node == null) return or;
            XmlNode attr = node.Attributes.GetNamedItem(name);
            if (attr == null) return or;
            return attr.Value;
        }
    }
}
