using UnityEngine;
using UnityEngine.UI;

public class ComboMeter : MonoBehaviour
{
    [Header("Combo Settings")]
    public int maxComboPoints = 10;
    public int pointsPerTrick = 1;
    public int currentComboPoints = 0;

    [Header("UI")]
    public Slider comboSlider; // Assign a Unity UI Slider in the inspector

    void Start()
    {
        UpdateUI();
    }

    public void AddComboPoint(int amount = 1)
    {
        currentComboPoints = Mathf.Clamp(currentComboPoints + amount, 0, maxComboPoints);
        UpdateUI();
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

    void UpdateUI()
    {
        if (comboSlider != null)
        {
            comboSlider.maxValue = maxComboPoints;
            comboSlider.value = currentComboPoints;
        }
    }
}