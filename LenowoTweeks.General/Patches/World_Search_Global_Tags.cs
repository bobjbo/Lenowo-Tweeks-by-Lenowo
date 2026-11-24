using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using Elements.Core;

using FrooxEngine;

using HarmonyLib;

using SkyFrost.Base;

namespace LenowoTweeks.General.Patches;

[HarmonyPatch(typeof(LegacyWorldListManager))]
public class World_Search_Global_Tags
{
	static bool needsInitialized = true;
	static List<string> optionalTags = [];
	static List<string> requiredTags = [];
	static List<string> excludedTags = [];

	private static void WorldSearchOnKeyConfigurationChanged(object? new_value)
	{
		// Clear out old tags
		optionalTags.Clear();
		requiredTags.Clear();
		excludedTags.Clear();

		// Parse the new tags string the same way as the world search bar.
		string str_value = new_value as string;
		if (str_value != null)
			SearchQueryParser.Parse(str_value, optionalTags, requiredTags, excludedTags);
	}

	// Setup function, bind the on changed function and call it once to make sure its initialized.
	public static bool Prepare()
	{
		if (needsInitialized)
		{
			LenowoTweeks_General.worldSearchGlobalTags.ConfigKey.OnChanged += WorldSearchOnKeyConfigurationChanged;
			WorldSearchOnKeyConfigurationChanged(LenowoTweeks_General.worldSearchGlobalTags.Value);
			needsInitialized = false;
		}

		return true;
	}

	// Go through the instructions and replace the 3 Clear lines on the 3 tag lists with the custom functions.
	[HarmonyPatch(typeof(LegacyWorldListManager), "UpdateList")]
	static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		var codes = new List<CodeInstruction>(instructions);
		for (var i = 0; i < codes.Count; i++)
		{
			if (codes[i].opcode != OpCodes.Ldfld) continue;

			string operand = codes[i].operand.ToString();
			if (operand.Substring(operand.Length - 5) == "Terms" && codes[i + 1].opcode == OpCodes.Callvirt && codes[i + 1].operand.ToString().Contains("Clear()"))
			{
				codes[i + 1].opcode = OpCodes.Call;
				switch (operand.Substring(operand.Length - 13, 8))
				{
					case "optional":
						codes[i + 1].operand = AccessTools.Method(typeof(World_Search_Global_Tags), "Optional_Fill");
						break;
					case "required":
						codes[i + 1].operand = AccessTools.Method(typeof(World_Search_Global_Tags), "Required_Fill");
						break;
					case "excluded":
						codes[i + 1].operand = AccessTools.Method(typeof(World_Search_Global_Tags), "Excluded_Fill");
						return codes.AsEnumerable(); // exclude is the last of the 3, so stop the loop.
					default:
						UniLog.Error($"World_Search_Global_Tags:Transpiler ERROR: \"{operand}\" is not expected!", false);
						break;
				}
			}
		}
		return codes.AsEnumerable(); // just-in-case the function fails to find the replacements.
	}

	static void Optional_Fill(List<string> _optionalTerms)
	{
		_optionalTerms.Clear();
		_optionalTerms.AddRange(optionalTags);
	}

	static void Required_Fill(List<string> _requiredTerms)
	{
		_requiredTerms.Clear();
		_requiredTerms.AddRange(requiredTags);
	}

	static void Excluded_Fill(List<string> _excludedTerms)
	{
		_excludedTerms.Clear();
		_excludedTerms.AddRange(excludedTags);
	}
}
