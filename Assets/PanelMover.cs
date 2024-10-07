using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelMover : MonoBehaviour
{

    public Vector2 holdingPosition;
    public Vector2 visiblePosition = Vector2.zero;
    public RectTransform panel;

    private void Awake()
    {
        panel = GetComponent<RectTransform>();
        // holdingPosition = panel.anchoredPosition;
    }

    public bool _isVisible = false;
    public bool isVisible
    {
        get { return _isVisible; }
        set
        {
            _isVisible = value;
            if(_isVisible) {
                panel.anchoredPosition = visiblePosition;
                if(isTimed)
                    StartCoroutine(HideAfterTime(showDuration));
            } else {
                panel.anchoredPosition = holdingPosition;
            }
        }
    }

    public bool isTimed = false;
    public float showDuration = 5;

    IEnumerator HideAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        isVisible = false;
    }

    [ContextMenu("TestPanel")]
    public void TestPanel()
    {
        isVisible = true;
    }

    [ContextMenu("DisablePanel")]
    public void DisablePanel()
    {
        isVisible = false;
    }

    [ContextMenu("SetPos")]
    public void SetPos()
    {
        SetVisiblePosition(visiblePosition);
    }

    public void SetVisiblePosition(Vector2 position)
    {
        visiblePosition = position;
        panel.anchoredPosition = visiblePosition;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
