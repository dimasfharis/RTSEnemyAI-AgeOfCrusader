using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;

#region Enum

public enum MonitorMenu
{
    Preferences,
    LogUnitFeature,
    LogBuildingFeature,
    LogWorldAcknowledge,
    LogMilitaryAction,
    LogGatheringPriority,
    LogPlayingStyle,
    LogGoalExecution,
    LogPlayerStatus
}

public enum ProfileType
{
    Aggressive = 0,
    Balanced = 1,
    Defensive = 2
}

#endregion

public class AIMonitoringUI : MonoBehaviour
{
    public static AIMonitoringUI Instance { get; private set; }

    [Header("Icon - Toggle Button (pojok kiri atas)")]
    [Tooltip("Kosongkan jika belum ada aset icon")]
    public Sprite toggleIcon;

    [Header("Icon - 9 Menu")]
    public Sprite iconPreferences;
    public Sprite iconUnitFeature;
    public Sprite iconBuildingFeature;
    public Sprite iconWorldAcknowledge;
    public Sprite iconMilitaryAction;
    public Sprite iconGatheringPriority;
    public Sprite iconPlayingStyle;
    public Sprite iconGoalExecution;
    public Sprite iconPlayerStatus;

    [Header("Settings")]
    [Tooltip("Lama animasi buka/tutup sliding menu (detik).")]
    public float slideDuration = 0.25f;
    [Tooltip("Tampilkan teks singkatan di tombol menu jika sprite icon belum diisi.")]
    public bool showDebugLabels = true;
    [Tooltip("Jumlah maksimal baris log yang disimpan per textbox, agar tidak membebani performa saat simulasi berjalan lama.")]
    public int maxLogLines = 500;

    #region Internal Data Structure

    private class LogPanelUI
    {
        public GameObject root;
        public ScrollRect player1Scroll;
        public Text player1Text;
        public ScrollRect player2Scroll;
        public Text player2Text;
    }

    private class DataPanelUI
    {
        public GameObject root;
        public Text player1Text;
        public Text player2Text;
    }

    private Font defaultFont;
    private RectTransform canvasRect;
    private RectTransform slidingPanel;
    private RectTransform iconColumn;
    private RectTransform infoColumn;

    private float panelWidthPixels;
    private bool isMenuOpen = false;
    private bool isAnimating = false;
    private MonitorMenu currentMenu = MonitorMenu.Preferences;
    private int cachedScreenWidth;
    private int cachedScreenHeight;

    private readonly Dictionary<MonitorMenu, GameObject> contentPanels = new Dictionary<MonitorMenu, GameObject>();
    private readonly Dictionary<MonitorMenu, LogPanelUI> logPanels = new Dictionary<MonitorMenu, LogPanelUI>();
    private readonly Dictionary<MonitorMenu, DataPanelUI> dataPanels = new Dictionary<MonitorMenu, DataPanelUI>();
    private readonly Dictionary<MonitorMenu, Image> menuIconImages = new Dictionary<MonitorMenu, Image>();

    #endregion

    #region Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        defaultFont = GetDefaultFont();
        EnsureEventSystem();
        BuildRootCanvas();
        Canvas.ForceUpdateCanvases();

        BuildSlidingPanel(); // Panel dibuat lebih dulu supaya tombol toggle selalu dirender di atasnya
        BuildToggleButton();

        SelectMenu(MonitorMenu.Preferences);

        cachedScreenWidth = Screen.width;
        cachedScreenHeight = Screen.height;
    }

    private void Update()
    {
        if (Screen.width != cachedScreenWidth || Screen.height != cachedScreenHeight)
        {
            cachedScreenWidth = Screen.width;
            cachedScreenHeight = Screen.height;
            RecalculatePanelWidth();
        }
    }

    private void RecalculatePanelWidth()
    {
        panelWidthPixels = canvasRect.rect.width * 0.2f;
        slidingPanel.sizeDelta = new Vector2(panelWidthPixels, slidingPanel.sizeDelta.y);
        if (!isAnimating)
        {
            float x = isMenuOpen ? 0f : -panelWidthPixels;
            slidingPanel.anchoredPosition = new Vector2(x, slidingPanel.anchoredPosition.y);
        }
    }

    #endregion

    #region Build: Root Canvas & Event System

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("Event System", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }

    private void BuildRootCanvas()
    {
        GameObject canvasGO = new GameObject("AIMonitoringUI_Canvas", typeof(RectTransform));
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // supaya tampil di atas UI lain

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();
        canvasRect = canvasGO.GetComponent<RectTransform>();
    }

    #endregion

    #region Build: Toggle Button

    private void BuildToggleButton()
    {
        RectTransform rt = CreateUIElement("ToggleButton", canvasRect);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(15, -15);
        rt.sizeDelta = new Vector2(50, 50);

        Image img = rt.gameObject.AddComponent<Image>();
        img.preserveAspect = true;
        if (toggleIcon != null)
        {
            img.sprite = toggleIcon;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(0.15f, 0.15f, 0.18f, 0.95f);
        }

        Button btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(ToggleMenu);

        if (toggleIcon == null && showDebugLabels)
        {
            RectTransform labelRT = CreateUIElement("Label", rt);
            StretchFull(labelRT);
            Text label = labelRT.gameObject.AddComponent<Text>();
            label.font = defaultFont;
            label.fontSize = 22;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = "\u2630"; // simbol hamburger
            label.raycastTarget = false;
        }

        rt.SetAsLastSibling(); // pastikan tombol selalu render di atas sliding panel
    }

    #endregion

    #region Build: Sliding Panel

    private void BuildSlidingPanel()
    {
        slidingPanel = CreateUIElement("SlidingMenuPanel", canvasRect);
        slidingPanel.anchorMin = new Vector2(0, 0);
        slidingPanel.anchorMax = new Vector2(0, 1);
        slidingPanel.pivot = new Vector2(0, 0.5f);

        panelWidthPixels = canvasRect.rect.width * 0.2f;
        slidingPanel.sizeDelta = new Vector2(panelWidthPixels, 0);
        slidingPanel.anchoredPosition = new Vector2(-panelWidthPixels, 0);

        Image bg = slidingPanel.gameObject.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.10f, 0.97f);

        BuildIconAndInfoColumns(slidingPanel);
        BuildAllMenuIcons();
        BuildAllContentPanels();
    }

    private void BuildIconAndInfoColumns(RectTransform parent)
    {
        // Kolom ikon menu (15%)
        iconColumn = CreateUIElement("IconColumn", parent);
        iconColumn.anchorMin = new Vector2(0f, 0f);
        iconColumn.anchorMax = new Vector2(0.15f, 1f);
        iconColumn.offsetMin = Vector2.zero;
        iconColumn.offsetMax = Vector2.zero;

        Image iconColBg = iconColumn.gameObject.AddComponent<Image>();
        iconColBg.color = new Color(0f, 0f, 0f, 0.25f);

        VerticalLayoutGroup iconVlg = iconColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        iconVlg.childAlignment = TextAnchor.UpperCenter;
        iconVlg.spacing = 8f;
        iconVlg.padding = new RectOffset(4, 4, 10, 10);
        iconVlg.childControlWidth = true;
        iconVlg.childControlHeight = true;
        iconVlg.childForceExpandWidth = false;
        iconVlg.childForceExpandHeight = false;

        // Kolom informasi (85%)
        infoColumn = CreateUIElement("InfoColumn", parent);
        infoColumn.anchorMin = new Vector2(0.15f, 0f);
        infoColumn.anchorMax = new Vector2(1f, 1f);
        infoColumn.offsetMin = Vector2.zero;
        infoColumn.offsetMax = Vector2.zero;

        Image infoColBg = infoColumn.gameObject.AddComponent<Image>();
        infoColBg.color = new Color(1f, 1f, 1f, 0.03f);
    }

    #endregion

    #region 9 Menu Icon

    private void BuildAllMenuIcons()
    {
        BuildMenuIcon(MonitorMenu.Preferences, iconPreferences);
        BuildMenuIcon(MonitorMenu.LogUnitFeature, iconUnitFeature);
        BuildMenuIcon(MonitorMenu.LogBuildingFeature, iconBuildingFeature);
        BuildMenuIcon(MonitorMenu.LogWorldAcknowledge, iconWorldAcknowledge);
        BuildMenuIcon(MonitorMenu.LogMilitaryAction, iconMilitaryAction);
        BuildMenuIcon(MonitorMenu.LogGatheringPriority, iconGatheringPriority);
        BuildMenuIcon(MonitorMenu.LogPlayingStyle, iconPlayingStyle);
        BuildMenuIcon(MonitorMenu.LogGoalExecution, iconGoalExecution);
        BuildMenuIcon(MonitorMenu.LogPlayerStatus, iconPlayerStatus);
    }

    private void BuildMenuIcon(MonitorMenu menu, Sprite icon)
    {
        RectTransform rt = CreateUIElement(menu.ToString() + "_Icon", iconColumn);

        LayoutElement le = rt.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = 40f;
        le.preferredHeight = 40f;
        le.flexibleWidth = 0f;
        le.flexibleHeight = 0f;

        Image img = rt.gameObject.AddComponent<Image>();
        img.preserveAspect = true;
        if (icon != null)
        {
            img.sprite = icon;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(0.3f, 0.3f, 0.34f, 1f);
        }

        Button btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => SelectMenu(menu));

        menuIconImages[menu] = img;

        if (icon == null && showDebugLabels)
        {
            RectTransform labelRT = CreateUIElement("Label", rt);
            StretchFull(labelRT);
            Text label = labelRT.gameObject.AddComponent<Text>();
            label.font = defaultFont;
            label.fontSize = 9;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = GetShortLabel(menu);
            label.raycastTarget = false; // supaya klik tetap terkena tombol induknya
        }
    }

    private string GetShortLabel(MonitorMenu menu)
    {
        switch (menu)
        {
            case MonitorMenu.Preferences: return "PRF";
            case MonitorMenu.LogUnitFeature: return "UNT";
            case MonitorMenu.LogBuildingFeature: return "BLD";
            case MonitorMenu.LogWorldAcknowledge: return "WLD";
            case MonitorMenu.LogMilitaryAction: return "MIL";
            case MonitorMenu.LogGatheringPriority: return "GTH";
            case MonitorMenu.LogPlayingStyle: return "STY";
            case MonitorMenu.LogGoalExecution: return "GOL";
            case MonitorMenu.LogPlayerStatus: return "STA";
            default: return "?";
        }
    }

    #endregion

    #region All Content Panel

    private void BuildAllContentPanels()
    {
        BuildPreferencesPanel(infoColumn);
        BuildLogPanel(infoColumn, MonitorMenu.LogUnitFeature, "LOG - UNIT FEATURE");
        BuildLogPanel(infoColumn, MonitorMenu.LogBuildingFeature, "LOG - BUILDING FEATURE");
        BuildDataPanel(infoColumn, MonitorMenu.LogWorldAcknowledge, "WORLD ACKNOWLEDGE");
        BuildLogPanel(infoColumn, MonitorMenu.LogMilitaryAction, "LOG - MILITARY ACTION");
        BuildDataPanel(infoColumn, MonitorMenu.LogGatheringPriority, "GATHERING PRIORITY");
        BuildLogPanel(infoColumn, MonitorMenu.LogPlayingStyle, "LOG - PLAYING STYLE");
        BuildLogPanel(infoColumn, MonitorMenu.LogGoalExecution, "LOG - GOAL EXECUTION");
        BuildDataPanel(infoColumn, MonitorMenu.LogPlayerStatus, "PLAYER STATUS (Decision Basis)");
    }

    // Preferences Panel
    private void BuildPreferencesPanel(RectTransform parent)
    {
        GameObject panel = new GameObject("Panel_Preferences", typeof(RectTransform));
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        StretchFull(rt);

        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 16, 16);
        vlg.spacing = 14f;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        CreateLabel(panel.transform, "PREFERENCES", 18, FontStyle.Bold);

        CreateLabel(panel.transform, "Player 1 profile", 14, FontStyle.Normal);
        CreateProfileDropdown(panel.transform, 1);

        CreateLabel(panel.transform, "Player 2 profile", 14, FontStyle.Normal);
        CreateProfileDropdown(panel.transform, 2);

        panel.SetActive(false);
        contentPanels[MonitorMenu.Preferences] = panel;
    }

    private Dropdown CreateProfileDropdown(Transform parent, int playerIndex)
    {
        List<string> options = new List<string> { "Aggressive", "Balanced", "Defensive" };
        Dropdown dd = CreateDropdown(parent, options);
        dd.onValueChanged.AddListener((int index) =>
        {
            ProfileType selected = (ProfileType)index; // urutan option HARUS sama dgn urutan enum ProfileType
            SetPlayerProfile(playerIndex, selected);
        });
        return dd;
    }

    // Dropdown Builder
    private Dropdown CreateDropdown(Transform parent, List<string> options)
    {
        GameObject ddGO = new GameObject("Dropdown", typeof(RectTransform));
        ddGO.transform.SetParent(parent, false);

        LayoutElement ddLE = ddGO.AddComponent<LayoutElement>();
        ddLE.preferredHeight = 32f;
        ddLE.flexibleHeight = 0f;

        Image ddBg = ddGO.AddComponent<Image>();
        ddBg.color = new Color(0.2f, 0.2f, 0.22f, 1f);

        Dropdown dropdown = ddGO.AddComponent<Dropdown>();
        dropdown.targetGraphic = ddBg;

        // Label teks pilihan yang sedang aktif
        RectTransform labelRT = CreateUIElement("Label", ddGO.transform);
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(10, 6);
        labelRT.offsetMax = new Vector2(-25, -6);
        Text labelText = labelRT.gameObject.AddComponent<Text>();
        labelText.font = defaultFont;
        labelText.fontSize = 14;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleLeft;
        dropdown.captionText = labelText;

        // Panah kecil di kanan
        RectTransform arrowRT = CreateUIElement("Arrow", ddGO.transform);
        arrowRT.anchorMin = new Vector2(1, 0.5f);
        arrowRT.anchorMax = new Vector2(1, 0.5f);
        arrowRT.pivot = new Vector2(1, 0.5f);
        arrowRT.anchoredPosition = new Vector2(-8, 0);
        arrowRT.sizeDelta = new Vector2(16, 16);
        Text arrowText = arrowRT.gameObject.AddComponent<Text>();
        arrowText.text = "\u25BC";
        arrowText.font = defaultFont;
        arrowText.fontSize = 10;
        arrowText.alignment = TextAnchor.MiddleCenter;
        arrowText.color = Color.white;
        arrowText.raycastTarget = false;

        // Template popup (nonaktif secara default, dipakai Dropdown saat diklik)
        RectTransform templateRT = CreateUIElement("Template", ddGO.transform);
        templateRT.anchorMin = new Vector2(0, 0);
        templateRT.anchorMax = new Vector2(1, 0);
        templateRT.pivot = new Vector2(0.5f, 1);
        templateRT.anchoredPosition = new Vector2(0, 2);
        templateRT.sizeDelta = new Vector2(0, 100);

        Image templateBg = templateRT.gameObject.AddComponent<Image>();
        templateBg.color = new Color(0.15f, 0.15f, 0.17f, 0.98f);

        ScrollRect templateScroll = templateRT.gameObject.AddComponent<ScrollRect>();
        templateScroll.horizontal = false;
        templateScroll.vertical = true;
        templateScroll.movementType = ScrollRect.MovementType.Clamped;

        RectTransform viewportRT = CreateUIElement("Viewport", templateRT);
        StretchFull(viewportRT);
        viewportRT.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
        Mask templateMask = viewportRT.gameObject.AddComponent<Mask>();
        templateMask.showMaskGraphic = false;
        templateScroll.viewport = viewportRT;

        RectTransform contentRT = CreateUIElement("Content", viewportRT);
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup contentVlg = contentRT.gameObject.AddComponent<VerticalLayoutGroup>();
        contentVlg.spacing = 2f;
        contentVlg.padding = new RectOffset(0, 0, 2, 2);
        contentVlg.childControlWidth = true;
        contentVlg.childControlHeight = true;
        contentVlg.childForceExpandWidth = true;
        contentVlg.childForceExpandHeight = false;

        ContentSizeFitter contentCsf = contentRT.gameObject.AddComponent<ContentSizeFitter>();
        contentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        templateScroll.content = contentRT;

        // Item template (1 baris opsi)
        RectTransform itemRT = CreateUIElement("Item", contentRT);
        LayoutElement itemLE = itemRT.gameObject.AddComponent<LayoutElement>();
        itemLE.preferredHeight = 28f;
        itemLE.flexibleHeight = 0f;

        Toggle itemToggle = itemRT.gameObject.AddComponent<Toggle>();

        RectTransform itemBgRT = CreateUIElement("Item Background", itemRT);
        StretchFull(itemBgRT);
        Image itemBg = itemBgRT.gameObject.AddComponent<Image>();
        itemBg.color = new Color(0.28f, 0.28f, 0.32f, 1f);
        itemToggle.targetGraphic = itemBg;

        RectTransform itemCheckRT = CreateUIElement("Item Checkmark", itemRT);
        itemCheckRT.anchorMin = new Vector2(0, 0.5f);
        itemCheckRT.anchorMax = new Vector2(0, 0.5f);
        itemCheckRT.pivot = new Vector2(0.5f, 0.5f);
        itemCheckRT.anchoredPosition = new Vector2(12, 0);
        itemCheckRT.sizeDelta = new Vector2(14, 14);
        Image itemCheck = itemCheckRT.gameObject.AddComponent<Image>();
        itemCheck.color = Color.white;
        itemToggle.graphic = itemCheck;

        RectTransform itemLabelRT = CreateUIElement("Item Label", itemRT);
        itemLabelRT.anchorMin = Vector2.zero;
        itemLabelRT.anchorMax = Vector2.one;
        itemLabelRT.offsetMin = new Vector2(26, 2);
        itemLabelRT.offsetMax = new Vector2(-6, -2);
        Text itemLabelText = itemLabelRT.gameObject.AddComponent<Text>();
        itemLabelText.font = defaultFont;
        itemLabelText.fontSize = 13;
        itemLabelText.color = Color.white;
        itemLabelText.alignment = TextAnchor.MiddleLeft;
        dropdown.itemText = itemLabelText;

        templateRT.gameObject.SetActive(false);
        dropdown.template = templateRT;

        dropdown.options.Clear();
        for (int i = 0; i < options.Count; i++)
        {
            dropdown.options.Add(new Dropdown.OptionData(options[i]));
        }
        dropdown.value = 0;
        dropdown.RefreshShownValue();

        return dropdown;
    }

    // Log Panel (menu 2,3,5,7,8)
    private void BuildLogPanel(RectTransform parent, MonitorMenu menu, string title)
    {
        GameObject panel = new GameObject("Panel_" + menu, typeof(RectTransform));
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        StretchFull(rt);

        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.spacing = 6f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        CreateLabel(panel.transform, title, 16, FontStyle.Bold);

        CreateLabel(panel.transform, "Player 1", 12, FontStyle.Bold);
        Text text1;
        ScrollRect scroll1 = CreateScrollableLogBox(panel.transform, out text1);

        CreateLabel(panel.transform, "Player 2", 12, FontStyle.Bold);
        Text text2;
        ScrollRect scroll2 = CreateScrollableLogBox(panel.transform, out text2);

        LogPanelUI panelData = new LogPanelUI();
        panelData.root = panel;
        panelData.player1Scroll = scroll1;
        panelData.player1Text = text1;
        panelData.player2Scroll = scroll2;
        panelData.player2Text = text2;
        logPanels[menu] = panelData;

        panel.SetActive(false);
        contentPanels[menu] = panel;
    }

    private ScrollRect CreateScrollableLogBox(Transform parent, out Text logText)
    {
        RectTransform boxRT = CreateUIElement("LogBox", parent);
        LayoutElement boxLE = boxRT.gameObject.AddComponent<LayoutElement>();
        boxLE.flexibleHeight = 1f;
        boxLE.minHeight = 60f;
        boxRT.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.06f, 1f);

        ScrollRect scrollRect = boxRT.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        RectTransform viewportRT = CreateUIElement("Viewport", boxRT);
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = new Vector2(4, 4);
        viewportRT.offsetMax = new Vector2(-4, -4);
        viewportRT.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
        Mask mask = viewportRT.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        scrollRect.viewport = viewportRT;

        RectTransform textRT = CreateUIElement("Content_LogText", viewportRT);
        textRT.anchorMin = new Vector2(0, 1);
        textRT.anchorMax = new Vector2(1, 1);
        textRT.pivot = new Vector2(0.5f, 1);
        textRT.anchoredPosition = Vector2.zero;
        textRT.sizeDelta = new Vector2(0, 0);

        Text txt = textRT.gameObject.AddComponent<Text>();
        txt.font = defaultFont;
        txt.fontSize = 12;
        txt.color = new Color(0.85f, 0.85f, 0.85f);
        txt.alignment = TextAnchor.UpperLeft;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.supportRichText = true;
        txt.text = "";

        ContentSizeFitter csf = textRT.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.content = textRT;
        logText = txt;
        return scrollRect;
    }

    // Data Panel (menu 4,6,9)
    private void BuildDataPanel(RectTransform parent, MonitorMenu menu, string title)
    {
        GameObject panel = new GameObject("Panel_" + menu, typeof(RectTransform));
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        StretchFull(rt);

        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.spacing = 6f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        CreateLabel(panel.transform, title, 16, FontStyle.Bold);

        CreateLabel(panel.transform, "Player 1", 12, FontStyle.Bold);
        Text text1 = CreateDataBox(panel.transform);
        text1.text = "data x: ....\ndata y: ....";

        CreateLabel(panel.transform, "Player 2", 12, FontStyle.Bold);
        Text text2 = CreateDataBox(panel.transform);
        text2.text = "data x: ....\ndata y: ....";

        DataPanelUI panelData = new DataPanelUI();
        panelData.root = panel;
        panelData.player1Text = text1;
        panelData.player2Text = text2;
        dataPanels[menu] = panelData;

        panel.SetActive(false);
        contentPanels[menu] = panel;
    }

    private Text CreateDataBox(Transform parent)
    {
        RectTransform boxRT = CreateUIElement("DataBox", parent);
        LayoutElement le = boxRT.gameObject.AddComponent<LayoutElement>();
        le.flexibleHeight = 1f;
        le.minHeight = 60f;
        boxRT.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.06f, 1f);

        RectTransform textRT = CreateUIElement("DataText", boxRT);
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(8, 8);
        textRT.offsetMax = new Vector2(-8, -8);

        Text dataText = textRT.gameObject.AddComponent<Text>();
        dataText.font = defaultFont;
        dataText.fontSize = 13;
        dataText.color = new Color(0.85f, 0.9f, 1f);
        dataText.alignment = TextAnchor.UpperLeft;
        dataText.horizontalOverflow = HorizontalWrapMode.Wrap;
        dataText.verticalOverflow = VerticalWrapMode.Overflow;
        dataText.text = "";

        return dataText;
    }

    #endregion

    #region UI Helper Utility

    private RectTransform CreateUIElement(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    private Text CreateLabel(Transform parent, string content, int fontSize, FontStyle style)
    {
        RectTransform rt = CreateUIElement("Label_" + content, parent);
        LayoutElement le = rt.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = fontSize + 10;
        le.flexibleHeight = 0f;

        Text txt = rt.gameObject.AddComponent<Text>();
        txt.font = defaultFont;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.color = Color.white;
        txt.text = content;
        txt.alignment = TextAnchor.MiddleLeft;
        return txt;
    }

    private Font GetDefaultFont()
    {
        Font f = null;
        try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
        if (f == null)
        {
            try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
        }
        return f;
    }

    #endregion

    #region Menu Choosing & Sliding Animation

    private void SelectMenu(MonitorMenu menu)
    {
        currentMenu = menu;

        foreach (KeyValuePair<MonitorMenu, GameObject> kvp in contentPanels)
        {
            kvp.Value.SetActive(kvp.Key == menu);
        }

        foreach (KeyValuePair<MonitorMenu, Image> kvp in menuIconImages)
        {
            bool selected = kvp.Key == menu;
            Image img = kvp.Value;
            bool hasSprite = img.sprite != null;
            Color baseColor = hasSprite ? Color.white : new Color(0.3f, 0.3f, 0.34f, 1f);
            img.color = selected ? new Color(0.35f, 0.65f, 1f, 1f) : baseColor;
        }
    }

    public void OpenMenu()
    {
        if (isAnimating || isMenuOpen) return;
        StartCoroutine(SlidePanel(true));
    }

    public void CloseMenu()
    {
        if (isAnimating || !isMenuOpen) return;
        StartCoroutine(SlidePanel(false));
    }

    public void ToggleMenu()
    {
        if (isAnimating) return;
        StartCoroutine(SlidePanel(!isMenuOpen));
    }

    private IEnumerator SlidePanel(bool open)
    {
        isAnimating = true;
        isMenuOpen = open;

        float startX = slidingPanel.anchoredPosition.x;
        float endX = open ? 0f : -panelWidthPixels;
        float t = 0f;

        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime;
            float ratio = Mathf.Clamp01(t / slideDuration);
            ratio = 1f - Mathf.Pow(1f - ratio, 3f); // ease-out
            float x = Mathf.Lerp(startX, endX, ratio);
            slidingPanel.anchoredPosition = new Vector2(x, slidingPanel.anchoredPosition.y);
            yield return null;
        }

        slidingPanel.anchoredPosition = new Vector2(endX, slidingPanel.anchoredPosition.y);
        isAnimating = false;
    }

    #endregion

    #region Public API - Log (Menu 2,3,5,7,8)
    // call this method from the AI logic

    private void AppendLog(MonitorMenu menu, int playerIndex, string message)
    {
        LogPanelUI panel;
        if (!logPanels.TryGetValue(menu, out panel))
        {
            Debug.LogWarning("[AIMonitoringUI] Menu " + menu + " bukan panel log.");
            return;
        }
        if (playerIndex != 1 && playerIndex != 2)
        {
            Debug.LogWarning("[AIMonitoringUI] playerIndex harus 1 atau 2.");
            return;
        }

        Text targetText = playerIndex == 1 ? panel.player1Text : panel.player2Text;
        ScrollRect targetScroll = playerIndex == 1 ? panel.player1Scroll : panel.player2Scroll;

        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        targetText.text += "[" + timestamp + "] " + message + "\n";

        // Batasi jumlah baris supaya tidak membebani performa saat simulasi berjalan lama
        string[] lines = targetText.text.Split('\n');
        if (lines.Length > maxLogLines)
        {
            int skip = lines.Length - maxLogLines;
            StringBuilder sb = new StringBuilder();
            for (int i = skip; i < lines.Length; i++)
            {
                sb.Append(lines[i]);
                if (i < lines.Length - 1) sb.Append('\n');
            }
            targetText.text = sb.ToString();
        }

        // Auto-scroll ke entri terbaru (paling bawah)
        Canvas.ForceUpdateCanvases();
        targetScroll.verticalNormalizedPosition = 0f;
    }

    public void ClearLog(MonitorMenu menu, int playerIndex)
    {
        LogPanelUI panel;
        if (!logPanels.TryGetValue(menu, out panel)) return;
        Text targetText = playerIndex == 1 ? panel.player1Text : panel.player2Text;
        targetText.text = "";
    }

    // Shortcut method per category, just call it from AI logic
    public void LogUnitFeature(int playerIndex, string message) { AppendLog(MonitorMenu.LogUnitFeature, playerIndex, message); }
    public void LogBuildingFeature(int playerIndex, string message) { AppendLog(MonitorMenu.LogBuildingFeature, playerIndex, message); }
    public void LogMilitaryAction(int playerIndex, string message) { AppendLog(MonitorMenu.LogMilitaryAction, playerIndex, message); }
    public void LogPlayingStyle(int playerIndex, string message) { AppendLog(MonitorMenu.LogPlayingStyle, playerIndex, message); }
    public void LogGoalExecution(int playerIndex, string message) { AppendLog(MonitorMenu.LogGoalExecution, playerIndex, message); }

    #endregion

    #region Public API - Data (menu 4,6,9)

    private void UpdateData(MonitorMenu menu, int playerIndex, string rawText)
    {
        DataPanelUI panel;
        if (!dataPanels.TryGetValue(menu, out panel))
        {
            Debug.LogWarning("[AIMonitoringUI] Menu " + menu + " bukan panel data.");
            return;
        }
        if (playerIndex != 1 && playerIndex != 2)
        {
            Debug.LogWarning("[AIMonitoringUI] playerIndex harus 1 atau 2.");
            return;
        }

        Text target = playerIndex == 1 ? panel.player1Text : panel.player2Text;
        target.text = rawText;
    }

    // Overload: send key-value, it will automatically formatted "key: value" per line
    private void UpdateData(MonitorMenu menu, int playerIndex, Dictionary<string, object> dataFields)
    {
        StringBuilder sb = new StringBuilder();
        foreach (KeyValuePair<string, object> kvp in dataFields)
        {
            sb.Append(kvp.Key).Append(": ").Append(kvp.Value).Append('\n');
        }
        UpdateData(menu, playerIndex, sb.ToString());
    }

    // Shortcut method per category
    public void UpdateWorldAcknowledgeData(int playerIndex, string rawText) { UpdateData(MonitorMenu.LogWorldAcknowledge, playerIndex, rawText); }
    public void UpdateWorldAcknowledgeData(int playerIndex, Dictionary<string, object> data) { UpdateData(MonitorMenu.LogWorldAcknowledge, playerIndex, data); }

    public void UpdateGatheringPriorityData(int playerIndex, string rawText) { UpdateData(MonitorMenu.LogGatheringPriority, playerIndex, rawText); }
    public void UpdateGatheringPriorityData(int playerIndex, Dictionary<string, object> data) { UpdateData(MonitorMenu.LogGatheringPriority, playerIndex, data); }

    public void UpdatePlayerStatusData(int playerIndex, string rawText) { UpdateData(MonitorMenu.LogPlayerStatus, playerIndex, rawText); }
    public void UpdatePlayerStatusData(int playerIndex, Dictionary<string, object> data) { UpdateData(MonitorMenu.LogPlayerStatus, playerIndex, data); }

    #endregion

    #region Profile Choosing

    private void SetPlayerProfile(int playerIndex, ProfileType profile)
    {
        // ==========================================================================
        // TODO: Isi logika untuk mengeset profile AI player di sini.
        // Contoh:
        //
        // if (playerIndex == 1)
        // {
        //     player1AIController.SetProfile(profile);
        // }
        // else
        // {
        //     player2AIController.SetProfile(profile);
        // }
        // ==========================================================================

        Debug.Log("[AIMonitoringUI] Player " + playerIndex + " profile diset ke: " + profile);
    }

    #endregion
}
