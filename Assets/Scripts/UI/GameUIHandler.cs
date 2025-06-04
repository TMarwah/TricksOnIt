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
        // m_HealthLabel = UIDoc.rootVisualElement.Q<Label>("HealthLabel");
        m_HealthBarMask = UIDoc.rootVisualElement.Q<VisualElement>("HealthBarMask");


        HealthChanged();
    }

        
    void HealthChanged()
    {
        Debug.Log("[GameUIHandler] HealthChanged event received.");

        float normalized = PlayerHealth.HealthNormalized();
        // m_HealthLabel.text = $"{PlayerHealth.CurrentHealth}/{PlayerHealth.MaxHealth}";

        // full bar width is 100% 
        m_HealthBarMask.style.width = Length.Percent(normalized * 100f);
    }
}