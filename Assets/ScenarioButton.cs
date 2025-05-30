using UnityEngine;
using UnityEngine.UI;

public class ScenarioButton : MonoBehaviour
{
    public int scenarioNumber;
    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        OpenOceanMain.inst.OnScenarioSelected(scenarioNumber);
    }
}