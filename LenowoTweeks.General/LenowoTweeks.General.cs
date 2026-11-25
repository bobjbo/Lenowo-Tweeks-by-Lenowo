using HarmonyLib;

using ResoniteModLoader;

using System.Reflection;

using FrooxEngine.UIX;

using LenowoTweeks.Core;
using LenowoTweeks.General.Patches;

using Elements.Core;

using FrooxEngine;






#if !RELEASE
using ResoniteHotReloadLib;
#endif

namespace LenowoTweeks.General;

public class LenowoTweeks_General : LenowoTweak
{
	private static Assembly ModAssembly => typeof(LenowoTweeks_General).Assembly;
	internal const string VERSION_CONSTANT = "2.1.1";
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


	public static ModConfigKey<bool> partyMode = new("Party Mode", "Turns any component selector into a party!!!", false);
	public static ModConfigKey<bool> shareTheParty = new("Share The Party", "No fun in a party if you are alone!", false);
	public static ModConfigKey<bool> noParty = new("No Party", ":(", false);

	public static ModConfigKey<bool> attacherMessages = new("Node Attacher Messages", "Makes Node Attachers have little messages", false);
	public static ModConfigKey<bool> changeBGColor = new("Override Node Attacher Background Color", "Makes Node Attachers have a custom backround color from your secondary UI Color", false);

	public static ModConfigKey<bool> addSpacesInNames = new("Add Spaces In Names", "Adds spaces in the component names, like 'Cast_colorX_To_float4' with 'Cast color X To float4'", false);

	public static ModConfigKey<bool> addDefaultTypes = new("Add Default Types", "When picking a generic type with this enabled, some types will be added by default at the top.", false);



	public static ModConfigKey<colorX> categoryColor = new("Catagory Color", "The color of catagories in the node browser", RadiantUI_Constants.Sub.YELLOW);
	public static ModConfigKey<colorX> genericFolderColor = new("Generic Folder Color", "The color of generic node folders in the node browser", RadiantUI_Constants.Sub.GREEN);
	public static ModConfigKey<colorX> groupingColor = new("Node Group Color", "The color of node groups in the node browser", RadiantUI_Constants.Sub.PURPLE);
	public static ModConfigKey<colorX> normalColor = new("Normal Color", "The color of normal types in the node browser", RadiantUI_Constants.Sub.CYAN);

	public static ModConfigKey<colorX?> backgroundColorOverride = new("Background Color", "The color of the background in the node browser", null);

	// Color Presets
	public static ModConfigButton defaultPreset = new("Preset: Default", () => SetBrowserColors(RadiantUI_Constants.Sub.YELLOW, RadiantUI_Constants.Sub.GREEN, RadiantUI_Constants.Sub.PURPLE, RadiantUI_Constants.Sub.CYAN));
	public static ModConfigButton midPreset = new("Preset: Mid", () => SetBrowserColors(RadiantUI_Constants.MidLight.YELLOW, RadiantUI_Constants.MidLight.GREEN, RadiantUI_Constants.MidLight.PURPLE, RadiantUI_Constants.MidLight.CYAN));
	public static ModConfigButton darkPreset = new("Preset: Dark", () => SetBrowserColors(RadiantUI_Constants.Dark.YELLOW, RadiantUI_Constants.Dark.GREEN, RadiantUI_Constants.Dark.PURPLE, RadiantUI_Constants.Dark.CYAN));
	public static ModConfigButton brightPreset = new("Preset: Bright", () => SetBrowserColors(RadiantUI_Constants.Hero.YELLOW, RadiantUI_Constants.Hero.GREEN, RadiantUI_Constants.Hero.PURPLE, RadiantUI_Constants.Hero.CYAN));
	public static ModConfigButton rgbPreset = new("Preset: RGB", () => SetBrowserColors(new(0.2f, 0f, 0f), new(0f, 0.2f, 0f), new(0f, 0f, 0.2f), new(0.1f), new(0f)));
	public static ModConfigButton rosaPreset = new("Preset: Rosa", () => SetBrowserColors(RadiantUI_Constants.Sub.PURPLE, RadiantUI_Constants.Sub.GREEN, RadiantUI_Constants.Dark.PURPLE, RadiantUI_Constants.Dark.CYAN, RadiantUI_Constants.Dark.PURPLE));



	public static void SetBrowserColors(colorX category, colorX generic, colorX grouping, colorX normal, colorX? bg = null)
	{
		categoryColor.Value = category;
		genericFolderColor.Value = generic;
		groupingColor.Value = grouping;
		normalColor.Value = normal;
		backgroundColorOverride.Value = bg;
	}


	public static ModConfiguration? Config;

	public static readonly Dictionary<string, Dictionary<string, List<IModConfigItem>>> SortedConfigKeys = new()
	{
		{
			"General", new()
			{
				{ "Base", [ valueFieldDroppingMode, worldSearchGlobalTags, getItemLink ] },
				{ "Create New", [ smallPrimitives, smallPrimitiveScale, removeMeshDefaultGizmo ] },
				{ "Component Selector",
				[
					/*partyMode, shareTheParty, noParty,*/
					changeBGColor, attacherMessages, addSpacesInNames, /*addDefaultTypes,*/
					categoryColor, groupingColor, genericFolderColor, normalColor, backgroundColorOverride,
					defaultPreset, midPreset, darkPreset, brightPreset, rgbPreset, rosaPreset
				] },
			}
		}
	};

	public override ModConfiguration? GetConfig => instance.GetConfiguration();
	public override Dictionary<string, Dictionary<string, List<IModConfigItem>>> GetKeys => SortedConfigKeys;
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
					if (configKey is ModConfigKey key)
					{
						builder.Key(key.ConfigKey);
					}
				}
			}
		}
	}

	public override void Init()
	{

#if !RELEASE
		HotReloader.RegisterForHotReload(this);
#endif

		Config = GetConfiguration();

		Config.Save(true);

		instance = this;

		harmony.PatchAll(ModAssembly);
	}

#if !RELEASE
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
