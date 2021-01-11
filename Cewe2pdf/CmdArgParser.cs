using System;
using System.Collections.Generic;
using System.Text;

namespace Cewe2pdf
{
    class CmdArgParser {
        public static void parse(string[] args)
        {
            // handle arguments, overwriting config
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-h" || args[i] == "--help")
                {
                    Console.WriteLine(
                      "Cewe2pdf usage:"
                      + "\n\t    argument | description\n"
                      + "\n\t      --help | lists all arguments."
                      + "\n\t          -h | same as --help."
                      + "\n\t    'in.mcf' | input file to convert (required)"
                      + "\n\t   'out.pdf' | output file to generate (optional)"
                      + "\n\t  -to-page 0 | convert only up to this page nr, 0 converts all."
                      + "\n\t-quality 1.0 | pixel size of images. Use lower value for higher resolution images in .pdf." // TODO: true?
                    );
                }
                else if (args[i].EndsWith(".mcf")) Program.mcfPath = args[i];
                else if (args[i].EndsWith(".pdf")) Program.pdfPath = args[i];
                else if (args[i] == "-to-page")
                {
                    i++;
                    if (i < args.Length) Config.toPage = Math.Max(0, Convert.ToInt32(args[i]));
                }
                else if (args[i] == "-quality")
                {
                    i++;
                    if (i < args.Length) Config.imgScale = Math.Clamp(float.Parse(args[i], System.Globalization.CultureInfo.InvariantCulture), 0.0f, 1.0f); // TODO: true?
                }
                else
                {
                    Log.Warning("invalid argument: '" + args[i] + "'.");
                }
            }
        }
    }
}
