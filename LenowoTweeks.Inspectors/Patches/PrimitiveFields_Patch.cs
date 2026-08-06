using System.Globalization;
using System.Reflection;

using Elements.Core;

using FrooxEngine;
using FrooxEngine.ProtoFlux;
using FrooxEngine.UIX;

using HarmonyLib;

namespace LenowoTweeks.Inspectors.Patches;

public enum FieldNameMode
{
	Normal,
	FieldName,
	Funny
}

[HarmonyPatch]
public class PrimitiveFields_Patch
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(PrimitiveMemberEditor), "BuildUI")]
	public static void Postfix(PrimitiveMemberEditor __instance, SyncRef<TextEditor> ____textEditor, RelayRef<IField> ____target)
	{
		Slot textEditorSlot = ____textEditor.Target.Slot;
		if (__instance.World.IsUserspace())
		{
			var parentDynSpace = __instance.Slot.GetComponentInParents<DynamicVariableSpace>();
			if (parentDynSpace != null && parentDynSpace.SpaceName.Value == "Config") return;
		}
		if (__instance.Slot.GetComponentInParents<ProtoFluxNodeVisual>() != null) return;

		Slot Panel = textEditorSlot.Parent.Parent.Parent; // The parent panel of the button aka the fucking field

		try
		{
			if (LenowoTweeks_Inspectors.modifiedInspectorUIX.Value)
			{
				var valueType = ____target.Target.ValueType;
				if (valueType == typeof(colorX))
				{
					return;
					//Panel = Panel.Parent.Parent;
				}
				if (__instance.Slot.Parent.Parent.GetComponent<MemberEditor>() != null) return;
				if (__instance.Slot.GetComponent<NullableMemberEditor>() != null) return;
				if (valueType.IsMatrixType())
				{
					Panel = Panel.Parent;
				}

				RectTransform Right = Panel.FindChildInHierarchy("Right").GetComponent<RectTransform>();

				Right.AnchorMin.Value = new float2(0.2f, 0);
				Right.AnchorMax.Value = new float2(0.8f, 1);
				Right.Slot.Parent.GetComponent<Image>().Tint.Value = ____target.Target.ValueType.GetTypeColor();
				Panel.FindChildInHierarchy("Left").Destroy();
				Panel.FindChild("Text").GetComponent<RectTransform>().AnchorMin.Value = new float2(0.01f, 0);
			}
		} catch { }

		if (!LenowoTweeks_Inspectors.expandedStringInputs.Value) return;

		if (____target.Target.ValueType == typeof(string) || ____target.Target.ValueType == typeof(Uri))
		{
			Text text = ____textEditor.Target.Text.Target as Text;
			text.Size.Value = 16f;
			text.HorizontalAutoSize.Value = false;
			text.VerticalAutoSize.Value = false;
			OverlappingLayout overlap = textEditorSlot.GetComponentOrAttach<OverlappingLayout>();
			overlap.PaddingBottom.Value = 2f;
			overlap.PaddingTop.Value = 2f;
			overlap.PaddingLeft.Value = 2f;
			overlap.PaddingRight.Value = 2f;

			Panel.GetComponentOrAttach<HorizontalLayout>().ForceExpandWidth.Value = false;
			Panel.GetComponentOrAttach<HorizontalLayout>().Spacing.Value = 4;
			Panel.GetComponent<LayoutElement>().MinHeight.Value = -1;

			string Text = LenowoTweeks_Inspectors.fieldNameMode.Value switch
			{
				FieldNameMode.Normal => "Content",
				FieldNameMode.FieldName => ____target.Target.Name,
				FieldNameMode.Funny => "Hi if you're reading this, you must be very vere lost. Don't worry friend! the way out of this hiearchy is just above me!",
				_ => "Content"
			};

			Slot ContentHolder = Panel.AddSlot(Text);
			ContentHolder.GetComponentOrAttach<VerticalLayout>();

			textEditorSlot.Parent.Parent.Parent = ContentHolder;

			Slot FieldName = Panel.FindChild("Text");
			FieldName.Parent = ContentHolder;
			FieldName.OrderOffset = -1;

			LayoutElement FieldLayoutElement = FieldName.GetComponentOrAttach<LayoutElement>();
			FieldLayoutElement.MinWidth.Value = 40;
			FieldLayoutElement.FlexibleWidth.Value = 0;
			FieldLayoutElement.MinHeight.Value = 24;

			textEditorSlot.GetComponent<LayoutElement>().MinHeight.Value = 24;

			textEditorSlot.GetComponentOrAttach<ContentSizeFitter>().VerticalFit.Value = SizeFit.PreferredSize;
			textEditorSlot.Parent.GetComponentOrAttach<ContentSizeFitter>().VerticalFit.Value = SizeFit.PreferredSize;
			textEditorSlot.Parent.Parent.GetComponentOrAttach<OverlappingLayout>();
			textEditorSlot.Parent.Parent.GetComponentOrAttach<ContentSizeFitter>().VerticalFit.Value = SizeFit.PreferredSize;
			textEditorSlot.Parent.Parent.Parent.GetComponentOrAttach<ContentSizeFitter>().VerticalFit.Value = SizeFit.MinSize;
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(BooleanMemberEditor), "BuildUI")]
	public static void BooleanPostfix(BooleanMemberEditor __instance, SyncRef<Button> ____button, RelayRef<IField> ____target)
	{
		if (!LenowoTweeks_Inspectors.modifiedInspectorUIX.Value) return;
		if (__instance.World.IsUserspace())
		{
			var parentDynSpace = __instance.Slot.GetComponentInParents<DynamicVariableSpace>();
			if (parentDynSpace != null && parentDynSpace.SpaceName.Value == "Config") return;
		}
		if (__instance.Slot.GetComponentInParents<ProtoFluxNodeVisual>() != null)
		{
			return;
		}

		if (__instance.Slot.GetComponentInParents<WorkerInspector>() == null)
		{
			return;
		}

		Slot FieldButton = ____button.Slot;
		Slot Panel = FieldButton.Parent.Parent; // The parent panel of the button aka the fucking field

		string Text = LenowoTweeks_Inspectors.fieldNameMode.Value switch
		{
			FieldNameMode.Normal => "",
			FieldNameMode.FieldName => ____target.Target.Name,
			FieldNameMode.Funny => "HaHa get booleaned nerd",
			_ => ""
		};

		if (!string.IsNullOrEmpty(Text)) Panel.Name = Text;

		try
		{
			var Right = Panel.FindChildInHierarchy("Right").GetComponent<RectTransform>();
			Right.AnchorMin.Value = new float2(0.2f, 0);
			Right.AnchorMax.Value = new float2(0.8f, 1);
			Right.Slot.Parent.GetComponent<Image>().Tint.Value = ____target.Target.ValueType.GetTypeColor();
			Panel.FindChild("Text").GetComponent<RectTransform>().AnchorMin.Value = new float2(0.01f, 0);


			Panel.FindChildInHierarchy("Left").Destroy();
		} catch { }

	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(QuaternionMemberEditor), "BuildUI")]
	public static void QuatragidyPostfix(QuaternionMemberEditor __instance, RelayRef<IField> ____target)
	{
		if (!LenowoTweeks_Inspectors.modifiedInspectorUIX.Value) return;
		if (__instance.World.IsUserspace())
		{
			var parentDynSpace = __instance.Slot.GetComponentInParents<DynamicVariableSpace>();
			if (parentDynSpace != null && parentDynSpace.SpaceName.Value == "Config") return;
		}
		if (__instance.Slot.GetComponentInParents<ProtoFluxNodeVisual>() != null)
		{
			return;
		}
		if (__instance.Slot.GetComponentInParents<WorkerInspector>() == null)
		{
			return;
		}

		Slot Panel = __instance.Slot.Parent.Parent;

		string Text = LenowoTweeks_Inspectors.fieldNameMode.Value switch
		{
			FieldNameMode.Normal => "",
			FieldNameMode.FieldName => ____target.Target.Name,
			FieldNameMode.Funny => "NINE ELEVEN",
			_ => ""
		};

		if (!string.IsNullOrEmpty(Text)) Panel.Name = Text;

		try
		{
			var Right = Panel.FindChildInHierarchy("Right").GetComponent<RectTransform>();
			Right.AnchorMin.Value = new float2(0.2f, 0);
			Right.AnchorMax.Value = new float2(0.8f, 1);
			Right.Slot.Parent.GetComponent<Image>().Tint.Value = ____target.Target.ValueType.GetTypeColor();
			Panel.FindChild("Text").GetComponent<RectTransform>().AnchorMin.Value = new float2(0.01f, 0);

			Panel.FindChildInHierarchy("Left").Destroy();
		} catch { }
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(EnumMemberEditor), "BuildUI")]
	public static void EatemPostfix(EnumMemberEditor __instance, RelayRef<IField> ____target)
	{
		if (!LenowoTweeks_Inspectors.modifiedInspectorUIX.Value) return;
		if (__instance.World.IsUserspace())
		{
			var parentDynSpace = __instance.Slot.GetComponentInParents<DynamicVariableSpace>();
			if (parentDynSpace != null && parentDynSpace.SpaceName.Value == "Config") return;
		}
		if (__instance.Slot.GetComponentInParents<ProtoFluxNodeVisual>() != null)
		{
			return;
		}
		if (__instance.Slot.GetComponentInParents<WorkerInspector>() == null)
		{
			return;
		}

		Slot Panel = __instance.Slot.Parent.Parent;
		if (Panel.GetComponent<MemberEditor>() != null) return;

		string Text = LenowoTweeks_Inspectors.fieldNameMode.Value switch
		{
			FieldNameMode.Normal => "",
			FieldNameMode.FieldName => ____target.Target.Name,
			FieldNameMode.Funny => "Eats you",
			_ => ""
		};

		if (!string.IsNullOrEmpty(Text)) Panel.Name = Text;

		try
		{
			var Right = Panel.FindChildInHierarchy("Right").GetComponent<RectTransform>();
			Right.AnchorMin.Value = new float2(0.2f, 0);
			Right.AnchorMax.Value = new float2(0.8f, 1);
			Right.Slot.Parent.GetComponent<Image>().Tint.Value = ____target.Target.ValueType.GetTypeColor();
			Panel.FindChild("Text").GetComponent<RectTransform>().AnchorMin.Value = new float2(0.01f, 0);

			Panel.FindChildInHierarchy("Left").Destroy();
		} catch { }
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(SliderMemberEditor), "BuildUI")]
	public static void SlidePostfix(SliderMemberEditor __instance, RelayRef<IField> ____target)
	{
		if (!LenowoTweeks_Inspectors.modifiedInspectorUIX.Value) return;
		if (__instance.World.IsUserspace())
		{
			var parentDynSpace = __instance.Slot.GetComponentInParents<DynamicVariableSpace>();
			if (parentDynSpace != null && parentDynSpace.SpaceName.Value == "Config") return;
		}
		if (__instance.Slot.GetComponentInParents<WorkerInspector>() == null)
		{
			return;
		}

		Slot Panel = __instance.Slot.Parent.Parent;

		string Text = LenowoTweeks_Inspectors.fieldNameMode.Value switch
		{
			FieldNameMode.Normal => "",
			FieldNameMode.FieldName => ____target.Target.Name,
			FieldNameMode.Funny => "YOOOO THIS SICK ASS SLIDEEEEEEE",
			_ => ""
		};

		if (!string.IsNullOrEmpty(Text)) Panel.Name = Text;

		try
		{
			var Right = Panel.FindChildInHierarchy("Right").GetComponent<RectTransform>();
			Right.AnchorMin.Value = new float2(0.2f, 0);
			Right.AnchorMax.Value = new float2(0.8f, 1);
			Right.Slot.Parent.GetComponent<Image>().Tint.Value = ____target.Target.ValueType.GetTypeColor();
			Panel.FindChild("Text").GetComponent<RectTransform>().AnchorMin.Value = new float2(0.01f, 0);

			Panel.FindChildInHierarchy("Left").Destroy();
		} catch { }
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(RefEditor), "Setup", argumentTypes: [typeof(ISyncRef), typeof(UIBuilder)])]
	public static void RefPostfix(RefEditor __instance, RelayRef<ISyncRef> ____targetRef)
	{
		if (!LenowoTweeks_Inspectors.modifiedInspectorUIX.Value) return;
		if (__instance.World.IsUserspace())
		{
			var parentDynSpace = __instance.Slot.GetComponentInParents<DynamicVariableSpace>();
			if (parentDynSpace != null && parentDynSpace.SpaceName.Value == "Config") return;
		}
		if (__instance.Slot.GetComponentInParents<ProtoFluxNodeVisual>() != null)
		{
			return;
		}
		if (__instance.Slot.GetComponentInParents<WorkerInspector>() == null)
		{
			return;
		}

		Slot Panel = __instance.Slot.Parent;

		string Text = LenowoTweeks_Inspectors.fieldNameMode.Value switch
		{
			FieldNameMode.Normal => "",
			FieldNameMode.FieldName => ____targetRef.Target.Name,
			FieldNameMode.Funny => "ref deez nuts",
			_ => ""
		};

		if (!string.IsNullOrEmpty(Text)) Panel.Name = Text;

		try
		{
			var Right = Panel.FindChildInHierarchy("Right").GetComponent<RectTransform>();
			Right.AnchorMin.Value = new float2(0.2f, 0);
			Right.AnchorMax.Value = new float2(0.8f, 1);
			Right.Slot.Parent.GetComponent<Image>().Tint.Value = ____targetRef.Target.TargetType.GetTypeColor();
			Panel.FindChild("Text").GetComponent<RectTransform>().AnchorMin.Value = new float2(0.01f, 0);

			Panel.FindChildInHierarchy("Left").Destroy();
		} catch { }
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(ColorMemberEditorBase), "BuildUI")]
	public static void ColorPostfix(ColorMemberEditorBase __instance, RelayRef<IField> ____target)
	{
		if (!LenowoTweeks_Inspectors.modifiedInspectorUIX.Value) return;
		if (__instance.World.IsUserspace())
		{
			var parentDynSpace = __instance.Slot.GetComponentInParents<DynamicVariableSpace>();
			if (parentDynSpace != null && parentDynSpace.SpaceName.Value == "Config") return;
		}
		if (__instance.Slot.GetComponentInParents<ProtoFluxNodeVisual>() != null)
		{
			return;
		}
		if (__instance.Slot.GetComponentInParents<WorkerInspector>() == null)
		{
			return;
		}

		Slot Panel = __instance.Slot.Parent.Parent;

		string Text = LenowoTweeks_Inspectors.fieldNameMode.Value switch
		{
			FieldNameMode.Normal => "",
			FieldNameMode.FieldName => ____target.Target.Name,
			FieldNameMode.Funny => "FUCK YOU",
			_ => ""
		};

		if (!string.IsNullOrEmpty(Text)) Panel.Name = Text;

		try
		{
			var Button = Panel.FindChild("Button");
			var Right = Button.FindChild("Right").GetComponent<RectTransform>();
			Right.AnchorMin.Value = new float2(0.2f, 0);
			Right.AnchorMax.Value = new float2(0.8f, 1);
			Right.Slot.Parent.GetComponent<Image>().Tint.Value = ____target.Target.ValueType.GetTypeColor();
			Panel.FindChild("Text").GetComponent<RectTransform>().AnchorMin.Value = new float2(0.01f, 0);

			Button.FindChild("Left").Destroy();
		} catch { }
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(TextureRefEditor), "Setup")]
	public static void TextureRefPostfix(TextureRefEditor __instance, RelayRef<AssetRef<ITexture2D>> ____targetRef)
	{
		if (!LenowoTweeks_Inspectors.modifiedInspectorUIX.Value) return;
		if (__instance.World.IsUserspace())
		{
			var parentDynSpace = __instance.Slot.GetComponentInParents<DynamicVariableSpace>();
			if (parentDynSpace != null && parentDynSpace.SpaceName.Value == "Config") return;
		}
		if (__instance.Slot.GetComponentInParents<WorkerInspector>() == null)
		{
			return;
		}

		Slot Panel = __instance.Slot.Parent;

		string Text = LenowoTweeks_Inspectors.fieldNameMode.Value switch
		{
			FieldNameMode.Normal => "",
			FieldNameMode.FieldName => ____targetRef.Target.Name,
			FieldNameMode.Funny => "picture frame idfk",
			_ => ""
		};

		if (!string.IsNullOrEmpty(Text)) Panel.Name = Text;

		try
		{
			var Right = Panel.FindChildInHierarchy("Right").GetComponent<RectTransform>();
			Right.AnchorMin.Value = new float2(0.2f, 0);
			Right.AnchorMax.Value = new float2(0.8f, 1);
			Right.Slot.Parent.GetComponent<Image>().Tint.Value = ____targetRef.Target.TargetType.GetTypeColor();
			Panel.FindChild("Text").GetComponent<RectTransform>().AnchorMin.Value = new float2(0.01f, 0);

			Panel.FindChildInHierarchy("Left").Destroy();
		} catch { }
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(NullableMemberEditor), "BuildUI")]
	public static void NullablePostfix(NullableMemberEditor __instance, RelayRef<IField> ____target)
	{
		if (!LenowoTweeks_Inspectors.modifiedInspectorUIX.Value) return;
		if (__instance.World.IsUserspace())
		{
			var parentDynSpace = __instance.Slot.GetComponentInParents<DynamicVariableSpace>();
			if (parentDynSpace != null && parentDynSpace.SpaceName.Value == "Config") return;
		}
		Slot editorSlot = __instance.Slot;
		if (editorSlot.GetComponentInParents<ProtoFluxNodeVisual>() != null)
		{
			return;
		}
		if (__instance.Slot.GetComponentInParents<WorkerInspector>() == null)
		{
			return;
		}

		Slot Panel = editorSlot.Parent.Parent;

		string Text = LenowoTweeks_Inspectors.fieldNameMode.Value switch
		{
			FieldNameMode.Normal => "",
			FieldNameMode.FieldName => ____target.Target.Name,
			FieldNameMode.Funny => "<alpha=#88><i>null</i></alpha>",
			_ => ""
		};

		if (!string.IsNullOrEmpty(Text)) Panel.Name = Text;

		var valueType = ____target.Target.ValueType;

		try
		{
			var Button = Panel.FindChild("Button");
			var Right = Button.FindChild("Right").GetComponent<RectTransform>();
			Right.AnchorMin.Value = new float2(0.2f, 0);
			Right.AnchorMax.Value = new float2(0.8f, 1);
			Right.Slot.Parent.GetComponent<Image>().Tint.Value = ____target.Target.ValueType.GetTypeColor();
			Panel.FindChild("Text").GetComponent<RectTransform>().AnchorMin.Value = new float2(0.01f, 0);

			Button.FindChild("Left").Destroy();
		} catch { }

		bool isNullableMatrix = valueType.IsGenericType && valueType.GenericTypeArguments.Last().IsMatrixType();

		if (!isNullableMatrix) return;

		__instance.StartTask(async () =>
		{
			await new Updates(1);
			List<PrimitiveMemberEditor> editors = editorSlot.GetComponents<PrimitiveMemberEditor>();
			List<List<PrimitiveMemberEditor>> grouped = editors.SplitToGroups(MathX.RoundToInt(MathX.Sqrt(editors.Count)));

			Panel.GetComponent<LayoutElement>().MinHeight.Value = 24 + (28 * grouped.Count);
			editorSlot.GetComponent<HorizontalLayout>().Destroy();
			editorSlot.Name = "Vertical Layout";
			var verticalLayout = editorSlot.AttachComponent<VerticalLayout>();
			verticalLayout.Spacing.Value = 4;
			grouped.ForEach((editorRow) =>
			{
				var horizontalSlot = editorSlot.AddSlot("Horizontal Layout");
				horizontalSlot.AttachComponent<HorizontalLayout>().Spacing.Value = 4;
				horizontalSlot.AttachComponent<LayoutElement>().MinHeight.Value = 24;
				var asList = editorRow.ToList();
				asList.ForEach((e) =>
				{
					var traverse = Traverse.Create(e);
					var button = traverse.Field<SyncRef<Button>>("_button").Value.Target;
					button.Slot.Parent = horizontalSlot;
					button.Slot.GetComponent<LayoutElement>().MinHeight.Value = 24;
				});
			});
			editorSlot.Children.Where(s => s.Name == "Text").ToList().ForEach(s => s.Destroy());
		});
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(SyncPlaybackEditor), "Setup")]
	public static void PlaybackPostfix(SyncPlaybackEditor __instance, RelayRef<SyncPlayback> ____playback)
	{
		if (!LenowoTweeks_Inspectors.modifiedInspectorUIX.Value) return;
		if (__instance.World.IsUserspace())
		{
			var parentDynSpace = __instance.Slot.GetComponentInParents<DynamicVariableSpace>();
			if (parentDynSpace != null && parentDynSpace.SpaceName.Value == "Config") return;
		}
		if (__instance.Slot.GetComponentInParents<ProtoFluxNodeVisual>() != null)
		{
			return;
		}
		if (__instance.Slot.GetComponentInParents<WorkerInspector>() == null)
		{
			return;
		}

		Slot Panel = __instance.Slot.Parent;

		string Text = LenowoTweeks_Inspectors.fieldNameMode.Value switch
		{
			FieldNameMode.Normal => "",
			FieldNameMode.FieldName => ____playback.Target.Name,
			FieldNameMode.Funny => "p",
			_ => ""
		};

		if (!string.IsNullOrEmpty(Text)) Panel.Name = Text;

		try
		{
			var Button = Panel.FindChild("Button");
			var Right = Button.FindChild("Right").GetComponent<RectTransform>();
			Right.AnchorMin.Value = new float2(0.2f, 0);
			Right.AnchorMax.Value = new float2(0.8f, 1);
			Right.Slot.Parent.GetComponent<Image>().Tint.Value = typeof(SyncPlayback).GetTypeColor();
			Panel.FindChild("Text").GetComponent<RectTransform>().AnchorMin.Value = new float2(0.01f, 0);

			Button.FindChild("Left").Destroy();
		} catch { }
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(DelegateEditor), "Setup")]
	public static void DelegatePostfix(DelegateEditor __instance, RelayRef<ISyncDelegate> ____targetDelegate)
	{
		if (!LenowoTweeks_Inspectors.modifiedInspectorUIX.Value) return;
		if (__instance.World.IsUserspace())
		{
			var parentDynSpace = __instance.Slot.GetComponentInParents<DynamicVariableSpace>();
			if (parentDynSpace != null && parentDynSpace.SpaceName.Value == "Config") return;
		}
		if (__instance.Slot.GetComponentInParents<ProtoFluxNodeVisual>() != null)
		{
			return;
		}
		if (__instance.Slot.GetComponentInParents<WorkerInspector>() == null)
		{
			return;
		}

		Slot Panel = __instance.Slot.Parent;
		Panel.Name = "deli sausage";

		string Text = LenowoTweeks_Inspectors.fieldNameMode.Value switch
		{
			FieldNameMode.Normal => "",
			FieldNameMode.FieldName => ____targetDelegate.Target.Name,
			FieldNameMode.Funny => "deli sausage",
			_ => ""
		};

		if (!string.IsNullOrEmpty(Text)) Panel.Name = Text;

		try
		{
			var Button = Panel.FindChild("Button");
			var Right = Button.FindChild("Right").GetComponent<RectTransform>();
			Right.AnchorMin.Value = new float2(0.2f, 0);
			Right.AnchorMax.Value = new float2(0.8f, 1);
			Right.Slot.Parent.GetComponent<Image>().Tint.Value = ____targetDelegate.Target.TargetType.GetTypeColor();
			Panel.FindChild("Text").GetComponent<RectTransform>().AnchorMin.Value = new float2(0.01f, 0);

			Button.FindChild("Left").Destroy();
		} catch { }
	}

	private static readonly MethodInfo ListOnChanges = AccessTools.Method(typeof(ListEditor), "OnChanges");

	[HarmonyPostfix]
	[HarmonyPatch(typeof(ListEditor), "Setup")]
	public static void ListCollapseing(ListEditor __instance, SyncRef<ISyncList> ____targetList)
	{
		if (!LenowoTweeks_Inspectors.listCollapsing.Value) return;
		if (__instance.World.IsUserspace())
		{
			var parentDynSpace = __instance.Slot.GetComponentInParents<DynamicVariableSpace>();
			if (parentDynSpace != null && parentDynSpace.SpaceName.Value == "Config") return;
		}
		if (__instance.Slot.GetComponentInParents<WorkerInspector>() == null)
		{
			return;
		}

		Slot Panel = __instance.Slot.Parent;
		Slot Texts = Panel.FindChild("Text");

		ButtonToggle bt = Texts.AttachComponent<ButtonToggle>();
		BooleanValueDriver<string> bvd = Texts.AttachComponent<BooleanValueDriver<string>>();
		Text TextText = Texts.GetComponent<Text>();
		Slot VL = Panel.FindChild("Vertical Layout");
		ValueCopy<bool> vc = VL.AttachComponent<ValueCopy<bool>>();
		ValueCopy<bool> vc2 = Panel.FindChild("Button").AttachComponent<ValueCopy<bool>>();
		string TextTextText = TextText.Content;

		vc.Source.Target = VL.ActiveSelf_Field;
		vc.Target.Target = bvd.State;
		vc2.Source.Target = VL.ActiveSelf_Field;
		vc2.Target.Target = vc2.Slot.ActiveSelf_Field;

		bt.TargetValue.Target = VL.ActiveSelf_Field;
		bvd.TargetField.Target = TextText.Content;
		bvd.FalseValue.Value = TextTextText + " (↑↑↑)";
		bvd.TrueValue.Value = TextTextText + " (↓↓↓)";
		TextText.ParseRichText.Value = true;

		int listItemCount = ____targetList.Target.Count;
		int maxItemCount = LenowoTweeks_Inspectors.maxListElementsForAutoCollapse.Value;
		VL.ActiveSelf = maxItemCount == -1 || listItemCount <= maxItemCount;

		VL.ActiveSelf_Field.OnValueChange += v => { ListOnChanges?.Invoke(__instance, new object[] { }); };

		if (LenowoTweeks_Inspectors.allowSearchingBlendshapes.Value && __instance is BlendshapeWeightListEditor editor)
		{
			Slot parent1 = Texts.Parent[1];
			Slot parent2 =  Texts.Parent[2];
			parent1.OrderOffset = 1;
			parent2.OrderOffset = 2;

			Slot root = Texts.Parent.AddSlot("BlendshapeSearch");
			ValueField<string> field = root.AttachComponent<ValueField<string>>();
			SyncMemberEditorBuilder.BuildField(field.Value, field.GetSyncMemberFieldInfo("Value"), root, null!);
			TextEditor tEditor = root.GetComponentInChildren<TextEditor>();
			tEditor?.FinishHandling.Value = TextEditor.FinishAction.NullOnWhitespace;
			(tEditor?.Text.Target as Text)?.NullContent.Value = "<alpha=#88><i>Search blendshapes</closeall>";

			field.Slot.ActiveSelf_Field.DriveFrom(VL.ActiveSelf_Field);
			field.Value.OnValueChange += content =>
			{
				SkinnedMeshRenderer? meshRenderer;
				if (editor.GetSyncMember("_targetList") is SyncRef<ISyncList> iList && iList.Target is { } list) //editor("_targetList")?.Target != null)
				{
					meshRenderer = (editor.GetSyncMember("_targetSkin") as SyncRef<SkinnedMeshRenderer>)?.Target;
					List<Predicate<string>> scoreIndicators = new List<Predicate<string>>
					{
						text2 => text2.StartsWith(content, false, CultureInfo.CurrentCulture),
						text2 => text2.StartsWith(content, true, CultureInfo.CurrentCulture),
						text2 => text2.EndsWith(content, false, CultureInfo.CurrentCulture),
						text2 => text2.EndsWith(content, true, CultureInfo.CurrentCulture),
						text2 => text2.Contains(content, StringComparison.OrdinalIgnoreCase)
					};
					List<string> members = Pool.BorrowList<string>();
					Dictionary<string, Slot> listLayoutElements = Pool.BorrowDictionary<string, Slot>();
					if (string.IsNullOrWhiteSpace(content))
					{
						for (int i = 0; i < list.Count; i++)
						{
							int idx = i;
							Slot slot = editor.Slot.FindChild(x => x.Tag == BlendshapeName(idx));
							if (slot != null)
							{
								slot.ActiveSelf = true;
								slot.OrderOffset = i;
							}
						}
					}
					else
					{
						for (int i = 0; i < list.Count; i++)
						{
							int idx = i;
							string text2 = BlendshapeName(idx);
							Slot slot = editor.Slot.FindChild(x => x.Tag == text2) ?? editor.Slot[idx];
							bool matchesFilter = FindCondition(text2);
							if (matchesFilter)
							{
								members.Add(text2);
								listLayoutElements[text2] = slot;
							}
							slot.ActiveSelf = matchesFilter;
							slot.Tag = text2;
						}
						if (members.Count > 0)
						{
							ScoreAndSort(members, scoreIndicators);
							for (int i = 0; i < members.Count; i++)
							{
								listLayoutElements[members[i]].OrderOffset = i;
							}
						}
					}
					Pool.Return(ref members);
					Pool.Return(ref listLayoutElements);
				}
				return;

				string BlendshapeName(int index) => meshRenderer?.BlendShapeName(index) ?? index.ToString();

				bool FindCondition(string text2) => text2.Contains(content, StringComparison.OrdinalIgnoreCase);
				
				static void ScoreAndSort(List<string> candidates, List<Predicate<string>> scoreIndicators)
				{
					candidates.Sort(delegate(string a, string b)
					{
						int scoreA = Score(a);
						int scoreB = Score(b);

						return scoreA == scoreB ? string.Compare(a, b, StringComparison.Ordinal) : scoreB.CompareTo(scoreA);

						int Score(string value)
						{
							int score = 0;

							for (int index = 0; index < scoreIndicators.Count; index++)
							{
								try
								{
									if (scoreIndicators[index](value))
									{
										score += 10 * (scoreIndicators.Count - index);
									}
								}
								catch
								{
									// ignored
								}
							}

							return score;
						}
					});
				}
			};
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(BlendshapeWeightListEditor), "GetElementName")]
	public static void BlendShapeListNameChanger(ISyncList list, int index, ref string __result)
	{
		__result = (LenowoTweeks_Inspectors.displayIndexWithBlendshape.Value && __result != index.ToString() ? $"[{index}] " : "") + __result;
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(ListEditor), "OnChanges")]
	public static bool ListNoLoady(ListEditor __instance)
	{
		Slot Panel = __instance.Slot.Parent;
		if (!(Panel.FindChild("Vertical Layout")?.ActiveSelf ?? true))
		{
			return false;
		}

		return true;
	}

	private static readonly MethodInfo BagOnChanges = AccessTools.Method(typeof(BagEditor), "OnChanges");

	[HarmonyPostfix]
	[HarmonyPatch(typeof(BagEditor), "Setup")]
	public static void BagCollapseing(BagEditor __instance, SyncRef<ISyncBag> ____targetBag)
	{
		if (!LenowoTweeks_Inspectors.bagCollapsing.Value) return;
		if (__instance.World.IsUserspace())
		{
			var parentDynSpace = __instance.Slot.GetComponentInParents<DynamicVariableSpace>();
			if (parentDynSpace != null && parentDynSpace.SpaceName.Value == "Config") return;
		}
		if (__instance.Slot.GetComponentInParents<WorkerInspector>() == null)
		{
			return;
		}

		Slot Panel = __instance.Slot.Parent;
		Slot Texts = Panel.FindChild("Text");
		if (Texts == null) return;
		ButtonToggle bt = Texts.AttachComponent<ButtonToggle>();
		BooleanValueDriver<string> bvd = Texts.AttachComponent<BooleanValueDriver<string>>();
		Text TextText = Texts.GetComponent<Text>();
		Button button = Texts.GetComponentOrAttach<Button>(out bool buttonAttached);
		if (buttonAttached)
		{
			var colorDriver = button.ColorDrivers.Add();
			colorDriver.NormalColor.Value = RadiantUI_Constants.TEXT_COLOR;
			colorDriver.HighlightColor.Value = RadiantUI_Constants.LABEL_COLOR;
			colorDriver.PressColor.Value = RadiantUI_Constants.HEADING_COLOR;
			colorDriver.ColorDrive.Target = TextText.Color;
		}
		Slot VL = Panel.FindChild("Vertical Layout");
		ValueCopy<bool> vc = VL.AttachComponent<ValueCopy<bool>>();
		ValueCopy<bool> vc2 = Panel.FindChild("Button").AttachComponent<ValueCopy<bool>>();
		string TextTextText = TextText.Content;

		vc.Source.Target = VL.ActiveSelf_Field;
		vc.Target.Target = bvd.State;
		vc2.Source.Target = VL.ActiveSelf_Field;
		vc2.Target.Target = vc2.Slot.ActiveSelf_Field;

		bt.TargetValue.Target = VL.ActiveSelf_Field;
		bvd.TargetField.Target = TextText.Content;
		bvd.FalseValue.Value = TextTextText + " (↑↑↑)";
		bvd.TrueValue.Value = TextTextText + " (↓↓↓)";
		TextText.ParseRichText.Value = true;

		int bagItemCount = ____targetBag.Target.Count;
		int maxItemCount = LenowoTweeks_Inspectors.maxBagElementsForAutoCollapse.Value;
		VL.ActiveSelf = maxItemCount == -1 || bagItemCount <= maxItemCount;

		VL.ActiveSelf_Field.OnValueChange += v => { BagOnChanges?.Invoke(__instance, new object[] { }); };
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(BagEditor), "OnChanges")]
	public static bool BagNoLoady(BagEditor __instance)
	{
		Slot Panel = __instance.Slot.Parent;
		if (!(Panel.FindChild("Vertical Layout")?.ActiveSelf ?? true))
		{
			return false;
		}

		return true;
	}
}
