using Newtonsoft.Json;
using System.Collections;
using System.IO;
using UnityEngine;

public class SaveManager : Singleton<SaveManager>
{
    private const string SAVE_FILE_NAME = "GameData.json";
    public static string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

    private const float AUTO_SAVE_INTERVAL = 10f;
    private Coroutine _coAutoSave;

    #region AutoSave
    public void StartAutoSave()
    {
        StopAutoSave();

        if (_coAutoSave == null)
        {
            _coAutoSave = StartCoroutine(CoAutoSave());
            Debug.Log($"SaveManager: Auto-save started (interval: {AUTO_SAVE_INTERVAL}s)");
        }
    }

    public void StopAutoSave()
    {
        if (_coAutoSave != null)
        {
            StopCoroutine(_coAutoSave);
            _coAutoSave = null;
            Debug.Log("SaveManager: Auto-save stopped");
        }
    }

    private IEnumerator CoAutoSave()
    {
        WaitForSeconds wait = new WaitForSeconds(AUTO_SAVE_INTERVAL);

        while (true)
        {
            Save();
            yield return wait;
        }
    }
    #endregion

    public void Save()
    {
        GameData gameData = GameManager.Instance.GameData;
        if (gameData == null)
        {
            Debug.Log("SaveManager: GameData is null, cannot save.");
            return;
        }

        string json = JsonConvert.SerializeObject(gameData);
        File.WriteAllText(SavePath, json);
        Debug.Log($"SaveManager: Game saved to {SavePath}");
    }

    public void Load()
    {
        // 세이브 파일이 없으면 기본 데이터로 시작
        if (File.Exists(SavePath) == false)
        {
            Debug.Log("SaveManager: No save file found. Starting with default data.");
            Reset();
            return;
        }

        try
        {
            string json = File.ReadAllText(SavePath);

            // 빈 문자열 또는 "null" 텍스트는 역직렬화 결과가 null이 되므로 명시적으로 예외 발생
            GameData loaded = JsonConvert.DeserializeObject<GameData>(json);
            if (loaded == null)
                throw new InvalidDataException("역직렬화 결과가 null입니다.");

            GameManager.Instance.GameData = loaded;
            Debug.Log($"SaveManager: Game loaded from {SavePath}");
        }
        catch (System.Exception ex)
        {
            // 손상된 파일 로드 실패 — JSON 파싱 오류, IO 오류, null 결과 등 모두 동일하게 처리
            Debug.LogError($"SaveManager: 세이브 파일 로드 실패. 기본 데이터로 복구합니다. path={SavePath}, ex={ex.GetType().Name}: {ex.Message}");

            // 손상 파일 백업 시도 — 실패해도 복구는 계속 진행
            string corruptedPath = SavePath + ".corrupted";
            try
            {
                // File.Move overwrite 오버로드 미지원 환경 대응 — 대상 존재 시 삭제 후 이동
                if (File.Exists(corruptedPath))
                    File.Delete(corruptedPath);
                File.Move(SavePath, corruptedPath);
                Debug.LogWarning($"SaveManager: 손상 파일을 {corruptedPath} 로 백업했습니다.");
            }
            catch (System.Exception backupEx)
            {
                Debug.LogWarning($"SaveManager: 손상 파일 백업 실패(무시). ex={backupEx.Message}");
            }

            // 기본 데이터로 복구 및 정상 디스크 상태 복원
            Reset();
        }
    }

    public void Reset()
    {
        GameData gameData = new GameData()
        {
            Gold = DataManager.Instance.GameConfig.InitialGold,
            Level = DataManager.Instance.GameConfig.InitialLevel
        };

        GameManager.Instance.GameData = gameData;
        Save();
    }

    public void Delete()
    {
        if (File.Exists(SavePath) == false)
        {
            Debug.LogWarning("SaveManager: No save file to delete.");
            return;
        }

        File.Delete(SavePath);
        Debug.Log("SaveManager: Save file deleted.");
    }
}
