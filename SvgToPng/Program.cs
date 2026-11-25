// Copyright (c) 2026 Robert Brendel. SPDX-License-Identifier: MS-PL
// Standalone SVG -> PNG command, usable independently of CEWE/PDF.
using System;
using System.IO;
using System.Xml;
if(args.Length!=4) { Console.Error.WriteLine("SvgToPng input.svg output.png width height\nMS-PL; SVG.NET and ExCSS notices: ../licenses"); return 2; }
try {
 int w=int.Parse(args[2]),h=int.Parse(args[3]);if(w<1||h<1||w>6000||h>6000)throw new ArgumentOutOfRangeException("size");
 using var reader=XmlReader.Create(args[0],new XmlReaderSettings{DtdProcessing=DtdProcessing.Ignore,XmlResolver=null});
 var xml=new XmlDocument{XmlResolver=null};xml.Load(reader);
 using var input=new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml.OuterXml));
 var svg=Svg.SvgDocument.Open<Svg.SvgDocument>(input);using var bitmap=svg.Draw(w,h);bitmap.Save(args[1],System.Drawing.Imaging.ImageFormat.Png);return 0;
} catch(Exception e) {Console.Error.WriteLine(e.Message);return 1;}
