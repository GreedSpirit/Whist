using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginUI : MonoBehaviour
{
    [Header ("Input Fields")]
    [SerializeField] private TMP_InputField _emailIDInputField;
    [SerializeField] private TMP_InputField _passwordInputField;
    [SerializeField] private TMP_InputField _nicknameInputField;

    [Header("Buttons")]
    [SerializeField] Button _loginButton;
    [SerializeField] Button _registerButton;

    private void Start()
    {
        _loginButton.onClick.AddListener(LoginClick);
        _registerButton.onClick.AddListener(RegisterClick);
    }

    void LoginClick()
    {
        string email = _emailIDInputField.text;
        string password = _passwordInputField.text;

        if(string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("이메일 혹은 비밀번호가 비어있습니다.");
            return;
        }

        AuthManager.Instance.Login(email, password);
    }

    void RegisterClick()
    {
        string email = _emailIDInputField.text;
        string password = _passwordInputField.text;
        string nickname = _nicknameInputField.text;

        if(string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(nickname))
        {
            Debug.LogWarning("비어있는 필드가 있습니다");
            return;
        }

        AuthManager.Instance.Register(email, password, nickname);
    }


}
