#if UNITY_EDITOR

using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// TMP_InputField를 자식으로 2개 이상 갖고 있는 게임 오브젝트를 선택한 뒤 Tools에서 사용하면 자동으로 넣어주도록 만든 스크립트
/// </summary>

public class UiTabNavigationSimpleSetting : EditorWindow
{
    [MenuItem("Tools/Tab Navigation Setting")]
    private static void SetInputFieldNavi()
    {
        // 현재 선택된 게임 오브젝트 가져오기
        GameObject selectedObject = Selection.activeGameObject;

        if (selectedObject == null)
        {
            Debug.LogWarning("선택된 오브젝트 없음");
            return;
        }

        List<TMP_InputField> inputFields = new List<TMP_InputField>();
        for (int i = 0; i < selectedObject.transform.childCount; i++)
        {
            TMP_InputField inputField = null;

            if (selectedObject.transform.GetChild(i).TryGetComponent<TMP_InputField>(out inputField))
                inputFields.Add(inputField);
        }


        if (inputFields.Count == 0 || inputFields.Count == 1)
        {
            Debug.Log("Navi가 없어도 됨");
            return;
        }

        for (int i = 0; i < inputFields.Count; i++)
        {
            IFNavi uiTabNavi = null;

            if (inputFields[i].TryGetComponent<IFNavi>(out uiTabNavi) == false)
                uiTabNavi = inputFields[i].gameObject.AddComponent<IFNavi>();

            uiTabNavi.InitFromEditor(i == 0 ? inputFields[inputFields.Count - 1] : inputFields[i - 1], i == inputFields.Count - 1 ? inputFields[0] : inputFields[i + 1]);
        }

        Debug.Log("Navi 세팅 완료(할당할 이벤트 발생 버튼이 있다면 따로 할당)");
    }
}
#endif