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
        
        const float SCALE = 0.4f;       // overall scale applied to pdf... does not affect resolution of images!

        private string _filePath;       // .mcf file path, used to construct images file path
        private XmlDocument _xmlDoc = new XmlDocument();
        private XmlNode _fotobook;      // the <fotobook> tag from .mcf file
        private XmlNode _stats;         // the <statistics> tag rfom .mcf file

        private List<XmlNode> _pages;   // all <page> nodes in .mcf
        private int _pageIterator = 0;  // keep track of current page

        public mcfParser(string pFilePath) {

            // load xml into memory
            try {
                _xmlDoc.Load(pFilePath);
            } catch (Exception e) {
                Console.WriteLine("[Error] Loading .mcf File: '" + pFilePath + "' failed with message: " + e.Message);
            }

            // store filepath
            _filePath = pFilePath;

            // get the root xml node 'fotobook'
            _fotobook = _xmlDoc.SelectSingleNode("fotobook");
            Debug.Assert(_fotobook != null, "[Error] Parsing '" + pFilePath + "' failed. <fotobook> tag not found.");

            // initialize page list
            _pages = new List<XmlNode>();

            // handle all relevant nodes in <fotobook>
            foreach (XmlNode node in _fotobook.ChildNodes) {

                switch (node.Name) {
                    case "page":
                        _pages.Add(node);
                        break;

                    case "statistics":
                        // TODO properly report to user
                        //Console.WriteLine("\nStatistics:\n\telapsed Time: " + node.Attributes.GetNamedItem("elapsedTimeNet").Value);
                        break;

                    default:
                        // these are not needed
                        Console.WriteLine("[Warning] Unhandled Node in <fotobook> '" + node.Name + "'.");
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

                            Area newArea;

                            switch (type) {

                                case "imagearea":
                                    // imagearea? image subnode exists!
                                    XmlNode image = node.SelectSingleNode("image");

                                    // the image file name stored in .mcf file (in format: "safecontainer:/imageName.jpg)
                                    string filename = getAttributeStr(image, "filename");

                                    // construct path to the Images folder next to .mcf file
                                    string path = _filePath.Substring(0, _filePath.Length - 4) + "_mcf-Dateien\\";

                                    // replace 'safecontainer:/' with actual path, in case filename does not exist, 
                                    // store "NULL", will render as magenta outline and print error.
                                    string filePath = filename != "" ? filename.Replace("safecontainer:/", path) : "NULL";

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

                                case "textarea": {
                                        // in <textarea> these exist:
                                        XmlNode text = node.SelectSingleNode("text");
                                        XmlNode textFormat = text.SelectSingleNode("textFormat");

                                        // NOTE: <font> stores several comma-separated values: Fontname,Fonstsize,...and more. Currently only handles these two
                                        string[] fontInfo = getAttributeStr(textFormat, "font").Split(",");

                                        // get the fontsize, take pdf scale into account and adjust to photobook settings
                                        int fontSize = (int)(Convert.ToInt32(fontInfo[1]) * SCALE * 3.26f); // somewhat matches the result in photobook

                                        // text color
                                        string color = getAttributeStr(textFormat, "foregroundColor");

                                        // by default align left
                                        string alignLabel = "ALIGNLEFT";

                                        // NOTE: <align> sometimes holds two comma-separated values (Horizontal and Vertical alignment)
                                        // for now only handles second (horizontal).
                                        string[] align = getAttributeStr(textFormat, "Alignment").Split(",");
                                        alignLabel = align.Last();

                                        // construct new area
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
                                    // there are more areatypes, for now just create an empty area that wont draw anything
                                    // and inform user.
                                    newArea = new Area();
                                    Console.WriteLine("[Warning] Unhandled area type in <page/area> '" + type + "'.");
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
                                Height = getAttributeF(position, "height") };
                            newArea.rotation = getAttributeF(position, "rotation");

                            // store new page in list
                            page.areas.Add(newArea);
                            break;

                        default:
                            // inform user about unhandled node
                            Console.WriteLine("[Warning] Unhandled Node in <page> '" + node.Name + "'.");
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
                } 
                else if (page.type == Page.Type.Spine) {
                    // check if next page is Fullcover (should be anyway)
                    XmlNode nextPage = _pages[_pageIterator + 1];
                    Page.Type nextType = Page.convert(nextPage.Attributes.GetNamedItem("type").Value);
                    if (nextType == Page.Type.Fullcover) xmlPage = nextPage;
                    else xmlPage = null;
                } 
                else if (page.type == Page.Type.Emptypage) {
                    // check if next page exists... otherwise end of book.
                    if (_pageIterator + 1 < _pages.Count) {
                        XmlNode nextPage = _pages[_pageIterator + 1];
                        xmlPage = nextPage;
                        isDouble = true;
                    } else {
                        xmlPage = null;
                    }
                } 
                else if (page.type == Page.Type.Normalpage && !isDouble) {
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

        string extractTextFromHTML(string html) {

            // the text to return
            string res = "";

            // text is stored in html inside the .mcf
            // html basically is xml so... parse it as xml
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(html);

            // get the node that contains span objects for each line of text
            XmlNode node = doc.SelectSingleNode("html/body/table/tr/td");

            // extract text from each <span> and store in single string with newline character
            foreach (XmlNode p in node.ChildNodes) {
                XmlNode span = p.SelectSingleNode("span");
                res += span?.InnerText + "\n"; // if span exists... add text + newline
            }

            // return all lines
            return res;
        }

        float getAttributeF(XmlNode node, string name, float or = 0.0f) {
            if (node == null) return or; // return default if node is null
            XmlNode attr = node.Attributes.GetNamedItem(name);
            if (attr == null) return or; // return default if attribute does not exist
            string value = attr.Value;
            if (value == "") return or; // return default if attribute was empty
            // convert attribute string to float, including pdf scale
            return (float)Convert.ToDouble(value.Replace(".", ",")) * SCALE;
        }

        string getAttributeStr(XmlNode node, string name, string or = "") {
            if (node == null) return or; // return default if node is null
            XmlNode attr = node.Attributes.GetNamedItem(name);
            if (attr == null) return or; // return default if attribute does not exist
            return attr.Value; // return string directly
        }

        public int pageCount() => _pages.Count; // return page count
    }
}
