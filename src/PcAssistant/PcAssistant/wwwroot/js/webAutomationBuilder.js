window.pcAssistantWebAutomation = {
    initializeBuilderSurface(surfaceId) {
        const surface = document.getElementById(surfaceId);
        if (!surface) {
            return;
        }

        this.destroyBuilderSurface(surfaceId);
        this.initializeToolCapture(surface);
        this.bindToolDragCursors(surface);

        if (!window.gsap) {
            return;
        }

        const ambient = surface.querySelector(".web-automation-ambient");
        const panels = surface.querySelectorAll(".web-automation-panel-sticky > section, .web-automation-panel-stack > section");
        const timeline = window.gsap.timeline();
        timeline.fromTo(
            surface.querySelectorAll("header, .web-automation-panel-sticky, [data-web-step-list]"),
            { y: 14, opacity: 0 },
            { y: 0, opacity: 1, duration: 0.45, stagger: 0.045, ease: "power2.out" });

        if (ambient) {
            const sweep = window.gsap.to(ambient, {
                "--pc-sweep": "100%",
                duration: 8,
                repeat: -1,
                yoyo: true,
                ease: "sine.inOut"
            });
            const drift = window.gsap.to(ambient, {
                backgroundPosition: "120px 80px",
                duration: 16,
                repeat: -1,
                yoyo: true,
                ease: "sine.inOut"
            });
            surface.__pcAssistantBuilderTweens = [timeline, sweep, drift];
        } else {
            surface.__pcAssistantBuilderTweens = [timeline];
        }

        panels.forEach((panel, index) => {
            window.gsap.fromTo(
                panel,
                { y: 10, opacity: 0.82 },
                { y: 0, opacity: 1, duration: 0.35, delay: index * 0.035, ease: "power2.out" });
        });
    },

    destroyBuilderSurface(surfaceId) {
        const surface = document.getElementById(surfaceId);
        if (!surface) {
            return;
        }

        if (surface.__pcAssistantToolCapture) {
            surface.removeEventListener("pointerdown", surface.__pcAssistantToolCapture, true);
            delete surface.__pcAssistantToolCapture;
        }
        this.unbindToolDragCursors(surface);
        this.destroyPreviewWorkflowSplitter(surfaceId);

        (surface.__pcAssistantBuilderTweens || []).forEach((tween) => tween?.kill?.());
        delete surface.__pcAssistantBuilderTweens;
    },

    initializeToolCapture(surface) {
        const capture = (event) => {
            const tool = event.target.closest("[data-web-tool-type]");
            if (!tool || !surface.contains(tool)) {
                return;
            }

            this.lastToolSource = {
                type: tool.dataset.webToolType,
                label: tool.dataset.webToolLabel || tool.textContent.trim(),
                rect: tool.getBoundingClientRect()
            };
        };

        surface.addEventListener("pointerdown", capture, true);
        surface.__pcAssistantToolCapture = capture;
    },

    bindToolDragCursors(surface) {
        if (surface.__pcAssistantToolDragCursorHandlers) {
            return;
        }

        const toolSelector = "[data-web-tool-type]";
        const setGrab = (event) => {
            const tool = event.target.closest(toolSelector);
            if (tool && surface.contains(tool)) {
                this.setGrabCursor(true, tool);
            }
        };
        const clearGrab = (event) => {
            if (document.documentElement.classList.contains("web-automation-cursor-grabbing")) {
                return;
            }

            if (!event.relatedTarget || !surface.contains(event.relatedTarget)) {
                this.setGrabCursor(false);
                return;
            }

            if (!event.relatedTarget.closest?.(toolSelector)) {
                this.setGrabCursor(false);
            }
        };
        const pointerDown = (event) => {
            const tool = event.target.closest(toolSelector);
            if (tool && surface.contains(tool)) {
                this.setDraggingCursor(true, tool);
            }
        };
        const dragStart = (event) => {
            const tool = event.target.closest(toolSelector);
            if (!tool || !surface.contains(tool)) {
                return;
            }

            this.setDraggingCursor(true, tool);
            event.dataTransfer.effectAllowed = "copy";
            event.dataTransfer.setData("text/plain", tool.dataset.webToolType || "");
        };
        const clearDragging = () => this.setDraggingCursor(false);

        surface.addEventListener("pointerover", setGrab);
        surface.addEventListener("pointerout", clearGrab);
        surface.addEventListener("pointerdown", pointerDown);
        surface.addEventListener("dragstart", dragStart);
        surface.addEventListener("dragend", clearDragging);
        window.addEventListener("pointerup", clearDragging);

        surface.__pcAssistantToolDragCursorHandlers = {
            setGrab,
            clearGrab,
            pointerDown,
            dragStart,
            clearDragging
        };
    },

    unbindToolDragCursors(surface) {
        const handlers = surface.__pcAssistantToolDragCursorHandlers;
        if (!handlers) {
            return;
        }

        surface.removeEventListener("pointerover", handlers.setGrab);
        surface.removeEventListener("pointerout", handlers.clearGrab);
        surface.removeEventListener("pointerdown", handlers.pointerDown);
        surface.removeEventListener("dragstart", handlers.dragStart);
        surface.removeEventListener("dragend", handlers.clearDragging);
        window.removeEventListener("pointerup", handlers.clearDragging);
        delete surface.__pcAssistantToolDragCursorHandlers;
    },

    initializePreviewWorkflowSplitter(surfaceId) {
        const surface = document.getElementById(surfaceId);
        const split = surface?.querySelector("[data-web-preview-workflow-split]");
        const splitter = split?.querySelector("[data-web-preview-workflow-splitter]");
        const previewPane = split?.querySelector(".web-automation-preview-pane");
        if (!surface || !split || !splitter || !previewPane) {
            return;
        }

        this.destroyPreviewWorkflowSplitter(surfaceId);

        const storageKey = split.dataset.webPreviewWorkflowStorageKey || "pc-assistant:web-automation:preview-workflow-split";
        const minHeight = 260;
        const maxHeight = 760;
        let isDragging = false;
        let startY = 0;
        let startHeight = 0;
        let pendingHeight = null;
        let animationFrame = 0;

        const getPreviewContent = () => previewPane.querySelector("iframe, img, [id^='web-preview-root-'] > div:first-child")
            || previewPane.querySelector("[id^='web-preview-root-']");
        const clampHeight = (height) => {
            const splitRect = split.getBoundingClientRect();
            const previewContent = getPreviewContent();
            const previewChrome = previewContent
                ? previewPane.getBoundingClientRect().height - previewContent.getBoundingClientRect().height
                : 0;
            const dynamicMax = Math.max(minHeight, Math.min(maxHeight, splitRect.height - previewChrome - 260));
            return Math.round(Math.min(Math.max(height, minHeight), dynamicMax));
        };
        const setHeight = (height, persist) => {
            const nextHeight = clampHeight(height);
            split.style.setProperty("--web-automation-preview-height", `${nextHeight}px`);
            if (persist) {
                try {
                    localStorage.setItem(storageKey, String(nextHeight));
                } catch {
                    // Ignore storage failures; resizing still works for this session.
                }
            }
        };
        const scheduleHeight = (height) => {
            pendingHeight = height;
            if (animationFrame) {
                return;
            }

            animationFrame = requestAnimationFrame(() => {
                animationFrame = 0;
                setHeight(pendingHeight, false);
            });
        };
        const readStoredHeight = () => {
            try {
                const stored = Number.parseInt(localStorage.getItem(storageKey) || "", 10);
                return Number.isFinite(stored) ? stored : null;
            } catch {
                return null;
            }
        };
        const beginResize = (event) => {
            if (event.button !== undefined && event.button !== 0) {
                return;
            }

            event.preventDefault();
            isDragging = true;
            startY = event.clientY;
            startHeight = getPreviewContent()?.getBoundingClientRect().height
                || previewPane.getBoundingClientRect().height;
            splitter.setPointerCapture?.(event.pointerId);
            split.classList.add("web-automation-split-resizing");
        };
        const resize = (event) => {
            if (!isDragging) {
                return;
            }

            event.preventDefault();
            scheduleHeight(startHeight + event.clientY - startY);
        };
        const endResize = (event) => {
            if (!isDragging) {
                return;
            }

            isDragging = false;
            if (animationFrame) {
                cancelAnimationFrame(animationFrame);
                animationFrame = 0;
            }
            splitter.releasePointerCapture?.(event.pointerId);
            split.classList.remove("web-automation-split-resizing");
            setHeight(pendingHeight ?? getPreviewContent()?.getBoundingClientRect().height ?? startHeight, true);
            pendingHeight = null;
        };
        const reset = () => {
            try {
                localStorage.removeItem(storageKey);
            } catch {
            }

            split.style.removeProperty("--web-automation-preview-height");
        };
        const keyResize = (event) => {
            const currentHeight = getPreviewContent()?.getBoundingClientRect().height
                || previewPane.getBoundingClientRect().height;
            if (event.key === "ArrowUp") {
                event.preventDefault();
                setHeight(currentHeight - 24, true);
            } else if (event.key === "ArrowDown") {
                event.preventDefault();
                setHeight(currentHeight + 24, true);
            } else if (event.key === "Home") {
                event.preventDefault();
                setHeight(minHeight, true);
            } else if (event.key === "End") {
                event.preventDefault();
                setHeight(maxHeight, true);
            }
        };

        const storedHeight = readStoredHeight();
        if (storedHeight !== null) {
            requestAnimationFrame(() => setHeight(storedHeight, false));
        }

        splitter.addEventListener("pointerdown", beginResize);
        splitter.addEventListener("pointermove", resize);
        splitter.addEventListener("pointerup", endResize);
        splitter.addEventListener("pointercancel", endResize);
        splitter.addEventListener("dblclick", reset);
        splitter.addEventListener("keydown", keyResize);
        surface.__pcAssistantPreviewWorkflowSplitter = {
            splitter,
            beginResize,
            resize,
            endResize,
            reset,
            keyResize
        };
    },

    destroyPreviewWorkflowSplitter(surfaceId) {
        const surface = document.getElementById(surfaceId);
        const state = surface?.__pcAssistantPreviewWorkflowSplitter;
        if (!surface || !state) {
            return;
        }

        state.splitter.removeEventListener("pointerdown", state.beginResize);
        state.splitter.removeEventListener("pointermove", state.resize);
        state.splitter.removeEventListener("pointerup", state.endResize);
        state.splitter.removeEventListener("pointercancel", state.endResize);
        state.splitter.removeEventListener("dblclick", state.reset);
        state.splitter.removeEventListener("keydown", state.keyResize);
        delete surface.__pcAssistantPreviewWorkflowSplitter;
    },

    initializeSortableSteps(elementId, dotNetRef) {
        const element = document.getElementById(elementId);
        if (!element) {
            return;
        }

        if (element.__pcAssistantSortable) {
            element.__pcAssistantDotNetRef = dotNetRef;
            this.bindStepHandleCursors(element);
            return;
        }

        if (window.Sortable) {
            const sortable = window.Sortable.create(element, {
                animation: 180,
                easing: "cubic-bezier(0.22, 1, 0.36, 1)",
                dataIdAttr: "data-step-id",
                draggable: "[data-step-id]",
                direction: "vertical",
                emptyInsertThreshold: 24,
                fallbackTolerance: 4,
                filter: "button:not(.web-automation-step-handle), input, select, textarea, a",
                handle: ".web-automation-step-handle",
                preventOnFilter: true,
                ghostClass: "sortable-ghost",
                chosenClass: "sortable-chosen",
                dragClass: "sortable-drag",
                fallbackClass: "sortable-fallback",
                fallbackOnBody: true,
                forceFallback: true,
                swapThreshold: 0.65,
                onChoose: (event) => this.markDragReady(event.item),
                onUnchoose: (event) => this.clearDragReady(event.item),
                onStart: (event) => this.animateDragStart(event.item),
                onClone: (event) => this.prepareDragClone(event.clone),
                onEnd: (event) => {
                    this.cleanupDragState(element, event.item);
                    return this.notifyStepOrder(element, element.__pcAssistantDotNetRef || dotNetRef);
                },
                onCancel: (event) => this.cleanupDragState(element, event.item)
            });
            element.__pcAssistantSortable = sortable;
            element.__pcAssistantDotNetRef = dotNetRef;
            this.bindStepHandleCursors(element);
            this.syncStepOrderSignature(element);
            this.animateStepCards(element);
            return;
        }

        const state = { dragged: null };
        const onDragStart = (event) => {
            const card = event.target.closest("[data-step-id]");
            if (!card) {
                return;
            }

            state.dragged = card;
            this.setDraggingCursor(true);
            card.classList.add("opacity-60");
            element.classList.add("web-automation-step-list-sorting");
            card.classList.add("web-automation-step-native-dragging");
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
                state.dragged.classList.remove("opacity-60", "web-automation-step-native-dragging");
            }

            element.classList.remove("web-automation-step-list-sorting");
            this.setDraggingCursor(false);
            state.dragged = null;
            this.notifyStepOrder(element, dotNetRef);
        };

        element.querySelectorAll("[data-step-id]").forEach((card) => card.setAttribute("draggable", "true"));
        element.addEventListener("dragstart", onDragStart);
        element.addEventListener("dragover", onDragOver);
        element.addEventListener("dragend", onDragEnd);
        element.__pcAssistantNativeSortable = { onDragStart, onDragOver, onDragEnd };
        element.__pcAssistantDotNetRef = dotNetRef;
        this.bindStepHandleCursors(element);
        this.syncStepOrderSignature(element);
        this.animateStepCards(element);
    },

    destroySortableSteps(elementId) {
        const element = document.getElementById(elementId);
        if (!element) {
            return;
        }

        this.setDraggingCursor(false);

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

        this.unbindStepHandleCursors(element);
        delete element.__pcAssistantDotNetRef;
    },

    bindStepHandleCursors(element) {
        if (element.__pcAssistantHandleCursorHandlers) {
            return;
        }

        const setGrab = (event) => {
            const handle = event.target.closest(".web-automation-step-handle");
            if (handle) {
                this.setGrabCursor(true, handle);
            }
        };
        const clearGrab = (event) => {
            if (document.documentElement.classList.contains("web-automation-cursor-grabbing")) {
                return;
            }

            if (!event.relatedTarget || !element.contains(event.relatedTarget)) {
                this.setGrabCursor(false);
                return;
            }

            if (!event.relatedTarget.closest?.(".web-automation-step-handle")) {
                this.setGrabCursor(false);
            }
        };
        const pointerDown = (event) => {
            const handle = event.target.closest(".web-automation-step-handle");
            if (handle) {
                const card = handle.closest("[data-step-id]");
                this.setDraggingCursor(true, card || handle);
            }
        };
        const pointerUp = () => this.setDraggingCursor(false);

        element.addEventListener("pointerover", setGrab);
        element.addEventListener("pointerout", clearGrab);
        element.addEventListener("pointerdown", pointerDown);
        window.addEventListener("pointerup", pointerUp);

        element.__pcAssistantHandleCursorHandlers = { setGrab, clearGrab, pointerDown, pointerUp };
    },

    unbindStepHandleCursors(element) {
        const handlers = element.__pcAssistantHandleCursorHandlers;
        if (!handlers) {
            return;
        }

        element.removeEventListener("pointerover", handlers.setGrab);
        element.removeEventListener("pointerout", handlers.clearGrab);
        element.removeEventListener("pointerdown", handlers.pointerDown);
        window.removeEventListener("pointerup", handlers.pointerUp);
        delete element.__pcAssistantHandleCursorHandlers;
    },

    bindUploadDropZone(elementId, dotNetRef) {
        const element = document.getElementById(elementId);
        if (!element) {
            return;
        }

        if (element.__pcAssistantUploadDrop) {
            element.__pcAssistantUploadDrop.dotNetRef = dotNetRef;
            return;
        }

        const setActive = (active) => {
            element.classList.toggle("web-upload-drop-active", active);
        };
        const prevent = (event) => {
            event.preventDefault();
            event.stopPropagation();
        };
        const dragEnter = (event) => {
            prevent(event);
            setActive(true);
        };
        const dragOver = (event) => {
            prevent(event);
            event.dataTransfer.dropEffect = "copy";
            setActive(true);
        };
        const dragLeave = (event) => {
            prevent(event);
            if (!element.contains(event.relatedTarget)) {
                setActive(false);
            }
        };
        const drop = async (event) => {
            prevent(event);
            setActive(false);
            const files = Array.from(event.dataTransfer?.files || []);
            if (files.length === 0) {
                return;
            }

            const payload = await Promise.all(files.map((file) => this.readDroppedUploadFile(file)));
            const state = element.__pcAssistantUploadDrop;
            await state?.dotNetRef?.invokeMethodAsync("ReceiveDroppedUploadFilesAsync", payload);
        };

        element.addEventListener("dragenter", dragEnter);
        element.addEventListener("dragover", dragOver);
        element.addEventListener("dragleave", dragLeave);
        element.addEventListener("drop", drop);
        element.__pcAssistantUploadDrop = { dotNetRef, dragEnter, dragOver, dragLeave, drop };
    },

    unbindUploadDropZone(elementId) {
        const element = document.getElementById(elementId);
        const state = element?.__pcAssistantUploadDrop;
        if (!element || !state) {
            return;
        }

        element.removeEventListener("dragenter", state.dragEnter);
        element.removeEventListener("dragover", state.dragOver);
        element.removeEventListener("dragleave", state.dragLeave);
        element.removeEventListener("drop", state.drop);
        delete element.__pcAssistantUploadDrop;
    },

    readDroppedUploadFile(file) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onerror = () => reject(reader.error || new Error("Could not read dropped file."));
            reader.onload = () => {
                const result = String(reader.result || "");
                const base64 = result.includes(",") ? result.substring(result.indexOf(",") + 1) : result;
                resolve({ name: file.name || "upload.bin", base64 });
            };
            reader.readAsDataURL(file);
        });
    },

    animateUploadRail(elementId, fileCount) {
        const rail = document.getElementById(elementId);
        const fill = rail?.querySelector(".web-upload-file-rail-fill");
        if (!window.gsap || !rail || !fill) {
            return;
        }

        window.requestAnimationFrame(() => {
            if (!rail.isConnected || !fill.isConnected) {
                return;
            }

            window.gsap.killTweensOf([rail, fill]);
            window.gsap.set(fill, { scaleX: 0, transformOrigin: "left center" });
            window.gsap.timeline({ defaults: { overwrite: true } })
                .fromTo(rail, { opacity: 0.68 }, { opacity: 1, duration: 0.16, ease: "power2.out" })
                .to(fill, { scaleX: 1, duration: 0.42, ease: "power3.out" }, 0)
                .fromTo(
                    fill,
                    { boxShadow: "0 0 0 rgba(52, 211, 153, 0)" },
                    { boxShadow: "0 0 18px rgba(52, 211, 153, 0.34)", duration: 0.24, ease: "sine.out" },
                    0.06)
                .to(fill, { boxShadow: "0 0 0 rgba(52, 211, 153, 0)", duration: 0.38, ease: "sine.inOut" });
        });
    },

    notifyStepOrder(element, dotNetRef) {
        const ids = element.__pcAssistantSortable
            ? element.__pcAssistantSortable.toArray()
            : Array.from(element.querySelectorAll("[data-step-id]")).map((item) => item.dataset.stepId);
        const signature = ids.join("|");
        if (!signature || signature === element.__pcAssistantLastOrderSignature) {
            return Promise.resolve();
        }

        return dotNetRef.invokeMethodAsync("OnStepOrderChanged", ids)
            .then(() => {
                element.__pcAssistantLastOrderSignature = signature;
            });
    },

    syncStepOrderSignature(element) {
        const ids = element.__pcAssistantSortable
            ? element.__pcAssistantSortable.toArray()
            : Array.from(element.querySelectorAll("[data-step-id]")).map((item) => item.dataset.stepId);
        element.__pcAssistantLastOrderSignature = ids.join("|");
    },

    syncStepDomOrder(stepIds) {
        const list = document.querySelector("[data-web-step-list]");
        if (!list || !Array.isArray(stepIds) || stepIds.length === 0) {
            return;
        }

        const ids = stepIds.map((id) => String(id));
        if (list.__pcAssistantSortable) {
            list.__pcAssistantSortable.sort(ids, true);
        } else {
            ids.forEach((id) => {
                const item = list.querySelector(`[data-step-id="${CSS.escape(id)}"]`);
                if (item) {
                    list.appendChild(item);
                }
            });
        }

        list.__pcAssistantLastOrderSignature = ids.join("|");
    },

    animateDragStart(item) {
        this.setDraggingCursor(true, item);
        if (!window.gsap || !item) {
            item?.classList.add("web-automation-step-native-dragging");
            return;
        }

        const list = item.closest("[data-web-step-list]");
        list?.classList.add("web-automation-step-list-sorting");
        item.classList.add("web-automation-step-native-dragging");
        window.gsap.killTweensOf(item);
        window.gsap.fromTo(item, { scale: 1 }, { scale: 1.012, duration: 0.14, ease: "power2.out" });
    },

    markDragReady(item) {
        item?.classList.add("web-automation-step-drag-ready");
    },

    clearDragReady(item) {
        item?.classList.remove("web-automation-step-drag-ready");
    },

    prepareDragClone(clone) {
        if (!clone) {
            return;
        }

        clone.classList.remove("sortable-ghost");
        clone.classList.add("sortable-fallback", "sortable-drag");
        clone.style.pointerEvents = "none";
        clone.style.transition = "none";
        clone.style.zIndex = "100000";
    },

    cleanupDragState(element, item) {
        element?.classList.remove("web-automation-step-list-sorting");
        this.setDraggingCursor(false);
        if (!item) {
            return;
        }

        item.classList.remove(
            "sortable-chosen",
            "sortable-ghost",
            "sortable-drag",
            "sortable-fallback",
            "web-automation-step-drag-ready",
            "web-automation-step-native-dragging");
        if (window.gsap) {
            window.gsap.killTweensOf(item);
            window.gsap.set(item, { clearProps: "transform,opacity,boxShadow,borderColor" });
        } else {
            item.style.transform = "";
            item.style.opacity = "";
        }
    },

    setDraggingCursor(isDragging, target) {
        document.documentElement.classList.toggle("web-automation-cursor-grabbing", isDragging);
        document.body.classList.toggle("web-automation-cursor-grabbing", isDragging);
        this.applyCursorOverride(isDragging ? "grabbing" : null, target);
        if (isDragging) {
            this.setGrabCursor(false);
        }
    },

    setGrabCursor(isGrab, target) {
        document.documentElement.classList.toggle("web-automation-cursor-grab", isGrab);
        document.body.classList.toggle("web-automation-cursor-grab", isGrab);
        this.applyCursorOverride(isGrab ? "grab" : null, target);
    },

    applyCursorOverride(cursor, target) {
        const previous = document.__pcAssistantCursorOverrideTargets || [];
        previous.forEach((element) => element?.style?.removeProperty("cursor"));

        if (!cursor) {
            document.__pcAssistantCursorOverrideTargets = [];
            return;
        }

        const targets = [
            document.documentElement,
            document.body,
            document.querySelector("[data-web-step-list]"),
            target,
            target?.closest?.("[data-step-id]"),
            target?.querySelector?.(".web-automation-step-handle")
        ].filter(Boolean);

        [...new Set(targets)].forEach((element) => {
            element.style.setProperty("cursor", cursor, "important");
        });
        document.__pcAssistantCursorOverrideTargets = targets;
    },

    animateStepCards(element) {
        if (!window.gsap || !element) {
            return;
        }

        const cards = Array.from(element.querySelectorAll("[data-step-id]"));
        window.gsap.fromTo(cards, { y: 8, opacity: 0.84 }, { y: 0, opacity: 1, duration: 0.28, stagger: 0.025, ease: "power2.out" });
    },

    animateStepOrderSaved() {
        if (!window.gsap) {
            return;
        }

        const list = document.querySelector("[data-web-step-list]");
        if (!list) {
            return;
        }

        window.gsap.fromTo(
            list.querySelectorAll("[data-step-id]"),
            { boxShadow: "0 0 0 rgba(56, 189, 248, 0)" },
            { boxShadow: "0 0 26px rgba(56, 189, 248, 0.18)", duration: 0.2, yoyo: true, repeat: 1, stagger: 0.025, ease: "sine.inOut" });
    },

    animateStepDeleted(listId, stepId) {
        const list = document.getElementById(listId);
        const item = list?.querySelector(`[data-step-id="${stepId}"]`);
        return this.animateStepDeletedItem(item);
    },

    animateStepDeletedById(stepId) {
        const item = document.querySelector(`[data-step-id="${stepId}"]`);
        return this.animateStepDeletedItem(item);
    },

    animateStepDeletedItem(item) {
        if (!window.gsap || !item) {
            return Promise.resolve();
        }

        return new Promise((resolve) => {
            window.gsap.timeline({ onComplete: resolve })
                .to(item, {
                    x: 18,
                    scale: 0.985,
                    borderColor: "rgba(251, 113, 133, 0.55)",
                    boxShadow: "0 0 34px rgba(251, 113, 133, 0.22)",
                    duration: 0.16,
                    ease: "power2.out"
                })
                .to(item, {
                    x: -32,
                    height: 0,
                    marginTop: 0,
                    marginBottom: 0,
                    paddingTop: 0,
                    paddingBottom: 0,
                    opacity: 0,
                    scale: 0.96,
                    duration: 0.28,
                    ease: "power2.in"
                });
        });
    },

    animateToast() {
        if (!window.gsap) {
            return;
        }

        const toast = document.querySelector("[data-web-automation-toast]");
        if (!toast) {
            return;
        }

        window.gsap.killTweensOf(toast);
        window.gsap.fromTo(
            toast,
            { y: -18, x: 18, opacity: 0, scale: 0.97 },
            { y: 0, x: 0, opacity: 1, scale: 1, duration: 0.34, ease: "back.out(1.7)" });
        window.gsap.to(toast, { opacity: 0, y: -12, duration: 0.24, delay: 4.2, ease: "power2.in" });
    },

    animateToolAddedToInsertionSlot(stepType) {
        if (!window.gsap) {
            return Promise.resolve();
        }

        const list = document.querySelector("[data-web-step-list]");
        if (!list) {
            return Promise.resolve();
        }

        const source = this.resolveToolAnimationSource(stepType);
        const placeholder = this.createInsertionPlaceholder(list);
        const targetRect = placeholder.getBoundingClientRect();
        if (!source) {
            return new Promise((resolve) => {
                window.gsap.fromTo(
                    placeholder,
                    { height: 0, opacity: 0, scale: 0.98 },
                    {
                        height: 82,
                        opacity: 1,
                        scale: 1,
                        duration: 0.24,
                        ease: "power2.out",
                        onComplete: () => {
                            placeholder.remove();
                            resolve();
                        }
                    });
            });
        }

        const ghost = document.createElement("div");
        ghost.className = "web-automation-tool-ghost";
        ghost.textContent = source.label;
        ghost.style.left = `${source.rect.left}px`;
        ghost.style.top = `${source.rect.top}px`;
        ghost.style.width = `${Math.max(source.rect.width, 150)}px`;
        document.body.appendChild(ghost);

        return new Promise((resolve) => {
            const timeline = window.gsap.timeline({
                onComplete: () => {
                    ghost.remove();
                    placeholder.remove();
                    resolve();
                }
            });

            timeline
                .fromTo(placeholder, { height: 0, opacity: 0 }, { height: 82, opacity: 1, duration: 0.18, ease: "power2.out" }, 0)
                .fromTo(ghost, { scale: 0.92, opacity: 0.35 }, { scale: 1, opacity: 1, duration: 0.16, ease: "power2.out" }, 0)
                .to(ghost, {
                    left: targetRect.left + 12,
                    top: targetRect.top + 12,
                    width: Math.max(targetRect.width - 24, 140),
                    scale: 0.94,
                    duration: 0.48,
                    ease: "power3.inOut"
                })
                .to(ghost, { opacity: 0, scale: 0.82, duration: 0.12, ease: "power2.in" })
                .to(placeholder, { opacity: 0.25, duration: 0.12, ease: "sine.out" }, "<");
        });
    },

    resolveToolAnimationSource(stepType) {
        const captured = this.lastToolSource?.type === stepType ? this.lastToolSource : null;
        if (captured) {
            return captured;
        }

        const tool = document.querySelector(`[data-web-tool-type="${stepType}"]`);
        if (!tool) {
            return null;
        }

        return {
            type: stepType,
            label: tool.dataset.webToolLabel || tool.textContent.trim(),
            rect: tool.getBoundingClientRect()
        };
    },

    createInsertionPlaceholder(list) {
        list.querySelectorAll(".sortable-ghost, .sortable-drag, .sortable-fallback").forEach((item) => item.remove());
        list.querySelectorAll(".web-automation-step-placeholder").forEach((item) => item.remove());
        const placeholder = document.createElement("div");
        placeholder.className = "web-automation-step-placeholder";
        placeholder.setAttribute("aria-hidden", "true");
        list.appendChild(placeholder);
        return placeholder;
    },

    animateNewStepCardById(stepId) {
        const target = document.querySelector(`[data-web-step-list] [data-step-id="${stepId}"]`);
        this.animateNewStepCard(target);
    },

    animateNewStepCard(target) {
        if (!window.gsap || !target) {
            return;
        }

        window.gsap.fromTo(
            target,
            { y: -10, scale: 0.985, boxShadow: "0 0 0 rgba(56, 189, 248, 0)" },
            { y: 0, scale: 1, boxShadow: "0 0 34px rgba(56, 189, 248, 0.24)", duration: 0.34, ease: "back.out(1.6)" });
        window.gsap.to(target, { boxShadow: "0 0 0 rgba(56, 189, 248, 0)", duration: 0.45, delay: 0.28, ease: "sine.out" });
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
            "Click targeting is not available for this preview. Open headed browser mode and manually enter a click selector.",
            (target) => this.resolveClickTarget(target));
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

    captureUploadTarget(frameId, dotNetRef) {
        return this.capturePreviewElement(
            frameId,
            dotNetRef,
            "NotifyUploadTargetCaptured",
            "Click the file input, upload label, or upload area in the preview. The real page click is blocked during targeting.",
            "Upload targeting is not available for this preview. Open headed browser mode and manually enter a file input selector.",
            (target) => this.resolveUploadTarget(target));
    },

    capturePreviewElement(frameId, dotNetRef, callbackName, successMessage, failureMessage, targetResolver) {
        const frame = document.getElementById(frameId);
        if (!frame || !frame.contentDocument) {
            return failureMessage;
        }

        try {
            const doc = frame.contentDocument;
            this.cancelActiveCapture(frame);
            this.ensurePreviewHighlightStyles(doc);
            this.clearPreviewHighlight(frame);
            const captureController = new AbortController();
            frame.__pcAssistantActiveCapture = captureController;
            let handled = false;
            let suppressTimer = null;
            const listenerOptions = { capture: true, signal: captureController.signal };
            const handler = (event) => {
                if (handled) {
                    return;
                }

                handled = true;
                event.preventDefault();
                event.stopPropagation();
                event.stopImmediatePropagation();
                const target = targetResolver ? targetResolver(event.target) : event.target;
                if (!target) {
                    handled = false;
                    return;
                }

                const selector = this.buildCssSelector(target);
                frame.dataset.lastSelector = selector;
                this.clearPreviewHighlight(frame);
                target.classList.add("pc-assistant-automation-target");
                captureController.abort();
                if (frame.__pcAssistantActiveCapture === captureController) {
                    delete frame.__pcAssistantActiveCapture;
                }

                doc.addEventListener("click", suppressClick, true);
                suppressTimer = setTimeout(() => {
                    doc.removeEventListener("click", suppressClick, true);
                }, 750);
                dotNetRef?.invokeMethodAsync(callbackName, selector);
            };
            const suppressClick = (event) => {
                if (suppressTimer) {
                    clearTimeout(suppressTimer);
                    suppressTimer = null;
                }

                event.preventDefault();
                event.stopPropagation();
                event.stopImmediatePropagation();
                doc.removeEventListener("click", suppressClick, true);
            };
            const hoverHandler = (event) => {
                this.clearPreviewHover(frame);
                const target = targetResolver ? targetResolver(event.target) : event.target;
                (target || event.target).classList.add("pc-assistant-automation-hover");
            };
            doc.addEventListener("mouseover", hoverHandler, listenerOptions);
            doc.addEventListener("pointerdown", handler, listenerOptions);
            doc.addEventListener("mousedown", handler, listenerOptions);
            doc.addEventListener("touchstart", handler, listenerOptions);
            doc.addEventListener("click", handler, listenerOptions);
            return successMessage;
        } catch {
            return failureMessage;
        }
    },

    cancelActiveCapture(frame) {
        const activeCapture = frame.__pcAssistantActiveCapture;
        if (activeCapture) {
            activeCapture.abort();
            delete frame.__pcAssistantActiveCapture;
        }
    },

    resolveInputTarget(target) {
        if (!target || target.nodeType !== Node.ELEMENT_NODE) {
            return null;
        }

        const editableSelector = this.inputTargetSelector();
        const direct = target.closest(editableSelector);
        if (direct) {
            const nestedEditable = direct.matches("input, textarea, select, [contenteditable]:not([contenteditable='false'])")
                ? direct
                : direct.querySelector("input, textarea, select, [contenteditable]:not([contenteditable='false'])");
            return nestedEditable || direct;
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

            return label.querySelector(editableSelector);
        }

        const describedControl = this.findAssociatedInput(target);
        if (describedControl) {
            return describedControl;
        }

        return null;
    },

    resolveUploadTarget(target) {
        if (!target || target.nodeType !== Node.ELEMENT_NODE) {
            return null;
        }

        const fileInputSelector = "input[type='file']";
        const direct = target.closest(fileInputSelector);
        if (direct) {
            return direct;
        }

        const label = target.closest("label");
        if (label) {
            if (label.control?.matches?.(fileInputSelector)) {
                return label.control;
            }

            const nested = label.querySelector(fileInputSelector);
            if (nested) {
                return nested;
            }

            const forId = label.getAttribute("for");
            if (forId) {
                const controlled = label.ownerDocument.getElementById(forId);
                if (controlled?.matches?.(fileInputSelector)) {
                    return controlled;
                }
            }
        }

        const associated = this.findAssociatedFileInput(target);
        if (associated) {
            return associated;
        }

        let current = target;
        for (let depth = 0; current && depth < 5; depth += 1) {
            const nested = current.querySelector?.(fileInputSelector);
            if (nested) {
                return nested;
            }

            current = current.parentElement;
        }

        return null;
    },

    inputTargetSelector() {
        return [
            "input",
            "textarea",
            "select",
            "[contenteditable]:not([contenteditable='false'])",
            "[role='textbox']",
            "[role='searchbox']",
            "[role='combobox']",
            "[role='spinbutton']",
            "[aria-multiline='true']"
        ].join(",");
    },

    findAssociatedInput(target) {
        const doc = target.ownerDocument;
        const associationAttributes = ["aria-controls", "aria-owns", "for"];
        for (const attribute of associationAttributes) {
            const id = target.closest(`[${attribute}]`)?.getAttribute(attribute);
            if (!id) {
                continue;
            }

            const candidate = doc.getElementById(id);
            if (candidate?.matches(this.inputTargetSelector())) {
                return candidate;
            }

            const nested = candidate?.querySelector?.(this.inputTargetSelector());
            if (nested) {
                return nested;
            }
        }

        return null;
    },

    findAssociatedFileInput(target) {
        const doc = target.ownerDocument;
        const associationAttributes = ["aria-controls", "aria-owns", "for"];
        for (const attribute of associationAttributes) {
            const id = target.closest(`[${attribute}]`)?.getAttribute(attribute);
            if (!id) {
                continue;
            }

            const candidate = doc.getElementById(id);
            if (candidate?.matches?.("input[type='file']")) {
                return candidate;
            }

            const nested = candidate?.querySelector?.("input[type='file']");
            if (nested) {
                return nested;
            }
        }

        const uploadHint = target.closest("[data-upload], [data-file-upload], [class*='upload'], [id*='upload']");
        return uploadHint?.querySelector?.("input[type='file']") || null;
    },

    resolveClickTarget(target) {
        if (!target || target.nodeType !== Node.ELEMENT_NODE) {
            return null;
        }

        const actionableSelector = [
            "a[href]",
            "button",
            "summary",
            "label",
            "select",
            "textarea",
            "input",
            "[onclick]",
            "[role='button']",
            "[role='link']",
            "[role='menuitem']",
            "[role='tab']",
            "[role='checkbox']",
            "[role='radio']",
            "[role='switch']",
            "[data-testid]",
            "[data-test-id]",
            "[data-test]"
        ].join(",");

        return target.closest(actionableSelector) || target;
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
        const doc = element.ownerDocument;
        const uniqueAttributeSelector = this.buildUniqueAttributeSelector(doc, element);
        if (uniqueAttributeSelector) {
            return uniqueAttributeSelector;
        }

        const parts = [];
        let current = element;
        while (current && current.nodeType === Node.ELEMENT_NODE && current !== doc.documentElement) {
            const part = this.buildSelectorPart(current);
            parts.unshift(part);
            const selector = parts.join(" > ");
            if (this.isUniqueSelector(doc, selector)) {
                return selector;
            }

            current = current.parentElement;
        }

        parts.unshift(doc.documentElement.localName.toLowerCase());
        return parts.join(" > ");
    },

    buildUniqueAttributeSelector(doc, element) {
        const candidates = [];
        if (element.id) {
            candidates.push(`#${CSS.escape(element.id)}`);
        }

        ["data-testid", "data-test-id", "data-test", "name", "aria-label", "href", "title"].forEach((attribute) => {
            const value = element.getAttribute(attribute);
            if (value) {
                candidates.push(`${element.localName.toLowerCase()}[${attribute}="${this.escapeAttribute(value)}"]`);
                candidates.push(`[${attribute}="${this.escapeAttribute(value)}"]`);
            }
        });

        return candidates.find((selector) => this.isUniqueSelector(doc, selector)) || null;
    },

    buildSelectorPart(element) {
        let part = element.localName.toLowerCase();
        const stableAttributes = ["data-testid", "data-test-id", "data-test", "name", "aria-label", "title"];
        for (const attribute of stableAttributes) {
            const value = element.getAttribute(attribute);
            if (value) {
                return `${part}[${attribute}="${this.escapeAttribute(value)}"]`;
            }
        }

        const stableClasses = Array.from(element.classList)
            .filter((item) => !item.startsWith("pc-assistant-automation-"))
            .filter((item) => /^[A-Za-z_-][A-Za-z0-9_-]*$/.test(item))
            .slice(0, 2);
        if (stableClasses.length > 0) {
            part += `.${stableClasses.map((item) => CSS.escape(item)).join(".")}`;
        }

        const parent = element.parentElement;
        if (!parent) {
            return part;
        }

        const sameTagSiblings = Array.from(parent.children)
            .filter((child) => child.localName === element.localName);
        if (sameTagSiblings.length > 1) {
            part += `:nth-of-type(${sameTagSiblings.indexOf(element) + 1})`;
        }

        return part;
    },

    isUniqueSelector(doc, selector) {
        try {
            return doc.querySelectorAll(selector).length === 1;
        } catch {
            return false;
        }
    }
};
