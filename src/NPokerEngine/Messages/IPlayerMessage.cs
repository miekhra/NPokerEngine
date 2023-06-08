namespace NPokerEngine.Messages
{
    public interface IPlayerMessage : IMessage
    {
        public string PlayerUuid { get; set; }
    }
}
