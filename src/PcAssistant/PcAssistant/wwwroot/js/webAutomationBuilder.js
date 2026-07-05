window.pcAssistantWebAutomation = {
    initializeSortableSteps(elementId, dotNetRef) {
        const element = document.getElementById(elementId);
        if (!element) {
            return;
        }

        this.destroySortableSteps(elementId);

        if (window.Sortable) {
            const sortable = window.Sortable.create(element, {
                animation: 150,
                draggable: "[data-step-id]",
                onEnd: () => this.notifyStepOrder(element, dotNetRef)
            });
            element.__pcAssistantSortable = sortable;
            return;
        }

        const state = { dragged: null };
        const onDragStart = (event) => {
            const card = event.target.closest("[data-step-id]");
            if (!card) {
                return;
            }

            state.dragged = card;
            card.classList.add("opacity-60");
            event.dataTransfer.effectAllowed = "move";
        };
        const onDragOver = (event) => {
            event.preventDefault();
            const target = event.target.closest("[data-step-id]");
            if (!target || !state.dragged || target === state.dragged) {
                return;
            }

            const rect = target.getBoundingClientRect();
            const after = event.clientY > rect.top + rect.height / 2;
            target.parentNode.insertBefore(state.dragged, after ? target.nextSibling : target);
        };
        const onDragEnd = () => {
            if (state.dragged) {
                state.dragged.classList.remove("opacity-60");
            }

            state.dragged = null;
            this.notifyStepOrder(element, dotNetRef);
        };

        element.querySelectorAll("[data-step-id]").forEach((card) => card.setAttribute("draggable", "true"));
        element.addEventListener("dragstart", onDragStart);
        element.addEventListener("dragover", onDragOver);
        element.addEventListener("dragend", onDragEnd);
        element.__pcAssistantNativeSortable = { onDragStart, onDragOver, onDragEnd };
    },

    destroySortableSteps(elementId) {
        const element = document.getElementById(elementId);
        if (!element) {
            return;
        }

        if (element.__pcAssistantSortable) {
            element.__pcAssistantSortable.destroy();
            delete element.__pcAssistantSortable;
        }

        const native = element.__pcAssistantNativeSortable;
        if (native) {
            element.removeEventListener("dragstart", native.onDragStart);
            element.removeEventListener("dragover", native.onDragOver);
            element.removeEventListener("dragend", native.onDragEnd);
            delete element.__pcAssistantNativeSortable;
        }
    },

    notifyStepOrder(element, dotNetRef) {
        const ids = Array.from(element.querySelectorAll("[data-step-id]")).map((item) => item.dataset.stepId);
        return dotNetRef.invokeMethodAsync("OnStepOrderChanged", ids);
    },

    captureSelector(frameId, dotNetRef) {
        return this.capturePreviewElement(
            frameId,
            dotNetRef,
            "NotifySelectorCaptured",
            "Click an element inside the preview to capture a CSS selector. The element under your pointer is highlighted.",
            "Selector capture is not available for this preview. Open headed browser mode and manually enter selector.");
    },

    captureClickTarget(frameId, dotNetRef) {
        return this.capturePreviewElement(
            frameId,
            dotNetRef,
            "NotifyClickTargetCaptured",
            "Click the element in the preview that this automation should click. The real page click is blocked during targeting.",
            "Click targeting is not available for this preview. Open headed browser mode and manually enter a click selector.");
    },

    captureInputTarget(frameId, dotNetRef) {
        return this.capturePreviewElement(
            frameId,
            dotNetRef,
            "NotifyInputTargetCaptured",
            "Click an input, textarea, select, or editable field in the preview. The real page click is blocked during targeting.",
            "Input targeting is not available for this preview. Open headed browser mode and manually enter an input selector.",
            (target) => this.resolveInputTarget(target));
    },

    capturePreviewElement(frameId, dotNetRef, callbackName, successMessage, failureMessage, targetResolver) {
        const frame = document.getElementById(frameId);
        if (!frame || !frame.contentDocument) {
            return failureMessage;
        }

        try {
            const doc = frame.contentDocument;
            this.ensurePreviewHighlightStyles(doc);
            this.clearPreviewHighlight(frame);
            const handler = (event) => {
                event.preventDefault();
                event.stopPropagation();
                const target = targetResolver ? targetResolver(event.target) : event.target;
                if (!target) {
                    return;
                }

                const selector = this.buildCssSelector(target);
                frame.dataset.lastSelector = selector;
                this.clearPreviewHighlight(frame);
                target.classList.add("pc-assistant-automation-target");
                doc.removeEventListener("click", handler, true);
                doc.removeEventListener("mouseover", hoverHandler, true);
                dotNetRef?.invokeMethodAsync(callbackName, selector);
            };
            const hoverHandler = (event) => {
                this.clearPreviewHover(frame);
                const target = targetResolver ? targetResolver(event.target) : event.target;
                (target || event.target).classList.add("pc-assistant-automation-hover");
            };
            doc.addEventListener("mouseover", hoverHandler, true);
            doc.addEventListener("click", handler, true);
            return successMessage;
        } catch {
            return failureMessage;
        }
    },

    resolveInputTarget(target) {
        if (!target || target.nodeType !== Node.ELEMENT_NODE) {
            return null;
        }

        const direct = target.closest("input, textarea, select, [contenteditable='true'], [role='textbox'], [role='combobox']");
        if (direct) {
            return direct;
        }

        const label = target.closest("label");
        if (label) {
            if (label.control) {
                return label.control;
            }

            const forId = label.getAttribute("for");
            if (forId) {
                return label.ownerDocument.getElementById(forId);
            }

            return label.querySelector("input, textarea, select, [contenteditable='true'], [role='textbox'], [role='combobox']");
        }

        return null;
    },

    highlightPreviewSelection(frameId, selectorType, selector, value) {
        const frame = document.getElementById(frameId);
        if (!frame || !frame.contentDocument) {
            return;
        }

        try {
            const doc = frame.contentDocument;
            this.ensurePreviewHighlightStyles(doc);
            this.clearPreviewHighlight(frame);

            const matches = this.resolvePreviewMatches(doc, selectorType, selector, value);
            matches.slice(0, 8).forEach((element) => element.classList.add("pc-assistant-automation-match"));
            const target = matches[0];
            if (target) {
                target.classList.add("pc-assistant-automation-target");
                target.scrollIntoView({ block: "center", inline: "center", behavior: "smooth" });
            }
        } catch {
            // Cross-origin and CSP-protected pages cannot be marked inside the inline preview.
        }
    },

    resolvePreviewMatches(doc, selectorType, selector, value) {
        const type = (selectorType || "Css").toLowerCase();
        const targetValue = value || selector || "";
        if (!selector && !targetValue) {
            return [];
        }

        if (type === "xpath") {
            const result = doc.evaluate(selector, doc, null, XPathResult.ORDERED_NODE_SNAPSHOT_TYPE, null);
            const nodes = [];
            for (let index = 0; index < result.snapshotLength; index += 1) {
                const node = result.snapshotItem(index);
                if (node?.nodeType === Node.ELEMENT_NODE) {
                    nodes.push(node);
                }
            }
            return nodes;
        }

        if (type === "text") {
            return this.findElementsByText(doc, targetValue);
        }

        if (type === "role") {
            return this.findElementsByRole(doc, selector, targetValue);
        }

        if (type === "label") {
            return this.findElementsByLabel(doc, targetValue);
        }

        if (type === "placeholder") {
            return Array.from(doc.querySelectorAll("[placeholder]"))
                .filter((element) => element.getAttribute("placeholder")?.toLowerCase().includes(targetValue.toLowerCase()));
        }

        if (type === "testid") {
            return Array.from(doc.querySelectorAll(`[data-testid="${this.escapeAttribute(targetValue)}"], [data-test-id="${this.escapeAttribute(targetValue)}"], [data-test="${this.escapeAttribute(targetValue)}"]`));
        }

        return Array.from(doc.querySelectorAll(selector));
    },

    findElementsByText(doc, text) {
        const normalized = text.toLowerCase().trim();
        if (!normalized) {
            return [];
        }

        return Array.from(doc.body.querySelectorAll("*"))
            .filter((element) => !["SCRIPT", "STYLE", "NOSCRIPT"].includes(element.tagName))
            .filter((element) => (element.innerText || element.textContent || "").toLowerCase().includes(normalized));
    },

    findElementsByRole(doc, role, name) {
        const normalizedRole = (role || "").toLowerCase();
        const roleSelector = normalizedRole ? `[role="${this.escapeAttribute(normalizedRole)}"], ${this.nativeRoleSelector(normalizedRole)}` : "*";
        return Array.from(doc.querySelectorAll(roleSelector))
            .filter((element) => !name || (element.innerText || element.textContent || element.getAttribute("aria-label") || "").toLowerCase().includes(name.toLowerCase()));
    },

    nativeRoleSelector(role) {
        switch (role) {
            case "button":
                return "button, input[type='button'], input[type='submit']";
            case "link":
                return "a[href]";
            case "textbox":
                return "input:not([type]), input[type='text'], input[type='email'], input[type='search'], input[type='url'], textarea";
            case "checkbox":
                return "input[type='checkbox']";
            case "radio":
                return "input[type='radio']";
            case "combobox":
                return "select";
            default:
                return "[data-pc-assistant-no-native-role]";
        }
    },

    findElementsByLabel(doc, text) {
        const normalized = text.toLowerCase();
        const labels = Array.from(doc.querySelectorAll("label"))
            .filter((label) => (label.innerText || label.textContent || "").toLowerCase().includes(normalized));
        return labels.flatMap((label) => {
            const control = label.control || (label.getAttribute("for") ? doc.getElementById(label.getAttribute("for")) : null);
            return control ? [control] : Array.from(label.querySelectorAll("input, textarea, select, button"));
        });
    },

    ensurePreviewHighlightStyles(doc) {
        if (doc.getElementById("pc-assistant-automation-highlight-styles")) {
            return;
        }

        const style = doc.createElement("style");
        style.id = "pc-assistant-automation-highlight-styles";
        style.textContent = `
            .pc-assistant-automation-match {
                outline: 2px dashed rgba(14, 165, 233, 0.75) !important;
                outline-offset: 3px !important;
                box-shadow: 0 0 0 6px rgba(14, 165, 233, 0.16) !important;
            }
            .pc-assistant-automation-hover,
            .pc-assistant-automation-target {
                outline: 3px solid #facc15 !important;
                outline-offset: 4px !important;
                box-shadow: 0 0 0 8px rgba(250, 204, 21, 0.24), 0 0 24px rgba(250, 204, 21, 0.55) !important;
            }
        `;
        doc.head.appendChild(style);
    },

    clearPreviewHighlight(frame) {
        this.clearPreviewHover(frame);
        const doc = frame.contentDocument;
        doc.querySelectorAll(".pc-assistant-automation-match, .pc-assistant-automation-target")
            .forEach((element) => element.classList.remove("pc-assistant-automation-match", "pc-assistant-automation-target"));
    },

    clearPreviewHover(frame) {
        const doc = frame.contentDocument;
        doc.querySelectorAll(".pc-assistant-automation-hover")
            .forEach((element) => element.classList.remove("pc-assistant-automation-hover"));
    },

    escapeAttribute(value) {
        return String(value || "").replace(/\\/g, "\\\\").replace(/"/g, "\\\"");
    },

    buildCssSelector(element) {
        if (element.id) {
            return `#${CSS.escape(element.id)}`;
        }

        const parts = [];
        let current = element;
        while (current && current.nodeType === Node.ELEMENT_NODE && parts.length < 4) {
            let part = current.tagName.toLowerCase();
            if (current.classList.length > 0) {
                part += `.${Array.from(current.classList).slice(0, 3).map((item) => CSS.escape(item)).join(".")}`;
            }
            parts.unshift(part);
            current = current.parentElement;
        }

        return parts.join(" > ");
    }
};
