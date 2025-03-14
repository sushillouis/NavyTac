using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChangeUIPanelTheme : MonoBehaviour
{
    public OpenOceanMain chosenPanel;
    public bool basic = false;

    // Start of Basic Panels

    [Header("Basic Panels")]
    [SerializeField]
    private PanelPlus basicLoginPanel;
    [SerializeField]
    private PanelPlus basicMapSelectPanel;
    [SerializeField]
    private PanelPlus basicHostOrJoinPanel;
    [SerializeField]
    private PanelPlus basicMainGamePanel;
    [SerializeField]
    private PanelPlus basicSingleMultiplayerPanel;
    [SerializeField]
    private RectTransform basicNetDebugConsolePanel;

    [Header("Basic Single / Multi player Screen")]
    [SerializeField]
    private Button basicSinglePlayerButton;
    [SerializeField]
    private Button basicMultiPlayerButton;
    [SerializeField]
    private Button basicSingleMultiQuitButton;

    [Header("Basic Host / Join Screen")]
    public TMP_InputField basicipAddressInputField;
    [SerializeField]
    private Button basicHostButton;
    [SerializeField]
    private Button basicClientButton;
    [SerializeField]
    private Button basicHostJoinQuitButton;

    [Header("Basic Login Screen")]
    public TMP_InputField basicLoginNameInputField;
    [SerializeField]
    private Button basicLoginButton;
    [SerializeField]
    private Button basicLoginQuitButton;

    [Header("Basic Map Select Screen")]
    [SerializeField]
    private Button basicStartButton;

    // Start of Metal Panels

    [Header("Metal Panels")]
    [SerializeField]
    private PanelPlus metalLoginPanel;
    [SerializeField]
    private PanelPlus metalMapSelectPanel;
    [SerializeField]
    private PanelPlus metalHostOrJoinPanel;
    [SerializeField]
    private PanelPlus metalMainGamePanel;
    [SerializeField]
    private PanelPlus metalSingleMultiplayerPanel;
    [SerializeField]
    private RectTransform metalNetDebugConsolePanel;

    [Header("Metal Single / Multi player Screen")]
    [SerializeField]
    private Button metalSinglePlayerButton;
    [SerializeField]
    private Button metalMultiPlayerButton;
    [SerializeField]
    private Button metalSingleMultiQuitButton;

    [Header("Metal Host / Join Screen")]
    public TMP_InputField metalipAddressInputField;
    [SerializeField]
    private Button metalHostButton;
    [SerializeField]
    private Button metalClientButton;
    [SerializeField]
    private Button metalHostJoinQuitButton;

    [Header("Metal Login Screen")]
    public TMP_InputField metalLoginNameInputField;
    [SerializeField]
    private Button metalLoginButton;
    [SerializeField]
    private Button metalLoginQuitButton;

    [Header("Metal Map Select Screen")]
    [SerializeField]
    private Button metalStartButton;
    
    
    // Start is called before the first frame update
    void Start()
    {
        basic = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void ChangeTheme()
    {
        if(basic == false)
        {
            // Panels
            chosenPanel.loginPanel = metalLoginPanel;
            chosenPanel.mapSelectPanel = metalMapSelectPanel;
            chosenPanel.HostOrJoinPanel = metalHostOrJoinPanel;
            chosenPanel.MainGamePanel = metalMainGamePanel;
            chosenPanel.SingleMultiplayerPanel = metalSingleMultiplayerPanel;
            chosenPanel.NetDebugConsolePanel = metalNetDebugConsolePanel;

            // Single/Multiplayer
            chosenPanel.SinglePlayerButton = metalSinglePlayerButton;
            chosenPanel.MultiPlayerButton = metalMultiPlayerButton;
            chosenPanel.SingleMultiQuitButton = metalSingleMultiQuitButton;

            // Host/Join
            chosenPanel.ipAddressInputField = metalipAddressInputField;
            chosenPanel.hostButton = metalHostButton;
            chosenPanel.clientButton = metalClientButton;
            chosenPanel.HostJoinQuitButton = metalHostJoinQuitButton;

            //Login
            chosenPanel.loginNameInputField = metalLoginNameInputField;
            chosenPanel.loginButton = metalLoginButton;
            chosenPanel.LoginQuitButton = metalLoginQuitButton;

            //MapSelect
            chosenPanel.startButton = metalStartButton;

            //Change the bool so next time the function is called it changes to the other theme
            basic = true;

        }

        if(basic == true)
        {
            // Panels
            chosenPanel.loginPanel = basicLoginPanel;
            chosenPanel.mapSelectPanel = basicMapSelectPanel;
            chosenPanel.HostOrJoinPanel = basicHostOrJoinPanel;
            chosenPanel.MainGamePanel = basicMainGamePanel;
            chosenPanel.SingleMultiplayerPanel = basicSingleMultiplayerPanel;
            chosenPanel.NetDebugConsolePanel = basicNetDebugConsolePanel;

            // Single/Multiplayer
            chosenPanel.SinglePlayerButton = basicSinglePlayerButton;
            chosenPanel.MultiPlayerButton = basicMultiPlayerButton;
            chosenPanel.SingleMultiQuitButton = basicSingleMultiQuitButton;

            // Host/Join
            chosenPanel.ipAddressInputField = basicipAddressInputField;
            chosenPanel.hostButton = basicHostButton;
            chosenPanel.clientButton = basicClientButton;
            chosenPanel.HostJoinQuitButton = basicHostJoinQuitButton;

            //Login
            chosenPanel.loginNameInputField = basicLoginNameInputField;
            chosenPanel.loginButton = basicLoginButton;
            chosenPanel.LoginQuitButton = basicLoginQuitButton;

            //MapSelect
            chosenPanel.startButton = basicStartButton;

            //Change the bool so next time the function is called it changes to the other theme
            basic = false;

        }
    }
}
