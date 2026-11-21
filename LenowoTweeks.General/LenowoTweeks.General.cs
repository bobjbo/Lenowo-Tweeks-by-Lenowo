using HarmonyLib;

using ResoniteModLoader;

using System.Reflection;

using FrooxEngine.UIX;

using LenowoTweeks.Core;
using LenowoTweeks.General.Patches;

using Elements.Core;
using FrooxEngine;






#if DEBUG
using ResoniteHotReloadLib;
#endif

namespace LenowoTweeks.General;

public class LenowoTweeks_General : LenowoTweak
{
	private static Assembly ModAssembly => typeof(LenowoTweeks_General).Assembly;
	internal const string VERSION_CONSTANT = "2.1.0";
	const string ModName = "Lenowo Tweeks (General)";

	public override string ModuleName => "General";
	public override string ModuleVersion => VERSION_CONSTANT;

	public override string Name => ModName;
	public override string Author => "Rosa";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/bobjbo/Lenowo-Tweeks-by-Lenowo";
	public const string harmonyID = "com.Lenowo.LenowoTweeks.General";
	static Harmony harmony = new(harmonyID);

	public static ResoniteMod? instance;

	public static ModConfigKey<ValueFieldDropMode> valueFieldDroppingMode = new("Valuefield Dropping Mode", "This controls how ValueFields are allowed to drop into TextFields.\t <size=90%>AlwaysAllow = Works as normal, AllowIfNotSelf = Prevent ValueFields under the same ObjectRoot as the TextField, NeverAllow = Never drop ValueFields</size>", ValueFieldDropMode.AlwaysAllow);
	public static ModConfigKey<string> worldSearchGlobalTags = new("Global World Search Filter", "Global Tags to apply to all world searches.", "", "World Search Global Tags");

	public static ModConfigKey<bool> getItemLink = new("Get Item Link", "If enabled, adds some buttons to the dash for getting a resdb or resrec. Same as the GetItemLink mod", false);

	public static ModConfigKey<bool> smallPrimitives = new("Small Primitives", "Adds a new option to the 'Create New' menu, to create smaller versions of the base primitives.", false);
	public static ModConfigKey<float> smallPrimitiveScale = new("Small Primitive Scale Multiplier", "How small the meshes should be if created through the 'Small 3D Models' Category.", 0.1f);
	public static ModConfigKey<bool> removeMeshDefaultGizmo = new("Remove Mesh Default Gizmo", "When creating a mesh from 'Create New', the gizmo will be cleared by default.", false);


	public static ModConfigKey<bool> partyMode = new("Party Mode", "Turns any component selector into a party!!!", true);
	public static ModConfigKey<bool> shareTheParty = new("Share The Party", "No fun in a party if you are alone!", true);

	public static ModConfigKey<bool> attacherMessages = new("Node Attacher Messages", "Makes Node Attachers have little messages", true);
	public static ModConfigKey<colorX> catagoryColor = new("Catagory Color", "The color of catagories in the node browser",RadiantUI_Constants.Sub.YELLOW);
	public static ModConfigKey<colorX> genericFolderColor = new("Generic Folder Color", "The color of generic node folders in the node browser",RadiantUI_Constants.Sub.GREEN);
	public static ModConfigKey<colorX> nodeFolderColor = new("Node Folder Color", "The color of node groups in the node browser",RadiantUI_Constants.Sub.PURPLE);
	public static ModConfigKey<colorX> genericColor = new("Generic Color", "The color of generics in the node browser",RadiantUI_Constants.Sub.CYAN);

	// Color Presets
	// catagoryColor, genercFolderColor, nodeFolderColor, genericColor
	// default: RadiantUI_Constants.Sub.YELLOW, RadiantUI_Constants.Sub.GREEN, RadiantUI_Constants.Sub.PURPLE, RadiantUI_Constants.Sub.CYAN
	// mid: RadiantUI_Constants.Mid.YELLOW, RadiantUI_Constants.Mid.GREEN, RadiantUI_Constants.Mid.PURPLE, RadiantUI_Constants.Mid.CYAN
	// dark: RadiantUI_Constants.Dark.YELLOW, RadiantUI_Constants.Dark.GREEN, RadiantUI_Constants.Dark.PURPLE, RadiantUI_Constants.Dark.CYAN
	// bright: RadiantUI_Constants.Hero.YELLOW, RadiantUI_Constants.Hero.GREEN, RadiantUI_Constants.Hero.PURPLE, RadiantUI_Constants.Hero.CYAN
	// Rosa: RadientUI_Constants.Sub.PURPLE, RadiantUI_Constants.Sub.GREEN, RadiantUI_Constants.Dark.PURPLE, RadiantUI_Constants.Dark.CYAN



	public static ModConfiguration? Config;

	public static readonly Dictionary<string, Dictionary<string, List<ModConfigKey>>> SortedConfigKeys = new()
	{
		{
			"General", new()
			{
				{ "Base", [ valueFieldDroppingMode, worldSearchGlobalTags, getItemLink ] },
				{ "Create New", [ smallPrimitives, smallPrimitiveScale, removeMeshDefaultGizmo ] },
				{ "Component Selector", [ partyMode, shareTheParty, attacherMessages, catagoryColor, nodeFolderColor, genericFolderColor, genericColor ] },
			}
		}
	};

	public override ModConfiguration? GetConfig => instance.GetConfiguration();
	public override Dictionary<string, Dictionary<string, List<ModConfigKey>>> GetKeys => SortedConfigKeys;
	public override int ConfigOrder => -9;

	public static void ModSettings_BuildModUi(UIBuilder ui)
	{
		LenowoTweeks_Core.ModSettings_BuildModUi(ui);
	}

	public override void DefineConfiguration(ModConfigurationDefinitionBuilder builder)
	{
		builder.AutoSave(true);
		foreach (var category in SortedConfigKeys.Values)
		{
			foreach (var configKeys in category.Values)
			{
				foreach (var configKey in configKeys)
				{
					builder.Key(configKey.ConfigKey);
				}
			}
		}
	}

	public override void Init()
	{

#if DEBUG
		HotReloader.RegisterForHotReload(this);
#endif

		Config = GetConfiguration();

		Config.Save(true);

		instance = this;

		harmony.PatchAll(ModAssembly);
	}

#if DEBUG
	static void BeforeHotReload()
	{
		CreateNewPatches.ClearModCategories();
		harmony.UnpatchAll(harmonyID);
	}

	static void OnHotReload(ResoniteMod modInstance)
	{
		instance = modInstance;
		Config = modInstance.GetConfiguration();
		LenowoTweeks_Core.RegisterModModule(modInstance);
		harmony.PatchAll(ModAssembly);
	}
#endif
}
