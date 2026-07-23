using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AARScenarioValue : MonoBehaviour
{
    public int scenarioNumber;
    void Update()
    {
    this.GetComponent<Button>().onClick.AddListener(() =>
      {
        OpenOceanMain.inst.lobbyState = LobbyState.AAR;
        OpenOceanMain.inst.AARPanelSetTexts(scenarioNumber);

      });
    }
}
