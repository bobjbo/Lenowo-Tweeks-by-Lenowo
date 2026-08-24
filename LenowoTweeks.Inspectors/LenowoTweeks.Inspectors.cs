using FrooxEngine;

using HarmonyLib;

using ResoniteModLoader;

using System.Reflection;

using Elements.Core;

using FrooxEngine.UIX;

using LenowoTweeks.Core;



#if DEBUG
using ResoniteHotReloadLib;
#endif

namespace LenowoTweeks.Inspectors;

public class LenowoTweeks_Inspectors : LenowoTweak
{
	private static Assembly ModAssembly => typeof(LenowoTweeks_Inspectors).Assembly;
	internal const string VERSION_CONSTANT = "2.0.1";
	const string ModName = "Lenowo Tweeks (Inspectors)";

	public override string ModuleName => "Inspectors";
	public override string ModuleVersion => VERSION_CONSTANT;

	public override string Name => ModName;
	public override string Author => "Rosa";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/bobjbo/Lenowo-Tweeks-by-Lenowo";
	public const string harmonyID = "com.Lenowo.LenowoTweeks.Inspectors";
	static Harmony harmony = new(harmonyID);

	public static ResoniteMod? instance;


	public static ModConfigKey<bool> nohelp = new("Disable Help Buttons", "disables the help buttons in the inspctor and context menu", false, "No Help");

	public static ModConfigKey<bool> expandedStringInputs = new("Expanded String Inputs", "This toggles if string fields on inspectors should get bigger based on the text in them.", false);

	public static ModConfigKey<bool> modifiedInspectorUIX = new("Modified Inspector UIX", "This toggles if the inspector UIX Fields should load in the custom way.", false);
	public static ModConfigKey<Patches.FieldNameMode> fieldNameMode = new("Field Name Mode", "This controls how the fields are named in the inspector.", Patches.FieldNameMode.Normal);
	public static ModConfigKey<bool> slotInspectorResetAll = new("Slot Inspector Reset All", "This toggles if a 'Reset All' button is created on inspectors", false);

	public static ModConfigKey<bool> copyComponentsToButtons = new("Copy Components To Buttons", "If enabled, try and find the slot at 'Config/UIComponents.[Type]', and copies the components to the buttons with the same type.\nThe types can be Component, Field, and MemberAction", false);

	public static ModConfigKey<bool> collapseComponents = new("Collapse Components", "This toggles if the components in the inspector should load collapsed.", false);
	public static ModConfigKey<colorX> collapsedComponentColor = new("Collapsed Component Color", "This text color of a collapsed component.", RadiantUI_Constants.MidLight.YELLOW);
	public static ModConfigKey<colorX> expandedComponentColor = new("Expanded Component Color", "This text color of a expanded component.", RadiantUI_Constants.LABEL_COLOR);

	public static ModConfigKey<bool> modifiedComponentHeaders = new("Modified Component Headers", "This toggles if the component headers can be modified on certain types", true);
	public static ModConfigKey<string> dynvarComponentHeaderName = new("Dynvar Component Header Name", "Replaces ['Variable','Field','Reference'] with the provided text on dynamic variables in the component header (Formatted as 'Variable;Field;Reference')", "");

	public static ModConfigKey<bool> listCollapsing = new("List Collapsing", "If lists should be able to collapse", true);
	public static ModConfigKey<bool> bagCollapsing = new("Bag Collapsing", "If bags should be able to collapse", true);

	public static ModConfigKey<int> maxListElementsForAutoCollapse = new("Max List Elements For Auto Collapse", "The maximum amount of list elements before it collapses by default. -1 to never collapse, -2 to always collapse.", 25);
	public static ModConfigKey<int> maxBagElementsForAutoCollapse = new("Max Bag Elements For Auto Collapse", "The maximum amount of bag elements before it collapses by default. -1 to never collapse, -2 to always collapse.", 25);

	public static ModConfigKey<bool> displayIndexWithBlendshape = new("Display Index With Blendshape", "If the index should be displayed with the blendshape name.", false);
	public static ModConfigKey<bool> allowSearchingBlendshapes = new("Allow Seaching For Blendshapes", "Add a search bar in the BlendShapeWeights list UIX", false);

	public static ModConfigKey<bool> enableAddChildrenBuilder = new("Enable Add Children Builder", "Enables a custom UIX panel for quickly creating UIX and Context Menu's", false);
	public static ModConfigKey<bool> childrenBuilderOnlyUIX = new("Add Children Builder - Only UIX", "Allows the Add Children Builder to be active, but only if the slot is under a canvas. requires Add Children Buidler to be enabled.", false);
	public static ModConfigKey<float> buttonMinHeightDefault = new("Button Min Height Default", "The default value for the min height property on the UIX Builder", 0f);

	public static ModConfigKey<bool> variableSpaceForWorkers = new("Variable Space For WorkerInspectors", "If enabled, the variable 'IsWorkerInspector' will be created and set to true for standalone WorkerInspectors", false);


	public static ModConfigKey<colorX> defaultUIXPanelColor = new("Default UIX Panel Color", "This controls the color that is used when creating a blank UIX panel", colorX.DarkGray);

	public static ModConfiguration? Config;

	public static readonly Dictionary<string, Dictionary<string, List<ModConfigKey>>> SortedConfigKeys = new()
	{
		{
			"Inspectors", new()
			{
				{ "Base", [ modifiedInspectorUIX, fieldNameMode, copyComponentsToButtons, nohelp, expandedStringInputs, slotInspectorResetAll, defaultUIXPanelColor, variableSpaceForWorkers ] },
				{ "Components", [ collapseComponents, collapsedComponentColor, expandedComponentColor, modifiedComponentHeaders, dynvarComponentHeaderName ]},
				{ "Lists", [ listCollapsing, maxListElementsForAutoCollapse, bagCollapsing, maxBagElementsForAutoCollapse ]},
				{ "Blendshapes", [ displayIndexWithBlendshape, allowSearchingBlendshapes ] },
				{ "Actions", [ enableAddChildrenBuilder, childrenBuilderOnlyUIX ] }
			}
		}
	};

	public static void GenerateUI(UIBuilder ui)
	{
		new ConfigUIBuilder(instance?.GetConfiguration()).BuildConfigUI(ui, SortedConfigKeys);
	}
	public static void ModSettings_BuildModUi(UIBuilder ui)
	{
		// this is stupid.
		// - The last mod in the list is likely the most recent version from HotReload. if not hotreloaded, will be the main instance.
		var mostRecentMod = ModLoader.Mods().Last(m => m.Name == ModName);
		mostRecentMod.InvokeMethod("GenerateUI", ui);
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
