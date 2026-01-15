using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class TabNaviManager : MonoBehaviour
{
    private InputAction mTabAction;
    private InputAction mSubmitAction;

    private void Awake()
    {
        // Tab Action
        mTabAction = new InputAction("TabNavi", binding: "<Keyboard>/tab");
        mTabAction.performed += OnTabPerformed;

        // Submit Action
        mSubmitAction = new InputAction("Submit", binding: "<Keyboard>/enter");
        mSubmitAction.performed += OnSubmitPerformed;
    }

    private void OnEnable()
    {
        mTabAction.Enable();
        mSubmitAction.Enable();
    }

    private void OnDisable()
    {
        mTabAction.Disable();
        mSubmitAction.Disable();
    }
    
    private void OnDestroy()
    {
        mTabAction.Dispose();
        mSubmitAction.Dispose();
    }

    private void OnTabPerformed(InputAction.CallbackContext context)
    {
        // 현재 유니티 EventSystem에서 선택된 오브젝트 가져오기
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
        
        if (currentSelected == null) return;

        if (currentSelected.TryGetComponent<IFNavi>(out var navi))
        {
            bool isShift = Keyboard.current.shiftKey.isPressed;
            navi.MoveFocus(isShift);
        }
    }

    private void OnSubmitPerformed(InputAction.CallbackContext context)
    {
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
        if (currentSelected == null) return;

        if (currentSelected.TryGetComponent<IFNavi>(out var navi))
        {
            navi.OnSubmit();
        }
    }
}