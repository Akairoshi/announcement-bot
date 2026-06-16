namespace AnnouncementBot.WebApi.Telegram.FSM;

public class ConversationStateStorage
{
    private readonly Dictionary<long, IConversationState> _states = new();

    public void Set(long userId, IConversationState state)
        => _states[userId] = state;

    public IConversationState? Get(long userId)
        => _states.TryGetValue(userId, out var state) ? state : null;

    public void Clear(long userId)
        => _states.Remove(userId);
}