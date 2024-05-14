# Ink-Localiser

**A simple tool to make it easier to localise Ink projects.**

![Tagged Ink File](docs/demo-tagged.png)

![Generated CSV File](docs/demo-csv.png)

## Overview

Inkle's Ink language is a great flow language for stitching together narrative-based games.

Because it's designed to mash small fragments of text together, it's not designed for localisation, or for associating lines of spoken audio to the source file.

But many studios don't use the more advanced text-manipulation features of Ink - they just use it for creating a flow of complete lines of text. It's a great solution for titles that care about branching dialogue. This means there's a problem - how do you translate each line? And how to you play the right audio for each line?

This tool takes a set of raw ink files, scans them for lines of text, and generates a localisation ID to associate with each line. It writes the ink files back out again with these IDs in the form of Ink tags at the end of each line.

This means that every line of meaninful text in the Ink file now has a unique ID attached, as a tag. That means you can use that ID for localisation or for triggering the correct audio.

The tool also optionally exports CSV or JSON files containing all the IDs and their associated text - which can then be used as a basis for localisation.

Each time the tool is run, it preserves the old IDs, just adding them to any newly appeared lines.

So for example, take this source file:
![Source Ink File](docs/demo-plain.png)

After the tool is run, the source file is rewritten like this:
![Tagged Ink File](docs/demo-tagged.png)

It also creates an optional CSV file like so:
![Generated CSV File](docs/demo-csv.png)

And an optional JSON file like so:
![Generated JSON File](docs/demo-json.png)

## Command-Line Tool
This is a command-line utility with a few arguments. A few simple examples:

Look for every Ink file in the `inkFiles` folder, process them for IDs, and output the data in the file `output/strings.json`:

`LocaliserTool.exe --folder=inkFiles/ --json=output/strings.json`

Look for every Ink file starting with `start` in the `inkFiles` folder, process them for IDs, and output the data in the file `output/strings.csv`:

`LocaliserTool.exe --folder=inkFiles/ --filePattern=start*.ink --csv=output/strings.csv`

### Arguments
* `--folder=<folder>`
    
    Root folder to scan for Ink files to localise relative to working dir. 
    e.g. `--folder=inkFiles/` 
    Default is the current working dir.

* `--filePattern=<folder>`

    Root folder to scan for Ink files to localise.
    e.g. `--filePattern=start-*.ink`
    Default is `*.ink`

* `--csv=<csvFile>`

    Path to a CSV file to export, relative to working dir.
    e.g. `--csv=output/strings.csv`
    Default is empty, so no CSV file will be exported.

* `--json=<jsonFile>`

    Path to a JSON file to export, relative to working dir.
    e.g. `--json=output/strings.json`
    Default is empty, so no JSON file will be exported.

* `--retag`

    Regenerate all localisation tag IDs, rather than keep old IDs.

* `--help`

    This help!

## Use in Development
Develop your Ink as normal! Treat that as the 'master copy' of your game, the source of truth for the flow and your primary language content.

Use LocaliserTool to add IDs to your Ink file and to extract a file of the content. Get that file localised/translated as you need for your title. Remember that you can re-run LocaliserTool every time you alter your Ink files and everything will be updated.

At runtime, load your Ink content, and also load the appropriate JSON or CSV (which should depend on your localisation). 

Use your Ink flow as normal, but when you progress the story instead of asking Ink for the text content at the current line or option, ask for the list of tags! 

Look for any tag starting with #id:, parse the ID from that tag yourself, and ask your CSV or JSON file for the actual string.

In other words - during runtime, just use Ink for logic, not for content. Grab the tags from Ink, and use your external text file (or WAV filenames!) as appropriate for the relevant language.

## The ID format

The IDs are constructed like this:

`<filename>_<knot>(_<stitch>)_<code>`

* `filename`: The root name of the Ink file this string is in.
* `knot`: The name of the containing knot this string is in.
* `stitch`: If this is inside a stitch, the name of that stitch
* `code`: A four-character random code which will be unique to this knot or knot/stitch combination.

This is mainly to make it easy during development to figure out where a line originated in the Ink files - it's fairly arbitrary, so IDs can be moved around safely without changing (even if the lookup will then be unhelpful). You can always delete an ID and let it regenerate if you want something more appropriate to the place where you've moved a line.

## Releases
You can find releases for various platforms [here](https://github.com/wildwinter/Ink-Localiser/releases
).

There's also a Lib version if you want to be able to access it via the DLL as part of your toolchain. The DLL depends on Inkle's `ink_compiler.dll` and `ink-engine-runtime.dll`.

## Caveats
This isn't very complicated or sophisticated, so your mileage may vary!

**WARNING**: This rewrites your `.ink` files! And it might break, you never know! It's always good practice to use version control in case a process eats your content, and this is another reason why!

**Inky might not notice**: If for some reason you run this tool while Inky is open, Inky will probably not reload the rebuilt `.ink` file. Use Ctrl-R or CMD-R to reload the file Inky is working on.

## Under the Hood
Developed in .NET / C#.

The tool internally uses Inkle's **Ink Parser** to chunk up the ink file into useful tokens, then sifts through that for textual content. Be warned that this isn't tested in huge numbers of situations - if you spot any weirdness, let me sknow!

## Acknowledgements
Obviously, huge thanks to [Inkle](https://www.inklestudios.com/) (and **Joseph Humfrey** in particular) for [Ink](https://www.inklestudios.com/ink/) and the ecosystem around it, it's made my life way easier.

## License and Attribution
This is licensed under the MIT license - you should find it in the root folder. If you're successfully or unsuccessfully using this tool, I'd love to hear about it!

You can find me [on Medium, here](https://wildwinter.medium.com/).