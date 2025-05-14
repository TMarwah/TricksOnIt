using UnityEngine;
using UnityEngine.UIElements;

public class GameUIHandler : MonoBehaviour
{
    public PlayerHealth PlayerHealth;
    public UIDocument UIDoc;

    private Label m_HealthLabel;
    private VisualElement m_HealthBarMask;
    

    private void Start()
    {
        PlayerHealth.OnHealthChange += HealthChanged;
        m_HealthLabel = UIDoc.rootVisualElement.Q<Label>("HealthLabel");
        m_HealthBarMask = UIDoc.rootVisualElement.Q<VisualElement>("HealthBarMask");


        HealthChanged();
    }

        
    void HealthChanged()
    {
        m_HealthLabel.text = $"{PlayerHealth.CurrentHealth}/{PlayerHealth.MaxHealth}";

        float healthRatio = (float)PlayerHealth.CurrentHealth / PlayerHealth.MaxHealth;
        float healthPercent = Mathf.Lerp(8, 88, healthRatio);
        m_HealthBarMask.style.width = Length.Percent(healthPercent);
    }
}