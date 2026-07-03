window.pcAssistantDirectoryPicker = {
    bind(inputId, dotNetRef) {
        const input = document.getElementById(inputId);
        if (!input || input.dataset.directoryPickerBound === "true") {
            return;
        }

        input.dataset.directoryPickerBound = "true";
        input.addEventListener("keydown", async (event) => {
            if (!["ArrowDown", "ArrowUp", "Tab", "Enter", "Escape"].includes(event.key)) {
                return;
            }

            const value = input.value.toLowerCase();
            if (!value.includes("@dir:") && !value.includes("@folder:")) {
                return;
            }

            event.preventDefault();
            event.stopPropagation();

            const handled = await dotNetRef.invokeMethodAsync("HandleDirectoryPickerKeyAsync", event.key);
            if (!handled && event.key === "Tab") {
                input.blur();
            }
        });
    }
};
