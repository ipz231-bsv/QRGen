using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

public class QRCodeHistory
{
    public required string Text { get; set; }
    public required string FilePath { get; set; }
}

public class QRCodeHistoryService
{
    private readonly string _historyFilePath = "qrcodes_history.json";

    // Метод для збереження історії QR-кодів
    public void SaveHistory(List<QRCodeHistory> history)
    {
        string json = JsonConvert.SerializeObject(history, Formatting.Indented);
        File.WriteAllText(_historyFilePath, json);
    }

    // Метод для завантаження історії QR-кодів
    public List<QRCodeHistory> LoadHistory()
    {
        if (!File.Exists(_historyFilePath))
        {
            return new List<QRCodeHistory>();
        }

        string json = File.ReadAllText(_historyFilePath);
        return JsonConvert.DeserializeObject<List<QRCodeHistory>>(json) ?? new List<QRCodeHistory>();
    }
}