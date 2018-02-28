﻿using System;
using System.Text;
using DWORD = System.UInt32;

namespace Decomp.Core
{
    public static class MissionTemplates
    {
        public static string[] Initialize()
        {
            var fID = new Text(Common.InputPath + "/mission_templates.txt");
            fID.GetString();
            int n = Convert.ToInt32(fID.GetString());
            var aMissionTemplates = new string[n];
            for (int i = 0; i < n; i++)
            {
                fID.GetWord();
                aMissionTemplates[i] = fID.GetWord();

                fID.GetWord();
                fID.GetWord();
                fID.GetWord();

                int iSpawnRecords = fID.GetInt();
                for (int j = 0; j < iSpawnRecords; j++)
                {
                    fID.GetWord();
                    fID.GetWord();
                    fID.GetWord();
                    fID.GetWord();
                    fID.GetWord();

                    int iItems = fID.GetInt();

                    for (int k = 0; k < iItems; k++)
                    {
                        //source.Write("{0},", Common.Items[mt.GetInt()]);
                        fID.GetWord();
                    }
                }
                int iTriggers = fID.GetInt();
                for (int t = 0; t < iTriggers; t++)
                {
                    fID.GetWord();
                    fID.GetWord();
                    fID.GetWord();

                    int iRecords = fID.GetInt();
                    for (int r = 0; r < iRecords; r++)
                    {
                        fID.GetWord();
                        int iParams = fID.GetInt();
                        for (int p = 0; p < iParams; p++)
                        {
                            fID.GetWord();
                        }
                    }

                    iRecords = fID.GetInt();
                    for (int r = 0; r < iRecords; r++)
                    {
                        fID.GetWord();
                        int iParams = fID.GetInt();
                        for (int p = 0; p < iParams; p++)
                        {
                            fID.GetWord();
                        }
                    }
                }
            }
            fID.Close();

            return aMissionTemplates;
        }

        public static string DecompileSpawnFlags(DWORD dwSpawnFlag)
        {
            var sbSpawnFlag = new StringBuilder(2048);
            DWORD dwTeam = (dwSpawnFlag & 0x0000F000) >> 12;
            if (dwTeam == 8)
                sbSpawnFlag.Append("mtef_team_member_2|");
            else if (dwTeam != 0)
                sbSpawnFlag.AppendFormat("mtef_team_{0}|", dwTeam - 1);

            string[] strSpawnFlags = { "mtef_enemy_party", "mtef_ally_party", "mtef_scene_source", "mtef_conversation_source", "mtef_visitor_source",
				"mtef_defenders", "mtef_attackers", "mtef_no_leader", "mtef_no_companions", "mtef_no_regulars", "mtef_infantry_first",
				"mtef_archers_first", "mtef_cavalry_first", "mtef_no_auto_reset", "mtef_reverse_order", "mtef_use_exact_number" };
			DWORD[] dwSpawnFlags = { 0x00000001, 0x00000002, 0x00000004, 0x00000008, 0x00000010, 0x00000040, 0x00000080, 0x00000100,
				0x00000200, 0x00000400, 0x00010000, 0x00020000, 0x00040000, 0x00080000, 0x01000000, 0x02000000 };

            for (int i = 0; i < dwSpawnFlags.Length; i++)
            {
                if ((dwSpawnFlag & dwSpawnFlags[i]) != 0)
                {
                    //strSpawnFlag = strSpawnFlag + strSpawnFlags[i] + "|";
                    sbSpawnFlag.Append(strSpawnFlags[i]);
                    sbSpawnFlag.Append('|');
                }
            }

            //strSpawnFlag = strSpawnFlag == "" ? "0" : strSpawnFlag.Remove(strSpawnFlag.Length - 1, 1);
            if (sbSpawnFlag.Length == 0)
                sbSpawnFlag.Append('0');
            else
                sbSpawnFlag.Length--;

            return sbSpawnFlag.ToString();
        }

        public static string DecompileAlterFlags(DWORD dwAlterFlag)
        {
            var sbAlterFlag = new StringBuilder(2048);
            string[] strAlterFlagsConst = { "af_override_everything", "af_override_all", "af_override_all_but_horse", "af_override_weapons" };
			DWORD[] dwAlterFlagsConst = { 0x000001FF, 0x000001BF, 0x000000BF, 0x0000000f };
			for (int i = 0; i < 4; i++ )
			{
				DWORD temp = dwAlterFlag & dwAlterFlagsConst[i];
				if(temp - dwAlterFlagsConst[i] == 0)
                {
                    sbAlterFlag.Append(strAlterFlagsConst[i]);
                    sbAlterFlag.Append('|');
					dwAlterFlag ^= dwAlterFlagsConst[i];
                    break;
				}
			}

            string[] strAlterFlags = { "af_require_civilian", "af_override_fullhelm", "af_override_horse", "af_override_gloves", "af_override_foot", "af_override_body",
				"af_override_head", "af_override_weapon_3", "af_override_weapon_2", "af_override_weapon_1", "af_override_weapon_0" };
			DWORD[] dwAlterFlags = { 0x10000000, 0x00000200, 0x00000100, 0x00000080, 0x00000040, 0x00000020,
				0x00000010, 0x00000008, 0x00000004, 0x00000002, 0x00000001 };
			for (int i = 0; i < dwAlterFlags.Length; i++ )
			{
				DWORD temp  = dwAlterFlag & dwAlterFlags[i];
				if(temp - dwAlterFlags[i] == 0)
                {
                    sbAlterFlag.Append(strAlterFlags[i]);
                    sbAlterFlag.Append('|');
					dwAlterFlag ^= dwAlterFlags[i];
				}
            }

            //for (int i = 0; i < dwAlterFlags.Length; i++)
            //{
            //    if (dwAlterFlag >= dwAlterFlags[i])
            //    {
            //        strAlterFlag = strAlterFlag + strAlterFlags[i] + "|";
            //        dwAlterFlag -= dwAlterFlags[i];
            //    }
            //}

            //strAlterFlag = strAlterFlag == "" ? "0" : strAlterFlag.Remove(strAlterFlag.Length - 1, 1);
            if (sbAlterFlag.Length == 0)
                sbAlterFlag.Append('0');
            else
                sbAlterFlag.Length--;

            return sbAlterFlag.ToString();
        }

        public static void Decompile()
        {
            var fMissionTemplates = new Text(Common.InputPath + "/mission_templates.txt");
            var fSource = new Win32FileWriter(Common.OutputPath + "/module_mission_templates.py");
            fSource.WriteLine(Header.Standard);
            fSource.WriteLine(Header.MissionTemplates);
            fMissionTemplates.GetString();
            int iMissionTemplates = fMissionTemplates.GetInt();
            for (int m = 0; m < iMissionTemplates; m++)
            {
                fMissionTemplates.GetWord();
                fSource.Write("  (\"{0}\",", fMissionTemplates.GetWord());

                DWORD dwFlag = fMissionTemplates.GetDWord();
                string strFlag = "";
                string[] strFlags = { "mtf_arena_fight", "mtf_battle_mode", "mtf_commit_casualties", "mtf_no_blood", "mtf_synch_inventory" };
                DWORD[] dwFlags = { 0x00000001, 0x00000002, 0x00000010, 0x00000100, 0x00010000 };
                for (int i = 0; i < dwFlags.Length; i++)
                {
                    if ((dwFlag & dwFlags[i]) != 0)
                    {
                        dwFlag ^= dwFlags[i];
                        strFlag += strFlags[i] + "|";
                    }
                }

                /*for (int i = dwFlags.Length - 1; i >= 0; i--)
                {
                    if (dwFlag >= dwFlags[i])
                    {
                        strFlag = strFlag + strFlags[i] + "|";
                    }
                }*/

                strFlag = strFlag == "" ? "0" : strFlag.Remove(strFlag.Length - 1, 1);

                /*var sbFlag = new StringBuilder(256);
                string[] strFlags = { "mtf_arena_fight", "mtf_battle_mode", "mtf_commit_casualties", "mtf_no_blood", "mtf_synch_inventory" };
                DWORD[] dwFlags = { 0x00000001, 0x00000002, 0x00000010, 0x00000100, 0x00010000 };
                for (int i = 0; i < dwFlags.Length; i++)
                {
                    if ((dwFlag & dwFlags[i]) != 0)
                    {
                        dwFlag ^= dwFlags[i];
                        sbFlag.Append(strFlags[i]);
                        sbFlag.Append('|');
                    }
                }
                if (sbFlag.Length == 0)
                    sbFlag.Append('0');
                else
                    sbFlag.Length--;*/

                fSource.Write(" {0},", strFlag);

                int iType = fMissionTemplates.GetInt();
                string strType = "";
                if (iType == 8)
                    strType = "charge";
                else if (iType == 10)
                    strType = "charge_with_ally";
                if (strType != "")
                    fSource.WriteLine(" {0},", strType);
                else
                    fSource.WriteLine(" {0},", iType);

                fSource.WriteLine("  \"{0}\",\r\n  [", fMissionTemplates.GetWord().Replace('_', ' '));
                int iSpawnRecords = fMissionTemplates.GetInt();
                for (int i = 0; i < iSpawnRecords; i++)
                {
                    int iNum = fMissionTemplates.GetInt();
                    DWORD dwSpawnFlag = fMissionTemplates.GetDWord();
                    DWORD dwAlterFlag = fMissionTemplates.GetDWord();
                    DWORD dwAIFlag = fMissionTemplates.GetDWord();
                    int iTroops = fMissionTemplates.GetInt();
                    int iItems = fMissionTemplates.GetInt();
                    fSource.Write("    ({0}, {1}, {2}", iNum, DecompileSpawnFlags(dwSpawnFlag), DecompileAlterFlags(dwAlterFlag));

                    if (dwAIFlag == 0x00000010)
                        fSource.Write(", aif_start_alarmed");
                    else
                        fSource.Write(", {0}", dwAIFlag);

                    fSource.Write(", {0}, [", iTroops);

                    string strItemList = "";
                    for (int j = 0; j < iItems; j++)
                    {
                        //fSource.Write("{0},", Common.Items[fMissionTemplates.GetInt()]);
                        strItemList = strItemList + $"itm_{Common.Items[fMissionTemplates.GetInt()]},";
                    }
                    if (strItemList.Length > 0)
                        strItemList = strItemList.Remove(strItemList.Length - 1, 1);
                    fSource.WriteLine("{0}]),", strItemList);
                }
                fSource.WriteLine("  ],\r\n  [");

                int iTriggers = fMissionTemplates.GetInt();
                for (int i = 0; i < iTriggers; i++)
                {
                    fSource.Write("    (");
                    for (int j = 0; j < 3; j++)
                    {
                        double dInterval = fMissionTemplates.GetDouble();
                        fSource.Write("{0}, ", Common.GetTriggerParam(dInterval));
                    }
                    fSource.Write("\r\n    [");

                    int iConditionRecords = fMissionTemplates.GetInt();
                    if (iConditionRecords != 0)
                    {
                        fSource.WriteLine();
                        Common.PrintStatement(ref fMissionTemplates, ref fSource, iConditionRecords, "      ");
                        fSource.Write("    ");
                    }
                    fSource.Write("],\r\n    [");
                    iConditionRecords = fMissionTemplates.GetInt();
                    if (iConditionRecords != 0)
                    {
                        fSource.WriteLine();
                        Common.PrintStatement(ref fMissionTemplates, ref fSource, iConditionRecords, "      ");
                        fSource.Write("    ");
                    }

                    fSource.Write("]),\r\n\r\n");
                }
                fSource.Write("  ]),\r\n\r\n");
            }
            fSource.Write("]");
            fSource.Close();
            fMissionTemplates.Close();

            Common.GenerateId("ID_mission_templates.py", Common.MissionTemplates, "mst");
        }
    }
}
