using ResoniteModLoader;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Text.RegularExpressions;

using System.Globalization;




#if DEBUG
using ResoniteHotReloadLib;
#endif

namespace LenowoTweeks.ConfigUpdater;

public class LenowoTweeks_ConfigUpdater : ResoniteMod
{
	internal const string VERSION_CONSTANT = "2.1.1";
	const string ModName = "Lenowo Tweeks (Config Updater)";
	public override string Name => ModName;
	public override string Author => "Rosa";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/bobjbo/Lenowo-Tweeks-by-Lenowo";

	static LenowoTweeks_ConfigUpdater()
	{
		CheckForConfig();
	}
	public override void OnEngineInit()
	{

#if DEBUG
		HotReloader.RegisterForHotReload(this);
#endif

	}

	public static void CheckForConfig()
	{
		string basePath = Path.Combine(Directory.GetCurrentDirectory(), "rml_config");
		string fullOriginalPath = Path.Combine(basePath, "LenowoTweeks.json");
		if (!File.Exists(fullOriginalPath)) return;
		{
			using StreamReader file = File.OpenText(fullOriginalPath);
			using JsonTextReader reader = new(file);
			JObject json = JObject.Load(reader);
			JObject allValues = (JObject)json.GetValue("values");

			JObject CoreConfig = [];
			JObject GeneralConfig = [];
			JObject InspectorConfig = [];
			JObject ProtoFluxConfig = [];
			JObject LooseConfig = []; // any non-matching fields are put here

			// the fun part: parsing and splitting
			foreach (var item in allValues)
			{
				string name = item.Key;
				JToken value = item.Value;

				string fixedName = name;

				fixedName = fixedName.Replace("_", " ");

				fixedName = Regex.Replace(fixedName, @"(?<=[a-z])(?=[A-Z])", " ");
				fixedName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(fixedName);
				fixedName = fixedName.Replace("Uix", "UIX");
				fixedName = fixedName.Replace("Ui", "UI");
				fixedName = fixedName.Replace("Gooberprint", "GooberPrint");

				switch (name)
				{
					case "ensure_config_space":
					case "primary_ui_color":
					case "secondary_ui_color":
						CoreConfig.Add(fixedName, value);
						break;

					case "valuefield_dropping_mode":
					case "worldSearchGlobalTags":
						GeneralConfig.Add(fixedName, value);
						break;

					case "nohelp":
						InspectorConfig.Add("No Help", value);
						ProtoFluxConfig.Add("No Help", value);
						break;

					case "modifiedInspectorUIX":
					case "expandedStringInputs":
					case "slotInspectorResetAll":
					case "default_uix_panel_color":
					case "collapseComponents":
					case "collapsedComponentColor":
					case "expanded_component_color":
					case "modified_component_headers":
					case "dynvar_component_header_name":
					case "listCollapsing":
					case "maxListElementsForAutoCollapse":
					case "enableAddChildrenBuilder":
						InspectorConfig.Add(fixedName, value);
						break;

					case "disableRelayBackground":
					case "expandedProtofluxStringInputs":
					case "disable_physical_interaction":
					case "initialize_protoflux_globals":
					case "rename_dynamic_variable_sources":
					case "protofluxEditableNames":
					case "allProtofluxEditableNames":
					case "allow_gooberprint_unpack":
					case "gooberprint_unpack_-_greedy_mode":
					case "nodeQuickInspect":
					case "useCustomProtofluxConnections":
					case "allow_flux_visuals_override":
					case "wireImageWrapMode":
					case "wireTextureFilterMode":
					case "wireConnectorFlipUV":
					case "wire_texture_color":
					case "connector_texture_color":
					case "customProtofluxWireUIX":
					case "customProtofluxConnectorUIX":
					case "customProtofluxEmptyConnectorUIX":
					case "collapsible_protoflux_nodes":
					case "protoflux_collapse_threshold":
					case "wiremanager_awake_update_delay":
						ProtoFluxConfig.Add(fixedName, value);
						break;

					case "threedprotoflux":
						ProtoFluxConfig.Add("3D Protoflux", value);
						break;

					default:
						LooseConfig.Add(fixedName, value);
						break;
				}
			}

			string corePath = Path.Combine(basePath, "LenowoTweeks.Core.json");
			string generalPath = Path.Combine(basePath, "LenowoTweeks.General.json");
			string inspectorsPath = Path.Combine(basePath, "LenowoTweeks.Inspectors.json");
			string protofluxPath = Path.Combine(basePath, "LenowoTweeks.ProtoFlux.json");
			string loosePath = Path.Combine(basePath, "LenowoTweeks.Private.json");

			File.WriteAllText(corePath, $"{{\"version\": \"1.0.0\",\n\"values\": \n{CoreConfig.ToString()}\n}}");
			File.WriteAllText(generalPath, $"{{\"version\": \"1.0.0\",\n\"values\": \n{GeneralConfig.ToString()}\n}}");
			File.WriteAllText(inspectorsPath, $"{{\"version\": \"1.0.0\",\n\"values\": \n{InspectorConfig.ToString()}\n}}");
			File.WriteAllText(protofluxPath, $"{{\"version\": \"1.0.0\",\n\"values\": \n{ProtoFluxConfig.ToString()}\n}}");
			File.WriteAllText(loosePath, $"{{\"version\": \"1.0.0\",\n\"values\": \n{LooseConfig.ToString()}\n}}");

			// make sure to actually close the file!
			// (this may not be needed as `using` should clean it up itself and this is inside a block)
			// (but just to be safe, and to be sure the file can be deleted)
			reader.Close();
			file.Close();
		}

		File.Delete(fullOriginalPath);
	}

#if DEBUG
	static void BeforeHotReload()
	{
	}
	static void OnHotReload(ResoniteMod modInstance)
	{
	}
#endif
}
