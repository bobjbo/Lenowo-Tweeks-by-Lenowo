using HarmonyLib;

using ResoniteModLoader;

using System.Reflection;

using FrooxEngine.UIX;

using LenowoTweeks.Core;



#if DEBUG
using ResoniteHotReloadLib;
#endif

namespace LenowoTweeks.General;

public class LenowoTweeks_General : LenowoTweak
{
	private static Assembly ModAssembly => typeof(LenowoTweeks_General).Assembly;
	internal const string VERSION_CONSTANT = "2.0.1";
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

	public static ModConfiguration? Config;

	public static readonly Dictionary<string, Dictionary<string, List<ModConfigKey>>> SortedConfigKeys = new()
	{
		{
			"General", new()
			{
				{ "Base", [ valueFieldDroppingMode, worldSearchGlobalTags ] }
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
