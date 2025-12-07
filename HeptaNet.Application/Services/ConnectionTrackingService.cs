using System.Collections.Concurrent;

namespace HeptaNet.Application.Services;

public interface IConnectionTrackingService
{
    void AddConnection(int userId, string connectionId);
    void RemoveConnection(int userId, string connectionId);
    IEnumerable<string> GetConnections(int userId);
    bool IsUserOnline(int userId);
    IEnumerable<int> GetOnlineUsers();
    int GetOnlineUserCount();
}

public class ConnectionTrackingService : IConnectionTrackingService
{
    private readonly ConcurrentDictionary<int, HashSet<string>> _connections = new();
    
    public void AddConnection(int userId, string connectionId)
    {
        _connections.AddOrUpdate(
            userId,
            new HashSet<string> { connectionId },
            (key, existingSet) =>
            {
                lock (existingSet)
                {
                    existingSet.Add(connectionId);
                }
                return existingSet;
            });
    }
    
    public void RemoveConnection(int userId, string connectionId)
    {
        if (_connections.TryGetValue(userId, out var connections))
        {
            lock (connections)
            {
                connections.Remove(connectionId);
                
                if (connections.Count == 0)
                {
                    _connections.TryRemove(userId, out _);
                }
            }
        }
    }
    
    public IEnumerable<string> GetConnections(int userId)
    {
        if (_connections.TryGetValue(userId, out var connections))
        {
            lock (connections)
            {
                return connections.ToList();
            }
        }
        return Enumerable.Empty<string>();
    }
    
    public bool IsUserOnline(int userId)
    {
        return _connections.ContainsKey(userId);
    }
    
    public IEnumerable<int> GetOnlineUsers()
    {
        return _connections.Keys.ToList();
    }
    
    public int GetOnlineUserCount()
    {
        return _connections.Count;
    }
}


