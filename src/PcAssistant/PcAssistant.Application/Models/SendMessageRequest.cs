namespace PcAssistant.Application.Models
{
    public class SendMessageRequest
    {
        public string AppPath { get; set; }
        public IList<string> RecipientNames { get; set; }
        public string Message { get; set; }
        public ushort SearchShortCutKey { get; set; } = VirtualKeys.K;
    }

    public static class VirtualKeys
    {
        public const ushort Control = 0x11;
        public const ushort Enter = 0x0D;
        public const ushort V = 0x56;
        public const ushort K = 0x4B;
        public const ushort F = 0x46;
    }
}
