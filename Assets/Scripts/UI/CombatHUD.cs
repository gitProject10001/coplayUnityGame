using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimal combat HUD: health bar, stamina bar, ranged charge pips.
/// Attach to a Canvas. Auto-finds PlayerController.
/// </summary>
public class CombatHUD : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;

    [Header("Colors")]
    public Color healthColor = new Color(0.9f, 0.15f, 0.15f);
    public Color healthBgColor = new Color(0.15f, 0.15f, 0.15f);
    public Color staminaColor = new Color(0.15f, 0.8f, 0.35f);
    public Color staminaBgColor = new Color(0.15f, 0.15f, 0.15f);
    public Color chargeActiveColor = new Color(0.95f, 0.8f, 0.2f);
    public Color chargeEmptyColor = new Color(0.2f, 0.2f, 0.2f);

    // Internal references
    private RectTransform healthFill;
    private RectTransform staminaFill;
    private Image[] chargePips;
    private Image healthFlashOverlay;
    private Image staminaFlashOverlay;
    private float healthFlashTimer;
    private float staminaFlashTimer;
    private float lastHealth;
    private float lastStamina;

    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
                player = playerObj.GetComponent<PlayerController>();
        }

        BuildUI();

        if (player != null)
        {
            lastHealth = player.currentHealth;
            lastStamina = player.currentStamina;
        }
    }

    void BuildUI()
    {
        // Container in bottom-left
        RectTransform container = CreatePanel("HUD_Container", transform);
        container.anchorMin = new Vector2(0f, 0f);
        container.anchorMax = new Vector2(0f, 0f);
        container.pivot = new Vector2(0f, 0f);
        container.anchoredPosition = new Vector2(30f, 30f);
        container.sizeDelta = new Vector2(250f, 80f);
        container.GetComponent<Image>().color = Color.clear;

        // Health bar
        float barWidth = 220f;
        float barHeight = 16f;
        healthFill = CreateBar("HealthBar", container, new Vector2(0f, 50f), barWidth, barHeight, healthColor, healthBgColor, out healthFlashOverlay);

        // Stamina bar (slightly narrower, below health)
        staminaFill = CreateBar("StaminaBar", container, new Vector2(0f, 28f), barWidth * 0.8f, barHeight * 0.65f, staminaColor, staminaBgColor, out staminaFlashOverlay);

        // Ranged charge pips
        int maxCharges = player != null ? player.maxRangedCharges : 4;
        chargePips = new Image[maxCharges];
        float pipSize = 12f;
        float pipSpacing = 16f;
        for (int i = 0; i < maxCharges; i++)
        {
            RectTransform pip = CreatePanel("Pip_" + i, container);
            pip.anchorMin = new Vector2(0f, 0f);
            pip.anchorMax = new Vector2(0f, 0f);
            pip.pivot = new Vector2(0f, 0f);
            pip.anchoredPosition = new Vector2(i * pipSpacing, 8f);
            pip.sizeDelta = new Vector2(pipSize, pipSize);
            chargePips[i] = pip.GetComponent<Image>();
            chargePips[i].color = chargeActiveColor;
        }
    }

    RectTransform CreateBar(string name, RectTransform parent, Vector2 position, float width, float height, Color fillColor, Color bgColor, out Image flashOverlay)
    {
        // Background
        RectTransform bg = CreatePanel(name + "_BG", parent);
        bg.anchorMin = new Vector2(0f, 0f);
        bg.anchorMax = new Vector2(0f, 0f);
        bg.pivot = new Vector2(0f, 0f);
        bg.anchoredPosition = position;
        bg.sizeDelta = new Vector2(width, height);
        bg.GetComponent<Image>().color = bgColor;

        // Fill
        RectTransform fill = CreatePanel(name + "_Fill", bg);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(1f, 1f);
        fill.pivot = new Vector2(0f, 0.5f);
        fill.anchoredPosition = Vector2.zero;
        fill.sizeDelta = Vector2.zero;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = fillColor;

        // Flash overlay (white, fades out on damage/use)
        RectTransform flash = CreatePanel(name + "_Flash", bg);
        flash.anchorMin = Vector2.zero;
        flash.anchorMax = new Vector2(1f, 1f);
        flash.anchoredPosition = Vector2.zero;
        flash.sizeDelta = Vector2.zero;
        flash.offsetMin = Vector2.zero;
        flash.offsetMax = Vector2.zero;
        flashOverlay = flash.GetComponent<Image>();
        flashOverlay.color = new Color(1f, 1f, 1f, 0f);

        return fill;
    }

    RectTransform CreatePanel(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        obj.AddComponent<Image>();
        return rt;
    }

    void Update()
    {
        if (player == null) return;

        // Health bar
        float healthPct = player.currentHealth / player.maxHealth;
        healthFill.anchorMax = new Vector2(healthPct, 1f);

        if (player.currentHealth < lastHealth)
        {
            healthFlashTimer = 0.15f;
            healthFlashOverlay.color = new Color(1f, 1f, 1f, 0.6f);
        }
        lastHealth = player.currentHealth;

        // Stamina bar
        float staminaPct = player.currentStamina / player.maxStamina;
        staminaFill.anchorMax = new Vector2(staminaPct, 1f);

        if (player.currentStamina < lastStamina - 5f)
        {
            staminaFlashTimer = 0.1f;
            staminaFlashOverlay.color = new Color(1f, 1f, 1f, 0.4f);
        }
        lastStamina = player.currentStamina;

        // Flash fade
        if (healthFlashTimer > 0f)
        {
            healthFlashTimer -= Time.unscaledDeltaTime;
            Color c = healthFlashOverlay.color;
            c.a = Mathf.Lerp(0f, 0.6f, healthFlashTimer / 0.15f);
            healthFlashOverlay.color = c;
        }
        if (staminaFlashTimer > 0f)
        {
            staminaFlashTimer -= Time.unscaledDeltaTime;
            Color c = staminaFlashOverlay.color;
            c.a = Mathf.Lerp(0f, 0.4f, staminaFlashTimer / 0.1f);
            staminaFlashOverlay.color = c;
        }

        // Ranged charge pips
        for (int i = 0; i < chargePips.Length; i++)
        {
            chargePips[i].color = i < player.currentRangedCharges ? chargeActiveColor : chargeEmptyColor;
        }
    }
}
