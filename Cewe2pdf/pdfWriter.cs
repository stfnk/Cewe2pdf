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

            // necessary for loading .ttf it seams
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            // TODO probably should add Cewe/Resources/fonts folder here as well, 
            // but currently doesn't require Cewe installation location for anything else
            FontFactory.RegisterDirectory("C:\\Windows\\Fonts"); 

            // start writing
            _doc.Open();
        }

        public void writePage(Page pPage) {
            
            // page size is given per <fotobook/page>. iTextSharp needs it set before adding page or opening document.
            _doc.SetPageSize(new Rectangle(0f, 0f, pPage.bundleSize.X, pPage.bundleSize.Y));

            // handle first page case
            if (!_doc.IsOpen()) _doc.Open();
            else _doc.NewPage();

            // fill background
            // this currently only supports single color backgrounds, see utils/cewe2data.py & DesignIdData.cs for current implementation
            // this should be updated to use background images from cewe installation folder directly, but
            // Cewe backgrounds are .webp files which iTextSharp does not support... silly (and faster) workaround for now. 
            PdfContentByte canvas = _writer.DirectContent;

            // background color can differ between left and right page, so handle them accordingly.
            // draw left backrgound
            canvas.Rectangle(0, 0, pPage.bundleSize.X / 2, pPage.bundleSize.Y);
            try {
                canvas.SetColorFill(DesignIdDatabase.backgroundColors[pPage.backgroundLeft != null ? pPage.backgroundLeft : pPage.backgroundRight]);
            } catch (Exception e) {
                // in case a background designID is not found in DesignIdData.cs, print information to console
                canvas.SetColorFill(BaseColor.MAGENTA);
                Log.Error("Missing background DesignID: <" + pPage.backgroundLeft + "> Please report this as an issue.");
            }
            canvas.Fill();

            // TODO de-duplicate this code...
            // draw right background
            canvas.Rectangle(0 + pPage.bundleSize.X / 2, 0, pPage.bundleSize.X / 2, pPage.bundleSize.Y);
            try {
                canvas.SetColorFill(DesignIdDatabase.backgroundColors[(pPage.backgroundRight != null ? pPage.backgroundRight : pPage.backgroundLeft)]);
            } catch (Exception e) {
                // in case a background designID is not found in DesignIdData.cs, print information to console
                canvas.SetColorFill(BaseColor.MAGENTA);
                Log.Error("Missing background DesignID: <" + pPage.backgroundLeft + "> Please report this as an issue.");
            }
            canvas.Fill();

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

                        Log.Error("Image path was NULL.");
                        continue;
                    }

                    // load image file
                    System.Drawing.Image sysImg = System.Drawing.Image.FromFile(imgArea.path);

                    // fix exif orientation
                    ExifRotate(sysImg);

                    // calculate somewhat good resizing TODO this should be improved to ensure consistent dpi for all images.
                    float scale = sysImg.Width / (float)imgArea.rect.Width * 0.5f; // magic number literally defines resolution, the smaller, the higher pixel resolution in final pdf.
                    scale = scale < 1.0 ? 1.0f : scale; // never scale image up

                    // resize image
                    sysImg = (System.Drawing.Image)(new System.Drawing.Bitmap(sysImg, new System.Drawing.Size((int)(sysImg.Width / scale), (int)(sysImg.Height / scale))));

                    // this is really silly and slooooow but works for now.
                    // write System.Drawing.Image to disk and re-read as iTextSharp.Image...
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
                    
                    // convert .mcf's html style color hex code to Color, based on: https://stackoverflow.com/a/2109904
                    int argb = Int32.Parse(textArea.color.Replace("#", ""), System.Globalization.NumberStyles.HexNumber);
                    System.Drawing.Color clr = System.Drawing.Color.FromArgb(argb);

                    // load the correct font
                    // NOTE: this only works if font is registered from Fonts directory. See constructor.
                    Font font = FontFactory.GetFont(textArea.font, textArea.fontsize, new BaseColor(clr));

                    // For testing draw text box outline
                    // textRect.Border = 1|2|4|8;
                    // textRect.BorderColor = BaseColor.RED;
                    // textRect.BorderWidth = 1.0f;
                    // _writer.DirectContent.Rectangle(textRect);

                    // apply rect to textbox
                    colText.SetSimpleColumn(textRect);

                    // the actual text object
                    Paragraph par = new Paragraph(textArea.text, font);

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

                    // add paragraph to textbox
                    colText.AddElement(par);

                    // draw textbox
                    colText.Go();
                }
            }
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
    }
}
