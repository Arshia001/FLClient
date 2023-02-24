using CafeBazaar.Games;
using CafeBazaar.Games.BasicApi;
using LightMessage.Common.Connection;
using LightMessage.Common.Util;
using Network;
using Network.Types;
using NotSoSimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

enum ServiceAvailability
{
    Active,
    Inaccessible,
    DownForMaintenance
}

enum AvailabilityCheckResult
{
    DontConnect,
    ConnectToProductionServer,
    ConnectToDevelopmentServer,
    ConnectToLocalServer
}

public class Startup : SingletonBehaviour<Startup>
{
    [SerializeField] LogLevel debugBuildNetworkLogLevel = LogLevel.Info;
    [SerializeField] string productionServerHostName = "";
    [SerializeField] string[] productionServerPredefinedAddresses = default;
    [SerializeField] string developmentServerHostName = "";
    [SerializeField] string[] developmentServerPredefinedAddresses = default;
    [SerializeField] int serverPort = default;

    [SerializeField] string serviceStatusHostName = "";
    [SerializeField] int serviceStatusPort = 5000;

    [SerializeField] bool skipAvailabilityCheckInEditor = false;
    [SerializeField] bool useLocalHostForAvailabilityCheck = false;
    [SerializeField] AvailabilityCheckResult editorConnectionMode = AvailabilityCheckResult.ConnectToProductionServer;

    HttpClient httpClient = new HttpClient();

    TaskCompletionSource<object> retryTCS;
    TaskCompletionSource<Guid?> loginTCS;

    GameObject retryButton;
    TextMeshProUGUI statusText;
    TextMeshProUGUI errorText;

    GameObject loginContainer;
    LoginStartup login;
    ForgotPasswordStartup forgotPassword;
    SelectLoginMethodStartup selectLoginMethod;

    CancellationTokenSource startCts;

    AvailabilityCheckResult availabilityCheckResult;

    protected override void Awake()
    {
        base.Awake();

        loginContainer = transform.Find("Login").gameObject;
        login = loginContainer.transform.Find("Frame/Login").GetComponent<LoginStartup>();
        forgotPassword = loginContainer.transform.Find("Frame/ForgotPassword").GetComponent<ForgotPasswordStartup>();
        selectLoginMethod = loginContainer.transform.Find("Frame/SelectLoginMethod").GetComponent<SelectLoginMethodStartup>();

        statusText = transform.Find("Status").GetComponent<TextMeshProUGUI>();
        errorText = transform.Find("ErrorDescription").GetComponent<TextMeshProUGUI>();
        retryButton = transform.Find("Retry").gameObject;

        loginContainer.SetActive(false);

        startCts = new CancellationTokenSource();
    }

    void Start()
    {
        TaskExtensions.RunIgnoreAsync(async () =>
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            if (IdleChecker.Instance.IsIdle)
                await DialogBox.Instance.Show("مثل اینه که مدتیه نیستی. هر وقت برگشتی خبر بده دوباره بریم تو بازی!", "بریم!");

            var connectionManager = ConnectionManager.Instance;

            while (true)
            {
                retryButton.SetActive(false);
                Translation.SetTextNoShape(statusText, "");

                try
                {
                    if (startCts.IsCancellationRequested)
                        return;

                    availabilityCheckResult = await CheckAvailabilityAndVersion();
                    if (availabilityCheckResult == AvailabilityCheckResult.DontConnect)
                    {
                        Application.Quit();
                        return;
                    }

                    if (startCts.IsCancellationRequested)
                        return;

                    await InitializeConnectionAndSetClientID(connectionManager);

                    if (startCts.IsCancellationRequested)
                        return;

                    await GetInitialClientData(connectionManager);

                    if (startCts.IsCancellationRequested)
                        return;

                    RegisterFirebaseTokenIfAvailable();

                    LoadGame();

                    break;
                }
                catch (Exception ex)
                {
                    if (startCts.IsCancellationRequested)
                        return;

                    connectionManager.Disconnect();
                    Debug.LogException(ex);

                    retryTCS = new TaskCompletionSource<object>();

                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        Translation.SetTextNoTranslate(statusText, $"ما نتونستیم به سرور بازی وصل شیم. مطمئنی اینترنتت وصله؟");
#if DEBUG
                        Translation.SetTextNoShape(errorText, GetExceptionDisplayString(ex));
#endif
                        retryButton.SetActive(true);
                    });

                    await retryTCS.Task;
                    retryTCS = null;

                    if (startCts.IsCancellationRequested)
                        return;

                    continue;
                }
            }
        });
    }

    public (LogLevel logLevel, string hostName, IPAddress[] predefinedIPs, int port) GetConnectionProperties()
    {
        var logLevel = Debug.isDebugBuild ? debugBuildNetworkLogLevel : LogLevel.Warning;

        switch (availabilityCheckResult)
        {
            case AvailabilityCheckResult.ConnectToDevelopmentServer:
                return (logLevel, developmentServerHostName, developmentServerPredefinedAddresses.Select(s => IPAddress.Parse(s)).ToArray(), serverPort);

            case AvailabilityCheckResult.ConnectToProductionServer:
                return (logLevel, productionServerHostName, productionServerPredefinedAddresses.Select(s => IPAddress.Parse(s)).ToArray(), serverPort);

            case AvailabilityCheckResult.ConnectToLocalServer:
                return (logLevel, null, new[] { IPAddress.Loopback }, serverPort);

            case AvailabilityCheckResult.DontConnect:
            default:
                throw new Exception("Cannot connect to server at this time");
        }
    }

    public Task<Guid> Connect(HandShakeMode mode, Guid? clientID, string email, string password, string bazaarToken)
    {
        var (logLevel, hostName, ips, port) = GetConnectionProperties();
        return ConnectionManager.Instance.Connect(logLevel, hostName, ips, port, mode, clientID, email, password, bazaarToken);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        startCts.Cancel();
    }

    void RegisterFirebaseTokenIfAvailable()
    {
        var token = FirebaseManager.Instance.Token;
        if (token != null)
            ConnectionManager.Instance.EndPoint<SystemEndPoint>().RegisterFcmToken(token);

#if UNITY_EDITOR
        if (availabilityCheckResult == AvailabilityCheckResult.ConnectToLocalServer)
        {
            Debug.LogWarning("Editor connecting to local server, will set mock FCM token");
            ConnectionManager.Instance.EndPoint<SystemEndPoint>().RegisterFcmToken("MOCK_FCM_TOKEN_FOR_LOCAL_SERVER");
        }
#endif
    }

    async Task InitializeConnectionAndSetClientID(ConnectionManager connectionManager)
    {
        MainThreadDispatcher.Instance.Enqueue(() =>
        {
            Translation.SetTextNoTranslate(statusText, "در حال اتصال...");
            Translation.SetTextNoShape(errorText, "");
        });

        connectionManager.Disconnect();

        var ds = DataStore.Instance;

        if (ds.HaveBazaarToken)
        {
            var loggedIn = await DoBazaarLogin(true);
            if (loggedIn)
            {
                var bgp = BazaarGamesPlatform.Instance;
                await Connect(HandShakeMode.BazaarToken, null, null, null, bgp.GetUserId());
                return;
            }
            else
            {
                loggedIn = await DoBazaarLogin(false);
                if (loggedIn)
                {
                    var bgp = BazaarGamesPlatform.Instance;
                    await Connect(HandShakeMode.BazaarToken, null, null, null, bgp.GetUserId());
                    return;
                }
                else
                    InformationToast.Instance.Enqueue("خطا در ورود به حساب کافه‌بازار");
            }
                
        }

        var clientID = ds.ClientID.Value;

        if (clientID == null)
        {
            clientID = await DoLogin();
            if (clientID == null)
                clientID = await Connect(HandShakeMode.ClientID, null, null, null, null);
        }
        else
            clientID = await Connect(HandShakeMode.ClientID, ds.ClientID, null, null, null);

        DataStore.Instance.ClientID.Value = clientID;
    }

    public BazaarGamesPlatform InitializeBazaarGamesPlatform()
    {
        var config = new BazaarGamesClientConfiguration.Builder().Build();
        BazaarGamesPlatform.InitializeInstance(config);
        return BazaarGamesPlatform.Activate();
    }

    Task<bool> DoBazaarLogin(bool silent)
    {
        var tcs = new TaskCompletionSource<bool>();
        var bgp = InitializeBazaarGamesPlatform();
        bgp.Authenticate(silent, response => tcs.TrySetResult(response));
        return tcs.Task;
    }

    async Task GetInitialClientData(ConnectionManager connectionManager)
    {
        MainThreadDispatcher.Instance.Enqueue(() => Translation.SetTextNoTranslate(statusText, "در حال دریافت اطلاعات حساب کاربری..."));

        var td = TransientData.Instance;
        td.Initialize();

        var (info, configData, goldPacks, coinRewardVideo, getCategoryAnswersVideo, gifts, avatarParts) =
            await connectionManager.EndPoint<SystemEndPoint>().GetStartupInfo();

        td.InitializeData(info, configData, coinRewardVideo, getCategoryAnswersVideo, gifts, avatarParts);

        IabManager.Instance.Initialize(goldPacks);

        AvatarPartRepository.Instance.Initialize(avatarParts);

        await GameRepository.Instance.RefreshSimpleInfoes();
    }

    void LoadGame()
    {
        MainThreadDispatcher.Instance.Enqueue(() => Translation.SetTextNoTranslate(statusText, "در حال بارگذاری بازی..."));

        SceneManager.LoadSceneAsync("Menu");
    }

#if DEBUG
    string GetExceptionDisplayString(Exception ex)
    {
        IEnumerable<Exception> GetSelfAndInnerExceptions(Exception e)
        {
            while (e != null)
            {
                yield return e;
                e = e.InnerException;
            }
        }

        return string.Join(" --> ", GetSelfAndInnerExceptions(ex).Select(e => $"{e.GetType().Name}: {e.Message}"));
    }
#endif

    async Task<AvailabilityCheckResult> CheckAvailabilityAndVersion()
    {
        if (!Application.isMobilePlatform && skipAvailabilityCheckInEditor)
            return editorConnectionMode;

        var version = GetVersion();
        Debug.Log("Client version: " + version.ToString());

        MainThreadDispatcher.Instance.Enqueue(() => Translation.SetTextNoTranslate(statusText, "در حال دریافت اطلاعات نسخه..."));

        var (availability, latest, minimum, lastCompatible) = await GetVersionInfoFromServer();

        if (availability == ServiceAvailability.DownForMaintenance)
        {
            await DialogBox.Instance.Show("ما داریم بازی رو به‌روزرسانی می‌کنیم و به زودی با امکانات جدید در دسترس قرار می‌گیره! بعدا بهمون سر بزن.", "باشه");
            return AvailabilityCheckResult.DontConnect;
        }
        else if (availability == ServiceAvailability.Inaccessible || availability != ServiceAvailability.Active)
        {
            await DialogBox.Instance.Show("با عرض شرمندگی، بازی الان در دسترس نیست. ما داریم رو این مشکل کار می‌کنیم و به زودی حل می‌شه.", "باشه");
            return AvailabilityCheckResult.DontConnect;
        }

        if (version < minimum)
        {
            await DialogBox.Instance.Show("خبر خوب! نسخه جدید بازی برای دانلود آمادست.", "بریم!");
            Application.OpenURL("https://cafebazaar.ir/app/ir.onehand.pwclient");
            return AvailabilityCheckResult.DontConnect;
        }
        else if (version < latest)
        {
            var result = await DialogBox.Instance.Show("خبر خوب! نسخه جدید بازی برای دانلود آمادست. می‌خوای دانلودش کنی؟", "بریم!", "الان نه");
            if (result == DialogBox.Result.Yes)
            {
                Application.OpenURL("https://cafebazaar.ir/app/ir.onehand.pwclient");
                Application.Quit();
                return AvailabilityCheckResult.DontConnect;
            }
        }
        else if (version > lastCompatible)
        {
            var result = await DialogBox.Instance.Show("اخطار: این نسخه هنوز منتشر نشده و اطلاعات حساب اصلی شما در این نسخه تا زمان انتشار رسمی قابل دسترسی نیست. ادامه می‌دهید؟", "بله", "خیر");
            if (result == DialogBox.Result.No)
            {
                Application.OpenURL("https://cafebazaar.ir/app/ir.onehand.pwclient");
                Application.Quit();
                return AvailabilityCheckResult.DontConnect;
            }
            else
                return AvailabilityCheckResult.ConnectToDevelopmentServer;
        }

        return AvailabilityCheckResult.ConnectToProductionServer;
    }

    async Task<(ServiceAvailability availability, uint latest, uint minimumSupported, uint lastCompatible)> GetVersionInfoFromServer()
    {
        var address = Application.isMobilePlatform || !useLocalHostForAvailabilityCheck ? $"{serviceStatusHostName}:{serviceStatusPort}" : "localhost:8075";
        var response = await httpClient.GetAsync($"http://{address}/status");
        var content = await response.Content?.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to contact status monitor service. Response code {response.StatusCode}, reason {response.ReasonPhrase}, content {content}");

        var json = JSON.Parse(content);

        if (!Enum.TryParse<ServiceAvailability>(json["status"].AsString, out var availability))
            throw new Exception($"Unknown availability value: {json["status"].AsString}");

        var latest =
            json["latestVersion"].AsInt.HasValue ?
            (uint)json["latestVersion"].AsInt.Value :
            throw new Exception("Latest version not specified in response");
        var minimum =
            json["minimumSupportedVersion"].AsInt.HasValue ?
            (uint)json["minimumSupportedVersion"].AsInt.Value :
            throw new Exception("Minimum supported version not specified in response");
        var lastCompatible =
            json["lastCompatibleVersion"].AsInt.HasValue ?
            (uint)json["lastCompatibleVersion"].AsInt.Value :
            latest;

        return (availability, latest, minimum, lastCompatible);
    }

    uint GetVersion()
    {
        var t = Resources.Load<TextAsset>("ClientVersion");

        uint result = 0;

        if (t != null)
        {
            if (!uint.TryParse(t.text, out result))
                result = 0;
            Resources.UnloadAsset(t);
        }

        return result;
    }

    public void Retry()
    {
        retryButton.SetActive(false);
        if (retryTCS != null)
            retryTCS.TrySetResult(null);
    }

    async Task<Guid?> DoLogin()
    {
        loginTCS = new TaskCompletionSource<Guid?>();

        ShowSelectLoginMethod();

        return await loginTCS.Task;
    }

    public void SetLoginMethodNewAccount() => CloseLogin(false);

    public void SetLoginMethodLoginWithEmail() => ShowLogin();

    public void SetLoginResult(Guid? id)
    {
        loginTCS?.TrySetResult(id);
        loginContainer.SetActive(false);
    }

    void HideLoginSubmenus()
    {
        forgotPassword.Hide();
        login.Hide();
        selectLoginMethod.Hide();
    }

    public void ShowLogin()
    {
        loginContainer.SetActive(true);
        HideLoginSubmenus();
        login.Show();
    }

    public void ShowForgotPassword()
    {
        loginContainer.SetActive(true);
        HideLoginSubmenus();
        forgotPassword.Show();
    }

    public void ShowSelectLoginMethod()
    {
        loginContainer.SetActive(true);
        HideLoginSubmenus();
        selectLoginMethod.Show();
    }

    public void CloseLogin(bool showLoginTip) => TaskExtensions.RunIgnoreAsync(async () =>
    {
        loginContainer.SetActive(false);
        if (showLoginTip)
            await DialogBox.Instance.Show("بعدا می‌تونی از بخش تنظیمات حساب، وارد حسابت بشی یا ثبت نام کنی.", "باشه");
        SetLoginResult(null);
    });

    public void CloseButtonClicked()
    {
        if (login.isActiveAndEnabled)
            ShowSelectLoginMethod();
        else if (forgotPassword.isActiveAndEnabled)
            ShowLogin();
    }
}
