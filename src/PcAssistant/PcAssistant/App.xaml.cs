using PcAssistant.Application.Abstractions.Services;

namespace PcAssistant
{
    public partial class App : Microsoft.Maui.Controls.Application
    {
        private readonly IAiApiProcessService _aiApiProcess;

        public App(IAiApiProcessService aiApiProcess)
        {
            _aiApiProcess = aiApiProcess;
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new MainPage()) { Title = "PcAssistant" };
            window.Destroying += (_, _) => _aiApiProcess.StopAsync().GetAwaiter().GetResult();
            return window;
        }
    }
}
