using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public interface IValidate
{
    bool Validate();
}

public interface IDataLoader<Key, Value> : IValidate
{
    Dictionary<Key, Value> MakeDict();
}

public class DataManager : Singleton<DataManager>
{
    private HashSet<IValidate> _loaders = new HashSet<IValidate>();

    public GameConfig GameConfig { get; private set; }
    public LocalizationConfig LocalizationConfig { get; private set; }
    public AdsConfig AdsConfig { get; private set; }
    public IAPConfig IAPConfig { get; private set; }
    // 환경별 서버 URL 설정 — ApiClient가 이 값을 참조하여 요청 URL 조립
    public NetworkConfig NetworkConfig { get; private set; }
    // 인증 관련 설정 (Google Sign-In Web Client ID 등)
    public AuthConfig AuthConfig { get; private set; }

    public Dictionary<string, TextData> TextDict { get; private set; } = new Dictionary<string, TextData>();
    public Dictionary<int, ItemData> ItemDict { get; private set;  } = new Dictionary<int, ItemData>();

    // 어느 진입 경로에서 호출되어도 중복 실행을 방지하는 가드 포함
    public void LoadData()
    {
        // 이미 적재된 경우 중복 실행 방지
        if (GameConfig != null) return;

        GameConfig = LoadScriptableObject<GameConfig>("GameConfig");
        LocalizationConfig = LoadScriptableObject<LocalizationConfig>("LocalizationConfig");
        AdsConfig = LoadScriptableObject<AdsConfig>("AdsConfig");
        IAPConfig = LoadScriptableObject<IAPConfig>("IAPConfig");
        // 환경별 서버 URL 설정 로드
        NetworkConfig = LoadScriptableObject<NetworkConfig>("NetworkConfig");
        // 인증 설정 로드 — GoogleSignInProvider가 WebClientId 참조
        AuthConfig = LoadScriptableObject<AuthConfig>("AuthConfig");

        // LoadJson이 null 반환 시(파일 미존재) 빈 딕셔너리로 안전하게 초기화
        TextDict = LoadJson<TextDataLoader, string, TextData>("TextData")?.MakeDict() ?? new Dictionary<string, TextData>();
        ItemDict = LoadJson<ItemDataLoader, int, ItemData>("ItemData")?.MakeDict() ?? new Dictionary<int, ItemData>();
        // TODO

        Validate();
    }

    // 개발 편의용 자동 초기화 — 어느 씬에서 시작해도 DataManager가 초기화되도록 보장
    // AfterSceneLoad를 사용하는 이유: BeforeSceneLoad 시점엔 ResourceManager 캐시가 비어 있어
    // Get<T>()가 null을 반환하므로, 씬 로드 완료 후 ResourceManager.LoadAll → LoadData 순서를 보장
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInit()
    {
        // ResourceManager 전체 캐시 프리로드 후 즉시 데이터 적재
        // LoadAll은 Resources.LoadAll 동기 foreach → onComplete 즉시 호출이므로
        // AfterSceneLoad → Start() 사이에 완전히 완료됨
        ResourceManager.Instance.LoadAll(
            onProgress: null,
            onComplete: () => Instance.LoadData()
        );
    }

    private T LoadScriptableObject<T>(string path) where T : ScriptableObject
    {
        T asset = ResourceManager.Instance.Get<T>(path);
        if (asset == null)
            Debug.LogError($"Failed to load ScriptableObject at path: {path}");

        return asset;
    }

    private Loader LoadJson<Loader, Key, Value>(string path) where Loader : IDataLoader<Key, Value>
    {
        TextAsset textAsset = ResourceManager.Instance.Get<TextAsset>(path);
        // JSON 파일 미존재 시 NPE 방지 — Resources/PreLoad 하위에 파일이 없으면 발생
        if (textAsset == null)
        {
            Debug.LogError($"[DataManager] JSON 파일을 찾을 수 없습니다: {path}");
            return default;
        }

        Loader loader = JsonConvert.DeserializeObject<Loader>(textAsset.text);
        _loaders.Add(loader);
        Debug.Log(path);

        return loader;
    }

    private bool Validate()
    {
        bool success = true;

        foreach (IValidate loader in _loaders)
        {
            if (loader.Validate() == false)
                success = false;
        }

        _loaders.Clear();

        return success;
    }
}
