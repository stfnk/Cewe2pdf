using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Linq;

namespace Cewe2pdf {

    class pdfWriter {

        private System.IO.FileStream _fileStream;
        private Document _doc = new Document();
        private PdfWriter _writer;

        public pdfWriter(string pOutPath) {
            // TODO add more exception checking...

            // Open file stream for exported pdf
            _fileStream = new System.IO.FileStream(pOutPath, System.IO.FileMode.Create);

            // initialize iTextSharp pdf writer
            _writer = PdfWriter.GetInstance(_doc, _fileStream);

            // just put something in there, doesn't really matter...
            _doc.AddAuthor("Cewe2pdf.exe");
            _doc.AddCreator("Cewe2Pdf");
            _doc.AddTitle("ConvertedCewePhotobook");

            // TODO: move font loading to Config class?
            // necessary for loading .ttf it seams
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            const string fontPath = "C:\\Windows\\Fonts"; // FIXME: windows only obviously
            Log.Info("Loading fonts from " + fontPath);
            FontFactory.RegisterDirectory(fontPath);

            string cwfontPath = Config.programPath + "\\Resources\\photofun\\fonts";
            Log.Info("Loading fonts from " + cwfontPath);
            FontFactory.RegisterDirectory(cwfontPath);

            Log.Info("Found " + FontFactory.RegisteredFonts.Count + " fonts.");

            // start writing
            _doc.Open();
        }

        public void writePage(Page pPage) {

            // page size is given per <fotobook/page>. iTextSharp needs it set before adding page or opening document.
            _doc.SetPageSize(new Rectangle(0f, 0f, pPage.bundleSize.X, pPage.bundleSize.Y));

            // handle first page case
            if (!_doc.IsOpen()) _doc.Open();
            else _doc.NewPage();

            // TOOD: de-duplicate
            // draw left part of background
            if (pPage.backgroundLeft != null) {

                PdfContentByte canvas = _writer.DirectContent;

                canvas.Rectangle(0, 0, pPage.bundleSize.X / 2, pPage.bundleSize.Y);
                canvas.SetColorFill(BaseColor.CYAN);
                canvas.Fill();

                string id = pPage.backgroundLeft;
                System.Drawing.Bitmap bmp = DesignIdConverter.getBitmapFromID(id);
                if (bmp == null)
                {
                    Log.Error("Background image for id '" + id + "' was null.");
                    canvas.SetColorFill(BaseColor.MAGENTA);
                    canvas.Fill();
                }
                else
                {
                    Image img = sysImageToITextImage(bmp);

                    float facY = pPage.bundleSize.Y / img.PlainHeight;
                    float facX = pPage.bundleSize.X / img.PlainWidth;
                    float fac = Math.Max(facX, facY);

                    img.ScalePercent(fac * 100f);

                    float yoffset = (img.ScaledHeight - pPage.bundleSize.Y) * -0.5f;
                    float xoffset = (img.ScaledWidth - pPage.bundleSize.X) * -0.5f;

                    img.SetAbsolutePosition(xoffset, yoffset);

                    Image imgCropped = cropImage(img, _writer, 0, 0, -xoffset + pPage.bundleSize.X / 2, img.ScaledHeight);

                    imgCropped.SetAbsolutePosition(xoffset, yoffset);
                    _writer.DirectContent.AddImage(imgCropped);
                }
            }

            // draw right background
            if (pPage.backgroundRight != null) {

                PdfContentByte canvas = _writer.DirectContent;

                canvas.Rectangle(pPage.bundleSize.X / 2, 0, pPage.bundleSize.X / 2, pPage.bundleSize.Y);
                canvas.SetColorFill(BaseColor.CYAN);
                canvas.Fill();

                string id = pPage.backgroundRight;
                System.Drawing.Bitmap bmp = DesignIdConverter.getBitmapFromID(id);
                if (bmp == null) {
                    Log.Error("Background image for id '" + id + "' was null.");
                    canvas.SetColorFill(BaseColor.MAGENTA);
                    canvas.Fill();
                } else {
                    Image img = sysImageToITextImage(bmp);

                    float facY = pPage.bundleSize.Y / img.PlainHeight;
                    float facX = pPage.bundleSize.X / img.PlainWidth;
                    float fac = Math.Max(facX, facY);

                    img.ScalePercent(fac * 100f);

                    float yoffset = (img.ScaledHeight - pPage.bundleSize.Y) * -0.5f;
                    float xoffset = (img.ScaledWidth - pPage.bundleSize.X) * -0.5f;

                    img.SetAbsolutePosition(xoffset, yoffset);

                    Image imgCropped = cropImage(img, _writer, pPage.bundleSize.X / 2f, 0, img.ScaledWidth, img.ScaledHeight);

                    imgCropped.SetAbsolutePosition(xoffset + pPage.bundleSize.X / 2, yoffset);
                    _writer.DirectContent.AddImage(imgCropped);
                }
            }

            // draw all content areas stored in this page
            // currently only supports <imagearea> and <textarea> from .mcf
            foreach (Area area in pPage.areas) {

                if (area is ImageArea) {
                    ImageArea imgArea = (ImageArea)area;

                    // if image path was not valid draw magenta outline and print error
                    if (imgArea.path == "NULL") {

                        // calculate rect dimensions
                        float pX = imgArea.rect.X;
                        float pY = pPage.bundleSize.Y - imgArea.rect.Y - imgArea.rect.Height;
                        Rectangle nullRect = new Rectangle(pX, pY, pX + imgArea.rect.Width, pY + imgArea.rect.Height);

                        // configure border
                        nullRect.Border = 1 | 2 | 4 | 8;
                        nullRect.BorderColor = BaseColor.MAGENTA;
                        nullRect.BorderWidth = 4.0f;

                        // draw to document
                        _writer.DirectContent.Rectangle(nullRect);

                        Log.Error("Image path was null. Probably caused by an empty image area.");
                        continue;
                    }

                    Log.Info("Rendering Image: '" + imgArea.path + "'.");

                    // load image file.
                    System.Drawing.Image sysImg;
                    try {
                        sysImg = System.Drawing.Image.FromFile(imgArea.path);
                    } catch (System.IO.FileNotFoundException e) {
                        Log.Error("Loading image failed. Image at '" + imgArea.path + "' not found.");
                        continue;
                    }

                    // fix exif orientation
                    ExifRotate(sysImg);

                    // calculate resizing factor, results in equal pixel density for all images.
                    float scale = 1f / imgArea.scale * Config.imgScale; // the higher this value, the lower pixel density is. 0.0f = original resolution
                    scale = scale < 1.0f ? 1.0f : scale; // never scale image up

                    // resize image
                    sysImg = (System.Drawing.Image)(new System.Drawing.Bitmap(sysImg, new System.Drawing.Size((int)(sysImg.Width / scale), (int)(sysImg.Height / scale))));

                    // this is really silly and slooooow but works for now.
                    // write System.Drawing.Image to disk and re-read as iTextSharp.Image...
                    // TODO at least only write to memory... didnt get that to work yet.
                    sysImg.Save("temp.jpg");
                    Image img = Image.GetInstance("temp.jpg");

                    // apply scale as defined in .mcf
                    img.ScalePercent(imgArea.scale * 100.0f * scale);

                    // calculate image position in pdf page
                    float posX = imgArea.rect.X + imgArea.cutout.X;
                    float posY = pPage.bundleSize.Y - imgArea.rect.Y - imgArea.rect.Height; // pdf origin is in lower left, mcf origin is in upper left

                    // yaaaaa... whatever. This way everything fits
                    float cropBottom = img.ScaledHeight - imgArea.rect.Height + imgArea.cutout.Y;

                    // crop image to mcf specified rect
                    Image cropped = cropImage(img, _writer, -imgArea.cutout.X, cropBottom, imgArea.rect.Width, imgArea.rect.Height);

                    // move to mcf specified position
                    cropped.SetAbsolutePosition(imgArea.rect.X, posY);

                    // draw the image
                    _writer.DirectContent.AddImage(cropped);

                    // draw image border if specified in .mcf
                    if (imgArea.border) {
                        // TODO mcf as an outside property that is currently not taken into account.
                        // seems like all borders are 'outside' in photobook.
                        // iTextSharp draws Borders centered (BorderWidth/2 pixels overlap image)
                        // this should be corrected.

                        // calc border rect
                        float pX = imgArea.rect.X;
                        float pY = pPage.bundleSize.Y - imgArea.rect.Y - imgArea.rect.Height;
                        Rectangle rect = new Rectangle(pX, pY, pX + imgArea.rect.Width, pY + imgArea.rect.Height);

                        // convert .mcf's html style color hex code to Color, based on: https://stackoverflow.com/a/2109904
                        int argb = Int32.Parse(imgArea.borderColor.Replace("#", ""), System.Globalization.NumberStyles.HexNumber);
                        System.Drawing.Color clr = System.Drawing.Color.FromArgb(argb);

                        // configure border
                        rect.Border = 1 | 2 | 4 | 8;
                        rect.BorderColor = new BaseColor(clr);
                        rect.BorderWidth = imgArea.borderWidth;

                        // draw border
                        _writer.DirectContent.Rectangle(rect);
                    }

                }
                else if (area is TextArea) {
                    TextArea textArea = (TextArea)area;

                    // just in case something went wrong
                    if (textArea.text == "") {
                        Log.Error("Text was empty.");
                        continue;
                    }

                    // iTextSharp textbox
                    ColumnText colText = new ColumnText(_writer.DirectContent);

                    // calculate rect
                    float llx = textArea.rect.X;
                    float lly = pPage.bundleSize.Y - textArea.rect.Y - textArea.rect.Height;
                    float urx = llx + textArea.rect.Width;
                    float ury = lly + textArea.rect.Height;
                    Rectangle textRect = new Rectangle(llx, lly, urx, ury);

                    // apply rect to textbox
                    colText.SetSimpleColumn(textRect);

                    // The actual text object
                    Paragraph par = new Paragraph();

                    // magic number that closely matches photobook
                    // TODO there is probably more information in the .mcf's css part
                    par.SetLeading(0, 1.3f);

                    // apply corrent alignment
                    if (textArea.align == "ALIGNHCENTER")
                        par.Alignment = Element.ALIGN_CENTER;
                    else if (textArea.align == "ALIGNLEFT")
                        par.Alignment = Element.ALIGN_LEFT;
                    else if (textArea.align == "ALIGNRIGHT")
                        par.Alignment = Element.ALIGN_RIGHT;

                    // add text chunks
                    foreach (TextElement elem in textArea.textElements) {
                        int style = 0;
                        style += elem.bold ? Font.BOLD : 0;
                        style += elem.italic ? Font.ITALIC : 0;
                        style += elem.underlined ? Font.UNDERLINE : 0;
                        Font fnt = FontFactory.GetFont(elem.family, elem.size, style, argb2BaseColor(elem.color));

                        par.Add(new Chunk(elem.text + (elem.newline ? "\n" : " "), fnt));
                    }

                    // add paragraph to textbox
                    colText.AddElement(par);

                    // draw textbox
                    colText.Go();

                }
            }

            // draw pagenumbers
            // TODO remove magic numbers, at least comment
            const float PAGE_NR_Y_OFFSET = -4.0f;
            const float PAGE_NR_X_OFFSET = 0.0f;
            float PAGE_NR_FONT_SIZE = Page.pageNoFontSize * 1.1f;
            float PAGE_NR_HEIGHT = PAGE_NR_FONT_SIZE + 12.0f; // add some extra space... this is needed.
            float PAGE_Y_POS = Page.pageNoMargin.Y + PAGE_NR_Y_OFFSET;

            // TODO de-duplicate all these conversions and move to helper method
            // convert .mcf's html style color hex code to Color, based on: https://stackoverflow.com/a/2109904
            int argb_ = Int32.Parse(Page.pageNoColor.Replace("#", ""), System.Globalization.NumberStyles.HexNumber);
            System.Drawing.Color clr_ = System.Drawing.Color.FromArgb(argb_);

            // left
            Paragraph pageNoLeft = new Paragraph(pPage.pageNoLeft, FontFactory.GetFont(Page.pageNoFont, PAGE_NR_FONT_SIZE, new BaseColor(clr_)));
            pageNoLeft.Alignment = Element.ALIGN_LEFT + Element.ALIGN_BOTTOM;

            ColumnText leftNo = new ColumnText(_writer.DirectContent);
            Rectangle leftNoRect = new Rectangle(Page.pageNoMargin.X + PAGE_NR_X_OFFSET, PAGE_Y_POS, 500, PAGE_Y_POS+PAGE_NR_HEIGHT);
            leftNo.SetSimpleColumn(leftNoRect);

            leftNo.AddElement(pageNoLeft);
            leftNo.Go();

            //leftNoRect.Border = 1 | 2 | 4 | 8;
            //leftNoRect.BorderColor = BaseColor.GREEN;
            //leftNoRect.BorderWidth = 1.0f;
            //_writer.DirectContent.Rectangle(leftNoRect);

            // right
            Paragraph pageNoRight = new Paragraph(pPage.pageNoRight, FontFactory.GetFont(Page.pageNoFont, PAGE_NR_FONT_SIZE, new BaseColor(clr_)));
            pageNoRight.Alignment = Element.ALIGN_RIGHT;

            ColumnText rightNo = new ColumnText(_writer.DirectContent);
            Rectangle rightNoRect = new Rectangle(pPage.bundleSize.X-Page.pageNoMargin.X - PAGE_NR_X_OFFSET - 500, PAGE_Y_POS, pPage.bundleSize.X-Page.pageNoMargin.X - PAGE_NR_X_OFFSET, PAGE_Y_POS + PAGE_NR_HEIGHT);
            rightNo.SetSimpleColumn(rightNoRect);

            rightNo.AddElement(pageNoRight);
            rightNo.Go();

            //rightNoRect.Border = 1 | 2 | 4 | 8;
            //rightNoRect.BorderColor = BaseColor.YELLOW;
            //rightNoRect.BorderWidth = 1.0f;
            //_writer.DirectContent.Rectangle(rightNoRect);

            //Console.WriteLine("Page drawn: " + pPage.type.ToString() + " left: " + pPage.pageNoLeft + "; right: " + pPage.pageNoRight + "!");

        }

        public Image cropImage(Image image, PdfWriter writer, float fromLeft, float fromBottom, float width, float height) {
            // from https://stackoverflow.com/a/14473667
            PdfContentByte cb = writer.DirectContent;
            PdfTemplate t = cb.CreateTemplate(width, height);
            float origWidth = image.ScaledWidth;
            float origHeight = image.ScaledHeight;
            t.AddImage(image, origWidth, 0, 0, origHeight, -fromLeft, -fromBottom);
            return Image.GetInstance(t);
        }

        public void close() {
            // close all files and filestreams...
            _doc.Close();
            _writer.Close();
            _fileStream.Close();
        }

        public System.Drawing.RotateFlipType ExifRotate(System.Drawing.Image img) {
            // for some reason iText does not respect orientation stored in metadata as it seams...
            // try to fix it with this weird stuff...
            // based on https://www.cyotek.com/blog/handling-the-orientation-exif-tag-in-images-using-csharp

            const int exifOrientationID = 0x112; //274

            if (!img.PropertyIdList.Contains(exifOrientationID)) {
                return System.Drawing.RotateFlipType.RotateNoneFlipNone;
            }

            var prop = img.GetPropertyItem(exifOrientationID);
            int val = BitConverter.ToUInt16(prop.Value, 0);
            var rot = System.Drawing.RotateFlipType.RotateNoneFlipNone;

            if (val == 3 || val == 4)
                rot = System.Drawing.RotateFlipType.Rotate180FlipNone;
            else if (val == 5 || val == 6)
                rot = System.Drawing.RotateFlipType.Rotate90FlipNone;
            else if (val == 7 || val == 8)
                rot = System.Drawing.RotateFlipType.Rotate270FlipNone;

            if (val == 2 || val == 4 || val == 5 || val == 7)
                rot |= System.Drawing.RotateFlipType.RotateNoneFlipX;

            if (rot != System.Drawing.RotateFlipType.RotateNoneFlipNone)
                img.RotateFlip(rot);

            return rot;
        }

        public static BaseColor argb2BaseColor(string color) {
            //// convert .mcf's html style color hex code to Color, based on: https://stackoverflow.com/a/2109904
            int argb = Int32.Parse(color.Replace("#", ""), System.Globalization.NumberStyles.HexNumber);
            System.Drawing.Color clr = System.Drawing.Color.FromArgb(argb);
            return new BaseColor(clr);
        }

        private static iTextSharp.text.Image sysImageToITextImage(System.Drawing.Image pImg) {
            // TODO: FIXME: avoid writing jpg to disk. Silly workaround for now.
            pImg.Save("temp.jpg");
            return Image.GetInstance("temp.jpg");
        }
    }
}
