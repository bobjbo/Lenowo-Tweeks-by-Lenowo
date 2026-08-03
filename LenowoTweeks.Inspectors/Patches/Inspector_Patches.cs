using Elements.Assets;
using Elements.Core;

using FrooxEngine;
using FrooxEngine.UIX;
using FrooxEngine.Undo;

using HarmonyLib;

using LenowoTweeks.Core;

using ProtoFlux.Core;

namespace LenowoTweeks.Inspectors.Patches;

[HarmonyPatch]
public class WorkerInspector_Patches
{

	[HarmonyPrefix]
	[HarmonyPatch(typeof(DevCreateNewForm), "BuildBlankUI")]
	public static bool BuildBlankUI(UIBuilder ui)
	{
		ui.Canvas.Size.Value = new float2(1920f, 1080f);
		ui.Panel(LenowoTweeks_Inspectors.defaultUIXPanelColor.Value, zwrite: true);
		return false;
	}

	public static colorX GetContrastingColor(colorX input)
	{
		var HSV = new ColorHSV(input);
		return HSV.V > .5f ? new colorX(0, 0, 0, 1) : new colorX(1, 1, 1, 1);
	}

	public static void SetUIColor(UIBuilder ui, colorX color)
	{
		ui.Style.TextColor = GetContrastingColor(color);
		ui.Style.ButtonColor = color;
	}


	[HarmonyPrefix]
	[HarmonyPatch(typeof(SceneInspector), "OnAddChildPressed")]
	public static bool TweekedOnAddChildPressed(SceneInspector __instance, ButtonEventData eventData)
	{
		SyncRef<Slot> ComponentView = __instance.ComponentView;

		if (!LenowoTweeks_Inspectors.enableAddChildrenBuilder.Value && !LenowoTweeks_Inspectors.childrenBuilderOnlyUIX.Value) return true;
		if (ComponentView.Target == null) return true;
		if (LenowoTweeks_Inspectors.childrenBuilderOnlyUIX.Value)
		{
			if (ComponentView.Target.GetComponentInParents<Canvas>() == null) return true;
		}

		Slot PanelRoot = __instance.LocalUserSpace.AddSlot("Add Child Dialog - " + __instance.LocalUser.UserName, false);
		PanelRoot.LocalScale *= 0.0008f;
		PanelRoot.GlobalPosition = eventData.globalPoint + __instance.Slot.Backward * 0.05f + __instance.Slot.Down * (250f * __instance.Slot.GlobalScale.Y);
		PanelRoot.GlobalRotation = __instance.Slot.GlobalRotation;
		UIBuilder ui = RadiantUI_Panel.SetupPanel(PanelRoot, "Add Child", new float2(500f, 500f), true, true);
		RadiantUI_Constants.SetupEditorStyle(ui);

		SetUIColor(ui, LenowoTweeks_Core.secondaryUIColor.Value);

		Slot UIVerticalLayout = ui.VerticalLayout(5, 10, Alignment.TopCenter, true, false).Slot;
		UIVerticalLayout.Parent.AttachComponent<Mask>();
		UIVerticalLayout.AttachComponent<ScrollRect>();
		UIVerticalLayout.AttachComponent<ContentSizeFitter>().VerticalFit.Value = SizeFit.MinSize;

		ui.Style.MinHeight = 50;

		LoadMainPage(ui, UIVerticalLayout, ComponentView, PanelRoot);

		return false;
	}


	public static void LoadMainPage(UIBuilder ui, Slot UIVerticalLayout, SyncRef<Slot> ComponentView, Slot PanelRoot)
	{
		SetUIColor(ui, LenowoTweeks_Core.primaryUIColor.Value);
		UIVerticalLayout.DestroyChildren();
		var button = ui.Button("Add Child");

		button.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			ComponentView.Target.AddSlot(ComponentView.Target.Name + " - Child").CreateSpawnUndoPoint();
			PanelRoot.Destroy();
		};

		if (ComponentView.Target.GetComponentInParents<Canvas>() == null)
		{
			var canvasButton = ui.Button("Add Canvas");

			canvasButton.IsPressed.OnValueChange += field =>
			{
				if (!field.Value) return;
				Slot newCanvas = ComponentView.Target.AddSlot("Canvas");
				newCanvas.LocalScale = new float3(0.0008f, 0.0008f, 0.0008f);
				newCanvas.CreateSpawnUndoPoint();
				newCanvas.AttachComponent<Canvas>();
				Image newImage = newCanvas.AddSlot("Image").AttachComponent<Image>();
				newImage.Material.Target = PanelRoot.World.RootSlot.GetComponent<UI_UnlitMaterial>();
				newImage.Tint.Value = LenowoTweeks_Inspectors.defaultUIXPanelColor.Value;
				PanelRoot.Destroy();
				ComponentView.Target = newCanvas;
			};
		}

		if (ComponentView.Target.GetComponentInParents<Canvas>() == null)
		{
			SetUIColor(ui, LenowoTweeks_Core.secondaryUIColor.Value);
			var ContextMenuBuilderFolder = ui.Button("Context Menu Builder");

			ContextMenuBuilderFolder.IsPressed.OnValueChange += field =>
			{
				if (!field.Value) return;
				LoadContextMenuBuilder(ui, UIVerticalLayout, ComponentView, PanelRoot);
			};
		}



		if (ComponentView.Target.GetComponentInParents<Canvas>() != null)
		{
			var emptyUIX = ui.Button("Add Empty UIX Slot");

			emptyUIX.IsPressed.OnValueChange += field =>
			{
				if (!field.Value) return;
				Slot newPanel = ComponentView.Target.AddSlot("Panel");
				newPanel.CreateSpawnUndoPoint();

				newPanel.AttachComponent<RectTransform>();

				PanelRoot.Destroy();
				ComponentView.Target = newPanel;
			};

			var image = ui.Button("Add Image");

			image.IsPressed.OnValueChange += field =>
			{
				if (!field.Value) return;
				Slot newPanel = ComponentView.Target.AddSlot("Image");
				newPanel.CreateSpawnUndoPoint();

				newPanel.AttachComponent<Image>();

				PanelRoot.Destroy();
				ComponentView.Target = newPanel;
			};

			SetUIColor(ui, LenowoTweeks_Core.secondaryUIColor.Value);
			var UIXBuilderFolder = ui.Button("UIX Builder");

			UIXBuilderFolder.IsPressed.OnValueChange += field =>
			{
				if (!field.Value) return;
				LoadUIXBuilder(ui, UIVerticalLayout, ComponentView, PanelRoot);
			};

		}
		ui.NestOut();
	}

	public static void LoadContextMenuBuilder(UIBuilder ui, Slot UIVerticalLayout, SyncRef<Slot> ComponentView, Slot PanelRoot)
	{
		ui.NestInto(UIVerticalLayout);
		UIVerticalLayout.DestroyChildren();

		ui.Style.TextColor = colorX.Black;
		var backToMain = ui.Button("Back");
		backToMain.Slot.GetComponent<Image>().Tint.Value = new colorX(1, 0, 0, 1);

		backToMain.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			ui.NestInto(UIVerticalLayout);
			LoadMainPage(ui, UIVerticalLayout, ComponentView, PanelRoot);
		};

		SetUIColor(ui, LenowoTweeks_Core.secondaryUIColor.Value);
		var ComponentSubmenu = ui.Button("Components");


		ComponentSubmenu.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			LoadContextComponentBuilder(ui, UIVerticalLayout, ComponentView, PanelRoot);
		};

		SetUIColor(ui, LenowoTweeks_Core.primaryUIColor.Value);
		if (ComponentView.Target.GetComponent<RootContextMenuItem>() == null)
		{
			var contextButton = ui.Button("Add Root Context Menu Item");

			contextButton.IsPressed.OnValueChange += field =>
			{
				if (!field.Value) return;
				Slot newButton = ComponentView.Target.AddSlot("Root Context Menu Item");
				newButton.CreateSpawnUndoPoint();

				var rootContext = newButton.AttachComponent<RootContextMenuItem>();

				var menuItem = newButton.AttachComponent<ContextMenuItemSource>();
				menuItem.LabelText = newButton.Name;

				rootContext.Item.Target = menuItem;
				PanelRoot.Destroy();
				ComponentView.Target = newButton;
			};
		}

		var noncontextButton = ui.Button("Add Context Menu Item");

		noncontextButton.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			Slot newButton = ComponentView.Target.AddSlot("Context Menu Item");
			newButton.CreateSpawnUndoPoint();

			var menuItem = newButton.AttachComponent<ContextMenuItemSource>();
			menuItem.LabelText = newButton.Name;

			PanelRoot.Destroy();
			ComponentView.Target = newButton;
		};

		var subMenuButton = ui.Button("Add Context Sub Menu");

		subMenuButton.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			Slot newButton = ComponentView.Target.AddSlot("Context Sub Menu");
			newButton.CreateSpawnUndoPoint();

			var menuItem = newButton.AttachComponent<ContextMenuItemSource>();
			menuItem.LabelText = newButton.Name;

			newButton.AttachComponent<ContextMenuSubmenu>().ItemsRoot.Target = newButton;

			if (ComponentView.Target.GetComponent<ContextMenuItemSource>() == null)
			{
				newButton.AttachComponent<RootContextMenuItem>().Item.Target = menuItem;
			}

			PanelRoot.Destroy();
			ComponentView.Target = newButton;
		};

		if (ComponentView.Target.GetComponent<ContextMenuItemSource>() != null)
		{
			var backButton = ui.Button("Add Context Back Button");

			backButton.IsPressed.OnValueChange += field =>
			{
				if (!field.Value) return;
				Slot newButton = ComponentView.Target.AddSlot("Back");
				newButton.CreateSpawnUndoPoint();

				var menuItem = newButton.AttachComponent<ContextMenuItemSource>();
				menuItem.LabelText = newButton.Name;
				menuItem.Color.Value = new colorX(1, 0, 0, 1);

				newButton.AttachComponent<ContextMenuSubmenu>().ItemsRoot.Target = ComponentView.Target.Parent;

				PanelRoot.Destroy();
				ComponentView.Target = newButton;
			};

			
		}
	}

	public static void LoadContextComponentBuilder(UIBuilder ui, Slot UIVerticalLayout, SyncRef<Slot> ComponentView, Slot PanelRoot)
	{
		ui.NestInto(UIVerticalLayout);
		UIVerticalLayout.DestroyChildren();

		ui.Style.TextColor = colorX.Black;
		var backToMain = ui.Button("Back");
		backToMain.Slot.GetComponent<Image>().Tint.Value = new colorX(1, 0, 0, 1);

		backToMain.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			ui.NestInto(UIVerticalLayout);
			LoadContextMenuBuilder(ui, UIVerticalLayout, ComponentView, PanelRoot);
		};
		
		SetUIColor(ui, LenowoTweeks_Core.primaryUIColor.Value);

		var SpriteProviderButton = ui.Button("Sprite Provider");

		SpriteProviderButton.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			AddFeature(ui, UIVerticalLayout, ComponentView, PanelRoot, "Sprite Provider");
		};

		var OptionDriver = ui.Button("Option Description Driver");

		OptionDriver.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			LoadBuilder(ui, UIVerticalLayout, ComponentView, PanelRoot, "Option Description Driver");
		};
	}

	public static void LoadUIXBuilder(UIBuilder ui, Slot UIVerticalLayout, SyncRef<Slot> ComponentView, Slot PanelRoot)
	{
		ui.NestInto(UIVerticalLayout);
		UIVerticalLayout.DestroyChildren();

		ui.Style.TextColor = colorX.Black;
		var backToMain = ui.Button("Back");
		backToMain.Slot.GetComponent<Image>().Tint.Value = new colorX(1, 0, 0, 1);

		backToMain.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			ui.NestInto(UIVerticalLayout);
			LoadMainPage(ui, UIVerticalLayout, ComponentView, PanelRoot);
		};

		SetUIColor(ui, LenowoTweeks_Core.secondaryUIColor.Value);
		var layoutBuilderSubmenu = ui.Button("Layout Builder");


		layoutBuilderSubmenu.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			LoadLayoutBuilder(ui, UIVerticalLayout, ComponentView, PanelRoot);
		};

		var fieldsBuilder = ui.Button("Field Builder");


		fieldsBuilder.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			LoadFieldBuilder(ui, UIVerticalLayout, ComponentView, PanelRoot);
		};

		var componentBuilder = ui.Button("Components");


		componentBuilder.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			LoadComponentAdder(ui, UIVerticalLayout, ComponentView, PanelRoot);
		};


		SetUIColor(ui, LenowoTweeks_Core.primaryUIColor.Value);

		var scrollAreasSubmenu = ui.Button("Scroll Area");

		scrollAreasSubmenu.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			LoadBuilder(ui, UIVerticalLayout, ComponentView, PanelRoot, "Scroll Rect");
		};

		var buttonButton = ui.Button("Button");

		buttonButton.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			LoadBuilder(ui, UIVerticalLayout, ComponentView, PanelRoot, "Button");
		};

		var textButton = ui.Button("Text");

		textButton.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			LoadBuilder(ui, UIVerticalLayout, ComponentView, PanelRoot, "Text");
		};

		var maskButton = ui.Button("Mask");

		maskButton.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			LoadBuilder(ui, UIVerticalLayout, ComponentView, PanelRoot, "Mask");
		};


		ui.NestOut();
	}

	public static void LoadComponentAdder(UIBuilder ui, Slot UIVerticalLayout, SyncRef<Slot> ComponentView, Slot PanelRoot)
	{
		UIVerticalLayout.DestroyChildren();
		ui.NestInto(UIVerticalLayout);

		ui.Style.TextColor = colorX.Black;
		var backToUIXBuilder = ui.Button("Back");
		backToUIXBuilder.Slot.GetComponent<Image>().Tint.Value = new colorX(1, 0, 0, 1);

		SetUIColor(ui, LenowoTweeks_Core.primaryUIColor.Value);

		backToUIXBuilder.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			ui.NestInto(UIVerticalLayout);
			LoadUIXBuilder(ui, UIVerticalLayout, ComponentView, PanelRoot);
		};

		var LayoutElementButton = ui.Button("Layout Element");

		LayoutElementButton.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			AddFeature(ui, UIVerticalLayout, ComponentView, PanelRoot, "Layout Element");
		};

		var SpriteProviderButton = ui.Button("Sprite Provider");

		SpriteProviderButton.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			AddFeature(ui, UIVerticalLayout, ComponentView, PanelRoot, "Sprite Provider");
		};

		var GradientImageButton = ui.Button("Gradient Image");

		GradientImageButton.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			AddFeature(ui, UIVerticalLayout, ComponentView, PanelRoot, "Gradient Image");
		};
	}

	public static void LoadLayoutBuilder(UIBuilder ui, Slot UIVerticalLayout, SyncRef<Slot> ComponentView, Slot PanelRoot)
	{
		UIVerticalLayout.DestroyChildren();
		ui.NestInto(UIVerticalLayout);

		var backToUIXBuilder = ui.Button("Back");
		backToUIXBuilder.Slot.GetComponent<Image>().Tint.Value = new colorX(1, 0, 0, 1);

		backToUIXBuilder.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			ui.NestInto(UIVerticalLayout);
			LoadUIXBuilder(ui, UIVerticalLayout, ComponentView, PanelRoot);
		};

		SetUIColor(ui, LenowoTweeks_Core.primaryUIColor.Value);

		var VerticalLayoutButton = ui.Button("Vertical Layout");

		VerticalLayoutButton.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			ui.NestInto(UIVerticalLayout);
			LoadBuilder(ui, UIVerticalLayout, ComponentView, PanelRoot, "Vertical Layout");
		};

		var HorizontalLayoutButton = ui.Button("Horizontal Layout");

		HorizontalLayoutButton.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			ui.NestInto(UIVerticalLayout);
			LoadBuilder(ui, UIVerticalLayout, ComponentView, PanelRoot, "Horizontal Layout");
		};

		var OverlappingLayoutButton = ui.Button("Overlapping Layout");

		OverlappingLayoutButton.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			ui.NestInto(UIVerticalLayout);
			LoadBuilder(ui, UIVerticalLayout, ComponentView, PanelRoot, "Overlapping Layout");
		};

	}

	public static void LoadFieldBuilder(UIBuilder ui, Slot UIVerticalLayout, SyncRef<Slot> ComponentView, Slot PanelRoot)
	{
		UIVerticalLayout.DestroyChildren();
		ui.NestInto(UIVerticalLayout);

		ui.Style.TextColor = colorX.Black;
		var backToUIXBuilder = ui.Button("Back");
		backToUIXBuilder.Slot.GetComponent<Image>().Tint.Value = new colorX(1, 0, 0, 1);

		backToUIXBuilder.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			ui.NestInto(UIVerticalLayout);
			LoadUIXBuilder(ui, UIVerticalLayout, ComponentView, PanelRoot);
		};

		SetUIColor(ui, LenowoTweeks_Core.primaryUIColor.Value);

		var VerticalLayoutButton = ui.Button("Text Field");

		VerticalLayoutButton.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			ui.NestInto(UIVerticalLayout);
			LoadBuilder(ui, UIVerticalLayout, ComponentView, PanelRoot, "String Field");
		};

		var HorizontalLayoutButton = ui.Button("Bool Field");

		HorizontalLayoutButton.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			ui.NestInto(UIVerticalLayout);
			LoadBuilder(ui, UIVerticalLayout, ComponentView, PanelRoot, "Bool Field");
		};

		var OverlappingLayoutButton = ui.Button("Float Field");

		OverlappingLayoutButton.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			ui.NestInto(UIVerticalLayout);
			LoadBuilder(ui, UIVerticalLayout, ComponentView, PanelRoot, "Float Field");
		};

		var FloatSlider = ui.Button("Slider");

		FloatSlider.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			ui.NestInto(UIVerticalLayout);
			LoadBuilder(ui, UIVerticalLayout, ComponentView, PanelRoot, "Slider");
		};

		var ReferenceField = ui.Button("Reference Field");

		ReferenceField.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			ui.NestInto(UIVerticalLayout);
			LoadBuilder(ui, UIVerticalLayout, ComponentView, PanelRoot, "Reference Field");
		};

	}



	public static void LoadBuilder(UIBuilder ui, Slot UIVerticalLayout, SyncRef<Slot> ComponentView, Slot PanelRoot, string builderType)
	{
		UIVerticalLayout.DestroyChildren();
		ui.NestInto(UIVerticalLayout);

		ui.Style.TextColor = colorX.Black;
		var backToLLayouts = ui.Button("Back");
		backToLLayouts.Slot.GetComponent<Image>().Tint.Value = new colorX(1, 0, 0, 1);

		backToLLayouts.IsPressed.OnValueChange += field =>
		{
			if (!field.Value) return;
			ui.NestInto(UIVerticalLayout);
			if (builderType == "Vertical Layout" || builderType == "Horizontal Layout" || builderType == "Overlapping Layout")
			{
				LoadLayoutBuilder(ui, UIVerticalLayout, ComponentView, PanelRoot);
			}
			else if (builderType == "Button" || builderType == "Text" || builderType == "Mask" || builderType == "Scroll Rect")
			{
				LoadUIXBuilder(ui, UIVerticalLayout, ComponentView, PanelRoot);
			}
			else if (builderType == "String Field" || builderType == "Bool Field" || builderType == "Float Field" || builderType == "Slider" || builderType == "Reference Field")
			{
				LoadFieldBuilder(ui, UIVerticalLayout, ComponentView, PanelRoot);
			}
			else if (builderType == "Option Description Driver")
			{
				LoadContextComponentBuilder(ui, UIVerticalLayout, ComponentView, PanelRoot);
			}
		};

		CreateHeader(ui, builderType);

		if (builderType == "Vertical Layout" || builderType == "Horizontal Layout" || builderType == "Overlapping Layout")
		{
			CreateFloatFieldWithText(ui, "Padding", 1);
			ui.NestInto(UIVerticalLayout);

			if (builderType != "Overlapping Layout")
			{
				CreateFloatFieldWithText(ui, "Spacing", 5);
				ui.NestInto(UIVerticalLayout);
			}

			CreateBoolFieldWithText(ui, "Force Expand Width", true);
			ui.NestInto(UIVerticalLayout);
			CreateBoolFieldWithText(ui, "Force Expand Height", true);
			ui.NestInto(UIVerticalLayout);
			CreateEnumFieldWithText<LayoutVerticalAlignment>(ui, "Vertical Alignment", LayoutVerticalAlignment.Middle);
			ui.NestInto(UIVerticalLayout);
			CreateEnumFieldWithText<LayoutHorizontalAlignment>(ui, "Horizontal Alignment", LayoutHorizontalAlignment.Center);
			ui.NestInto(UIVerticalLayout);
			CreateBuildButton(ui, UIVerticalLayout, ComponentView, PanelRoot, builderType);
		}
		else if (builderType == "Scroll Rect")
		{
			ui.NestInto(UIVerticalLayout);
			CreateEnumFieldWithText<LayoutVerticalAlignment>(ui, "Vertical Alignment", LayoutVerticalAlignment.Top);
			ui.NestInto(UIVerticalLayout);
			CreateEnumFieldWithText<LayoutHorizontalAlignment>(ui, "Horizontal Alignment", LayoutHorizontalAlignment.Left);
			ui.NestInto(UIVerticalLayout);
			CreateEnumFieldWithText<SizeFit>(ui, "Horizontal Fit", SizeFit.Disabled);
			ui.NestInto(UIVerticalLayout);
			CreateEnumFieldWithText<SizeFit>(ui, "Vertical Fit", SizeFit.MinSize);
			ui.NestInto(UIVerticalLayout);
			CreateFloatFieldWithText(ui, "Padding", 1);
			ui.NestInto(UIVerticalLayout);
			CreateFloatFieldWithText(ui, "Spacing", 5);
			ui.NestInto(UIVerticalLayout);
			CreateBuildButton(ui, UIVerticalLayout, ComponentView, PanelRoot, builderType);
		}
		else if (builderType == "Button")
		{
			CreateFloatFieldWithText(ui, "Min Height", LenowoTweeks_Inspectors.buttonMinHeightDefault.Value);
			ui.NestInto(UIVerticalLayout);
			CreateFloatFieldWithText(ui, "Min Width", -1);
			ui.NestInto(UIVerticalLayout);
			CreateStringFieldWithText(ui, "Text", "Button");
			ui.NestInto(UIVerticalLayout);
			CreateBuildButton(ui, UIVerticalLayout, ComponentView, PanelRoot, builderType);
		}
		else if (builderType == "String Field" || builderType == "Bool Field" || builderType == "Float Field" || builderType == "Slider" || builderType == "Reference Field")
		{
			CreateFloatFieldWithText(ui, "Min Height", LenowoTweeks_Inspectors.buttonMinHeightDefault.Value);
			ui.NestInto(UIVerticalLayout);
			if (builderType == "Slider")
			{
				ui.NestInto(UIVerticalLayout);
				CreateFloatFieldWithText(ui, "Min", 0);
				ui.NestInto(UIVerticalLayout);
				CreateFloatFieldWithText(ui, "Max", 5);
			}
			ui.NestInto(UIVerticalLayout);
			CreateBuildButton(ui, UIVerticalLayout, ComponentView, PanelRoot, builderType);
		}
		else if (builderType == "Text")
		{
			CreateFloatFieldWithText(ui, "Min Height", LenowoTweeks_Inspectors.buttonMinHeightDefault.Value);
			ui.NestInto(UIVerticalLayout);
			CreateFloatFieldWithText(ui, "Min Width", -1);
			ui.NestInto(UIVerticalLayout);
			CreateStringFieldWithText(ui, "Text", "Text");
			ui.NestInto(UIVerticalLayout);
			CreateBuildButton(ui, UIVerticalLayout, ComponentView, PanelRoot, builderType);
		}
		else if (builderType == "Mask")
		{
			CreateBoolFieldWithText(ui, "Show Mash Graphic", false);
			ui.NestInto(UIVerticalLayout);
			CreateBuildButton(ui, UIVerticalLayout, ComponentView, PanelRoot, builderType);

		}
		else if (builderType == "Option Description Driver")
		{
			ui.HorizontalLayout(5);
			var boolButton = ui.Button();
			ui.Text("bool").Slot.Parent = boolButton.Slot;
			var intButton = ui.Button();
			ui.Text("int").Slot.Parent = intButton.Slot;
			var floatButton = ui.Button();
			ui.Text("float").Slot.Parent = floatButton.Slot;
			ui.NestInto(UIVerticalLayout);
			var typeField = CreateTypeFieldWithText(ui, "Type", typeof(bool));
			boolButton.IsPressed.OnValueChange += (v) => typeField.Value = typeof(bool);
			intButton.IsPressed.OnValueChange += (v) => typeField.Value = typeof(int);
			floatButton.IsPressed.OnValueChange += (v) => typeField.Value = typeof(float);
			ui.NestInto(UIVerticalLayout);
			CreateBuildButton(ui, UIVerticalLayout, ComponentView, PanelRoot, builderType);
		}


	}

	public static void CreateFloatFieldWithText(UIBuilder ui, string name, float defaultVal)
	{
		var hl = ui.HorizontalLayout(5, 5);
		ui.Text(name);
		var field = ui.FloatField();
		field.ParsedValue.Value = defaultVal;
		var layoutImage = hl.Slot.AttachComponent<Image>();
		layoutImage.Sprite.Target = ui.Style.ButtonSprite;
		layoutImage.NineSliceSizing.Value = NineSliceSizing.FixedSize;
		hl.Slot.Name = name;

	}

	public static void CreateStringFieldWithText(UIBuilder ui, string name, string defaultVal)
	{
		var hl = ui.HorizontalLayout(5, 5);
		ui.Text(name);
		var field = hl.Slot.AttachComponent<ValueField<string>>();
		field.Value.Value = defaultVal;
		Text Text = ui.TextField(field.Value).Slot.FindChild("Text").GetComponent<Text>();

		ValueCopy<string> stringCopy = hl.Slot.AttachComponent<ValueCopy<string>>();
		stringCopy.Target.Target = field.Value;
		stringCopy.Source.Target = Text.Content;

		hl.Slot.AttachComponent<Image>().Sprite.Target = ui.Style.ButtonSprite;
		hl.Slot.GetComponent<Image>().NineSliceSizing.Value = NineSliceSizing.FixedSize;
		hl.Slot.Name = name;

	}

	public static SyncType CreateTypeFieldWithText(UIBuilder ui, string name, Type defaultVal)
	{
		var hl = ui.HorizontalLayout(5, 5);
		ui.Text(name);
		var field = hl.Slot.AttachComponent<TypeField>();
		field.Type.Value = defaultVal;
		SyncMemberEditorBuilder.BuildField(field.Type, null, hl.Slot, null);
		
		hl.Slot.AttachComponent<Image>().Sprite.Target = ui.Style.ButtonSprite;
		hl.Slot.GetComponent<Image>().NineSliceSizing.Value = NineSliceSizing.FixedSize;
		hl.Slot.Name = name;

		return field.Type;
	}

	public static void CreateEnumFieldWithText<T>(UIBuilder ui, string name, T defaultVal)
	{
		var hl = ui.HorizontalLayout(padding: 5);
		ui.Text(name);
		var field = hl.Slot.AttachComponent<ValueField<T>>().Value;
		field.Value = defaultVal;
		EnumMemberEditor Editor = ui.EnumMemberEditor(field);
		Editor.Slot.FindChild("Horizontal Layout").GetComponent<LayoutElement>().MinWidth.Value = 200f;

		Editor.Slot.FindChild("Horizontal Layout").Children.ToList()[1].FindChild("Text").GetComponent<Text>().Color.Value = new colorX(1, 1, 1, 1);

		hl.Slot.AttachComponent<Image>().Sprite.Target = ui.Style.ButtonSprite;
		hl.Slot.GetComponent<Image>().NineSliceSizing.Value = NineSliceSizing.FixedSize;
		hl.Slot.Name = name;

	}

	public static void CreateBoolFieldWithText(UIBuilder ui, string name, bool defaultVal)
	{
		var hl = ui.HorizontalLayout(padding: 5);
		ui.Text(name);
		var field = hl.Slot.AttachComponent<ValueField<bool>>();
		field.Value.Value = defaultVal;

		ui.NestInto(hl.Slot);
		ui.BooleanMemberEditor(field.Value).Slot.FindChild("Panel").FindChild("Image").FindChild("Image").GetComponent<Image>().Tint.Value = new colorX(1, 1, 1, 1);

		hl.Slot.AttachComponent<Image>().Sprite.Target = ui.Style.ButtonSprite;
		hl.Slot.GetComponent<Image>().NineSliceSizing.Value = NineSliceSizing.FixedSize;
		hl.Slot.Name = name;

	}

	public static void CreateHeader(UIBuilder ui, string text)
	{
		var bg = ui.Image();
		bg.Sprite.Target = ui.Style.ButtonSprite;
		bg.NineSliceSizing.Value = NineSliceSizing.FixedSize;
		Text texts = ui.Text(text);
		texts.Slot.Parent = bg.Slot;
		texts.Color.Value = new colorX(0, 0, 0, 1);
	}

	public static void AddFeature(UIBuilder ui, Slot UIVerticalLayout, SyncRef<Slot> ComponentView, Slot PanelRoot, string builderType)
	{
		Slot Slot = ComponentView.Target;

		if (builderType == "Layout Element")
		{
			Slot.AttachComponent<LayoutElement>();
		}
		else if (builderType == "Sprite Provider")
		{
			Slot.AttachComponent<SpriteProvider>();
		}
		else if (builderType == "Gradient Image")
		{
			Slot.AttachComponent<GradientImage>();
		}
		PanelRoot.Destroy();
	}

	public static void CreateBuildButton(UIBuilder ui, Slot UIVerticalLayout, SyncRef<Slot> ComponentView, Slot PanelRoot, string builderType)
	{
		var bg = ui.Button();

		ui.Text("Build!!").Slot.Parent = bg.Slot;

		bg.IsPressed.OnValueChange += f =>
		{
			if (!f.Value) return;
			Slot NewSlot = ComponentView.Target.AddSlot(builderType);

			if (builderType == "Vertical Layout" || builderType == "Horizontal Layout")
			{

				var LayoutType = NewSlot.AttachComponent(builderType == "Horizontal Layout" ? typeof(HorizontalLayout) : typeof(VerticalLayout));
				DirectionalLayout Layout = (DirectionalLayout)LayoutType;

				Layout.SetPadding(UIVerticalLayout.FindChild("Padding").FindChild("Button").GetComponent<FloatTextEditorParser>().ParsedValue.Value);
				Layout.Spacing.Value = UIVerticalLayout.FindChild("Spacing").FindChild("Button").GetComponent<FloatTextEditorParser>().ParsedValue.Value;
				Layout.ForceExpandWidth.Value = UIVerticalLayout.FindChild("Force Expand Width").GetComponent<ValueField<bool>>().Value;
				Layout.ForceExpandHeight.Value = UIVerticalLayout.FindChild("Force Expand Height").GetComponent<ValueField<bool>>().Value;
				Layout.VerticalAlign.Value = UIVerticalLayout.FindChild("Vertical Alignment").GetComponent<ValueField<LayoutVerticalAlignment>>().Value;
				Layout.HorizontalAlign.Value = UIVerticalLayout.FindChild("Horizontal Alignment").GetComponent<ValueField<LayoutHorizontalAlignment>>().Value;
				ComponentView.Target = Layout.Slot;
			}
			else if (builderType == "Overlapping Layout")
			{
				var Layout = NewSlot.AttachComponent<OverlappingLayout>();

				float Padding = UIVerticalLayout.FindChild("Padding").FindChild("Button").GetComponent<FloatTextEditorParser>().ParsedValue.Value;
				Layout.PaddingBottom.Value = Padding;
				Layout.PaddingTop.Value = Padding;
				Layout.PaddingLeft.Value = Padding;
				Layout.PaddingRight.Value = Padding;

				Layout.ForceExpandWidth.Value = UIVerticalLayout.FindChild("Force Expand Width").GetComponent<ValueField<bool>>().Value;
				Layout.ForceExpandHeight.Value = UIVerticalLayout.FindChild("Force Expand Height").GetComponent<ValueField<bool>>().Value;
				Layout.VerticalAlign.Value = UIVerticalLayout.FindChild("Vertical Alignment").GetComponent<ValueField<LayoutVerticalAlignment>>().Value;
				Layout.HorizontalAlign.Value = UIVerticalLayout.FindChild("Horizontal Alignment").GetComponent<ValueField<LayoutHorizontalAlignment>>().Value;
				ComponentView.Target = Layout.Slot;
			}
			else if (builderType == "Scroll Rect")
			{
				NewSlot.AttachComponent<Mask>();
				Slot Content = NewSlot.AddSlot("Content");
				Content.AttachComponent<ScrollRect>();
				Content.AttachComponent<ContentSizeFitter>();

				VerticalLayout VL = Content.AttachComponent<VerticalLayout>();
				VL.SetPadding(UIVerticalLayout.FindChild("Padding").FindChild("Button").GetComponent<FloatTextEditorParser>().ParsedValue.Value);
				VL.Spacing.Value = UIVerticalLayout.FindChild("Spacing").FindChild("Button").GetComponent<FloatTextEditorParser>().ParsedValue.Value;
				VL.ForceExpandWidth.Value = true;
				VL.ForceExpandHeight.Value = false;
				VL.HorizontalAlign.Value = LayoutHorizontalAlignment.Center;
				VL.VerticalAlign.Value = LayoutVerticalAlignment.Top;

				Content.GetComponent<ContentSizeFitter>().VerticalFit.Value = UIVerticalLayout.FindChild("Vertical Fit").GetComponent<ValueField<SizeFit>>().Value;
				Content.GetComponent<ContentSizeFitter>().HorizontalFit.Value = UIVerticalLayout.FindChild("Horizontal Fit").GetComponent<ValueField<SizeFit>>().Value;

				Content.GetComponent<ScrollRect>().HorizontalAlign.Value = UIVerticalLayout.FindChild("Horizontal Alignment").GetComponent<ValueField<LayoutHorizontalAlignment>>().Value;
				Content.GetComponent<ScrollRect>().VerticalAlign.Value = UIVerticalLayout.FindChild("Vertical Alignment").GetComponent<ValueField<LayoutVerticalAlignment>>().Value;
				ComponentView.Target = Content;
			}
			else if (builderType == "Button")
			{
				LenowoTweeks_Inspectors.buttonMinHeightDefault.Value = UIVerticalLayout.FindChild("Min Height").FindChild("Button").GetComponent<FloatTextEditorParser>().ParsedValue.Value;
				Image image = NewSlot.AttachComponent<Image>();
				image.Sprite.Target = ui.Style.ButtonSprite;
				image.NineSliceSizing.Value = NineSliceSizing.FixedSize;
				image.Tint.Value = ui.Style.ButtonColor;
				NewSlot.AttachComponent<Button>();


				Slot Text = NewSlot.AddSlot("Text");
				Text.AttachComponent<Text>().Content.Value = UIVerticalLayout.FindChild("Text").GetComponent<ValueField<string>>().Value;
				Text TextText = Text.GetComponent<Text>();
				TextText.Size.Value = 50f;
				TextText.HorizontalAlign.Value = TextHorizontalAlignment.Center;
				TextText.VerticalAlign.Value = TextVerticalAlignment.Middle;
				TextText.Color.Value = new colorX(1, 1, 1, 1);

				LayoutElement LE = NewSlot.AttachComponent<LayoutElement>();
				LE.MinHeight.Value = UIVerticalLayout.FindChild("Min Height").FindChild("Button").GetComponent<FloatTextEditorParser>().ParsedValue.Value;
				LE.MinWidth.Value = UIVerticalLayout.FindChild("Min Width").FindChild("Button").GetComponent<FloatTextEditorParser>().ParsedValue.Value;
			}
			else if (builderType == "String Field" || builderType == "Bool Field" || builderType == "Float Field" || builderType == "Slider" || builderType == "Reference Field")
			{
				LenowoTweeks_Inspectors.buttonMinHeightDefault.Value = UIVerticalLayout.FindChild("Min Height").FindChild("Button").GetComponent<FloatTextEditorParser>().ParsedValue.Value;

				UIBuilder uitwo = new(NewSlot);

				SetUIColor(ui, LenowoTweeks_Core.secondaryUIColor.Value);

				if (builderType == "Float Field")
				{
					var field = uitwo.FloatField();
					field.Slot.Parent.AttachComponent<LayoutElement>().MinHeight.Value = LenowoTweeks_Inspectors.buttonMinHeightDefault.Value;
				}
				else if (builderType == "Bool Field")
				{
					var feld = NewSlot.AttachComponent<ValueField<bool>>();
					uitwo.NestInto(feld.Slot);
					var field = uitwo.BooleanMemberEditor(feld.Value);
					field.Slot.AttachComponent<LayoutElement>().MinHeight.Value = LenowoTweeks_Inspectors.buttonMinHeightDefault.Value;
					uitwo.NestOut();
				}
				else if (builderType == "String Field")
				{
					var field = uitwo.TextField("Text Field");
					field.Slot.Parent.AttachComponent<LayoutElement>().MinHeight.Value = LenowoTweeks_Inspectors.buttonMinHeightDefault.Value;
				}
				else if (builderType == "Slider")
				{
					var image = NewSlot.AttachComponent<Image>();
					image.Tint.Value = new colorX(0, 0, 0, 1);
					image.Sprite.Target = ui.Style.ButtonSprite;
					image.NineSliceSizing.Value = NineSliceSizing.FixedSize;

					NewSlot.AttachComponent<LayoutElement>().MinHeight.Value = LenowoTweeks_Inspectors.buttonMinHeightDefault.Value;
					var feld = NewSlot.AttachComponent<ValueField<float>>();
					uitwo.SliderMemberEditor(UIVerticalLayout.FindChild("Min").FindChild("Button").GetComponent<FloatTextEditorParser>().ParsedValue.Value, UIVerticalLayout.FindChild("Max").FindChild("Button").GetComponent<FloatTextEditorParser>().ParsedValue.Value, feld.Value);
					feld.Slot.FindChild("Horizontal Layout").FindChild("Button").GetComponent<Image>().Sprite.Target = ui.Style.ButtonSprite;
					feld.Slot.FindChild("Horizontal Layout").FindChild("Button").GetComponent<Image>().NineSliceSizing.Value = NineSliceSizing.FixedSize;
				}
				else if (builderType == "Reference Field")
				{
					var feld = NewSlot.AttachComponent<ReferenceField<Slot>>();
					var field = uitwo.RefMemberEditor(feld.Reference);
					field.Slot.AttachComponent<LayoutElement>().MinHeight.Value = LenowoTweeks_Inspectors.buttonMinHeightDefault.Value;
				}
			}
			else if (builderType == "Text")
			{
				var Text = NewSlot.AttachComponent<Text>();
				Text.Content.Value = UIVerticalLayout.FindChild("Text").GetComponent<ValueField<string>>().Value;
				NewSlot.AttachComponent<LayoutElement>().MinHeight.Value = LenowoTweeks_Inspectors.buttonMinHeightDefault.Value;

			}
			else if (builderType == "Mask")
			{
				var Image = NewSlot.AttachComponent<Image>();
				var Mask = NewSlot.AttachComponent<Mask>();
				Mask.ShowMaskGraphic.Value = UIVerticalLayout.FindChild("Show Mash Graphic").GetComponent<ValueField<bool>>().Value;
			}
			else if (builderType == "Option Description Driver")
			{
				NewSlot.Destroy();
				Type? customType = UIVerticalLayout.FindChild("Type").GetComponent<TypeField>().Type?.Value;
				if (customType == null)
				{
					ComponentView.Target.AttachComponent<ValueOptionDescriptionDriver<bool>>();
				}
				else if (customType.IsEnginePrimitive())
				{
					ComponentView.Target.AttachComponent(typeof(ValueOptionDescriptionDriver<>).MakeGenericType(customType));
				}
				// for some reason this doesnt work
				// not like anyone will be using this anyways, right
				else if (customType is IWorldElement)
				{
					ComponentView.Target.AttachComponent(typeof(ReferenceOptionDescriptionDriver<>).MakeGenericType(customType));
				}
				else
				{
					ComponentView.Target.AttachComponent<ValueOptionDescriptionDriver<bool>>();
				}
			}
			PanelRoot.Destroy();

		};
	}
}
