using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class DrawingSession
{
    public string playerName;
    public string playerId;
    public string sessionId;
    public string timestamp;
    public List<DrawingCommand> commands = new List<DrawingCommand>();
    
    public DrawingSession()
    {
        sessionId = System.Guid.NewGuid().ToString();
        timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        commands = new List<DrawingCommand>();
    }
    
    public DrawingSession(string playerName, string playerId)
    {
        this.playerName = playerName;
        this.playerId = playerId;
        sessionId = System.Guid.NewGuid().ToString();
        timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        commands = new List<DrawingCommand>();
    }
    
    public void AddCommand(DrawingCommand command)
    {
        commands.Add(command);
    }
    
    public void ClearCommands()
    {
        commands.Clear();
    }
    
    public int GetCommandCount()
    {
        return commands.Count;
    }
    
    public DrawingCommand GetLastCommand()
    {
        if (commands.Count > 0)
            return commands[commands.Count - 1];
        return null;
    }
    
    public void RemoveLastCommand()
    {
        if (commands.Count > 0)
            commands.RemoveAt(commands.Count - 1);
    }
    
    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }
    
    public static DrawingSession FromJson(string json)
    {
        return JsonUtility.FromJson<DrawingSession>(json);
    }
    
    public int GetEstimatedSize()
    {
        // Estima o tamanho em bytes do JSON serializado
        return ToJson().Length;
    }
}
