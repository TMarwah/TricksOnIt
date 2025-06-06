using UnityEngine;
using TMPro;

public class ComboMeter : MonoBehaviour
{
    [Header("Combo Settings")]
    public int maxComboPoints = 100;
    public int pointsPerTrick = 5;
    public float currentComboPoints = 0f;
    public float scoreDrainRate = 0.01f;
    public float scoreDrainDelay = 5f; // Delay before draining starts

    private float lastScoreIncreaseTime = 0f; // Tracks the last time combo points were added

    [Header("UI")]
    public TextMeshProUGUI comboPointsText;
    public TextMeshProUGUI comboRatingText;

    private float pulseTimer = 0f;
    private float pulseSpeed = 2f;
    private float pulseScale = 1.2f;

    private string lastRating = "";
    private float lastPoints = -1f;
    private float slamScale = 2.2f;
    private float slamDecaySpeed = 8f;
    public AudioClip trickSuccessSFX;

    void Start()
    {
        UpdateUI();
    }

    void Update()
    {
        // Drain combo points over time
        if (currentComboPoints > 0)
        {
            // Check if enough time has passed since the last score increase
            if (Time.time - lastScoreIncreaseTime >= scoreDrainDelay)
            {
                currentComboPoints = Mathf.Max(0, currentComboPoints - (scoreDrainRate * (Time.time - lastScoreIncreaseTime)));
            }
        }

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
        UpdateUI();
    }

    public void AddComboPoint(int amount = 1)
    {
        currentComboPoints = Mathf.Clamp(currentComboPoints + amount, 0, maxComboPoints);
        lastScoreIncreaseTime = Time.time; // Update the last score increase time
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
            comboPointsText.text = Mathf.FloorToInt(currentComboPoints).ToString();

            // SLAM effect when number changes (from AddComboPoint)
            if (slamNumber && Mathf.Abs(currentComboPoints - lastPoints) >= 5)
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
                AudioSource.PlayClipAtPoint(trickSuccessSFX, transform.position);
                comboRatingText.transform.localScale = new Vector3(slamScale, slamScale, 1f);
                lastRating = rating;
            }
        }
    }

    string GetComboRating()
    {
        float percent = (float)currentComboPoints / maxComboPoints;
        if (percent >= 5f / 6f) return "S";
        if (percent >= 4f / 6f) return "A";
        if (percent >= 3f / 6f) return "B";
        if (percent >= 2f / 6f) return "C";
        if (percent >= 1f / 6f) return "D";
        return "F";
    }

    Color GetRatingColor(string rating)
    {
        switch (rating)
        {
            case "S": return new Color(Random.value, Random.value, Random.value);
            case "A": return Color.green;
            case "B": return Color.Lerp(Color.green, Color.yellow, 0.5f);
            case "C": return Color.yellow;
            case "D": return new Color(1f, 0.65f, 0f); // orange
            case "F": return Color.red;
            default: return Color.white;
        }
    }
}