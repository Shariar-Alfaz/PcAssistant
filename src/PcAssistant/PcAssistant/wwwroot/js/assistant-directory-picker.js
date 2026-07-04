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

            if (!input.value.includes("@")) {
                return;
            }

            event.preventDefault();
            event.stopPropagation();

            const handled = await dotNetRef.invokeMethodAsync("HandleDirectoryPickerKeyAsync", event.key);
            if (handled) {
                return;
            }

            if (event.key === "Tab") {
                input.blur();
            }
        });
    },
    focus(inputId) {
        requestAnimationFrame(() => {
            const input = document.getElementById(inputId);
            if (!input) {
                return;
            }

            input.focus({ preventScroll: true });
            const length = input.value.length;
            input.setSelectionRange(length, length);
        });
    }
};
