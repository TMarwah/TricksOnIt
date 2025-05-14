using UnityEngine;
using UnityEngine.UIElements;

public class GameUIHandler : MonoBehaviour
{
    public PlayerControl PlayerControl;
    public UIDocument UIDoc;

    private Label m_HealthLabel;

    private void Start()
    {
        PlayerControl.OnHealthChange += HealthChanged;
        m_HealthLabel = UIDoc.rootVisualElement.Q<Label>("HealthLabel");

        HealthChanged();
    }

        
    void HealthChanged()
    {
        m_HealthLabel.text = $"{PlayerControl.CurrentHealth}/{PlayerControl.MaxHealth}";
    }
}