## Cewe2pdf

This commandline program converts **CEWE FOTOWELT** `.mcf` Photobook project files to high quality `.pdf` documents.
It's still a very early version with lots of missing features, see the [Known Issues](#known-issues) section below.

If you encounter a bug or the generated pdf contains errors please [report an Issue](https://github.com/stfnk/Cewe2pdf/issues)
Describe the problem and attach the `cewe2pdf.log` file, located next to the `Cewe2pdf.exe`.
_This file may contain file- and foldernames from your computer. Open it in Notepad to review its content._

## Installation
Head over to the [release](https://github.com/stfnk/Cewe2pdf/releases) section and download the latest build for your platform.</br>
**Note:** *Currently only Windows Binaries provided.*</br>

To run, this program requires the `.NET Core 3.1` runtime installed on your System. Get the appropriate version from:</br>
https://dotnet.microsoft.com/download/dotnet-core/3.1
</br>(`.NET Core Runtime 3.1.5` is probably enough, but any version should work.)

## Usage
This is a commandline only program, there is no graphical interface to interact with. [Detailed Instructions](#detailed-instructions-for-windows)

### I know what I'm doing:
Conversion with default settings:

    Cewe2pdf <pathTo.mcf> <pathTo.pdf>

List options:

    Cewe2pdf --help

### Detailed instructions for Windows
Extract all files from the downloaded `.zip` to any location on your computer, for example your Desktop. Press **WindowsKey**+**R** to open the _Run_ Dialog. Type `cmd` and hit **Enter**. A commandprompt should open up.
Navigate to the folder that contains the program, if you extracted the `.zip` to your Desktop, type:</br>

    cd Desktop\Cewe2pdf

and hit **Enter**. It should now look like this:</br>

    C:\Users\<username>\Desktop\Cewe2pdf>

now you need to get the path to your `.mcf` file. Navigate to it in Windows Explorer, Right click on the `.mcf`, click `Properties` and under the `Security` tab, copy the full file path.

Back in the commandline, type `Cewe2pdf.exe "` paste the filename with **Ctrl**+**V** and add another `"`. It should now look like this:
    
    Cewe2pdf.exe "C:\Users\username\Documents\MyPhotobook.mcf"

add one Space and type `MyPhotobookConverted.pdf`

The full command should look similar to this:

    Cewe2pdf.exe "C:\Users\username\Documents\MyPhotobook.mcf" MyPhotobookConverted.pdf

Now hit **Enter** and Conversion should start. This may take several minutes.
Once finished, the `.pdf` is located right next to the `Cewe2pdf.exe`, in this case on your Desktop in the Cewe2pdf folder, named `MyPhotobookConverted.pdf`


### Known Issues
_Currently only the following features are supported!_
* Images
* Text Boxes
* Image Borders
* Single colored Backgrounds

This works for my usecase, please report missing elements [here](https://github.com/stfnk/Cewe2pdf/issues).</br>
Please also attach the `cewe2pdf.log` file, which is located next to the `Cewe2pdf.exe` _This file may contain file- and foldernames from your computer. Open it in Notepad to review its content._


## Development

This program is written in C# for the .NET Core 3.1 runtime and should build on any platform. It uses [iTextSharp 5](https://github.com/itext/itextsharp/) for pdf rendering, and `System.Drawing.Commons` for image loading.</br>
`.mcf` files are just plain XML files, that are parsed using the C# native `System.XML` API.</br>
Photobook backgrounds are stored as `.webp` images in the CEWE installation directory. For now i use [this](https://github.com/stfnk/Cewe2pdf/blob/master/util/cewe2data.py) python script to autogenerate a C# Dictionary with background colors, which requires the `Pillow (5.2.0)` Library:

    python -m pip install Pillow


## License
The Code in this repository is licensed under the MIT License, but note that `iTextSharp` uses the AGPL License.

    MIT License

    Copyright (C) 2020 Stefan Kreller

    Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
