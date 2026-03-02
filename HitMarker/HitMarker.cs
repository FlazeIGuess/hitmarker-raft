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
    
    // State
    private Coroutine hideCoroutine;
    
    // Extra Settings API integration
    static bool ExtraSettingsAPI_Loaded = false;

    public void Start()
    {
        Instance = this;
        
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
    }
    
    // ============================================================================
    // EXTRA SETTINGS API INTEGRATION
    // ============================================================================
    
    /// <summary>
    /// Called when Extra Settings API loads this mod's settings
    /// </summary>
    public void ExtraSettingsAPI_Load()
    {
        LoadSettingsFromAPI();
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
            
            UpdateHitmarkerSize();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HitMarker] Error loading settings: {ex.Message}");
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
    
    // ============================================================================
    // EXTRA SETTINGS API STUB METHODS
    // These are replaced by the API at runtime
    // ============================================================================
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float ExtraSettingsAPI_GetSliderValue(string SettingName) => 0f;

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
            
            if (hitmarkerCanvas != null)
            {
                Destroy(hitmarkerCanvas.gameObject);
                hitmarkerCanvas = null;
                hitmarkerImage = null;
                imageComponent = null;
            }
            
            Instance = null;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HitMarker] ERROR during OnModUnload: {ex.Message}");
            
            harmony = null;
            hideCoroutine = null;
            hitmarkerCanvas = null;
            hitmarkerImage = null;
            imageComponent = null;
            Instance = null;
        }
    }

    public void ShowHitmarker()
    {
        try
        {
            if (hitmarkerImage == null || imageComponent == null)
            {
                return;
            }
            
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }
            
            hitmarkerImage.SetActive(true);
            hideCoroutine = StartCoroutine(FadeHitmarker(fadeInDuration, displayDuration, fadeOutDuration));
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HitMarker] ERROR in ShowHitmarker(): {ex.Message}");
        }
    }

    private System.Collections.IEnumerator FadeHitmarker(float fadeInTime, float displayTime, float fadeOutTime)
    {
        if (imageComponent == null)
        {
            yield break;
        }
        
        // Phase 1: Fade In
        float elapsedTime = 0f;
        Color color = imageComponent.color;
        
        while (elapsedTime < fadeInTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInTime);
            color.a = alpha;
            imageComponent.color = color;
            yield return null;
        }
        
        color.a = 1f;
        imageComponent.color = color;
        
        // Phase 2: Display
        yield return new WaitForSeconds(displayTime);
        
        // Phase 3: Fade Out
        elapsedTime = 0f;
        
        while (elapsedTime < fadeOutTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutTime);
            color.a = alpha;
            imageComponent.color = color;
            yield return null;
        }
        
        color.a = 0f;
        imageComponent.color = color;
        
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
            hitmarkerCanvas.sortingOrder = 1000;
            
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
            
            imageComponent.sprite = hitmarkerSprite;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HitMarker] ERROR during UI creation: {ex.Message}");
        }
    }
}

// ============================================================================
// HARMONY PATCHES
// ============================================================================

[HarmonyPatch(typeof(Network_Host), "DamageEntity")]
public class HarmonyPatch_NetworkHost_DamageEntity
{
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
            
            if (HitMarker.Instance != null)
            {
                HitMarker.Instance.ShowHitmarker();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HitMarker] ERROR in DamageEntity_Postfix: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(AI_StateMachine_Boar), "OnDamageTaken")]
public class HarmonyPatch_Boar_OnDamageTaken
{
    [HarmonyPostfix]
    public static void Postfix(
        AI_StateMachine_Boar __instance,
        float damage,
        Vector3 hitPoint,
        Vector3 hitNormal,
        EntityType damageType)
    {
        try
        {
            if (damage <= 0f || damageType == EntityType.None)
            {
                return;
            }
            
            if (HitMarker.Instance != null)
            {
                HitMarker.Instance.ShowHitmarker();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HitMarker] ERROR in OnDamageTaken_Boar_Postfix: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(AI_StateMachine_Animal), "OnDamageTaken")]
public class HarmonyPatch_Animal_OnDamageTaken
{
    [HarmonyPostfix]
    public static void Postfix(
        AI_StateMachine_Animal __instance,
        float damage,
        Vector3 hitPoint,
        Vector3 hitNormal,
        EntityType damageType)
    {
        try
        {
            if (damage <= 0f || damageType == EntityType.None)
            {
                return;
            }
            
            if (HitMarker.Instance != null)
            {
                HitMarker.Instance.ShowHitmarker();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HitMarker] ERROR in OnDamageTaken_Animal_Postfix: {ex.Message}");
        }
    }
}
