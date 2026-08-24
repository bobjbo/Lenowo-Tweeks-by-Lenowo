#pragma warning disable CS8603 // Possible null reference return.

using System.Reflection;

using Elements.Core;

using FrooxEngine;
using FrooxEngine.UIX;

using ResoniteModLoader;

// i wrote this code like 5 months ago, just spent almost a whole day just getting it to work here
// i would say its worth it though

namespace LenowoTweeks.Core;

#region Config Types
public class ModConfigKey
{
	private readonly Type defaultType;
	public ModConfigurationKey ConfigKey;

	public string ConfigName;
	public string ConfigKeyName;
	public string ConfigDescription;

	public ModConfigKey(string keyName, string name, string description, Type type, ModConfigurationKey key)
	{
		defaultType = type;
		ConfigKey = key;
		ConfigName = name;
		ConfigKeyName = keyName;
		ConfigDescription = description;
	}

	public Type ValueType() => defaultType;

	public virtual void OnConfigExists() { }

	public virtual void BuildField(ConfigUIBuilder builder, UIBuilder ui) { }
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
}
#endregion

#region Config UI Builder
public class ConfigUIBuilder()
{
	public ModConfiguration? ThisConfig;

	public int currentConfigIndex = 0;

	public ConfigUIBuilder(ModConfiguration? config) : this()
	{
		ThisConfig = config;
	}

	public void BuildConfigUI(UIBuilder ui, Dictionary<string, Dictionary<string, List<ModConfigKey>>> configKeys)
	{
		if (ThisConfig == null) return;

		currentConfigIndex = 0;

		foreach (var kv in configKeys)
		{
			BuildSection(ui, kv.Key, kv.Value);
		}
	}

	public void BuildSection(UIBuilder ui, string name, Dictionary<string, List<ModConfigKey>> SubGroups)
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
				item.BuildField(this, ui);
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
		if (ThisConfig == null) return; // even though we already do this, the linter doesnt know that. this just tells it that ThisConfig is never null here.

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
		syncField.OnValueChange += (syncF) => HandleConfigFieldChange(syncF, ThisConfig, key);

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

		// prevent a null string from existing
		if (value is string v2 && v2 == null) value = (T)(object)"";

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
		var curVal = syncField.Value;
		// prevent a null string from existing
		if (curVal is string v2 && v2 == null) curVal = (T)(object)"";
		if (isSet && (Equals(configValue, curVal) || !Equals(curVal, curVal)))
		{
			configKey.Value = configValue;
			return; // Skip if new value is unmodified or is logically inconsistent (self != self)
		}

		try
		{
			if (!configKey.TypedConfigKey.Validate(curVal)) return;
		} catch { return; }

		configKey.Value = curVal;

		modConfiguration.Save(true);
	}
}
#endregion
