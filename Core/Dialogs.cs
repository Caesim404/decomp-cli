﻿using System.Text;
using DWORD = System.UInt32;

namespace Decomp.Core
{
    public static class Dialogs
    {
        public static string[] Initialize()
        {
            return Win32FileReader.ReadAllLines(Common.InputPath + @"\dialog_states.txt");
        }

        public static void Decompile()
        {
            var fDialogs = new Text(Common.InputPath + @"\conversation.txt");
            var fSource = new Win32FileWriter(Common.OutputPath + @"\module_dialogs.py");
            fSource.WriteLine(Header.Standard);
            fSource.WriteLine(Header.Dialogs);
            fDialogs.GetString();
            int iDialogs = fDialogs.GetInt();
            for (int t = 0; t < iDialogs; t++)
            {
                fDialogs.GetWord();
                DWORD dwDialogPartner = fDialogs.GetUInt();
                int iStartingDialogState = fDialogs.GetInt();
                var sbDialogPartner = new StringBuilder(256);

                string[] strRepeatsPrefix = { "repeat_for_factions", "repeat_for_parties", "repeat_for_troops", "repeat_for_100", "repeat_for_1000" };
                uint iRepeat = (dwDialogPartner & 0x00007000) >> 12;
                if (iRepeat != 0)
                {
                    sbDialogPartner.Append(strRepeatsPrefix[iRepeat - 1]);
                    sbDialogPartner.Append('|');
                }

                string[] strPartnerPrefix = { "plyr", "party_tpl", "auto_proceed", "multi_line" };
                int[] iPartnerPrefix = { 0x00010000, 0x00020000, 0x00040000, 0x00080000 };
                for (int i = 0; i < 4; i++)
                {
                    if ((iPartnerPrefix[i] & dwDialogPartner) != 0)
                    {
                        sbDialogPartner.Append(strPartnerPrefix[i]);
                        sbDialogPartner.Append('|');
                    }
                }

                DWORD dwPartner = dwDialogPartner & 0x00000FFF;
                if (dwPartner == 0x00000FFF)
                {
                    sbDialogPartner.Append("anyone|");
                }
		        else if(dwPartner != 0)
		        {
		            sbDialogPartner.Append(dwPartner < Common.Troops.Length ? "trp_" + Common.Troops[dwPartner] + "|" : $"{dwPartner}|");
		        }

                DWORD dwOther = (dwDialogPartner & 0xFFF00000) >> 20;
                if (dwOther != 0)
                {
                    sbDialogPartner.Append(dwOther < Common.Troops.Length ? "other(trp_" + Common.Troops[dwOther] + ")|" : $"other({dwOther})|");
                }

                if (sbDialogPartner.Length == 0)
                    sbDialogPartner.Append('0');
                else
                    sbDialogPartner.Length--;

                fSource.Write("  [{0}, \"{1}\",\r\n    [", sbDialogPartner, Common.DialogStates[iStartingDialogState]);

                int iRecords = fDialogs.GetInt();
                if (iRecords != 0)
                {
                    fSource.WriteLine();
                    Common.PrintStatement(ref fDialogs, ref fSource, iRecords, "      ");
                    fSource.WriteLine("    ],");
                }
                else
                    fSource.WriteLine("],");

                string strDialogText = fDialogs.GetWord();
                fSource.WriteLine("    \"{0}\",", strDialogText.Replace('_', ' '));

                int iEndingDialogState = fDialogs.GetInt();
                fSource.Write("    \"{0}\",\r\n    [", Common.DialogStates[iEndingDialogState]);

                iRecords = fDialogs.GetInt();
                if (iRecords != 0)
                {
                    fSource.WriteLine();
                    Common.PrintStatement(ref fDialogs, ref fSource, iRecords, "      ");
                    fSource.Write("    ]");
                }
                else
                    fSource.Write("]");

                string strVoiceOver = fDialogs.GetWord();
                if (strVoiceOver.Trim() != "NO_VOICEOVER")
                    fSource.Write(",\r\n    [\"{0}\"]", strVoiceOver);

                fSource.WriteLine("],\r\n");
            }
            fSource.Write("]");
            fSource.Close();
            fDialogs.Close();
        }
    }
}
