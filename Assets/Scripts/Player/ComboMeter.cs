using UnityEngine;
using TMPro;

public class ComboMeter : MonoBehaviour
{
    [Header("Combo Settings")]
    public int maxComboPoints = 100;
    public int pointsPerTrick = 5;
    public int currentComboPoints = 0;

    [Header("UI")]
    public TextMeshProUGUI comboPointsText;
    public TextMeshProUGUI comboRatingText;

    private float pulseTimer = 0f;
    private float pulseSpeed = 2f;
    private float pulseScale = 1.2f;

    private string lastRating = "";
    private int lastPoints = -1;
    private float slamScale = 2.2f;
    private float slamDecaySpeed = 8f;

    void Start()
    {
        UpdateUI();
    }

    void Update()
    {
        // Pulsate the rating letter
        if (comboRatingText != null)
        {
            pulseTimer += Time.deltaTime * pulseSpeed;
            float targetScale = 1f + Mathf.Sin(pulseTimer) * 0.1f * pulseScale;

            if (comboRatingText.transform.localScale.x > 1f)
            {
                float newScale = Mathf.Lerp(comboRatingText.transform.localScale.x, targetScale, Time.deltaTime * slamDecaySpeed);
                comboRatingText.transform.localScale = new Vector3(newScale, newScale, 1f);
            }
            else
            {
                comboRatingText.transform.localScale = new Vector3(targetScale, targetScale, 1f);
            }
        }

        // Pulsate the points number
        if (comboPointsText != null)
        {
            float targetScale = 1f + Mathf.Sin(pulseTimer) * 0.1f * pulseScale;
            if (comboPointsText.transform.localScale.x > 1f)
            {
                float newScale = Mathf.Lerp(comboPointsText.transform.localScale.x, targetScale, Time.deltaTime * slamDecaySpeed);
                comboPointsText.transform.localScale = new Vector3(newScale, newScale, 1f);
            }
            else
            {
                comboPointsText.transform.localScale = new Vector3(targetScale, targetScale, 1f);
            }
        }
    }

    public void AddComboPoint(int amount = 1)
    {
        currentComboPoints = Mathf.Clamp(currentComboPoints + amount, 0, maxComboPoints);
        UpdateUI(true);
    }

    public bool SpendComboPoint(int amount = 1)
    {
        if (currentComboPoints >= amount)
        {
            currentComboPoints -= amount;
            UpdateUI();
            return true;
        }
        return false;
    }

    public bool HasComboPoints(int amount = 1)
    {
        return currentComboPoints >= amount;
    }

    void UpdateUI(bool slamNumber = false)
    {
        // Update number
        if (comboPointsText != null)
        {
            comboPointsText.text = currentComboPoints.ToString();

            // SLAM effect when number changes (from AddComboPoint)
            if (slamNumber && currentComboPoints != lastPoints)
            {
                comboPointsText.transform.localScale = new Vector3(slamScale, slamScale, 0.5f);
                lastPoints = currentComboPoints;
            }
        }

        // Update rating letter
        if (comboRatingText != null)
        {
            string rating = GetComboRating();
            comboRatingText.text = rating;
            comboRatingText.color = GetRatingColor(rating);

            // SLAM effect when the letter changes
            if (slamNumber && rating != lastRating)
            {
                comboRatingText.transform.localScale = new Vector3(slamScale, slamScale, 1f);
                lastRating = rating;
            }
        }
    }

    string GetComboRating()
    {
        float percent = (float)currentComboPoints / maxComboPoints;
        if (percent >= 5f / 6f) return "A";
        if (percent >= 4f / 6f) return "B";
        if (percent >= 3f / 6f) return "C";
        if (percent >= 2f / 6f) return "D";
        if (percent >= 1f / 6f) return "E";
        return "F";
    }

    Color GetRatingColor(string rating)
    {
        switch (rating)
        {
            case "A": return Color.green;
            case "B": return Color.Lerp(Color.green, Color.yellow, 0.5f);
            case "C": return Color.yellow;
            case "D": return new Color(1f, 0.65f, 0f); // orange
            case "E": return new Color(1f, 0.3f, 0f);  // reddish-orange
            case "F": return Color.red;
            default: return Color.white;
        }
    }
}