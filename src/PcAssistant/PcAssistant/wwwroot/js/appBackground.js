(() => {
    const state = {
        initialized: false,
        tweens: []
    };

    const motionQuery = window.matchMedia?.("(prefers-reduced-motion: reduce)");

    function canAnimate() {
        return Boolean(window.gsap) && !motionQuery?.matches;
    }

    function initialize() {
        const backgrounds = Array.from(document.querySelectorAll(".pc-app-background, .pc-chat-background"));
        if (backgrounds.length === 0 || state.initialized || !canAnimate()) {
            return;
        }

        const gsap = window.gsap;
        state.initialized = true;

        state.tweens.push(gsap.to(backgrounds.map((background) => background.querySelector(".pc-bg-grid")).filter(Boolean), {
            backgroundPosition: "96px 64px, -64px 88px",
            duration: 18,
            ease: "none",
            repeat: -1
        }));

        state.tweens.push(gsap.fromTo(backgrounds.map((background) => background.querySelector(".pc-bg-sweep")).filter(Boolean), {
            xPercent: -38,
            opacity: 0.32
        }, {
            xPercent: 38,
            opacity: 0.58,
            duration: 6.5,
            ease: "sine.inOut",
            repeat: -1,
            yoyo: true
        }));

        state.tweens.push(gsap.to(backgrounds.map((background) => background.querySelector(".pc-bg-circuit-a")).filter(Boolean), {
            x: 34,
            y: -20,
            opacity: 0.72,
            duration: 8,
            ease: "sine.inOut",
            repeat: -1,
            yoyo: true
        }));

        state.tweens.push(gsap.to(backgrounds.map((background) => background.querySelector(".pc-bg-circuit-b")).filter(Boolean), {
            x: -30,
            y: 22,
            opacity: 0.66,
            duration: 9.5,
            ease: "sine.inOut",
            repeat: -1,
            yoyo: true
        }));

        state.tweens.push(gsap.to(backgrounds.flatMap((background) => Array.from(background.querySelectorAll(".pc-bg-signal"))), {
            backgroundPosition: "180px 0",
            opacity: 0.72,
            duration: 3.8,
            ease: "sine.inOut",
            repeat: -1,
            yoyo: true,
            stagger: 0.8
        }));
    }

    function destroy() {
        state.tweens.forEach((tween) => tween?.kill?.());
        state.tweens = [];
        state.initialized = false;
    }

    motionQuery?.addEventListener?.("change", () => {
        destroy();
        initialize();
    });

    window.pcAssistantBackground = {
        initialize,
        destroy
    };
})();
