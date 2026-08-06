using HarmonyLib;

using ResoniteModLoader;

using System.Reflection;

using Elements.Core;

using Renderite.Shared;

using FrooxEngine.UIX;

using LenowoTweeks.Core;



#if DEBUG
using ResoniteHotReloadLib;
#endif

namespace LenowoTweeks.ProtoFlux;

public class LenowoTweeks_ProtoFlux : LenowoTweak
{
	private static Assembly ModAssembly => typeof(LenowoTweeks_ProtoFlux).Assembly;
	internal const string VERSION_CONSTANT = "2.0.1";
	const string ModName = "Lenowo Tweeks (ProtoFlux)";

	public override string ModuleName => "ProtoFlux";
	public override string ModuleVersion => VERSION_CONSTANT;

	public override string Name => ModName;
	public override string Author => "Rosa";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/bobjbo/Lenowo-Tweeks-by-Lenowo";
	public const string harmonyID = "com.Lenowo.LenowoTweeks.ProtoFlux";
	static Harmony harmony = new(harmonyID);

	public static ResoniteMod? instance;

	public static ModConfigKey<bool> nohelp = new("Disable Help Buttons", "disables the help buttons in the context menu", false, "No Help");

	public static ModConfigKey<bool> disableRelayBackground = new("Disable Relay Background", "Disables the background for relays.", false);
	public static ModConfigKey<bool> expandedProtofluxStringInputs = new("Expanded String Inputs (ProtoFlux)", "This toggles if string fields on protoflux should get bigger based on the text in them", false, "Expanded Protoflux String Inputs");
	public static ModConfigKey<bool> disablePhysicalInteraction = new("Disable Physical Interaction", "Disables physical touch on protoflux nodes.", false);

	public static ModConfigKey<bool> initializeProtofluxGlobals = new("Initialize Protoflux Globals", "When enabled, this will try to initialize globals in protoflux, like string inputs for DynamicInputs or booleans in Update", false);


	public static ModConfigKey<bool> protofluxEditableNames = new("Protoflux Editable Node Names", "Allows Protoflux inputs/displays/calls to be renamed using a button", false, "Protoflux Editable Names");
	public static ModConfigKey<bool> allProtofluxEditableNames = new("Allow editing all protoflux names", "Allows ALL Protoflux nodes to be renamed. Requires editable node names to be enabled.", false, "All Protoflux Editable Names");

	public static ModConfigKey<bool> AllowGooberUnpack = new("Allow GooberPrint Unpack", "If the held slot is a GooberPrint packed slot, allow the spawning of a new print and auto unpack rather than normal unpacking", false);
	public static ModConfigKey<bool> GreedyGooberUnpack = new("GooberPrint Unpack - Greedy Mode", "If the held slot is a GooberPrint packed slot, A GPFolder tagged slot, or has a child tagged GPFolder, allow spawning a gooberprint on that slot.", false);

	public static ModConfigKey<bool> InspectNodeShortcut = new("Allow Opening Inspector On Nodes", "If enabled, adds a new context menu item, which will open an inspector on the hovered ProtoFluxNode", false, "Node Quick Inspect");

	public static ModConfigKey<bool> useCustomProtofluxConnections = new("Use Custom Protoflux Connections", "This toggles if protoflux should use the above uris when generating.", false);
	public static ModConfigKey<bool> AllowFluxVisualsOverride = new("Allow Flux Visuals Override", "If enabled, allows you to override the wires/connectors with another users. set in the context menu while hovering a node, clear in the context menu when set", false);

	public static ModConfigKey<TextureWrapMode> wireImageWrapMode = new("Wire Image Wrap Mode", "The wrapping mode to use for the protoflux wire texture", TextureWrapMode.Repeat);
	public static ModConfigKey<TextureFilterMode> wireTextureFilterMode = new("Wire Texture Filter Mode", "The mode to use for the wire texture filtering.", TextureFilterMode.Point);
	public static ModConfigKey<bool> wireConnectorFlipUV = new("Wire Connectors: Flip UV", "Toggles if the protoflux connector visual uses left=output right=input rather than the other way around.", false, "Wire Connector Flip UV");

	public static ModConfigKey<colorX> wireTextureColor = new("Wire Texture Color", "This controls the color to be multiplied with the type color of the wires. white=normal, black=black, red=string normal but int black", new colorX(1f));
	public static ModConfigKey<colorX> connectorTextureColor = new("Connector Texture Color", "This controls the color to be multiplied with the type color of the connectors. white=normal, black=black, red=string normal but int black", new colorX(1f));

	public static ModConfigKey<Uri> customProtofluxWireUIX = new("Custom Protoflux Wire Image", "This url is used to replace the wires on protoflux nodes.", new("resdb:///1199546a9976a6a907aebfd4e4b45663f7559efd007f03e28e93f26773812f99.png"), "Custom Protoflux Wire UIX");
	public static ModConfigKey<Uri> customProtofluxConnectorUIX = new("Custom Protoflux Connector Image", "This url is used to replace the connectors on protoflux nodes.", new("resdb:///b09b97338a59244be33fcf1b3366f23bf823c09dd556d7818b65b79801611b47.png"), "Custom Protoflux Connector UIX");
	public static ModConfigKey<Uri?> customProtofluxEmptyConnectorUIX = new("Custom Protoflux Empty Connector Image", "This url is used to replace the connectors on protoflux nodes when nothing is connected.", null, "Custom Protoflux Empty Connector UIX");


	public static ModConfigKey<bool> collapsibleProtoflux = new("Collapsible Protoflux Nodes", "Allows Protoflux Nodes to be collapsed", false);
	public static ModConfigKey<int> collapseThreshold = new("Protoflux Collapse Threshold", "How many inputs/outputs a node can have before it collapses", 2);
	public static ModConfigKey<int> collapseAwakeDelay = new("WireManager Awake Update Delay", "How many updates to wait before running the onAwake function (this might need to be increased if users are lagging)", 5, "Wire Manager Awake Update Delay");

#if DEBUG
	// any settings for features that are being developed basically

	public static ModConfigKey<bool> threedprotoflux = new("3D Protoflux", "make flux 3d. currently makes flux 2d because i havent started on that part yet (and its also hardcoded to not run)", false);

#endif

	public static ModConfiguration? Config;

	public static readonly Dictionary<string, Dictionary<string, List<ModConfigKey>>> SortedConfigKeys = new()
	{
		{
			"ProtoFlux", new()
			{
				{ "Base", [ disableRelayBackground, expandedProtofluxStringInputs, nohelp, disablePhysicalInteraction, initializeProtofluxGlobals ] },
				{ "Node Renaming", [ protofluxEditableNames, allProtofluxEditableNames ] },
				{ "Context Menu", [ AllowGooberUnpack, GreedyGooberUnpack, InspectNodeShortcut ] },
				{ "Custom Connections", [ useCustomProtofluxConnections, AllowFluxVisualsOverride, wireImageWrapMode, wireTextureFilterMode, wireConnectorFlipUV, wireTextureColor, connectorTextureColor ] },
				{ "Custom Connections - Image URLs", [ customProtofluxWireUIX, customProtofluxConnectorUIX, customProtofluxEmptyConnectorUIX ] },
				{ "Collapsing", [ collapsibleProtoflux, collapseThreshold, collapseAwakeDelay ]}
			}
		},
#if DEBUG
		{
			"Debug Settings", new()
			{
				// mostly for finding debug/dev settings easier
				{ "Base", [ threedprotoflux ]}
			}
		},
#endif
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

		LenowoTweeks_Core.RegisterModule(this);

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
