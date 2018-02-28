#define RELEASE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Decomp.Core.Operators;

namespace Decomp.Core
{
    public static class Decompiler
    {
        private static string GetDirectory(this DirectoryNotFoundException exception)
        {
            var message = exception.Message;
            var beginPos = message.IndexOf('"');
            var endPos = message.IndexOf('"', beginPos + 1);
            return Path.GetDirectoryName(message.Substring(beginPos + 1, endPos - beginPos - 1));
        }

        private static string Status
        {
            set { Console.WriteLine(value); }
        }

        private static Thread _workThread = new Thread(Decompile);

        public static bool Alive => _workThread.IsAlive;

        public static void StopDecompilation()
        {
            _workThread.Abort();
        }

        public static void StartDecompilation()
        {
            _workThread = new Thread(Decompile);
            _workThread.Start();
        }

        public static void Decompile()
        {
            Status = "";

            bool isSingleFile = Common.InputFile != null;

            if (!File.Exists(Common.InputPath) && !Directory.Exists(Common.InputPath))
            {
                Status = "Input file/directory `" + Common.InputPath + "` not found";
                return;
            }

            if (!Directory.Exists(Common.OutputPath))
            {
                try
                {
                    Directory.CreateDirectory(Common.OutputPath);
                }
                catch
                {
                    Status = "Unable to create the output directory `" + Common.OutputPath + "`";
                    return;
                }
            }

            try
            {
                if (!isSingleFile)
                {
                    InitializeOpCodes();
                    InitializeModuleData();
                }
                else
                {
                    var f = InitializeTrie[GetSingleFileName()] ?? (() => { });
                    f();
                }
            }
            catch (FileNotFoundException ex)
            {
                Status = "File not found `" + ex.FileName + "`";
                return;
            }
            catch (DirectoryNotFoundException ex)
            {
                Status = "Directory not found `" + ex.GetDirectory()  + "`";
                return;
            }
            catch (ThreadAbortException)
            {
                Status = "Decompilation canceled";
                return;
            }
            catch (Exception e) 
            {
                Status = "Decompilation error:\n" + e.Message + "\n" + e.StackTrace;
                return;
            }

#if RELEASE
            try
            {
#endif
                if (isSingleFile)
                    ProcessSingleFile();
                else
                    ProcessFullModule();

#if RELEASE
            }
            catch (ThreadAbortException)
            {
                Status = "Decompilation canceled";
                return;
            }
            catch (Exception ex)
            {
                Status = "Decompilation error:\n" + ex.Message + "\n" + ex.StackTrace;
            }
#endif

            Status = "Finished decompiling!";
        }

        private static void InitializeOpCodes()
        {
            Common.Operators = new Dictionary<int, Operator>();

            var operators = Operator.GetCollection(Common.SelectedMode);
            foreach (var op in operators) Common.Operators[op.Code] = op;
        }

        private static void InitializeModuleData()
        {
            Status = $"Initializing scripts.txt";
            Common.Procedures = Scripts.Initialize();
            Status = "Initializing quick_strings.txt";
            Common.QuickStrings = QuickStrings.Initialize();
            Status = "Initializing strings.txt";
            Common.Strings = Strings.Initialize();
            Status = "Initializing item_kinds1.txt";
            Common.Items = Text.GetFirstStringFromFile(Common.InputPath + "/item_kinds1.txt") == "itemsfile version 2"
                ? Vanilla.Items.GetIdFromFile(Common.InputPath + "/item_kinds1.txt") : Items.Initialize();
            Status = "Initializing troops.txt";
            Common.Troops = Text.GetFirstStringFromFile(Common.InputPath + "/troops.txt") == "troopsfile version 1"
                ? Vanilla.Troops.GetIdFromFile(Common.InputPath + "/troops.txt") : Troops.Initialize();
            Status = "Initializing factions.txt";
            Common.Factions = Factions.Initialize();
            Status = "Initializing quests.txt";
            Common.Quests = Quests.Initialize();
            Status = "Initializing party_templates.txt";
            Common.PTemps = PartyTemplates.Initialize();
            Status = "Initializing parties.txt";
            Common.Parties = Parties.Initialize();
            Status = "Initializing menus.txt";
            Common.Menus = Menus.Initialize();
            Status = "Initializing sounds.txt";
            Common.Sounds = Sounds.Initialize();
            Status = "Initializing skills.txt";
            Common.Skills = Skills.Initialize();
            Status = "Initializing meshes.txt";
            Common.Meshes = Meshes.Initialize();
            Status = "Initializing variables.txt";
            Common.Variables = Scripts.InitializeVariables();
            Status = "Initializing dialog_states.txt";
            Common.DialogStates = Dialogs.Initialize();
            Status = "Initializing scenes.txt";
            Common.Scenes = Scenes.Initialize();
            Status = "Initializing mission_templates.txt";
            Common.MissionTemplates = MissionTemplates.Initialize();
            Status = "Initializing particle_systems.txt";
            Common.ParticleSystems = ParticleSystems.Initialize();
            Status = "Initializing scene_props.txt";
            Common.SceneProps = SceneProps.Initialize();
            Status = "Initializing map_icons.txt";
            Common.MapIcons = MapIcons.Initialize();
            Status = "Initializing presentations.txt";
            Common.Presentations = Presentations.Initialize();
            Status = "Initializing tableau_materials.txt";
            Common.Tableaus = TableauMaterials.Initialize();
            Status = "Initializing actions.txt";
            Common.Animations = Common.IsVanillaMode ? Vanilla.Animations.GetIdFromFile(Common.InputPath + "/actions.txt") : Animations.Initialize();
            Status = "Initializing music.txt";
            Common.Music = Music.Initialize();
            Status = "Initializing skins.txt";
            Common.Skins = Skins.Initialize();
            Status = "Initializing finished";
        }

        private static void ProcessFile(string strFileName)
        {
            if (!File.Exists(Common.InputPath + "/" + strFileName))
            {
                Status = "File not found " + Common.InputPath + "/" + strFileName;
                return;
            }

            var sw = Stopwatch.StartNew();
            var dblTime = sw.ElapsedTicks * 1000.0 / Stopwatch.Frequency;
            
            var fInput = new Text(Common.InputPath + "/" + strFileName);
            var strFirstString = fInput.GetString();
            if (strFirstString == null)
            {
                Status = "Unknown file format";
                return;
            }
            int iFirstNumber;
            var bFirstNumber = Int32.TryParse(strFirstString, out iFirstNumber);
            fInput.Close();

            if (strFirstString == "scriptsfile version 1")
                Scripts.Decompile();
            else if (strFirstString == "triggersfile version 1")
                Triggers.Decompile();
            else if (strFirstString == "simple_triggers_file version 1")
                SimpleTriggers.Decompile();
            else if (strFirstString == "dialogsfile version 2") //Warband dialogs file
                Dialogs.Decompile();
            else if (strFirstString == "dialogsfile version 1") //M&B v1.011/v1.010 dialogs file
                Vanilla.Dialogs.Decompile();
            else if (strFirstString == "menusfile version 1")
                Menus.Decompile();
            else if (strFirstString == "factionsfile version 1")
                Factions.Decompile();
            else if (strFirstString == "infopagesfile version 1")
                InfoPages.Decompile();
            else if (strFirstString == "itemsfile version 3") //Warband items file
                Items.Decompile();
            else if (strFirstString == "itemsfile version 2") //M&B v1.011/v1.010 items file
                Vanilla.Items.Decompile();
            else if (strFirstString == "map_icons_file version 1")
                MapIcons.Decompile();
            else if (strFirstString == "missionsfile version 1")
                MissionTemplates.Decompile();
            else if (strFirstString == "particle_systemsfile version 1")
                ParticleSystems.Decompile();
            else if (strFirstString == "partiesfile version 1")
                Parties.Decompile();
            else if (strFirstString == "partytemplatesfile version 1")
                PartyTemplates.Decompile();
            else if (strFirstString == "postfx_paramsfile version 1")
                Postfx.Decompile();
            else if (strFirstString == "presentationsfile version 1")
                Presentations.Decompile();
            else if (strFirstString == "questsfile version 1")
                Quests.Decompile();
            else if (strFirstString == "scene_propsfile version 1")
                SceneProps.Decompile();
            else if (strFirstString == "scenesfile version 1")
                Scenes.Decompile();
            else if (strFirstString == "skins_file version 1" && Common.SelectedMode == Mode.Caribbean) //Caribbean skins file
                Caribbean.Skins.Decompile();
            else if (strFirstString == "skins_file version 1") //Warband skins file
                Skins.Decompile();
            else if (strFirstString == "soundsfile version 3") //Warband sounds file
                Sounds.Decompile();
            else if (strFirstString == "soundsfile version 2") //M&B v1.011/v1.010 sounds file
                Vanilla.Sounds.Decompile();
            else if (strFirstString == "stringsfile version 1")
                Strings.Decompile();
            else if (strFirstString == "troopsfile version 2") //Warband troops file
                Troops.Decompile();
            else if (strFirstString == "troopsfile version 1") //M&B v1.011/v1.010 troops file
                Vanilla.Troops.Decompile();
            else if (bFirstNumber && strFileName == "tableau_materials.txt")
                TableauMaterials.Decompile();
            else if (bFirstNumber && strFileName == "skills.txt")
                Skills.Decompile();
            else if (bFirstNumber && strFileName == "music.txt")
                Music.Decompile();
            else if (bFirstNumber && strFileName == "actions.txt")
            {
                if (Common.IsVanillaMode)
                    Vanilla.Animations.Decompile();
                else
                    Animations.Decompile();
            }
            else if (bFirstNumber && strFileName == "meshes.txt")
                Meshes.Decompile();
            else if (bFirstNumber && strFileName == "flora_kinds.txt")
                Flora.Decompile();
            else if (strFileName == "ground_specs.txt")
                GroundSpecs.Decompile();
            else if (bFirstNumber && strFileName == "skyboxes.txt")
                Skyboxes.Decompile();
            else 
                Status = "Unknown format in " + Common.InputPath + "/" + strFileName;
        }

        private static string GetSingleFileName()
        {
            return Path.GetFileName(Common.InputFile);
        }

        public static string GetShadersFullFileName(out bool founded)
        {
            founded = true;
            if (File.Exists(Common.InputPath + "/mb_2a.fxo")) return Common.InputPath + "/mb_2a.fxo";
            if (File.Exists(Common.InputPath + "/mb_2b.fxo")) return Common.InputPath + "/mb_2b.fxo";
            if (File.Exists(Common.InputPath + "/mb.fx")) return Common.InputPath + "/mb.fx";
            founded = false;
            return "";
        }

        private static void ProcessSingleFile()
        {
            var strFileName = GetSingleFileName();
            Common.NeedId = false;

            var ext = Path.GetExtension(strFileName);
            if (ext == ".fx" || ext == ".fxo")
            {
                Shaders.Shaders.Decompile(Common.InputPath + "/" + strFileName);
                return;
            }

            string[] strModFiles = { "actions.txt", "conversation.txt", "factions.txt", "info_pages.txt", "item_kinds1.txt", "map_icons.txt",
            "menus.txt", "meshes.txt", "mission_templates.txt", "music.txt", "particle_systems.txt", "parties.txt", "party_templates.txt",
            "postfx.txt", "presentations.txt", "quests.txt", "scene_props.txt", "scenes.txt", "scripts.txt", "simple_triggers.txt",
            "skills.txt", "skins.txt", "sounds.txt", "strings.txt", "tableau_materials.txt", "triggers.txt", "troops.txt",
            "flora_kinds.txt", "ground_specs.txt", "skyboxes.txt" };

            string strFileToProcess = strModFiles.FirstOrDefault(t => t == strFileName);
            ProcessFile(strFileToProcess);
        }

        private static void ProcessFullModule()
        {
            File.Copy(Common.InputPath + "/variables.txt", Common.OutputPath + "/variables.txt", true);

            var decompileShaders = Common.DecompileShaders;

            if (!Common.IsVanillaMode)
                Win32FileWriter.WriteAllText(Common.OutputPath + "/module_constants.py", Header.Standard + Common.ModuleConstantsText);
            else
                Win32FileWriter.WriteAllText(Common.OutputPath + "/module_constants.py", Header.Standard + Common.ModuleConstantsVanillaText);

            string[] strModFiles = { "actions.txt", "conversation.txt", "factions.txt", "info_pages.txt", "item_kinds1.txt", "map_icons.txt",
            "menus.txt", "meshes.txt", "mission_templates.txt", "music.txt", "particle_systems.txt", "parties.txt", "party_templates.txt",
            "postfx.txt", "presentations.txt", "quests.txt", "scene_props.txt", "scenes.txt", "scripts.txt", "simple_triggers.txt",
            "skills.txt", "skins.txt", "sounds.txt", "strings.txt", "tableau_materials.txt", "triggers.txt", "troops.txt" };
            string[] strModDataFiles = { "flora_kinds.txt", "ground_specs.txt", "skyboxes.txt" };

            int iNumFiles = strModFiles.Length;
            if (Common.IsVanillaMode) iNumFiles -= 2;

            iNumFiles += strModDataFiles.Count(strModDataFile => File.Exists(Common.InputPath + "/Data" + strModDataFile));

            bool b;
            var sShadersFile = GetShadersFullFileName(out b);
            if (b && decompileShaders) iNumFiles++;
            
            double dblProgressForOneFile = 100.0 / iNumFiles, dblProgress = 0;
            
            foreach (var strModFile in strModFiles.Where(strModFile => !(Common.IsVanillaMode && (strModFile == "info_pages.txt" || strModFile == "postfx.txt"))))
            {
                ProcessFile(strModFile);
                dblProgress += dblProgressForOneFile;
                Status = $"Decompiling {dblProgress:F2}%";
            }

            if (b && decompileShaders)
            {
                ProcessShaders(sShadersFile);
                dblProgress += dblProgressForOneFile;
                Status = $"Decompiling  {dblProgress:F2}%";
            }

            Common.InputPath += "/Data";

            foreach (var strModDataFile in strModDataFiles.Where(strModDataFile => File.Exists(Common.InputPath + "/" + strModDataFile)))
            {
                ProcessFile(strModDataFile);
                dblProgress += dblProgressForOneFile;
                Status = $"Decompiling  {dblProgress:F2}%";
            }
        }

        private static void ProcessShaders(string sShadersFile)
        {
            var sw = Stopwatch.StartNew();
            Shaders.Shaders.Decompile(sShadersFile);
        }
        
        private static readonly SimpleTrie<Action> InitializeTrie = new SimpleTrie<Action>
        {
            ["actions.txt"] = () => { },
            ["conversation.txt"] = () => { InitializeOpCodes(); InitializeModuleData(); },
            ["factions.txt"] = () => { },
            ["info_pages.txt"] = () => { },
            ["item_kinds1.txt"] = () => { InitializeOpCodes(); InitializeModuleData(); },
            ["map_icons.txt"] = () => { },
            ["menus.txt"] = () => { InitializeOpCodes(); InitializeModuleData(); },
            ["meshes.txt"] = () => { },
            ["mission_templates.txt"] = () => { InitializeOpCodes(); InitializeModuleData(); },
            ["music.txt"] = () => { },
            ["particle_systems.txt"] = () => { },
            ["parties.txt"] = () => {
                Status = "Initializing troops.txt";
                Common.Troops = Text.GetFirstStringFromFile(Common.InputPath + "/troops.txt") == "troopsfile version 1"
                    ? Vanilla.Troops.GetIdFromFile(Common.InputPath + "/troops.txt") : Troops.Initialize();
                Status = "Initializing factions.txt";
                Common.Factions = Factions.Initialize();
                Status = "Initializing map_icons.txt";
                Common.MapIcons = MapIcons.Initialize();
            },
            ["party_templates.txt"] = () => {
                Status = "Initializing troops.txt";
                Common.Troops = Text.GetFirstStringFromFile(Common.InputPath + "/troops.txt") == "troopsfile version 1"
                    ? Vanilla.Troops.GetIdFromFile(Common.InputPath + "/troops.txt") : Troops.Initialize();
                Status = "Initializing factions.txt";
                Common.Factions = Factions.Initialize();
                Status = "Initializing map_icons.txt";
                Common.MapIcons = MapIcons.Initialize();
            },
            ["postfx.txt"] = () => { },
            ["presentations.txt"] = () => { InitializeOpCodes(); InitializeModuleData(); },
            ["quests.txt"] = () => { },
            ["scene_props.txt"] = () => { InitializeOpCodes(); InitializeModuleData(); },
            ["scenes.txt"] = () => { },
            ["scripts.txt"] = () => { InitializeOpCodes(); InitializeModuleData(); },
            ["simple_triggers.txt"] = () => { InitializeOpCodes(); InitializeModuleData(); },
            ["skills.txt"] = () => { },
            ["skins.txt"] = () => { },
            ["sounds.txt"] = () => { },
            ["strings.txt"] = () => { },
            ["tableau_materials.txt"] = () => { InitializeOpCodes(); InitializeModuleData(); },
            ["triggers.txt"] = () => { InitializeOpCodes(); InitializeModuleData(); },
            ["troops.txt"] = () => {
                Status = "Initializing item_kinds1.txt";
                Common.Items = Text.GetFirstStringFromFile(Common.InputPath + "/item_kinds1.txt") == "itemsfile version 2"
                    ? Vanilla.Items.GetIdFromFile(Common.InputPath + "/item_kinds1.txt") : Items.Initialize();
                Status = "Initializing scenes.txt";
                Common.Scenes = Scenes.Initialize();
            }
        };
    }
}
