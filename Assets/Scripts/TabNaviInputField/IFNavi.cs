using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class IFNavi : MonoBehaviour
{
    [Header("InputField Connect")]
    public TMP_InputField PrevInputField;
    public TMP_InputField NextInputField;
    
    [Header("Event")]
    public Button EventButton;

    public void MoveFocus(bool isShiftPressed)
    {
        if (isShiftPressed && PrevInputField != null)
            PrevInputField.ActivateInputField();
        else if (NextInputField != null)
            NextInputField.ActivateInputField();
    }

    public void OnSubmit()
    {
        EventButton?.onClick.Invoke();
    }

    #if UNITY_EDITOR
    public void InitFromEditor(TMP_InputField prev, TMP_InputField next)
    {
        PrevInputField = prev;
        NextInputField = next;
    }
    #endif
}