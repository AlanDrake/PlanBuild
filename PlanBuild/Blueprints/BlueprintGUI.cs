﻿using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PlanBuild.Blueprints
{
    internal class BlueprintGUI
    {
        public static BlueprintGUI Instance;

        private GameObject MenuPrefab;
        private GameObject ContainerPrefab;

        public GameObject Window { get; set; }
        public void Toggle(bool shutWindow = false, bool openWindow = false)
        {
            bool newState;

            // Requesting window shut.
            if (shutWindow)
            {
                //Window.SetActive(false);
                newState = false;
            }
            // Requesting open window.
            else if (openWindow)
            {
                //Window.SetActive(true);
                newState = true;
            }
            // Toggle current state
            else
            {
                //Window.SetActive(!Window.activeSelf);
                newState = !Window.activeSelf;
            }
            Window.SetActive(newState);

            // Toggle input
            GUIManager.BlockInput(newState);
        }

        public ActionAppliedOverlay ActionAppliedOverlay { get; set; }

        public BlueprintMenuElements MenuElements { get; set; }

        public BlueprintTab CurrentTab { get; set; }

        public BlueprintTab LocalTab { get; set; } = new BlueprintTab();

        public BlueprintTab ServerTab { get; set; } = new BlueprintTab();

        public static void Init()
        {
            if (!GUIManager.IsHeadless())
            {
                Instance = new BlueprintGUI();
                AssetBundle bundle = AssetUtils.LoadAssetBundleFromResources("blueprintmenuui", typeof(PlanBuildPlugin).Assembly);
                Instance.MenuPrefab = bundle.LoadAsset<GameObject>("BlueprintMenu");
                Instance.ContainerPrefab = bundle.LoadAsset<GameObject>("BPDetailsContainer");
                bundle.Unload(false);

                GUIManager.OnPixelFixCreated += Instance.Register;
            }
        }

        public static bool IsAvailable()
        {
            return Instance != null && Instance.Window != null;
        }

        public void Register()
        {
            if (!Window)
            {
                Jotunn.Logger.LogDebug("Recreating BlueprintGUI");

                // Assigning the main window, so we can disable/enable it as we please.
                Window = UnityEngine.Object.Instantiate(MenuPrefab, GUIManager.PixelFix.transform);

                // Setting some vanilla styles
                foreach (Text txt in Window.GetComponentsInChildren<Text>(true))
                {
                    txt.font = GUIManager.Instance.AveriaSerifBold;
                    if (txt.GetComponentsInParent<InputField>(true) == null)
                    {
                        var outline = txt.gameObject.AddComponent<Outline>();
                        outline.effectColor = Color.black;
                    }
                }

                try
                {
                    RectTransform windowRectTrans = Window.GetComponent<RectTransform>();

                    // The window is positioned in the center of the screen --
                    Vector2 bottomLeftCorner = new Vector2((-1 * (Screen.width / 2)), (-1 * (Screen.height / 2)));
                    Vector2 skillWindowSize = new Vector2(600, 400);
                    Vector2 bottomAlignedWindow = new Vector2(-(skillWindowSize.x / 2), bottomLeftCorner.y + skillWindowSize.y / 2);

                    // Half of the screen, - half of our window, centered position.
                    Vector2 centerOScreen = new Vector2((Screen.width / 2) - (windowRectTrans.rect.size.x / 2), (Screen.height / 2) - (windowRectTrans.rect.size.y / 2));
                    windowRectTrans.anchoredPosition = new Vector2(0, 0);

                    // Simple drag and drop script. -- allows for drag/drop of any ui component.
                    Window.AddComponent<UIDragDrop>();
                    Jotunn.Logger.LogDebug($"BlueprintGUI position was set: {windowRectTrans.anchoredPosition.x}, {windowRectTrans.anchoredPosition.y}");

                    try
                    {
                        ActionAppliedOverlay = new ActionAppliedOverlay();
                        ActionAppliedOverlay.Register(Window.transform.Find("ActionAppliedOverlay"));
                    }
                    catch (Exception ex)
                    {
                        Jotunn.Logger.LogDebug($"Failed in the action overlay: {ex}");
                    }

                    try
                    {
                        MenuElements = new BlueprintMenuElements();
                        MenuElements.CloseButton = Window.transform.Find("CloseButton").GetComponent<Button>();
                    }
                    catch (Exception ex)
                    {
                        Jotunn.Logger.LogDebug($"Failed in the menu elements: {ex}");
                    }

                    try
                    {
                        LocalTab.TabElements.Register(Window.transform, tabName: "MyTab", buttonSearchName: "MyTabButton");
                        LocalTab.ListDisplay.Register(LocalTab.TabElements.TabTransform, ContainerPrefab, BlueprintLocation.Local);
                        LocalTab.DetailDisplay.Register(LocalTab.TabElements.TabTransform, ContainerPrefab, BlueprintLocation.Local);
                        LocalTab.TabElements.TabButton.onClick.AddListener(() =>
                        {
                            CurrentTab = LocalTab;
                        });
                        CurrentTab = LocalTab;
                    }
                    catch (Exception ex)
                    {
                        Jotunn.Logger.LogDebug($"Failed in myTab: {ex}");
                    }

                    try
                    {
                        ServerTab.TabElements.Register(Window.transform, tabName: "ServerTab", buttonSearchName: "ServerTabButton");
                        ServerTab.ListDisplay.Register(ServerTab.TabElements.TabTransform, ContainerPrefab, BlueprintLocation.Server);
                        ServerTab.DetailDisplay.Register(ServerTab.TabElements.TabTransform, ContainerPrefab, BlueprintLocation.Server);
                        ServerTab.TabElements.TabButton.onClick.AddListener(() =>
                        {
                            CurrentTab = ServerTab;
                        });
                    }
                    catch (Exception ex)
                    {
                        Jotunn.Logger.LogDebug($"Failed in ServerTab: {ex}");
                    }

                    // Init blueprint lists
                    ReloadBlueprints(BlueprintLocation.Both);
                }
                catch (Exception ex)
                {
                    Jotunn.Logger.LogDebug($"Failed to load Blueprint Window: {ex}");
                }
                
                // Dont display directly
                Window.SetActive(false);
            }
        }

        /// <summary>
        ///     Loop through the tab displays and DestroyImmediate all <see cref="BlueprintListDisplay"/> instances
        /// </summary>
        /// <param name="location"></param>
        public void ClearBlueprints(BlueprintLocation location)
        {
            if (location == BlueprintLocation.Both || location == BlueprintLocation.Local)
            {
                foreach (var detail in LocalTab.ListDisplay.Blueprints)
                {
                    GameObject.DestroyImmediate(detail.ContentHolder);
                }
                LocalTab.ListDisplay.Blueprints.Clear();
            }
            if (location == BlueprintLocation.Both || location == BlueprintLocation.Server)
            {
                foreach (var detail in ServerTab.ListDisplay.Blueprints)
                {
                    GameObject.DestroyImmediate(detail.ContentHolder);
                }
                ServerTab.ListDisplay.Blueprints.Clear();
            }
        }

        /// <summary>
        ///     Loop through the tab display, clear them and reload from the blueprint dictionary
        /// </summary>
        /// <param name="location"></param>
        public void ReloadBlueprints(BlueprintLocation location)
        {
            ClearBlueprints(location);

            if (location == BlueprintLocation.Both || location == BlueprintLocation.Local)
            {
                foreach (var entry in BlueprintManager.LocalBlueprints)
                {
                    LocalTab.ListDisplay.AddBlueprint(entry.Key, entry.Value);
                }
            }
            if (location == BlueprintLocation.Both || location == BlueprintLocation.Server)
            {
                foreach (var entry in BlueprintManager.ServerBlueprints)
                {
                    ServerTab.ListDisplay.AddBlueprint(entry.Key, entry.Value);
                }
            }
        }

        /// <summary>
        ///     Display the details of a blueprint values on the content side
        /// </summary>
        /// <param name="blueprint"></param>
        /// <param name="tab"></param>
        public static void SetActiveDetails(BlueprintDetailContent blueprint, BlueprintLocation tab)
        {
            BlueprintTab tabToUse = null;
            switch (tab)
            {
                case BlueprintLocation.Local:
                    tabToUse = Instance.LocalTab;
                    break;
                case BlueprintLocation.Server:
                    tabToUse = Instance.ServerTab;
                    break;
                default:
                    break;
            }
            if (tabToUse == null) return;
            tabToUse.DetailDisplay.SetActive(blueprint);
        }

        public static void RefreshBlueprints(BlueprintLocation originTab)
        {
            switch (originTab)
            {
                case BlueprintLocation.Local:
                    // Get the local blueprint list
                    BlueprintSync.GetLocalBlueprints();
                    break;
                case BlueprintLocation.Server:
                    // Get the server blueprint list
                    Instance.ActionAppliedOverlay.Toggle();
                    BlueprintSync.GetServerBlueprints((bool success, string message) =>
                    {
                        Instance.ActionAppliedOverlay.Toggle();
                    }, useCache: false);
                    break;
                default:
                    break;
            }
        }

        public static void SaveBlueprint(BlueprintDetailContent detail, BlueprintLocation originTab)
        {
            switch (originTab)
            {
                case BlueprintLocation.Local:
                    // Save the blueprint changes
                    if (detail != null && BlueprintManager.LocalBlueprints.TryGetValue(detail.ID, out var bplocal))
                    {
                        if (bplocal.Name != detail.Name || bplocal.Description != detail.Description)
                        {
                            bplocal.Name = detail.Name ?? bplocal.Name;
                            bplocal.Description = detail.Description ?? bplocal.Description;
                            BlueprintSync.SaveLocalBlueprint(bplocal.ID);
                        }
                    }
                    break;
                case BlueprintLocation.Server:
                    // Upload the blueprint to the server again to save the changes
                    if (detail != null && BlueprintManager.ServerBlueprints.TryGetValue(detail.ID, out var bpserver))
                    {
                        if (bpserver.Name != detail.Name || bpserver.Description != detail.Description)
                        {
                            bpserver.Name = detail.Name ?? bpserver.Name;
                            bpserver.Description = detail.Description ?? bpserver.Description;
                            
                            Instance.ActionAppliedOverlay.Toggle();
                            BlueprintSync.SaveServerBlueprint(bpserver.ID, (bool success, string message) =>
                            {
                                Instance.ActionAppliedOverlay.Toggle();
                            });
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        public static void TransferBlueprint(BlueprintDetailContent detail, BlueprintLocation originTab)
        {
            switch (originTab)
            {
                case BlueprintLocation.Local:
                    // Push local blueprint to the server
                    if (detail != null && BlueprintManager.LocalBlueprints.ContainsKey(detail.ID))
                    {
                        Instance.ActionAppliedOverlay.Toggle();
                        BlueprintSync.PushBlueprint(detail.ID, (bool success, string message) =>
                        {
                            Instance.ActionAppliedOverlay.Toggle();
                        });
                    }
                    break;
                case BlueprintLocation.Server:
                    // Save server blueprint locally
                    if (detail != null && BlueprintManager.ServerBlueprints.ContainsKey(detail.ID))
                    {
                        BlueprintSync.PullBlueprint(detail.ID);
                    }
                    break;
                default:
                    break;
            }
        }

        public static void DeleteBlueprint(BlueprintDetailContent detail, BlueprintLocation originTab)
        {
            switch (originTab)
            {
                case BlueprintLocation.Local:
                    // Remove local blueprint
                    if (detail != null && BlueprintManager.LocalBlueprints.ContainsKey(detail.ID))
                    {
                        BlueprintManager.LocalBlueprints[detail.ID].Destroy();
                        BlueprintManager.LocalBlueprints.Remove(detail.ID);
                    }
                    break;
                case BlueprintLocation.Server:
                    // TODO: Remove server blueprint when admin
                    break;
                default:
                    break;
            }
        }
    }

    internal class BlueprintMenuElements
    {
        public Button CloseButton { get; set; }
    }

    internal class BlueprintTab
    {
        // Moved things out to seperate classes to make it easier to understand the flow.
        public BlueprintTabElements TabElements { get; set; } = new BlueprintTabElements();

        // Holds Lists of Blueprints within the tab.
        public BlueprintListDisplay ListDisplay { get; set; } = new BlueprintListDisplay();

        // Holds Detail of the selected blueprint.
        public BlueprintDetailDisplay DetailDisplay { get; set; } = new BlueprintDetailDisplay();
    }

    internal class BlueprintTabElements
    {
        public Transform TabTransform { get; set; }
        public Button TabButton { get; set; }
        public Text TabText { get; set; }
        // This is to indicate it is the activate tab.
        public Image TabButtonSelector { get; set; }

        public void Register(Transform window, string tabName, string buttonSearchName)
        {
            try
            {
                TabTransform = window.Find($"{tabName}");
                TabButton = window.Find($"{buttonSearchName}").GetComponent<Button>();
                TabText = window.Find($"{buttonSearchName}/Label").GetComponent<Text>();
                TabButtonSelector = window.Find($"{buttonSearchName}/Enabled").GetComponent<Image>();
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug($"Failed in BlueprintTabElements: {ex}");
            }
        }
    }

    internal class BlueprintListDisplay
    {
        public BlueprintLocation TabType { get; set; } = BlueprintLocation.Local;

        private GameObject BlueprintDetailPrefab { get; set; }

        // Parent for the Content Holder - Where we push new things.
        public Transform ScrollContentParent { get; set; }

        public UIConfirmationOverlay ConfirmationOverlay { get; set; } = new UIConfirmationOverlay();

        // All the blueprints that exist in this tab's list.
        public List<BlueprintDetailContent> Blueprints { get; set; } = new List<BlueprintDetailContent>();

        public BlueprintDetailContent AddBlueprint(string id, Blueprint bp)
        {
            if (Blueprints.Any(i => i.ID == id))
            {
                Jotunn.Logger.LogDebug($"Blueprint {id} already exists in {this}.");
                return null;
            }

            BlueprintDetailContent newBp = new BlueprintDetailContent();
            newBp.ID = id;
            try
            {
                newBp.ContentHolder = UnityEngine.Object.Instantiate(BlueprintDetailPrefab, ScrollContentParent);
                newBp.IconButton = newBp.ContentHolder.transform.Find("IconButton").GetComponent<Button>();
                newBp.Icon = newBp.ContentHolder.transform.Find("IconButton/BPImage").GetComponent<Image>();
                newBp.SortUpButton = newBp.ContentHolder.transform.Find("SortUpButton").GetComponent<Button>();
                newBp.SortDownButton = newBp.ContentHolder.transform.Find("SortDownButton").GetComponent<Button>();
                newBp.Text = newBp.ContentHolder.transform.Find("Text").GetComponent<Text>();
                
                newBp.Name = bp.Name;
                newBp.Creator = bp.Creator;
                newBp.Description = bp.Description;
                newBp.Text.text = bp.ToGUIString();
                if (bp.Thumbnail != null)
                {
                    newBp.Icon.sprite = Sprite.Create(bp.Thumbnail, new Rect(0f, 0f, bp.Thumbnail.width, bp.Thumbnail.height), Vector2.zero);
                }
                newBp.IconButton.onClick.AddListener(() =>
                {
                    BlueprintGUI.SetActiveDetails(newBp, TabType);
                });
                Blueprints.Add(newBp);

                FixSortButtons();
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug($"Failed to load new blueprint: {ex}");
            }
            return newBp;
        }

        public BlueprintDetailContent RemoveBlueprint(string id)
        {
            BlueprintDetailContent blueprintToRemove = Blueprints.FirstOrDefault(i => i.ID == id);
            if (blueprintToRemove != null)
            {
                Blueprints.Remove(blueprintToRemove);
                // I said see-yah later.
                GameObject.Destroy(blueprintToRemove.ContentHolder);
                FixSortButtons();
                return blueprintToRemove;
            }
            return null;
        }

        public void MoveUp(string id)
        {
            BlueprintDetailContent toMoveup = Blueprints.FirstOrDefault(i => i.ID == id);
            if (toMoveup == null) return;

            int indexOfBp = Blueprints.IndexOf(toMoveup);
            if (indexOfBp == 0) return;

            BlueprintDetailContent detailToSwapWith = Blueprints[indexOfBp - 1];
            Swap(toMoveup, detailToSwapWith);
        }

        public void MoveDown(string id)
        {
            BlueprintDetailContent toMoveup = Blueprints.FirstOrDefault(i => i.ID == id);
            if (toMoveup == null) return;

            int indexOfBp = Blueprints.IndexOf(toMoveup);
            if (indexOfBp >= Blueprints.Count) return;

            BlueprintDetailContent detailToSwapWith = Blueprints[indexOfBp + 1];
            Swap(toMoveup, detailToSwapWith);
        }

        private void Swap(BlueprintDetailContent from, BlueprintDetailContent to)
        {
            string idCopy = from.ID;
            Sprite spriteToCopy = from.Icon.sprite;
            string descriptionToCopy = from.Text.text;

            from.ID = to.ID;
            from.Icon.sprite = to.Icon.sprite;
            from.Text.text = to.Text.text;

            from.ID = idCopy;
            from.Icon.sprite = spriteToCopy;
            to.Text.text = descriptionToCopy;
        }

        public void FixSortButtons()
        {
            foreach (var blueprintDetail in Blueprints)
            {
                blueprintDetail.SortUpButton.gameObject.SetActive(true);
                blueprintDetail.SortDownButton.gameObject.SetActive(true);

                // Remove any old listeners
                blueprintDetail.SortDownButton.onClick.RemoveAllListeners();
                blueprintDetail.SortUpButton.onClick.RemoveAllListeners();

                blueprintDetail.SortUpButton.onClick.AddListener(() =>
                {
                    MoveUp(blueprintDetail.ID);
                });

                blueprintDetail.SortDownButton.onClick.AddListener(() =>
                {
                    MoveDown(blueprintDetail.ID);
                });
            }

            BlueprintDetailContent firstPrintDetailContent = Blueprints.FirstOrDefault();
            if (firstPrintDetailContent != null)
            {
                // Set the first to not have sort down (enable the sort down button).
                firstPrintDetailContent.SortUpButton.gameObject.SetActive(false);
            }
            BlueprintDetailContent lastBlueprintDetail = Blueprints.LastOrDefault();
            if (lastBlueprintDetail != null)
            {
                // Set the first to not have sort down (enable the sort down button).
                lastBlueprintDetail.SortDownButton.gameObject.SetActive(false);
            }
        }

        public void Register(Transform tabTrans, GameObject uiBlueprintDetailPrefab, BlueprintLocation tabType)
        {
            TabType = tabType;
            try
            {
                BlueprintDetailPrefab = uiBlueprintDetailPrefab;
                ScrollContentParent = tabTrans.Find("BlueprintScrollView/Viewport/Content");
                ConfirmationOverlay = new UIConfirmationOverlay();
                Transform overlayParent = tabTrans.Find("BlueprintConfirmationOverlay");
                ConfirmationOverlay.Register(overlayParent);
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug($"Failed in BlueprintListDisplay: {ex}");
            }
        }
    }

    internal class BlueprintDetailDisplay
    {
        public BlueprintLocation TabType { get; set; } = BlueprintLocation.Local;

        //Use Id passed to link more details or whatever..
        public BlueprintDetailContent SelectedBlueprintDetail { get; set; }

        // Inputs Fields.
        public InputField Name { get; set; }
        public InputField Creator { get; set; }
        public InputField Description { get; set; }

        // Main Action Buttons
        public Button RefreshButton { get; set; }
        public Button SaveButton { get; set; }
        public Button TransferButton { get; set; }
        public Button DeleteButton { get; set; }

        // Overlay screens, for confirmations.
        public UIConfirmationOverlay ConfirmationOverlay { get; set; } = new UIConfirmationOverlay();

        // Managed Lists
        // Images of the Blueprint Selected.
        public List<Sprite> Icons { get; set; } = new List<Sprite>();

        public void SetActive(BlueprintDetailContent blueprint)
        {
            // Grab additional details from the id..or append model.
            SelectedBlueprintDetail = blueprint;
            Name.text = blueprint.Name;
            Creator.text = blueprint.Creator;
            Description.text = blueprint.Description;

            SaveButton.onClick.RemoveAllListeners();
            TransferButton.onClick.RemoveAllListeners();
            DeleteButton.onClick.RemoveAllListeners();

            SaveButton.onClick.AddListener(() =>
            {
                BlueprintGUI.SaveBlueprint(blueprint, TabType);
            });

            TransferButton.onClick.AddListener(() =>
            {
                BlueprintGUI.TransferBlueprint(blueprint, TabType);
            });

            DeleteButton.onClick.AddListener(() =>
            {
                BlueprintGUI.DeleteBlueprint(blueprint, TabType);
            });
        }

        public void Register(Transform tabTrans, GameObject uiBlueprintIconPrefab, BlueprintLocation tabType)
        {
            TabType = tabType;
            try
            {
                // Registering confirmation overlay.
                ConfirmationOverlay = new UIConfirmationOverlay();
                Transform overlayParent = tabTrans.Find("ConfirmationOverlay");
                ConfirmationOverlay.Register(overlayParent);

                Name = tabTrans.Find("Name").GetComponent<InputField>();
                Creator = tabTrans.Find("Creator").GetComponent<InputField>();
                Description = tabTrans.Find("Description").GetComponent<InputField>();

                RefreshButton = tabTrans.Find("RefreshButton").GetComponent<Button>();
                SaveButton = tabTrans.Find("SaveButton").GetComponent<Button>();
                TransferButton = tabTrans.Find("TransferButton").GetComponent<Button>();
                DeleteButton = tabTrans.Find("DeleteButton").GetComponent<Button>();

                // Add valheim refresh icon
                var img = RefreshButton.transform.Find("Image").GetComponent<Image>();
                img.sprite = GUIManager.Instance.GetSprite("refresh_icon");
                var outline = img.gameObject.AddComponent<Outline>();
                outline.effectColor = Color.black;

                // Refresh button is global
                RefreshButton.onClick.AddListener(() =>
                {
                    BlueprintGUI.RefreshBlueprints(TabType);
                });
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug($"Failed in BlueprintDetailDisplay: {ex}");
            }
        }
    }

    internal class BlueprintDetailContent
    {
        public GameObject ContentHolder { get; set; }
        public string ID { get; set; }
        // UI Elements.
        public string Name { get; set; }
        public string Creator { get; set; }
        public string Description { get; set; }
        public Text Text { get; set; }
        public Image Icon { get; set; }
        // Use this as select button.
        public Button IconButton { get; set; }
        public Button SortUpButton { get; set; }
        public Button SortDownButton { get; set; }
    }

    internal class UIConfirmationOverlay
    {
        public Transform ContentHolder { get; set; }
        public Text ConfirmationDisplayText { get; set; }
        public Button CancelButton { get; set; }
        public Button ConfirmButton { get; set; }

        public void Register(Transform overlayTransform)
        {
            ContentHolder = overlayTransform;
            ConfirmationDisplayText = overlayTransform.Find("ConfirmText").GetComponent<Text>();
            CancelButton = overlayTransform.Find("CancelButton").GetComponent<Button>();
            ConfirmButton = overlayTransform.Find("ConfirmationButton").GetComponent<Button>();
        }
    }

    internal class ActionAppliedOverlay
    {
        public Transform ContentHolder { get; set; }
        public Text DisplayText { get; set; }

        public void Toggle()
        {
            ContentHolder.gameObject.SetActive(!ContentHolder.gameObject.activeSelf);
        }

        public void Register(Transform overlayTransform)
        {
            ContentHolder = overlayTransform;
            DisplayText = overlayTransform.Find("ConfirmText").GetComponent<Text>();
        }
    }
}