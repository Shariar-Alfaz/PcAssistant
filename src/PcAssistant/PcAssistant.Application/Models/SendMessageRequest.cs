namespace PcAssistant.Application.Models
{
    public class SendMessageRequest
    {
        public string AppPath { get; set; } = string.Empty;
        public string AppDisplayName { get; set; } = string.Empty;
        public IList<string> RecipientNames { get; set; } = [];
        public string Message { get; set; } = string.Empty;
        public ushort SearchShortCutKey { get; set; } = VirtualKeys.K;
    }

    public static class VirtualKeys
    {
        public const ushort Control = 0x11;
        public const ushort Menu = 0x12;
        public const ushort Enter = 0x0D;
        public const ushort V = 0x56;
        public const ushort K = 0x4B;
        public const ushort F = 0x46;
        public const ushort E = 0x45;
        public const ushort N = 0x4E;
        public const ushort A = 0x41;
    }
}
