using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cewe2pdf
{
    class CmdArgParser {
        public static bool parse(string[] args, out List<string> options)
        {
            options = new List<string>();

            // handle arguments
            for (int i = 0; i < args.Length; i++)
            {

                if (args[i] == "-h" || args[i] == "-help") {
                    string optList = "";
                    int padLen = 32;
                    foreach (KeyValuePair<string, string> opt in Config.optionList) {
                        string cmd = "-" + opt.Key; // TODO: pad for nicer formating
                        cmd = cmd.PadLeft(padLen);

                        optList += "\t" + cmd + " | " + opt.Value + "\n";
                    }
                    Console.WriteLine(
                      "  Cewe2pdf usage: "
                      + "'Cewe2pdf.exe \"path/to/input.mcf\" \"optional/path/to/result.pdf\" -to_page=0'\n"
                      + "\n\t" + "argument".PadLeft(padLen) + " | description\n"
                      + "\n\t" + "\"in.mcf\"".PadLeft(padLen) + " | input file to convert (required)"
                      + "\n\t" + "\"out.pdf\"".PadLeft(padLen) + " | output file to generate (optional)"
                      + "\n\n"
                      + optList
                      + "\n\t" + "-help || -h".PadLeft(padLen) + " | lists all arguments"
                      + "\n\t" + "-version || -v".PadLeft(padLen) + " | prints program version"
                      + "\n\t" + "-verbose".PadLeft(padLen) + " | enables info level console output (default for Debug builds"
                      + "\n\t" + "-silent".PadLeft(padLen) + " | enables error level console output (default for Release builds)"
                    );
                    return false;
                } else if (args[i].EndsWith(".mcf")) Program.mcfPath = args[i];
                else if (args[i].EndsWith(".pdf")) Program.pdfPath = args[i];
                else if (args[i] == "-verbose") Log.level = Log.Level.Info;
                else if (args[i] == "-silent") Log.level = Log.Level.Error;
                else if (args[i] == "-v" || args[i] == "-version") {
                    // printed by default, dont run software
                    return false;
                } else {
                    if (args[i].StartsWith("-")) {
                        // check if arg is available in options
                        foreach (string opt in Config.optionList.Keys) {
                            string argop = args[i].Substring(1); // remove leading "-"
                            if (argop.StartsWith(opt.Split("=").First())) {
                                options.Add(argop);
                                break;
                            }
                        }
                    } else {
                        Log.Warning("invalid argument: '" + args[i] + "'.");
                    }
                }
            }

            return true;
        }
    }
}
