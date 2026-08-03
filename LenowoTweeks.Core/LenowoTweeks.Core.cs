using FrooxEngine;

using HarmonyLib;

using ResoniteModLoader;

using System.Reflection;

using Elements.Core;

using FrooxEngine.UIX;



#if DEBUG
using ResoniteHotReloadLib;
#endif

namespace LenowoTweeks.Core;

public class LenowoTweeks_Core : LenowoTweak
{
	private static Assembly ModAssembly => typeof(LenowoTweeks_Core).Assembly;
	internal const string VERSION_CONSTANT = "2.0.1";
	const string ModName = "Lenowo Tweeks (Core)";

	public override string ModuleName => "Core";
	public override string ModuleVersion => VERSION_CONSTANT;

	public override string Name => ModName;
	public override string Author => "Rosa";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/bobjbo/Lenowo-Tweeks-by-Lenowo";
	public const string harmonyID = "com.Lenowo.LenowoTweeks.Core";
	static Harmony harmony = new(harmonyID);

	public static ResoniteMod? instance;

	public static ModConfigKey<bool> ensureConfigSpace = new("Ensure Config Space", "If enabled, will ensure that the 'mod config' slot exists when loading into a world", false);

	public static ModConfigKey<colorX> primaryUIColor = new("Primary UI Color", "Controls the primary color used for any custom UI in this mod", RadiantUI_Constants.Hero.YELLOW);
	public static ModConfigKey<colorX> secondaryUIColor = new("Secondary UI Color", "Controls the primary color used for any custom UI in this mod", RadiantUI_Constants.Hero.ORANGE);


	public static ModConfiguration? Config;

	public static readonly Dictionary<string, Dictionary<string, List<ModConfigKey>>> SortedConfigKeys = new()
	{
		{
			"Mod Specific", new()
			{
				{ "Base", [ ensureConfigSpace, primaryUIColor, secondaryUIColor ] }
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

	public static List<ILenowoModule> RegisteredModules = [];

	public static void RegisterModule(ILenowoModule module)
	{
		if (RegisteredModules.Any(m => m.ModuleName == module.ModuleName))
		{
			RegisteredModules[RegisteredModules.FindIndex(m => m.ModuleName == module.ModuleName)] = module;
		}
		else RegisteredModules.Add(module);
	}
	public static void RegisterModModule(ResoniteMod module)
	{
		if (module.GetType().InheritsFrom(typeof(ILenowoModule)))
		{
			RegisterModule((ILenowoModule)module);
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
