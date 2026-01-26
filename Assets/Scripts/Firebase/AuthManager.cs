using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
public class AuthManager : Singleton<AuthManager>
{
    public User CurrentUser {get; private set;}

    private FirebaseAuth _auth; //파이어베이스 인증 진행을 위한 객체
    private DatabaseReference _databaseReference; //DB에 대한 정보를 불러올 수 있는 객체

    public event Action<bool> OnLoginDone;
    public event Action<bool> OnRegisterDone;

    private void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result; //비동기 작업 결과를 기억해서
            if(dependencyStatus == DependencyStatus.Available) // 이용 가능하다는 결과를 받으면
            {
                InitializeFirebase(); // 인증 정보 및 데이터 베이스 정보 기억
            }
            else
            {
                Debug.LogError($"Firebase 초기화 실패 : {dependencyStatus}");
            }
        });
    }

    private void InitializeFirebase()
    {
        _auth = FirebaseAuth.DefaultInstance;
        _databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
        Debug.Log("1. Firebase 초기화 완료");
    }

    public void Login(string email, string password)
    {
        StartCoroutine(LoginCor(email, password));
    }

    public void Register(string email, string password, string nickname)
    {
        StartCoroutine(RegisterCor(email, password, nickname));
    }

    private IEnumerator LoginCor(string email, string password)
    {
        Task<AuthResult> LoginTask = _auth.SignInWithEmailAndPasswordAsync(email, password);

        yield return new WaitUntil(predicate: () => LoginTask.IsCompleted);

        if(LoginTask.Exception != null)
        {
            Debug.Log("다음과 같은 이유로 로그인 실패 : " + LoginTask.Exception);

            FirebaseException firebaseEx = LoginTask.Exception.GetBaseException() as FirebaseException;
            AuthError errorCode = (AuthError) firebaseEx.ErrorCode;

            string message = "";

            switch (errorCode)
            {
                case AuthError.MissingEmail:
                    message = "이메일 누락";
                    break;
                case AuthError.MissingPassword:
                    message = "패스워드 누락";
                    break;
                case AuthError.WrongPassword:
                    message = "패스워드 틀림";
                    break;
                case AuthError.InvalidEmail:
                    message = "이메일 형식이 옳지 않음";
                    break;
                case AuthError.UserNotFound:
                    message = "아이디가 존재하지 않음";
                    break;
                default:
                    message = "관리자에게 문의 바랍니다";
                    break;
            }
            Debug.LogWarning(message);
        }
        else
        {
            FirebaseUser user = LoginTask.Result.User;
            Debug.Log("로그인 인증 성공, DB 데이터 로드 시도");

            StartCoroutine(LoadUserData(user.UserId));
        }
    }

    private IEnumerator LoadUserData(string userId)
    {
        Task<DataSnapshot> dbTask = _databaseReference.Child("users").Child(userId).GetValueAsync();
        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if(dbTask.Exception != null)
        {
            Debug.LogError("DB 데이터 로드 실패 : " + dbTask.Exception);
        }
        else
        {
            // DB에 Json으로 저장했으니 User 객체로 변환 시켜주기
            DataSnapshot snapshot = dbTask.Result;
            string json = snapshot.GetRawJsonValue();

            CurrentUser = JsonUtility.FromJson<User>(json);

            Debug.Log($"데이터 로드 완료. 읽어온 닉네임 {CurrentUser.Nickname}");

            OnLoginDone?.Invoke(true);
        }
    }

    private IEnumerator RegisterCor(string email, string password, string userName)
    {
        Task<AuthResult> RegisterTask = _auth.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(predicate: () => RegisterTask.IsCompleted);

        if (RegisterTask.Exception != null)
        {
            Debug.LogWarning(message: "실패 사유" + RegisterTask.Exception);
            FirebaseException firebaseEx = RegisterTask.Exception.GetBaseException() as FirebaseException;
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
            string message = "회원가입 실패";
            switch (errorCode)
            {
                case AuthError.MissingEmail:
                    message = "이메일 누락";
                    break;
                case AuthError.MissingPassword:
                    message = "패스워드 누락";
                    break;
                case AuthError.WeakPassword:
                    message = "패스워드 약함";
                    break;
                case AuthError.EmailAlreadyInUse:
                    message = "중복 이메일";
                    break;
                default:
                    message = "기타 사유. 관리자 문의 바람";
                    break;
            }
            Debug.LogWarning(message);
        }
        else
        {
            FirebaseUser newUser = RegisterTask.Result.User;
            CreateUserInitData(newUser.UserId, userName);
        }
    }

    private void CreateUserInitData(string userId, string nickname)
    {
        User newUser = new User(nickname);
        string json = JsonUtility.ToJson(newUser);

        _databaseReference.Child("users").Child(userId).SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("DB 데이터 생성 성공");
                CurrentUser = newUser;
                OnRegisterDone?.Invoke(false);
                OnLoginDone?.Invoke(true);
            }
        });
    }

    public void UpdatePlayerStats(bool isWin)
    {
        if(CurrentUser == null) return;

        if(isWin) CurrentUser.Wins++;
        else CurrentUser.Loses++;

        string json = JsonUtility.ToJson(CurrentUser);
        _databaseReference.Child("users").Child(_auth.CurrentUser.UserId).SetRawJsonValueAsync(json);

        Debug.Log($"전적 업데이트 : {CurrentUser.Wins}승 {CurrentUser.Loses}패");
    }

    public void FetchAllUsers(Action<List<User>> onLoaded)
    {
        _databaseReference.Child("users").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("랭킹 데이터 로드 실패");
                return;
            }

            List<User> userList = new List<User>();
            DataSnapshot snapshot = task.Result;

            foreach (DataSnapshot child in snapshot.Children)
            {
                string json = child.GetRawJsonValue();
                if (!string.IsNullOrEmpty(json))
                {
                    User user = JsonUtility.FromJson<User>(json);
                    userList.Add(user);
                }
            }
            
            // 콜백으로 데이터 전달
            onLoaded?.Invoke(userList);
        });
    }
}
