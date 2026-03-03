/*
 * HitMarker - Visual hit feedback mod for Raft
 * Copyright (C) 2024 Flaze
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program. If not, see <https://www.gnu.org/licenses/>.
 */

using RaftModLoader;
using UnityEngine;
using UnityEngine.UI;
using HMLLibrary;
using HarmonyLib;
using System.Runtime.CompilerServices;
using System.IO;

public class HitMarker : Mod
{
    // Singleton Instance (for access from static Harmony patches)
    public static HitMarker Instance { get; private set; }
    
    // Harmony instance
    private Harmony harmony;
    
    // UI Components
    private Canvas hitmarkerCanvas;
    private GameObject hitmarkerImage;
    private Image imageComponent;
    private RectTransform hitmarkerRect;
    
    // Configuration (can be changed via Extra Settings API)
    private float displayDuration = 0.5f;
    private float fadeInDuration = 0.1f;
    private float fadeOutDuration = 0.15f;
    private float hitmarkerSize = 50f;
    private string customImagePath = "";
    
    // Color customization
    private float hitmarkerColorR = 1f;
    private float hitmarkerColorG = 1f;
    private float hitmarkerColorB = 1f;
    
    // Combo system
    private int comboCount = 0;
    private int killCount = 0;
    private float comboResetTime = 5f;
    private float lastHitTime = 0f;
    private bool showComboCounter = true;
    private GameObject comboCounterText;
    private Text comboTextComponent;
    private Coroutine comboBlinkCoroutine;
    
    // Default combo text color (#F2E2C5)
    private Color defaultComboColor = new Color(0.95f, 0.89f, 0.77f, 1f);
    
    // ComboCounter mod detection
    private bool comboCounterModDetected = false;
    private float comboModCheckTimer = 0f;
    private const float comboModCheckInterval = 2f; // check every 2 seconds
    
    // State
    private Coroutine hideCoroutine;
    private Sprite defaultHitmarkerSprite;
    private Sprite customHitmarkerSprite;
    
    // Extra Settings API integration
    static bool ExtraSettingsAPI_Loaded = false;

    public void Start()
    {
        Instance = this;
        
        // Check for ComboCounter immediately
        comboCounterModDetected = IsComboCounterModLoaded();
        
        // DON'T hide settings here - UI doesn't exist yet!
        // Will be done in ExtraSettingsAPI_Load() instead
        
        try
        {
            harmony = new Harmony("com.hitmarker.mod");
            harmony.PatchAll(typeof(HitMarker).Assembly);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HitMarker] ERROR applying Harmony patches: {ex.Message}");
        }
        
        CreateHitmarkerUI();
        
        // Only create combo counter if ComboCounter mod is not loaded
        if (!comboCounterModDetected)
        {
            CreateComboCounterUI();
        }
    }
    
    /// <summary>
    /// Called by ComboCounter mod to notify HitMarker that it's loaded.
    /// Also called automatically by the periodic Update() check.
    /// </summary>
    public void NotifyComboCounterLoaded()
    {
        if (!comboCounterModDetected)
            OnComboCounterLoaded();
    }

    private void OnComboCounterLoaded()
    {
        comboCounterModDetected = true;

        // Destroy existing combo counter UI
        if (comboCounterText != null)
        {
            Destroy(comboCounterText);
            comboCounterText = null;
            comboTextComponent = null;
        }

        // Stop any running combo coroutines
        if (comboBlinkCoroutine != null)
        {
            StopCoroutine(comboBlinkCoroutine);
            comboBlinkCoroutine = null;
        }

        // Reset combo state
        comboCount = 0;
        killCount = 0;

        // Hide combo settings (if ExtraSettingsAPI is already loaded)
        if (ExtraSettingsAPI_Loaded)
            RuntimeSettingsAPIHelper.HideSection(this, "HitMarker", "comboSection");
    }

    private void OnComboCounterUnloaded()
    {
        comboCounterModDetected = false;

        // Recreate combo counter UI
        if (comboCounterText == null)
            CreateComboCounterUI();

        // Show combo settings again
        if (ExtraSettingsAPI_Loaded)
            RuntimeSettingsAPIHelper.ShowSection(this, "HitMarker", "comboSection");
    }
    
    private bool IsComboCounterModLoaded()
    {
        try
        {
            // Check if ComboCounter mod is actually running (not just installed)
            var loadedMods = ModManagerPage.modList;
            if (loadedMods != null)
            {
                foreach (var mod in loadedMods)
                {
                    if (mod != null && mod.jsonmodinfo != null)
                    {
                        if (mod.jsonmodinfo.name == "ComboCounter"
                            && mod.modinfo != null
                            && mod.modinfo.modState == ModInfo.ModStateEnum.running
                            && mod.modinfo.mainClass != null)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[HitMarker] Could not check for ComboCounter mod: {ex.Message}");
        }
        
        return false;
    }
    
    public void Update()
    {
        // Periodically check whether ComboCounter mod has been loaded or unloaded at runtime
        comboModCheckTimer += Time.deltaTime;
        if (comboModCheckTimer >= comboModCheckInterval)
        {
            comboModCheckTimer = 0f;
            bool isNowLoaded = IsComboCounterModLoaded();
            if (isNowLoaded && !comboCounterModDetected)
                OnComboCounterLoaded();
            else if (!isNowLoaded && comboCounterModDetected)
                OnComboCounterUnloaded();
        }

        // Skip built-in combo counter logic while ComboCounter mod is active
        if (comboCounterModDetected)
            return;

        // Check if combo should reset
        if (comboCount > 0 && Time.time - lastHitTime > comboResetTime)
        {
            ResetCombo();
        }

        // Check if combo is about to expire and start blinking
        if (comboCount > 0 && showComboCounter)
        {
            float timeRemaining = comboResetTime - (Time.time - lastHitTime);
            float blinkThreshold = comboResetTime * 0.3f; // Start blinking at 30% time remaining

            if (timeRemaining <= blinkThreshold && comboBlinkCoroutine == null)
            {
                comboBlinkCoroutine = StartCoroutine(BlinkComboCounter());
            }
        }
    }
    
    // ============================================================================
    // EXTRA SETTINGS API INTEGRATION
    // ============================================================================
    
    /// <summary>
    /// Called when Extra Settings API loads this mod's settings
    /// </summary>
    public void ExtraSettingsAPI_Load()
    {
        ExtraSettingsAPI_Loaded = true;
        LoadSettingsFromAPI();
        
        // Apply correct visibility for combo section now that the settings UI exists
        if (comboCounterModDetected)
            RuntimeSettingsAPIHelper.HideSection(this, "HitMarker", "comboSection");
    }
    
    /// <summary>
    /// Called when settings menu is opened
    /// </summary>
    public void ExtraSettingsAPI_SettingsOpen()
    {
        // Settings are loaded when menu opens
        LoadSettingsFromAPI();
    }
    
    /// <summary>
    /// Called when settings menu is closed
    /// </summary>
    public void ExtraSettingsAPI_SettingsClose()
    {
        // Apply any changed settings
        LoadSettingsFromAPI();
        UpdateHitmarkerSize();
    }
    
    /// <summary>
    /// Called when a button is pressed in the settings
    /// </summary>
    public void ExtraSettingsAPI_ButtonPress(string SettingName)
    {
        try
        {
            if (SettingName == "loadCustomImage")
            {
                LoadCustomImageFromPath();
            }
            else if (SettingName == "resetToDefault")
            {
                ResetToDefaultImage();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HitMarker] Error in button press: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Load all settings from the Extra Settings API
    /// </summary>
    private void LoadSettingsFromAPI()
    {
        if (!ExtraSettingsAPI_Loaded)
            return;
            
        try
        {
            hitmarkerSize = ExtraSettingsAPI_GetSliderValue("hitmarkerSize");
            displayDuration = ExtraSettingsAPI_GetSliderValue("displayDuration");
            fadeInDuration = ExtraSettingsAPI_GetSliderValue("fadeInDuration");
            fadeOutDuration = ExtraSettingsAPI_GetSliderValue("fadeOutDuration");
            customImagePath = ExtraSettingsAPI_GetInputValue("customImagePath");
            
            // Color settings
            hitmarkerColorR = ExtraSettingsAPI_GetSliderValue("hitmarkerColorR");
            hitmarkerColorG = ExtraSettingsAPI_GetSliderValue("hitmarkerColorG");
            hitmarkerColorB = ExtraSettingsAPI_GetSliderValue("hitmarkerColorB");
            
            // Combo settings
            showComboCounter = ExtraSettingsAPI_GetCheckboxState("showComboCounter");
            comboResetTime = ExtraSettingsAPI_GetSliderValue("comboResetTime");
            
            UpdateHitmarkerSize();
            UpdateHitmarkerColor();
            UpdateComboVisibility();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HitMarker] Error loading settings: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Load a custom hitmarker image from the specified path
    /// </summary>
    private void LoadCustomImageFromPath()
    {
        if (!ExtraSettingsAPI_Loaded)
            return;
            
        customImagePath = ExtraSettingsAPI_GetInputValue("customImagePath");
        
        // Trim whitespace and remove quotes if present
        if (!string.IsNullOrWhiteSpace(customImagePath))
        {
            customImagePath = customImagePath.Trim().Trim('"').Trim('\'');
        }
        
        if (string.IsNullOrWhiteSpace(customImagePath))
        {
            Debug.LogWarning("[HitMarker] No image path specified");
            return;
        }
        
        if (!File.Exists(customImagePath))
        {
            Debug.LogError($"[HitMarker] File not found: {customImagePath}");
            return;
        }
        
        try
        {
            byte[] fileData = File.ReadAllBytes(customImagePath);
            
            if (fileData == null || fileData.Length == 0)
            {
                Debug.LogError("[HitMarker] Failed to read image file");
                return;
            }
            
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            
            if (texture.LoadImage(fileData))
            {
                customHitmarkerSprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                
                if (imageComponent != null)
                {
                    imageComponent.sprite = customHitmarkerSprite;
                }
                
            }
            else
            {
                Debug.LogError("[HitMarker] Failed to load image data into texture");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HitMarker] Error loading custom image: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Reset to the default hitmarker image
    /// </summary>
    private void ResetToDefaultImage()
    {
        if (imageComponent != null && defaultHitmarkerSprite != null)
        {
            imageComponent.sprite = defaultHitmarkerSprite;
        }
    }
    
    /// <summary>
    /// Update the hitmarker size in the UI
    /// </summary>
    private void UpdateHitmarkerSize()
    {
        if (hitmarkerRect != null)
        {
            hitmarkerRect.sizeDelta = new Vector2(hitmarkerSize, hitmarkerSize);
        }
    }
    
    /// <summary>
    /// Update the hitmarker color
    /// </summary>
    private void UpdateHitmarkerColor()
    {
        if (imageComponent != null)
        {
            Color currentColor = imageComponent.color;
            imageComponent.color = new Color(hitmarkerColorR, hitmarkerColorG, hitmarkerColorB, currentColor.a);
        }
        
        // Also update combo counter color
        UpdateComboColor();
    }
    
    /// <summary>
    /// Update combo counter text color (uses default combo color, not hitmarker color)
    /// </summary>
    private void UpdateComboColor()
    {
        if (comboTextComponent != null)
        {
            Color currentColor = comboTextComponent.color;
            comboTextComponent.color = new Color(defaultComboColor.r, defaultComboColor.g, defaultComboColor.b, currentColor.a);
        }
    }
    
    /// <summary>
    /// Update combo counter visibility
    /// </summary>
    private void UpdateComboVisibility()
    {
        if (comboCounterText != null)
        {
            comboCounterText.SetActive(showComboCounter && comboCount > 0);
        }
    }
    
    /// <summary>
    /// Increment combo counter
    /// </summary>
    private void IncrementCombo(bool isKill = false)
    {
        // Skip if ComboCounter mod is loaded
        if (comboCounterModDetected)
        {
            return;
        }
        
        comboCount++;
        if (isKill)
        {
            killCount++;
        }
        lastHitTime = Time.time;
        
        // Stop blinking if it was blinking
        if (comboBlinkCoroutine != null)
        {
            StopCoroutine(comboBlinkCoroutine);
            comboBlinkCoroutine = null;
            
            // Reset combo text visibility
            if (comboTextComponent != null)
            {
                Color currentColor = comboTextComponent.color;
                comboTextComponent.color = new Color(currentColor.r, currentColor.g, currentColor.b, 1f);
            }
        }
        
        if (showComboCounter)
        {
            UpdateComboDisplay();
        }
    }
    
    /// <summary>
    /// Reset combo counter
    /// </summary>
    private void ResetCombo()
    {
        comboCount = 0;
        killCount = 0;
        
        // Stop blinking coroutine if running
        if (comboBlinkCoroutine != null)
        {
            StopCoroutine(comboBlinkCoroutine);
            comboBlinkCoroutine = null;
        }
        
        if (comboCounterText != null)
        {
            comboCounterText.SetActive(false);
        }
    }
    
    /// <summary>
    /// Update combo counter display
    /// </summary>
    private void UpdateComboDisplay()
    {
        if (comboTextComponent != null && comboCounterText != null)
        {
            if (comboCount == 1)
            {
                if (killCount > 0)
                {
                    comboTextComponent.text = "KILLED!";
                }
                else
                {
                    comboTextComponent.text = "HIT!";
                }
            }
            else
            {
                string killText = killCount > 0 ? $" - {killCount} KILLED!" : "";
                comboTextComponent.text = $"{comboCount}x COMBO!{killText}";
            }
            comboCounterText.SetActive(true);
        }
    }
    
    /// <summary>
    /// Blink combo counter when time is almost up
    /// </summary>
    private System.Collections.IEnumerator BlinkComboCounter()
    {
        if (comboTextComponent == null)
        {
            yield break;
        }
        
        float blinkSpeed = 0.15f; // Blink every 0.15 seconds
        
        while (comboCount > 0)
        {
            // Fade out
            Color currentColor = comboTextComponent.color;
            comboTextComponent.color = new Color(currentColor.r, currentColor.g, currentColor.b, 0.3f);
            
            yield return new WaitForSeconds(blinkSpeed);
            
            // Fade in
            currentColor = comboTextComponent.color;
            comboTextComponent.color = new Color(currentColor.r, currentColor.g, currentColor.b, 1f);
            
            yield return new WaitForSeconds(blinkSpeed);
        }
        
        comboBlinkCoroutine = null;
    }
    
    // ============================================================================
    // EXTRA SETTINGS API STUB METHODS
    // These are replaced by the API at runtime
    // ============================================================================
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public float ExtraSettingsAPI_GetSliderValue(string SettingName) => 0f;
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public string ExtraSettingsAPI_GetInputValue(string SettingName) => "";
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool ExtraSettingsAPI_GetCheckboxState(string SettingName) => false;
    


    public void OnModUnload()
    {
        try
        {
            if (harmony != null)
            {
                harmony.UnpatchAll(harmony.Id);
                harmony = null;
            }
            
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }
            
            if (comboBlinkCoroutine != null)
            {
                StopCoroutine(comboBlinkCoroutine);
                comboBlinkCoroutine = null;
            }
            
            if (hitmarkerCanvas != null)
            {
                Destroy(hitmarkerCanvas.gameObject);
                hitmarkerCanvas = null;
                hitmarkerImage = null;
                imageComponent = null;
                comboCounterText = null;
                comboTextComponent = null;
            }
            
            Instance = null;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HitMarker] ERROR during OnModUnload: {ex.Message}");
            
            harmony = null;
            hideCoroutine = null;
            comboBlinkCoroutine = null;
            hitmarkerCanvas = null;
            hitmarkerImage = null;
            imageComponent = null;
            comboCounterText = null;
            comboTextComponent = null;
            Instance = null;
        }
    }

    public void ShowHitmarker()
    {
        ShowHitmarker(false);
    }
    
    public void ShowHitmarker(bool isKill)
    {
        try
        {
            if (hitmarkerImage == null || imageComponent == null)
            {
                return;
            }
            
            // Increment combo
            IncrementCombo(isKill);
            
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }
            
            hitmarkerImage.SetActive(true);
            hideCoroutine = StartCoroutine(FadeHitmarker(fadeInDuration, displayDuration, fadeOutDuration, isKill));
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HitMarker] ERROR in ShowHitmarker(): {ex.Message}");
        }
    }

    private System.Collections.IEnumerator FadeHitmarker(float fadeInTime, float displayTime, float fadeOutTime, bool isKill = false)
    {
        if (imageComponent == null)
        {
            yield break;
        }
        
        // Get current color with custom RGB values
        // If it's a kill, make it red
        Color baseColor;
        if (isKill)
        {
            baseColor = new Color(1f, 0f, 0f, 0f); // Red for kills
        }
        else
        {
            baseColor = new Color(hitmarkerColorR, hitmarkerColorG, hitmarkerColorB, 0f);
        }
        
        // Phase 1: Fade In
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeInTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInTime);
            baseColor.a = alpha;
            imageComponent.color = baseColor;
            yield return null;
        }
        
        baseColor.a = 1f;
        imageComponent.color = baseColor;
        
        // Phase 2: Display
        yield return new WaitForSeconds(displayTime);
        
        // Phase 3: Fade Out
        elapsedTime = 0f;
        
        while (elapsedTime < fadeOutTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutTime);
            baseColor.a = alpha;
            imageComponent.color = baseColor;
            yield return null;
        }
        
        baseColor.a = 0f;
        imageComponent.color = baseColor;
        
        if (hitmarkerImage != null)
        {
            hitmarkerImage.SetActive(false);
        }
        
        hideCoroutine = null;
    }

    private void CreateHitmarkerUI()
    {
        try
        {
            GameObject canvasObject = new GameObject("HitMarkerCanvas");
            hitmarkerCanvas = canvasObject.AddComponent<Canvas>();
            hitmarkerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hitmarkerCanvas.sortingOrder = 1;
            
            CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObject.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasObject);
            
            hitmarkerImage = new GameObject("HitMarkerImage");
            hitmarkerImage.transform.SetParent(hitmarkerCanvas.transform, false);
            
            imageComponent = hitmarkerImage.AddComponent<Image>();
            hitmarkerImage.SetActive(false);
            
            hitmarkerRect = hitmarkerImage.GetComponent<RectTransform>();
            hitmarkerRect.anchorMin = new Vector2(0.5f, 0.5f);
            hitmarkerRect.anchorMax = new Vector2(0.5f, 0.5f);
            hitmarkerRect.pivot = new Vector2(0.5f, 0.5f);
            hitmarkerRect.anchoredPosition = Vector2.zero;
            hitmarkerRect.sizeDelta = new Vector2(hitmarkerSize, hitmarkerSize);
            
            imageComponent.color = new Color(1f, 1f, 1f, 0f);
            
            Sprite hitmarkerSprite = null;
            
            try
            {
                byte[] fileData = GetEmbeddedFileBytes("hitmarker.png");
                
                if (fileData != null && fileData.Length > 0)
                {
                    Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    texture.filterMode = FilterMode.Bilinear;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    
                    if (texture.LoadImage(fileData))
                    {
                        hitmarkerSprite = Sprite.Create(
                            texture,
                            new Rect(0, 0, texture.width, texture.height),
                            new Vector2(0.5f, 0.5f),
                            100f
                        );
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HitMarker] Error loading custom hitmarker: {ex.Message}");
            }
            
            if (hitmarkerSprite == null)
            {
                Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[32 * 32];
                
                for (int y = 0; y < 32; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        if (x == 16 || y == 16 || x == 15 || y == 15)
                        {
                            pixels[y * 32 + x] = Color.white;
                        }
                        else
                        {
                            pixels[y * 32 + x] = Color.clear;
                        }
                    }
                }
                
                texture.SetPixels(pixels);
                texture.Apply();
                
                hitmarkerSprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, 32, 32),
                    new Vector2(0.5f, 0.5f),
                    32f
                );
            }
            
            // Store the default sprite for later reset
            defaultHitmarkerSprite = hitmarkerSprite;
            imageComponent.sprite = hitmarkerSprite;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HitMarker] ERROR during UI creation: {ex.Message}");
        }
    }
    
    private void CreateComboCounterUI()
    {
        try
        {
            comboCounterText = new GameObject("ComboCounterText");
            comboCounterText.transform.SetParent(hitmarkerCanvas.transform, false);
            
            comboTextComponent = comboCounterText.AddComponent<Text>();
            
            // Try to find Raft's UI font by searching for existing UI Text components
            Font raftFont = null;
            try
            {
                // Search for existing UI Text components in the game
                Text[] allTexts = Resources.FindObjectsOfTypeAll<Text>();
                foreach (Text text in allTexts)
                {
                    if (text.font != null && text.font.name != "Arial")
                    {
                        raftFont = text.font;
                        break;
                    }
                }
                
                // If not found via Text components, search all fonts
                if (raftFont == null)
                {
                    Font[] allFonts = Resources.FindObjectsOfTypeAll<Font>();
                    foreach (Font font in allFonts)
                    {
                        string fontName = font.name.ToLower();
                        if (fontName.Contains("chinese") || fontName.Contains("rock") || 
                            fontName.Contains("raft") || fontName.Contains("game"))
                        {
                            raftFont = font;
                            break;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[HitMarker] Could not search for Raft fonts: {ex.Message}");
            }
            
            // Use found font or fallback to Arial
            if (raftFont != null)
                comboTextComponent.font = raftFont;
            else
                comboTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            
            comboTextComponent.fontSize = 24;
            comboTextComponent.fontStyle = FontStyle.Bold;
            comboTextComponent.alignment = TextAnchor.MiddleCenter;
            
            // Use default combo color (#F2E2C5)
            comboTextComponent.color = defaultComboColor;
            
            RectTransform comboRect = comboCounterText.GetComponent<RectTransform>();
            comboRect.anchorMin = new Vector2(0.5f, 0.5f);
            comboRect.anchorMax = new Vector2(0.5f, 0.5f);
            comboRect.pivot = new Vector2(0.5f, 0.5f);
            comboRect.anchoredPosition = new Vector2(0f, -70f); // Below hitmarker
            comboRect.sizeDelta = new Vector2(400f, 80f);
            
            // Add outline for better visibility
            Outline outline = comboCounterText.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(3f, 3f);
            
            comboCounterText.SetActive(false);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HitMarker] ERROR during combo UI creation: {ex.Message}");
        }
    }
}

// ============================================================================
// HARMONY PATCHES
// ============================================================================

[HarmonyPatch(typeof(Network_Host), "DamageEntity")]
public class HarmonyPatch_NetworkHost_DamageEntity
{
    private static float healthBeforeDamage = -1f;
    
    [HarmonyPrefix]
    public static void Prefix(
        Network_Entity entity,
        float damage,
        EntityType damageInflictorEntityType)
    {
        try
        {
            healthBeforeDamage = -1f;
            
            if (damage <= 0f || damageInflictorEntityType != EntityType.Player)
            {
                return;
            }
            
            if (entity != null)
            {
                var stats = entity.GetComponent<Stat_Health>();
                if (stats != null)
                {
                    healthBeforeDamage = stats.Value;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HitMarker] ERROR in DamageEntity_Prefix: {ex.Message}");
        }
    }
    
    [HarmonyPostfix]
    public static void Postfix(
        Network_Entity entity,
        Transform hitTransform,
        float damage,
        Vector3 hitPoint,
        Vector3 hitNormal,
        EntityType damageInflictorEntityType)
    {
        try
        {
            if (damage <= 0f || damageInflictorEntityType != EntityType.Player)
            {
                return;
            }
            
            // Check if entity is still alive (has health remaining)
            if (entity != null)
            {
                var stats = entity.GetComponent<Stat_Health>();
                if (stats != null)
                {
                    // Only show hitmarker if entity was alive before the hit
                    if (healthBeforeDamage > 0f)
                    {
                        // Check if this hit killed the entity
                        bool isDeadNow = stats.Value <= 0f;
                        
                        if (HitMarker.Instance != null)
                        {
                            HitMarker.Instance.ShowHitmarker(isDeadNow);
                        }
                    }
                    // If entity was already dead (healthBeforeDamage <= 0), don't show hitmarker at all
                    return;
                }
            }
            
            if (HitMarker.Instance != null)
            {
                HitMarker.Instance.ShowHitmarker(false);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HitMarker] ERROR in DamageEntity_Postfix: {ex.Message}");
        }
    }
}
