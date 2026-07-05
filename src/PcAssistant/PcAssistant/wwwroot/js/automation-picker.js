window.pcAssistantAutomationPicker = {
    bind(rootId, dotNetRef) {
        if (window.pcAssistantAutomationPickerState?.rootId === rootId) {
            window.pcAssistantAutomationPickerState.dotNetRef = dotNetRef;
            return;
        }

        window.pcAssistantAutomationPickerState = {
            rootId,
            dotNetRef
        };

        document.addEventListener("pointerdown", async (event) => {
            const state = window.pcAssistantAutomationPickerState;
            const root = document.getElementById(state?.rootId);
            if (!root || root.contains(event.target)) {
                return;
            }

            await state.dotNetRef.invokeMethodAsync("CloseAppOptionsFromOutsideAsync");
        }, true);
    }
};
