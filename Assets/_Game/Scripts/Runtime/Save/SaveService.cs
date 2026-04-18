using System;
using System.IO;
using UnityEngine;

namespace CampusRPG.Save
{
    public sealed class SaveService : MonoBehaviour
    {
        [SerializeField] private string fileName = "slot_auto_chapter01.json";

        public string FullPath => Path.Combine(Application.persistentDataPath, "Save", fileName);

        public void Save(ChapterSaveData data)
        {
            try
            {
                string directory = Path.GetDirectoryName(FullPath);

                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(FullPath, json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"SaveService failed to save chapter data to '{FullPath}': {exception.Message}");
            }
        }

        public bool TryLoad(out ChapterSaveData data)
        {
            data = null;

            if (!File.Exists(FullPath))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(FullPath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    return false;
                }

                data = JsonUtility.FromJson<ChapterSaveData>(json);
                return data != null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"SaveService failed to load chapter data from '{FullPath}': {exception.Message}");
                data = null;
                return false;
            }
        }

        public void DeleteSave()
        {
            try
            {
                if (File.Exists(FullPath))
                {
                    File.Delete(FullPath);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"SaveService failed to delete chapter data at '{FullPath}': {exception.Message}");
            }
        }
    }
}
