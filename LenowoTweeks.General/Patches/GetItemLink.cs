using System.Reflection;

using Elements.Assets;
using Elements.Core;

using FrooxEngine;
using FrooxEngine.Store;
using FrooxEngine.UIX;

using HarmonyLib;

namespace LenowoTweeks.General;

// directly copied from https://github.com/EIA485/NeosGetItemLink/blob/master/GetItemLink/GetItemLink.cs
// mainly here as an rml version of the mod as its pure bepisloader now

[HarmonyPatch(typeof(InventoryBrowser))]
public static class GetItemLinkPatch
{
	static FieldInfo itemInfo = typeof(InventoryItemUI).GetField("Item", BindingFlags.Instance | BindingFlags.NonPublic);
	static FieldInfo directoryInfo = typeof(InventoryItemUI).GetField("Directory", BindingFlags.Instance | BindingFlags.NonPublic);
	const InventoryBrowser.SpecialItemType UniqueSIT = (InventoryBrowser.SpecialItemType)(-1);// doing this so the buttons show up on component init

	const string ButtonsRootName = "GetItemLink Buttons";
	const string GetAssetTag = "Get Asset URI";
	const string GetRecordTag = "Get Record URI";
	const string EditRecordTag = "Edit Record";

	[HarmonyPrefix]
	[HarmonyPatch("OnItemSelected")]
	public static void OnItemSelectedPrefix(ref InventoryBrowser.SpecialItemType __state, Sync<InventoryBrowser.SpecialItemType> ____lastSpecialItemType)
	{
		if (!LenowoTweeks_General.getItemLink.Value) return;
		__state = ____lastSpecialItemType.Value;
	}
	[HarmonyPostfix]
	[HarmonyPatch("OnItemSelected")]
	public static void OnItemSelectedPostfix(InventoryBrowser __instance, BrowserItem currentItem, InventoryBrowser.SpecialItemType __state, SyncRef<Slot> ____buttonsRoot)
	{
		if (!LenowoTweeks_General.getItemLink.Value) return;
		if (__instance.World != Userspace.UserspaceWorld) return;
		if (__state == InventoryBrowser.ClassifyItem(currentItem as InventoryItemUI) || __state != UniqueSIT) return;

		Slot buttonRoot = ____buttonsRoot.Target[0];
		UIBuilder ui = new(buttonRoot);
		RadiantUI_Constants.SetupDefaultStyle(ui);
		var hori = ui.HorizontalLayout(4);
		hori.Slot.Name = ButtonsRootName;

		// Weird workaround to force UIX reflow, otherwise buttons are invisible
		hori.PaddingLeft.Value = 1;
		__instance.RunInUpdates(0, () =>
		{
			hori.PaddingLeft.Value = 0;
		});

		AddButton(
			(IButton button, ButtonEventData eventData) => ItemLink(button, __instance.SelectedInventoryItem, false),
			GetAssetTag, colorX.Purple, OfficialAssets.Graphics.Badges.Cheese,
			ui
		);

		AddButton(
			(IButton button, ButtonEventData eventData) => ItemLink(button, __instance.SelectedInventoryItem, true),
			GetRecordTag, colorX.Brown, OfficialAssets.Graphics.Badges.potato,
			ui
		);

		AddButton(
			(IButton button, ButtonEventData eventData) =>
			{
				RecordEditForm editForm;
				var overlayMngr = __instance.Slot.GetComponentInParents<ModalOverlayManager>();
				if (overlayMngr == null)
				{
					var slot = __instance.LocalUserSpace.AddSlot("Record Edit Form");
					slot.PositionInFrontOfUser(float3.Backward, float3.Right * 0.5f);
					editForm = RecordEditForm.OpenDialogWindow(slot);
				}
				else
				{
					editForm = overlayMngr.OpenModalOverlay(new float2(.25f, .8f), "Edit Record").Slot.AttachComponent<RecordEditForm>();
				}
				Record r = GetRecord(__instance.SelectedInventoryItem);
				if (r == null) return;
				AccessTools.Method(typeof(RecordEditForm), "Setup").Invoke(editForm, new object[] { null, r });
			},
			EditRecordTag, colorX.Orange, OfficialAssets.Graphics.Icons.Dash.Settings,
			ui
		);
	}

	[HarmonyPrefix]
	[HarmonyPatch("OnChanges")]
	public static void OnChangesPrefix(InventoryBrowser __instance, SyncRef<Slot> ____buttonsRoot)
	{
		if (!LenowoTweeks_General.getItemLink.Value) return;
		if (__instance.World != Userspace.UserspaceWorld) return;

		Slot buttonRoot = ____buttonsRoot.Target[0];
		bool enableButtons = __instance.SelectedInventoryItem != null;
		Slot buttons = buttonRoot.FindChild(ButtonsRootName);
		if (buttons == null) return;

		foreach (var child in buttons.Children)
		{
			if (child.Tag == GetAssetTag)
			{
				child.GetComponent<Button>().Enabled = enableButtons && (GetLink(__instance.SelectedInventoryItem, false) != null);
				child[0].GetComponent<Image>().Tint.Value = colorX.Black;
			}
			else if (child.Tag == GetRecordTag)
			{
				child.GetComponent<Button>().Enabled = enableButtons && (GetLink(__instance.SelectedInventoryItem, true) != null);
				child[0].GetComponent<Image>().Tint.Value = colorX.Black;
			}
			else if (child.Tag == EditRecordTag)
			{
				child.GetComponent<Button>().Enabled = enableButtons && GetRecord(__instance.SelectedInventoryItem) != null;
				child[0].GetComponent<Image>().Tint.Value = colorX.Black;
			}
		}
	}

	static List<InventoryBrowser> invBrowserInstances = [];

	[HarmonyPostfix]
	[HarmonyPatch("OnAwake")]
	public static void InitializeSyncMembersPostfix(InventoryBrowser __instance)
	{
		if (__instance.World != Userspace.UserspaceWorld) return;
		invBrowserInstances.Add(__instance);
		SetupConfigChanges();
	}

	public static void SetupConfigChanges()
	{
		LenowoTweeks_General.getItemLink.ConfigKey.OnChanged += GetItemLinkOnChanges;
		GetItemLinkOnChanges(LenowoTweeks_General.getItemLink.Value);
	}

	public static void GetItemLinkOnChanges(object? newValue)
	{
		foreach (var instance in invBrowserInstances)
		{
			if (instance == null) continue;
			if (LenowoTweeks_General.getItemLink.Value)
			{
				Traverse.Create(instance).Field<Sync<InventoryBrowser.SpecialItemType>>("_lastSpecialItemType").Value.Value = UniqueSIT;
			}
			else
			{
				var buttonsRoot = Traverse.Create(instance).Field<SyncRef<Slot>>("_buttonsRoot").Value;
				if (buttonsRoot == null) return;
				if (buttonsRoot.Target == null) return;
				if (buttonsRoot.Target.ChildrenCount == 0) return;
				Slot buttonRoot = buttonsRoot.Target[0];
				if (buttonRoot == null) return;
				Slot buttons = buttonRoot.FindChild(ButtonsRootName);
				buttons?.Destroy();
			}
		}
	}

	public static void AddButton(ButtonEventHandler onPress, string tag, colorX tint, Uri sprite, UIBuilder ui)
	{
		var userButton = ui.Button(sprite, tint);
		var buttonSlot = userButton.Slot;
		buttonSlot.Tag = tag;
		userButton.LocalPressed += onPress;
		userButton.ColorDrivers.RemoveAt(userButton.ColorDrivers.Count - 1);
		buttonSlot[0].GetComponent<Image>().Tint.Value = colorX.Black;

		// https://github.com/Psychpsyo/Tooltippery Support, implemented based on the readme
		buttonSlot.AttachComponent<Comment>().Text.Value = "TooltipperyLabel:" + tag;
	}

	public static void ItemLink(IButton button, InventoryItemUI Item, bool type)
	{
		string link = GetLink(Item, type);
		if (link != null)
		{
			Engine.Current.InputInterface.Clipboard.SetText(link);
			button.Slot[0].GetComponent<Image>().Tint.Value = colorX.White;
		}
		else
		{
			button.Slot[0].GetComponent<Image>().Tint.Value = colorX.Red;
		}
	}

	static Record GetRecord(InventoryItemUI item)
	{
		return (Record)itemInfo.GetValue(item) ?? ((RecordDirectory)directoryInfo.GetValue(item)).EntryRecord;
	}

	static string? GetLink(InventoryItemUI item, bool type)
	{
		Record record = GetRecord(item);
		return type ? record?.GetUrl(Engine.Current.PlatformProfile).ToString() : record?.AssetURI;
	}
}
