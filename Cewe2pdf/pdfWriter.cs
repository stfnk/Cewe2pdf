using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Linq;

namespace Cewe2pdf {
    class pdfWriter {

        System.IO.FileStream _fileStream;

        private Document _doc = new Document();

        private PdfWriter _writer;

        public pdfWriter(string pOutPath) {

            _fileStream = new System.IO.FileStream(pOutPath, System.IO.FileMode.Create);

            _writer = PdfWriter.GetInstance(_doc, _fileStream);

            _doc.AddAuthor("Foo");
            _doc.AddCreator("Cewe2Pdf");
            _doc.AddTitle("Cewe2PdfTest");

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            FontFactory.RegisterDirectory("C:\\Windows\\Fonts");

            _doc.Open();
        }

        public void writePage(Page pPage) {

            _doc.SetPageSize(new Rectangle(0f, 0f, pPage.bundleSize.X, pPage.bundleSize.Y));

            if (!_doc.IsOpen()) _doc.Open();
            else _doc.NewPage();

            // fill background
            PdfContentByte canvas = _writer.DirectContent;

            // draw left
            canvas.Rectangle(0, 0, pPage.bundleSize.X / 2, pPage.bundleSize.Y);
            try {
                canvas.SetColorFill(DesignIdDatabase.backgroundColors[pPage.backgroundLeft != null ? pPage.backgroundLeft : pPage.backgroundRight]);
            } catch (Exception e) {
                canvas.SetColorFill(BaseColor.MAGENTA);
                Console.WriteLine("Missing background: " + pPage.backgroundLeft);
            }
            canvas.Fill();

            // draw right
            canvas.Rectangle(0 + pPage.bundleSize.X / 2, 0, pPage.bundleSize.X / 2, pPage.bundleSize.Y);
            try {
                canvas.SetColorFill(DesignIdDatabase.backgroundColors[(pPage.backgroundRight != null ? pPage.backgroundRight : pPage.backgroundLeft)]);
            } catch (Exception e) {
                canvas.SetColorFill(BaseColor.MAGENTA);
                Console.WriteLine("Missing background: " + pPage.backgroundRight);
            }
            canvas.Fill();

            // draw content
            foreach (Area area in pPage.areas) {
                if (area is ImageArea) {
                    ImageArea imgArea = (ImageArea)area;
                    if (imgArea.path == "NULL") {

                        float pX = imgArea.rect.X;
                        float pY = pPage.bundleSize.Y - imgArea.rect.Y - imgArea.rect.Height;

                        Rectangle empty = new Rectangle(pX, pY, pX + imgArea.rect.Width, pY + imgArea.rect.Height);
                        empty.Border = 1 | 2 | 4 | 8;
                        empty.BorderColor = BaseColor.LIGHT_GRAY;
                        empty.BorderWidth = 4.0f;
                        _writer.DirectContent.Rectangle(empty);

                        continue;
                    }

                    System.Drawing.Image sysImg = System.Drawing.Image.FromFile(imgArea.path);
                    Console.WriteLine("now rotate");
                    System.Drawing.RotateFlipType rot = ExifRotate(sysImg);

                    // calculate somewhat good scaling
                    float scale = sysImg.Width / (float)imgArea.rect.Width * 0.5f;
                    scale = scale < 1.0 ? 1.0f : scale;
                    Console.WriteLine("calced scale: " + scale);

                    sysImg = (System.Drawing.Image)(new System.Drawing.Bitmap(sysImg, new System.Drawing.Size((int)(sysImg.Width / scale), (int)(sysImg.Height / scale))));

                    Image img;
                    try {
                        img = Image.GetInstance(sysImg, sysImg.RawFormat);
                    } catch (Exception e) {
                        sysImg.Save("temp.jpg");
                        Console.WriteLine("cheat... save to file: temp.jpg");
                        img = Image.GetInstance("temp.jpg");
                    }

                    img.ScalePercent(imgArea.scale * 100.0f * scale);

                    float posX = imgArea.rect.X + imgArea.cutout.X;
                    float posY = pPage.bundleSize.Y - imgArea.rect.Y - imgArea.rect.Height;

                    float cropBottom = img.ScaledHeight - imgArea.rect.Height + imgArea.cutout.Y;

                    //Console.WriteLine("crop: " + imgArea.path + " is x:" + imgArea.cutout.X + " is y:" + imgArea.cutout.Y);
                    Image cropped = cropImage(img, _writer, -imgArea.cutout.X, cropBottom, imgArea.rect.Width, imgArea.rect.Height);

                    cropped.SetAbsolutePosition(imgArea.rect.X, posY);

                    _writer.DirectContent.AddImage(cropped);

                    // draw image border
                    if (imgArea.border) {
                        float pX = imgArea.rect.X;
                        float pY = pPage.bundleSize.Y - imgArea.rect.Y - imgArea.rect.Height;

                        Rectangle rect = new Rectangle(pX, pY, pX + imgArea.rect.Width, pY + imgArea.rect.Height);

                        int argb = Int32.Parse(imgArea.borderColor.Replace("#", ""), System.Globalization.NumberStyles.HexNumber);
                        System.Drawing.Color clr = System.Drawing.Color.FromArgb(argb);

                        rect.Border = 1 | 2 | 4 | 8;
                        rect.BorderColor = new BaseColor(clr);
                        rect.BorderWidth = imgArea.borderWidth;

                        _writer.DirectContent.Rectangle(rect);
                    }

                } else if (area is TextArea) {
                    TextArea textArea = (TextArea)area;
                    if (textArea.text == "") continue;

                    ColumnText colText = new ColumnText(_writer.DirectContent);

                    float llx = textArea.rect.X;
                    float lly = pPage.bundleSize.Y - textArea.rect.Y - textArea.rect.Height;

                    float urx = llx + textArea.rect.Width;
                    float ury = lly + textArea.rect.Height;

                    int argb = Int32.Parse(textArea.color.Replace("#", ""), System.Globalization.NumberStyles.HexNumber);
                    System.Drawing.Color clr = System.Drawing.Color.FromArgb(argb);

                    Font font = FontFactory.GetFont(textArea.font, textArea.fontsize, new BaseColor(clr));

                    Rectangle textRect = new Rectangle(llx, lly, urx, ury);

#if DEBUG_DRAW
                    textRect.Border = 1|2|4|8;
                    textRect.BorderColor = BaseColor.RED;
                    textRect.BorderWidth = 1.0f;
                    _writer.DirectContent.Rectangle(textRect);
#endif
                    colText.SetSimpleColumn(textRect);

                    Paragraph par = new Paragraph(textArea.text, font);


                    par.SetLeading(0, 1.3f);
                    if (textArea.align == "ALIGNHCENTER")
                        par.Alignment = Element.ALIGN_CENTER;
                    else if (textArea.align == "ALIGNLEFT")
                        par.Alignment = Element.ALIGN_LEFT;
                    else if (textArea.align == "ALIGNRIGHT")
                        par.Alignment = Element.ALIGN_RIGHT;

                    colText.AddElement(par);
                    colText.Go();
                }
            }
        }

        public Image cropImage(Image image, PdfWriter writer, float fromLeft, float fromBottom, float width, float height) {
            PdfContentByte cb = writer.DirectContent;
            PdfTemplate t = cb.CreateTemplate(width, height);
            float origWidth = image.ScaledWidth;
            float origHeight = image.ScaledHeight;
            t.AddImage(image, origWidth, 0, 0, origHeight, -fromLeft, -fromBottom);
            return Image.GetInstance(t);
        }

        public void close() {
            _doc.Close();
            _writer.Close();
            _fileStream.Close();
            Console.WriteLine("Wrote pdf.");
        }


        private const int exifOrientationID = 0x112; //274
        private const int ExifOrientationTagId = 0x112; //274

        public System.Drawing.RotateFlipType ExifRotate(System.Drawing.Image img) {
            if (!img.PropertyIdList.Contains(exifOrientationID)) {
                Console.WriteLine("no exif info found.");
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
