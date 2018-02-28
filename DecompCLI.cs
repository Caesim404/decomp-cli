using System;
using System.IO;
using System.Collections.Generic;
using Decomp.Core;

using NDesk.Options;

namespace decomp_cli
{
    class DecompCLI
    {
        static void Help(OptionSet opts)
        {
            Console.WriteLine("Usage: decomp-cli [OPTIONS] input output");
            Console.WriteLine("Decompiles Mount&Blade modules.");
            Console.WriteLine();
            Console.WriteLine("input\t - file or directory to decompile");
            Console.WriteLine("output\t - directory to write decompiled files to");
            Console.WriteLine();
            Console.WriteLine("Options:");
            opts.WriteOptionDescriptions(Console.Out);
        }

        static void ListModuleTypes()
        {
            Console.WriteLine("Module types:");

            foreach(Mode type in Enum.GetValues(typeof(Mode)))
            {
                Console.WriteLine($"    {(int)type} - {type}");
            }
        }

        static int Main(string[] args)
        {
            bool showHelp = false;
            bool listModTypes = false;
            Mode modType = Mode.WarbandScriptEnhancer450;

            Common.NeedId = true;
            Common.DecompileShaders = false;

            var opts = new OptionSet () {
                { "m|module-type=", "module type", (Mode v) => modType = v },
                { "l|list-module-types", "list available module types", v => listModTypes = true },
                { "n|no-id", "don't write ID files", v => Common.NeedId = false },
                { "s|shaders", "decompile shaders (windows only)", v => Common.DecompileShaders = true },
                { "h|help",  "show help message and exit", v => showHelp = true },
            };

            List<string> extra;
            try
            {
                extra = opts.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return 0;
            }

            if (showHelp)
            {
                Help(opts);
                return 0;
            }

            if (listModTypes)
            {
                ListModuleTypes();
                return 0;
            }

            if (!Enum.IsDefined(typeof(Mode), (int)modType))
            {
                Console.WriteLine($"Error: Invalid module type {modType}");
                return 1;
            }

            Common.SelectedMode = modType;

            if (extra.Count < 2)
            {
                Console.WriteLine("Error: missing `input` and/or `output`");
                return 1;
            }

            Common.InputPath = extra[0];

            if (!File.GetAttributes(Common.InputPath).HasFlag(FileAttributes.Directory))
            {
                Common.InputFile = Path.GetFileName(Common.InputPath);
                Common.InputPath = Path.GetDirectoryName(Common.InputPath);
            }

            Common.OutputPath = extra[1];

            Decompiler.Decompile();

            return 0;
        }
    }
}
