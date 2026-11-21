#pragma warning disable CS8603 // Possible null reference return.

using System.Collections.Immutable;
using System.Reflection;

using Elements.Core;

using FrooxEngine;
using FrooxEngine.UIX;

using ResoniteModLoader;

// i wrote this code like 5 months ago, just spent almost a whole day just getting it to work here
// i would say its worth it though

namespace LenowoTweeks.Core;

using KeyType = Dictionary<string, Dictionary<string, List<IModConfigItem>>>;

#region Config Types
public interface IModConfigItem { }

public class ModConfigKey(string keyName, string name, string description, Type type, ModConfigurationKey key) : IModConfigItem
{
	private readonly Type defaultType = type;
	public ModConfigurationKey ConfigKey = key;

	public string ConfigName = name;
	public string ConfigKeyName = keyName;
	public string ConfigDescription = description;

	public Type ValueType() => defaultType;

	public virtual void OnConfigExists() { }

	public virtual void BuildField(ConfigUIBuilder builder, UIBuilder ui) { }
	public virtual void SetDynamicVariable(DynamicVariableSpace space, string confKey) { }
}

public class ModConfigKey<T> : ModConfigKey
{
	public ModConfigurationKey<T> TypedConfigKey;
	public T DefaultValue;
	public T Value
	{
		get => TypedConfigKey.Value;
		set
		{
			TypedConfigKey.Value = value;
		}
	}
	public bool ValueDefined = false;

	public ModConfigKey(string name, string description, T defaultValue, string overrideKey = "") :
		base(!string.IsNullOrEmpty(overrideKey) ? overrideKey : name, name, description, typeof(T),
		new ModConfigurationKey<T>(!string.IsNullOrEmpty(overrideKey) ? overrideKey : name, description, () => defaultValue))
	{
		DefaultValue = defaultValue;
		TypedConfigKey = (ModConfigurationKey<T>)ConfigKey!;
	}

	public override void BuildField(ConfigUIBuilder builder, UIBuilder ui)
	{
		builder.BuildGenericField(ui, this);
	}

	public override void SetDynamicVariable(DynamicVariableSpace space, string confKey)
	{
		space.TryWriteValue(confKey, Value);
	}
}

public class ModConfigButton(string name, Action onClicked) : IModConfigItem
{
	public string ButtonLabel = name;

	public Action OnClicked = onClicked;
}

#endregion

#region Config UI Builder
public class ConfigUIBuilder()
{
	public List<LenowoTweak> CurModules = [];
	public Dictionary<LenowoTweak, KeyValuePair<ModConfiguration, KeyType>> mappings = [];
	public Dictionary<ModConfiguration, List<IModConfigItem>> keyMappings = [];

	public List<IModConfigItem> allKeys = [];

	public int currentConfigIndex = 0;

	public ConfigUIBuilder(IEnumerable<LenowoTweak> modules) : this()
	{
		CurModules = [.. modules];
		CurModules.Sort((a, b) => a.ConfigOrder - b.ConfigOrder);
		mappings = CurModules
			.Where(m => m.GetConfig != null)
			.ToDictionary(
				keySelector: (m) => m,
				elementSelector: (m) => new KeyValuePair<ModConfiguration, KeyType>(m.GetConfig, m.GetKeys)
			);
		keyMappings = [];
		foreach (var mapping in mappings)
		{
			var triplelist = mapping.Value.Value.Select(v1 => v1.Value.Select(v2 => v2.Value));
			List<List<IModConfigItem>> doublelist = [];
			foreach (var v in triplelist) doublelist.AddRange(v);
			List<IModConfigItem> singlelist = [];
			foreach (var v in doublelist) singlelist.AddRange(v);
			keyMappings.Add(mapping.Value.Key, singlelist);
		}

		allKeys = [];
		foreach (var key in keyMappings)
		{
			allKeys.AddRange(key.Value);
		}
	}

	public void BuildConfigUI(UIBuilder ui)
	{
		if (mappings.Count == 0) return;

		KeyType mergedConfigs = [];
		foreach (var kv in mappings)
		{
			var keys = kv.Value.Value;
			foreach (var kv2 in keys)
			{
				if (!mergedConfigs.TryGetValue(kv2.Key, out var kv3))
				{
					kv3 = [];
					mergedConfigs.Add(kv2.Key, kv3);
				}
				foreach (var values in kv2.Value)
				{
					if (!kv3.TryGetValue(values.Key, out var kv4))
					{
						kv4 = [];
						kv3.Add(values.Key, kv4);
					}
					kv4.AddRange(values.Value.Where((v) => !kv4.Contains(v)));
				}
			}
		}
		var sortedList = mergedConfigs.ToList();
		sortedList.Sort((a, b) => (a.Key == "Debug Settings" ? 1 : 0) - (b.Key == "Debug Settings" ? 1 : 0));
		var sortedDict = sortedList.ToDictionary();

		currentConfigIndex = 0;

		foreach (var kv in sortedDict)
		{
			BuildSection(ui, kv.Key, kv.Value);
		}
	}

	public void BuildSection(UIBuilder ui, string name, Dictionary<string, List<IModConfigItem>> SubGroups)
	{
		VerticalLayout? sectionLayout = null;
		BuildTitle(ui, name, 48, () =>
		{
			bool sectionActive = !sectionLayout?.Slot.ActiveSelf ?? false;
			sectionLayout?.Slot.ActiveSelf = sectionActive;
		});
		ui.Style.MinHeight = -1;
		sectionLayout = ui.VerticalLayout(4, 0, Alignment.TopLeft, true, false);
		foreach (var kv in SubGroups)
		{
			VerticalLayout? groupLayout = null;
			if (kv.Key != "Base") BuildTitle(ui, kv.Key, 32, () =>
			{
				bool groupActive = !groupLayout?.Slot.ActiveSelf ?? false;
				groupLayout?.Slot.ActiveSelf = groupActive;
			});
			ui.Style.MinHeight = -1;
			groupLayout = ui.VerticalLayout(4, 0, Alignment.TopLeft, true, false);
			foreach (var item in kv.Value)
			{
				if (item is ModConfigKey key)
				{
					key.BuildField(this, ui);
				}
				else if (item is ModConfigButton button)
				{
					BuildButton(ui, button);
				}
				currentConfigIndex++;
			}
			ui.NestOut();
		}
		ui.NestOut();
	}

	public void BuildTitle(UIBuilder ui, string title, float size, Action onClicked)
	{
		RadiantUI_Constants.SetupDefaultStyle(ui);
		ui.Style.MinHeight = size;
		ui.Style.TextAutoSizeMax = size;
		var baseAlignment = ui.Style.TextAlignment;
		ui.Style.TextAlignment = Alignment.TopLeft;
		ui.Spacer(size);
		Slot root = ui.Empty("Title bar");
		ui.NestInto(root);
		var labelName = new LocaleString(title, "{0}", true, true, null);
		var button = ui.Button(labelName);
		button.LocalPressed += (_, _) =>
		{
			onClicked();
		};
		ui.NestOut();
		ui.Style.TextAlignment = baseAlignment;
	}

	// Duplicated from ResoniteModSettings but modified for my own uses

	public const float ITEM_HEIGHT = 24f;

	public void BuildGenericField<T>(UIBuilder ui, ModConfigKey<T> key)
	{
		ModConfiguration? keyConfig = keyMappings.FirstOrDefault(k => k.Value.Contains(key), new()).Key;
		if (keyConfig == null) return;

		bool isType = typeof(T) == typeof(Type);
		if (!(isType || DynamicValueVariable<T>.IsValidGenericType))
		{
			// if a dynvar cannot support it, it probably isnt valid.
			// however, this may allow for list support?
			return;
		}

		string configSlotName = $"{LenowoTweeks_Core.harmonyID}.{key.ConfigKeyName}";
		string configDescription = key.ConfigDescription;

		RadiantUI_Constants.SetupEditorStyle(ui);

		ui.Style.MinHeight = ITEM_HEIGHT;

		Slot root = ui.Empty(configSlotName);

		ui.NestInto(root);

		SyncField<T> syncField;

		FieldInfo? fieldInfo = null;


		if (!isType)
		{
			var dynvar = root.AttachComponent<DynamicValueVariable<T>>();
			dynvar.VariableName.Value = $"Config/{configSlotName}";

			syncField = dynvar.Value;
			fieldInfo ??= dynvar.GetSyncMemberFieldInfo(4);
		}
		else
		{
			var dynvar = root.AttachComponent<DynamicReferenceVariable<SyncType>>();
			dynvar.VariableName.Value = $"Config/{configSlotName}";

			var typeField = root.AttachComponent<TypeField>();
			dynvar.Reference.TrySet(typeField.Type);

			syncField = typeField.Type as SyncField<T>;
			fieldInfo ??= typeField.GetSyncMemberFieldInfo(3);
		}

		var initialValue = key.Value;

		syncField.Value = initialValue;
		syncField.OnValueChange += (syncF) => HandleConfigFieldChange(syncF, keyConfig, key);

		// Validate the value changes
		// LocalFilter changes the value passed to InternalSetValue
		syncField.LocalFilter = (value, field) => ValidateConfigField(value, key);


		RadiantUI_Constants.SetupDefaultStyle(ui);
		ui.Style.TextAutoSizeMax = 24f;

		// Build ui

		ui.Image(new ColorHSL((currentConfigIndex + 1) / 10f % 1, 0.8f, 0.1f, 0.5f));
		ui.HorizontalElementWithLabel<Component>(key.ConfigName, 0.55f, () =>
		{// Using HorizontalElementWithLabel because it formats nicer than SyncMemberEditorBuilder with text
		 // Get first split, then Text in that split
			Slot nameSlot = ui.Root.Parent[0][0];

			ui.HorizontalLayout(4f, childAlignment: Alignment.MiddleLeft).ForceExpandHeight.Value = false;
			ui.Style.FlexibleWidth = 10f;

			SyncMemberEditorBuilder.Build(syncField, null, fieldInfo, ui, 0f); // Using null for name makes it skip generating text
			ui.Style.FlexibleWidth = -1f;

			var memberActions = ui.Root[0]?.GetComponentInChildren<InspectorMemberActions>()?.Slot;
			if (memberActions != null && typeof(T) == typeof(dummy))
			{
				memberActions.Destroy();
			}
			if (memberActions != null && nameSlot != null && typeof(T) != typeof(dummy))
			{
				// Prevent desktop user getting stuck with context menu open
				var vrSync = memberActions.AttachComponent<DynamicValueVariableDriver<bool>>();
				vrSync.Target.TrySet(memberActions.ActiveSelf_Field);
				vrSync.VariableName.Value = "vr_active";

				memberActions.Parent = nameSlot.Parent;
				memberActions.OrderOffset = -1;

				var layout = memberActions.AttachComponent<LayoutElement>();

				layout.PreferredHeight.Value = ITEM_HEIGHT;
				layout.MinHeight.Value = ITEM_HEIGHT;
				layout.MinWidth.Value = ITEM_HEIGHT;

				nameSlot.CopyComponent(layout);

				var horizontal = nameSlot.Parent.AttachComponent<HorizontalLayout>();
				horizontal.Spacing.Value = 4f;
				horizontal.HorizontalAlign.Value = LayoutHorizontalAlignment.Left;
				horizontal.ForceExpand = false;

				nameSlot.AttachComponent<Button>();
				nameSlot.AttachComponent<FieldDriveReceiver<T>>().TryAssignField(syncField);
				nameSlot.AttachComponent<ValueReceiver<T>>().TryAssignField(syncField);

				//((IValueFieldProxySource)memberActions.AttachComponent<ValueFieldProxySource<T>>()).Field = syncField;
			}

			// Update the root layout element so I don't need to do checks for every field size
			var fieldElement = ui.Root[0]?.GetComponent<LayoutElement>();
			if (fieldElement != null)
			{
				// account for user's config value
				float diff = ITEM_HEIGHT / 24f;
				fieldElement.MinHeight.Value *= diff;

				root.GetComponent<LayoutElement>().MinHeight.Value = fieldElement.MinHeight.Value;


				// go over nested elements and apply new size
				var layouts = fieldElement.Slot.GetComponentsInChildren<LayoutElement>(element => element.MinHeight.Value == 24f);
				foreach (LayoutElement layout in layouts)
				{
					layout.MinHeight.Value = ITEM_HEIGHT;
				}
			}

			ui.NestOut();

			return null;
		});
		ui.NestOut();
		ui.NestInto(ui.Empty("Description"));
		ui.Style.TextAlignment = Alignment.MiddleLeft;
		ui.Style.TextAutoSizeMax = 16;
		ui.Text(key.ConfigDescription);

		ui.Style.MinHeight = -1f;
		ui.NestOut();
	}
	private T ValidateConfigField<T>(T value, ModConfigKey<T> configKey)
	{
		bool isValid = false;

		try
		{
			isValid = configKey.TypedConfigKey.Validate(value);
		} catch
		{
			//optionsRoot.LocalUser.IsDirectlyInteracting()
		}

		if (!isValid)
		{ // Fallback if validation fails
			return configKey.Value; // Set to old value if is set Else set to default for that value
		}
		return value;
	}
	private void HandleConfigFieldChange<T>(SyncField<T> syncField, ModConfiguration modConfiguration, ModConfigKey<T> configKey)
	{
		bool isSet = modConfiguration.TryGetValue(configKey.TypedConfigKey, out T configValue);
		if (isSet && (Equals(configValue, syncField.Value) || !Equals(syncField.Value, syncField.Value)))
		{
			configKey.Value = configValue;
			return; // Skip if new value is unmodified or is logically inconsistent (self != self)
		}

		try
		{
			if (!configKey.TypedConfigKey.Validate(syncField.Value)) return;
		} catch { return; }

		configKey.Value = syncField.Value;

		modConfiguration.Save(true);
	}

	public void BuildButton(UIBuilder ui, ModConfigButton button)
	{
		ui.PushStyle();
		ui.Style.MinHeight = 24;
		var element = ui.Button(button.ButtonLabel);
		element.LocalPressed += (_, _) =>
		{
			button.OnClicked();
			RefreshConfig(element.Slot);
		};
		ui.PopStyle();
	}

	public void RefreshConfig(Slot slot)
	{
		var space = slot.FindSpace("Config");
		foreach (var key in allKeys)
		{
			if (key is ModConfigKey k)
			{
				string configName = $"{LenowoTweeks_Core.harmonyID}.{k.ConfigKeyName}";
				k.SetDynamicVariable(space, configName);
			}
		}
	}
}
#endregion
