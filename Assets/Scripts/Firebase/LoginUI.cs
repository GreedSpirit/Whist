using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginUI : MonoBehaviour
{
    public const string lobbySceneName = "LobbyScene";

    [Header ("Input Fields")]
    [SerializeField] private TMP_InputField _emailIDInputField;
    [SerializeField] private TMP_InputField _passwordInputField;
    [SerializeField] private TMP_InputField _registerEmailIDInputField;
    [SerializeField] private TMP_InputField _registerPasswordInputField;
    [SerializeField] private TMP_InputField _registernicknameInputField;

    [Header("Buttons")]
    [SerializeField] private Button _loginButton;
    [SerializeField] private Button _registerPanelButton;
    [SerializeField] private Button _gameExitButton;
    [SerializeField] private Button _registerButton;
    [SerializeField] private Button _registerpanelExitButton;
    [SerializeField] private Button _enterButton;
    [SerializeField] private Button _returnButton;

    [Header("RegisterPanel")]
    [SerializeField] private GameObject _registerPanel;

    [Header("EnterPanel")]
    [SerializeField] private GameObject _enterPanel;
    [SerializeField] private TextMeshProUGUI _nicknameText;

    private void Start()
    {
        AuthManager.Instance.OnLoginDone += EnterPanelActivate;
        AuthManager.Instance.OnRegisterDone += RegisterPanelActivate;
        AuthManager.Instance.OnRegisterDone += RegisterIFClear;

        _loginButton.onClick.AddListener(LoginClick);
        _registerPanelButton.onClick.AddListener(() => RegisterPanelActivate(true));
        _gameExitButton.onClick.AddListener(ExitGame);

        _registerButton.onClick.AddListener(RegisterClick);
        _registerpanelExitButton.onClick.AddListener(() => RegisterPanelActivate(false));
        _registerpanelExitButton.onClick.AddListener(() => RegisterIFClear(true));

        _enterButton.onClick.AddListener(EnterClick);
        _returnButton.onClick.AddListener(() => EnterPanelActivate(false));
        
    }

    private void LoginClick()
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

    private void RegisterClick()
    {
        string email = _registerEmailIDInputField.text;
        string password = _registerPasswordInputField.text;
        string nickname = _registernicknameInputField.text;

        if(string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(nickname))
        {
            Debug.LogWarning("비어있는 필드가 있습니다");
            return;
        }

        AuthManager.Instance.Register(email, password, nickname);
    }

    private void RegisterPanelActivate(bool set)
    {
        LoginUIActivate(!set);

        _registerPanel.SetActive(set);
    }

    private void RegisterIFClear(bool set)
    {
        if (!set)
        {
            _emailIDInputField.text = _registerEmailIDInputField.text;
            _passwordInputField.text = _registerPasswordInputField.text;
        }

        _registerEmailIDInputField.text = "";
        _registerPasswordInputField.text = "";
        _registernicknameInputField.text = "";
    }

    private void LoginUIActivate(bool set)
    {
        _emailIDInputField.gameObject.SetActive(set);
        _passwordInputField.gameObject.SetActive(set);
        _loginButton.gameObject.SetActive(set);
        _registerPanelButton.gameObject.SetActive(set);
    }

    private void EnterPanelActivate(bool set)
    {
        LoginUIActivate(!set);

        _nicknameText.text = $"{AuthManager.Instance.CurrentUser.Nickname}님 입장하시겠습니다";
        _enterPanel.SetActive(set);
    }

    private void EnterClick()
    {
        SceneManager.LoadScene(lobbySceneName);
    }

    private void ExitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
