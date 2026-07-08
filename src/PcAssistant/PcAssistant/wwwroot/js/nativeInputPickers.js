(() => {
    const pickerTypes = new Set(["date", "time", "datetime-local", "month", "week"]);

    function isPickerInput(element) {
        return element instanceof HTMLInputElement
            && pickerTypes.has(element.type)
            && !element.disabled
            && !element.readOnly;
    }

    function isIconZone(input, event) {
        const rect = input.getBoundingClientRect();
        const iconZoneWidth = Math.min(48, rect.width);
        return event.clientX >= rect.right - iconZoneWidth;
    }

    function openPicker(input) {
        input.focus({ preventScroll: true });
        if (typeof input.showPicker === "function") {
            input.showPicker();
        }
    }

    document.addEventListener("pointerdown", (event) => {
        const input = event.target;
        if (!isPickerInput(input) || !isIconZone(input, event)) {
            return;
        }

        event.preventDefault();
        openPicker(input);
    }, true);

    document.addEventListener("keydown", (event) => {
        const input = event.target;
        if (!isPickerInput(input) || !["Enter", " ", "ArrowDown"].includes(event.key)) {
            return;
        }

        event.preventDefault();
        openPicker(input);
    }, true);
})();
