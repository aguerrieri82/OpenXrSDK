    (() => {

        if (window.xrStereoUi)
            return;

        const state = {
            baseDistance: 1.0,
            eyeX: 0.0,
            pixelsPerMeterX: 1000.0,
            pixelsPerMeterY: 1000.0,
            viewportWidth: window.innerWidth,
            viewportHeight: window.innerHeight,
            frame: 0,
            activeElements: []
        };

        function ensureDebugOverlay() {
            let el = document.getElementById("__xr_debug_overlay");

            if (el)
                return el;

            el = document.createElement("div");
            el.id = "__xr_debug_overlay";

            el.style.position = "fixed";
            el.style.left = "8px";
            el.style.top = "8px";
            el.style.zIndex = "2147483647";
            el.style.pointerEvents = "none";

            el.style.padding = "4px 7px";
            el.style.borderRadius = "4px";
            el.style.background = "rgba(0, 0, 0, 0.75)";
            el.style.color = "#00ff00";
            el.style.font = "12px monospace";
            el.style.lineHeight = "14px";
            el.style.whiteSpace = "pre";
            el.style.transform = "translateZ(0)";
            el.style.willChange = "contents";

            document.documentElement.appendChild(el);

            return el;
        }

        function updateDebugOverlay() {
            const el = ensureDebugOverlay();

            const eye =
                state.eyeX < 0 ? "Left" :
                    state.eyeX > 0 ? "Right" :
                        "Mono";

            el.textContent =
                `frame: ${state.frame}\n` +
                `eye: ${eye}\n` +
                `eyeX: ${state.eyeX.toFixed(4)}`;
        }

        function parseNumber(value, fallback) {
            if (value == null)
                return fallback;

            const n = Number.parseFloat(value);
            return Number.isFinite(n) ? n : fallback;
        }

        function getDepthMeters(el, style) {
            if (el.dataset.xrDepth != null)
                return parseNumber(el.dataset.xrDepth, 0.0);

            if (el.dataset.xrElevation != null)
                return parseNumber(el.dataset.xrElevation, 0.0);

            let v = style.getPropertyValue("--xr-depth");
            if (v && v.trim().length > 0)
                return parseNumber(v, 0.0);

            v = style.getPropertyValue("--xr-elevation");
            if (v && v.trim().length > 0)
                return parseNumber(v, 0.0);

            return 0.0;
        }

        function collectElements() {
            const all = document.body
                ? document.body.querySelectorAll("*")
                : document.querySelectorAll("*");

            const result = [];

            for (const el of all) {
                if (!el.dataset)
                    continue;

                if (el.dataset.xrDepth != null ||
                    el.dataset.xrElevation != null) {
                    result.push(el);
                    continue;
                }

                const style = getComputedStyle(el);

                if (style.getPropertyValue("--xr-depth").trim().length > 0 ||
                    style.getPropertyValue("--xr-elevation").trim().length > 0) {
                    result.push(el);
                }
            }

            return result;
        }

        function updateState(info) {
            if (!info)
                return;

            if (info.baseDistance != null)
                state.baseDistance = Number(info.baseDistance);

            if (info.eyeX != null)
                state.eyeX = Number(info.eyeX);

            if (info.pixelsPerMeterX != null)
                state.pixelsPerMeterX = Number(info.pixelsPerMeterX);

            if (info.pixelsPerMeterY != null)
                state.pixelsPerMeterY = Number(info.pixelsPerMeterY);

            if (info.viewportWidth != null)
                state.viewportWidth = Number(info.viewportWidth);

            if (info.viewportHeight != null)
                state.viewportHeight = Number(info.viewportHeight);
        }

        function restorePrevious() {
            for (const el of state.activeElements) {
                if (!el || !el.isConnected)
                    continue;

                el.style.transform = "";
                el.style.transformOrigin = "";
                el.style.removeProperty("--xr-frame");
            }

            state.activeElements.length = 0;
        }

        function postFrameReady(reason) {

            const frame = state.frame;
            const eyeX = state.eyeX;

            //requestAnimationFrame(() => {
                requestAnimationFrame(() => {
                    window.cefSharp.postMessage(JSON.stringify({
                        type: "xrStereoUiFrameReady",
                        frame: frame,
                        eyeX: eyeX,
                        reason: reason || "refresh",
                        time: performance.now()
                    }));
                });
           //});
        }

        function ensureDirtyLayer() {
            let el = document.getElementById("__xr_dirty_layer");

            if (el)
                return el;

            el = document.createElement("div");
            el.id = "__xr_dirty_layer";

            el.style.position = "fixed";
            el.style.left = "0";
            el.style.top = "0";
            el.style.width = "100vw";
            el.style.height = "100vh";
            el.style.pointerEvents = "none";
            el.style.zIndex = "2147483647";

            // Not zero, otherwise Chromium may optimize it away.
            el.style.opacity = "0.001";

            // Force its own composited layer.
            el.style.willChange = "background-color, transform";
            el.style.transform = "translateZ(0)";

            document.documentElement.appendChild(el);

            return el;
        }

        function dirtyFullFrame() {
            const el = ensureDirtyLayer();

            const v = state.frame & 1;

            // Real visible/composited change over the whole viewport.
            // Tiny opacity makes it visually almost invisible but still dirty.
            el.style.backgroundColor = v
                ? "rgb(0, 0, 0)"
                : "rgb(1, 1, 1)";

            el.style.transform = v
                ? "translateZ(0) translateX(0px)"
                : "translateZ(0) translateX(0.001px)";
        }
        function refresh(info) {
            updateState(info);

            state.frame++;

            updateDebugOverlay();

            restorePrevious();

            // Force layout after removing previous XR transforms.
            if (document.body)
                document.body.offsetHeight;

            const D = state.baseDistance;
            const eyeX = state.eyeX;
            const ppmX = state.pixelsPerMeterX;
            const ppmY = state.pixelsPerMeterY;
            const vw = state.viewportWidth || window.innerWidth;
            const vh = state.viewportHeight || window.innerHeight;

            const cxScreen = vw * 0.5;
            const cyScreen = vh * 0.5;

            const elements = collectElements();
            const jobs = [];

            // Measure all first, apply later.
            // This avoids parent transform affecting child rects.
            for (const el of elements) {

                const style = getComputedStyle(el);
                const depth = getDepthMeters(el, style);

                if (depth === 0.0)
                    continue;

                debugger;

                const rect = el.getBoundingClientRect();

                if (rect.width <= 0 || rect.height <= 0)
                    continue;

                const cx = rect.left + rect.width * 0.5;
                const cy = rect.top + rect.height * 0.5;

                const xMeters = (cx - cxScreen) / ppmX;
                const yMeters = (cyScreen - cy) / ppmY;

                // Positive depth/elevation means closer to the viewer.
                const z = Math.max(0.001, D - depth);

                const projectedX = cxScreen + ((xMeters - eyeX) / z) * D * ppmX;
                const projectedY = cyScreen - (yMeters / z) * D * ppmY;

                jobs.push({
                    el: el,
                    dx: projectedX - cx,
                    dy: projectedY - cy,
                    scale: D / z
                });
            }

            for (const job of jobs) {
                job.el.style.transformOrigin = "50% 50%";
                job.el.style.transform =
                    `translate(${job.dx}px, ${job.dy}px) scale(${job.scale})`;

                job.el.style.setProperty("--xr-frame", String(state.frame));

                state.activeElements.push(job.el);
            }

            dirtyFullFrame();

            document.documentElement.style.setProperty("--xr-eye-x", String(eyeX));
            document.documentElement.style.setProperty("--xr-frame", String(state.frame));

            postFrameReady("refresh");

            return state.frame;
        }

        function reset() {
            restorePrevious();

            document.documentElement.style.removeProperty("--xr-eye-x");
            document.documentElement.style.removeProperty("--xr-frame");
        }

        window.xrStereoUi = {
            refresh,
            reset,
            state
        };

        console.info("Stereo script injected");

    })();