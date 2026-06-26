(() => {

    if (window.xrStereoUi)
        return;

    const state = {
        viewProj: null,
        panelWorld: null,

        panelWidthMeters: 1.0,
        panelHeightMeters: 1.0,

        viewportWidth: window.innerWidth,
        viewportHeight: window.innerHeight,

        // Positive CSS depth means "towards viewer".
        // Flip this if your panel local space has the opposite convention.
        depthSign: -1.0,

        frame: 0,
        activeElements: []
    };

    const XR_LOG_ENABLED = true;
    const XR_LOG_ELEMENTS = 0;

    let xrLogElementCount = 0;

    function xrLog(kind, data) {
        if (!XR_LOG_ENABLED)
            return;

        console.log(`[XR] ${kind}`, data);
    }

    function xrWarn(kind, data) {
        if (!XR_LOG_ENABLED)
            return;

        console.warn(`[XR] ${kind}`, data);
    }

    function xrLogMatrix(name, m) {
        if (!XR_LOG_ENABLED)
            return;

        if (!m) {
            console.log(`[XR] ${name}: null`);
            return;
        }

        console.log(
            `[XR] ${name}`,
            [m[0], m[1], m[2], m[3]],
            [m[4], m[5], m[6], m[7]],
            [m[8], m[9], m[10], m[11]],
            [m[12], m[13], m[14], m[15]]);
    }

    function parseNumber(value, fallback) {
        if (value == null)
            return fallback;

        const n = Number.parseFloat(value);
        return Number.isFinite(n) ? n : fallback;
    }

    function parseArray16(value, fallback) {
        if (!value || value.length !== 16)
            return fallback;

        const result = new Array(16);

        for (let i = 0; i < 16; i++) {
            const n = Number(value[i]);

            if (!Number.isFinite(n))
                return fallback;

            result[i] = n;
        }

        return result;
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

            if (el.id === "__xr_dirty_layer" ||
                el.id === "__xr_debug_overlay")
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

        state.viewProj = parseArray16(info.viewProj, state.viewProj);
        state.panelWorld = parseArray16(info.panelWorld, state.panelWorld);
        state.eye = info.eye;

        if (info.panelWidthMeters != null)
            state.panelWidthMeters = Number(info.panelWidthMeters);

        if (info.panelHeightMeters != null)
            state.panelHeightMeters = Number(info.panelHeightMeters);

        if (info.viewportWidth != null)
            state.viewportWidth = Number(info.viewportWidth);

        if (info.viewportHeight != null)
            state.viewportHeight = Number(info.viewportHeight);

        if (info.depthSign != null)
            state.depthSign = Number(info.depthSign);
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

    function mulMat4Vec4(m, x, y, z, w) {
        // Column-major convention, same as OpenGL-style matrices:
        // result = m * vec4.
        return {
            x: m[0] * x + m[4] * y + m[8] * z + m[12] * w,
            y: m[1] * x + m[5] * y + m[9] * z + m[13] * w,
            z: m[2] * x + m[6] * y + m[10] * z + m[14] * w,
            w: m[3] * x + m[7] * y + m[11] * z + m[15] * w
        };
    }

    function projectLocalToScreen(localX, localY, localZ) {
        const world = mulMat4Vec4(
            state.panelWorld,
            localX,
            localY,
            localZ,
            1.0);

        const clip = mulMat4Vec4(
            state.viewProj,
            world.x,
            world.y,
            world.z,
            world.w);

        if (Math.abs(clip.w) < 0.000001) {
            xrWarn("project-zero-w", {
                eye: state.eye,
                local: { x: localX, y: localY, z: localZ },
                world,
                clip
            });

            return null;
        }

        const ndcX = clip.x / clip.w;
        const ndcY = clip.y / clip.w;

        if (ndcX < -2 || ndcX > 2 || ndcY < -2 || ndcY > 2) {
            xrWarn("project-outside", {
                eye: state.eye,
                local: { x: localX, y: localY, z: localZ },
                world,
                clip,
                ndc: { x: ndcX, y: ndcY }
            });
        }

        const x = (ndcX * 0.5 + 0.5) * state.viewportWidth;
        const y = (0.5 - ndcY * 0.5) * state.viewportHeight;

        return { x, y };
    }

    function pixelToPanelLocal(px, py, depthMeters) {
        const u = px / state.viewportWidth;
        const v = py / state.viewportHeight;

        return {
            x: (u - 0.5) * state.panelWidthMeters,
            y: (0.5 - v) * state.panelHeightMeters,
            z: state.depthSign * depthMeters
        };
    }

    function distance2(a, b) {
        const dx = a.x - b.x;
        const dy = a.y - b.y;
        return Math.sqrt(dx * dx + dy * dy);
    }

    function postFrameReady(reason) {
        const frame = state.frame;

        requestAnimationFrame(() => {
            window.cefSharp.postMessage(JSON.stringify({
                type: "xrStereoUiFrameReady",
                frame: frame,
                reason: reason || "refresh",
                time: performance.now()
            }));
        });
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
        el.style.opacity = "0.001";
        el.style.willChange = "background-color, transform";
        el.style.transform = "translateZ(0)";

        document.documentElement.appendChild(el);

        return el;
    }

    function dirtyFullFrame() {
        const el = ensureDirtyLayer();

        const v = state.frame & 1;

        el.style.backgroundColor = v
            ? "rgb(0, 0, 0)"
            : "rgb(1, 1, 1)";

        el.style.transform = v
            ? "translateZ(0) translateX(0px)"
            : "translateZ(0) translateX(0.001px)";
    }

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

        el.textContent =
            `frame: ${state.frame}\n` +
            `panel: ${state.panelWidthMeters.toFixed(3)} x ${state.panelHeightMeters.toFixed(3)} m\n` +
            `eye: ${state.eye}`;
    }

    function check() {

        return collectElements().length > 0;
    }

    function refresh(info) {

        updateState(info);

        state.frame++;

        xrLogElementCount = 0;

        updateDebugOverlay();

        restorePrevious();

        if (document.body)
            document.body.offsetHeight;

        const panelCenter0 = projectLocalToScreen(0, 0, 0);
        const panelCenterDepth = projectLocalToScreen(0, 0, state.depthSign * 0.05);


        const elements = collectElements();
        const jobs = [];
        /**
        xrLog("panel-center", {
            frame: state.frame,
            eye: state.eye,
            z0: panelCenter0,
            zDepth005: panelCenterDepth,
            delta: panelCenter0 && panelCenterDepth
                ? {
                    dx: panelCenterDepth.x - panelCenter0.x,
                    dy: panelCenterDepth.y - panelCenter0.y
                }
                : null
        }); */
        //xrLogMatrix("viewProj", state.viewProj);
        //xrLogMatrix("panelWorld", state.panelWorld);

        for (const el of elements) {
            const style = getComputedStyle(el);
            const depth = getDepthMeters(el, style);

            if (depth === 0.0)
                continue;

            console.log(depth);

            const rect = el.getBoundingClientRect();

            if (rect.width <= 0 || rect.height <= 0)
                continue;

            const cx = rect.left + rect.width * 0.5;
            const cy = rect.top + rect.height * 0.5;

            const centerBaseLocal = pixelToPanelLocal(cx, cy, 0.0);
            const leftBaseLocal = pixelToPanelLocal(rect.left, cy, 0.0);
            const rightBaseLocal = pixelToPanelLocal(rect.right, cy, 0.0);
            const topBaseLocal = pixelToPanelLocal(cx, rect.top, 0.0);
            const bottomBaseLocal = pixelToPanelLocal(cx, rect.bottom, 0.0);

            const centerDepthLocal = pixelToPanelLocal(cx, cy, depth);
            const leftDepthLocal = pixelToPanelLocal(rect.left, cy, depth);
            const rightDepthLocal = pixelToPanelLocal(rect.right, cy, depth);
            const topDepthLocal = pixelToPanelLocal(cx, rect.top, depth);
            const bottomDepthLocal = pixelToPanelLocal(cx, rect.bottom, depth);

            const centerBaseScreen = projectLocalToScreen(
                centerBaseLocal.x,
                centerBaseLocal.y,
                centerBaseLocal.z);

            const leftBaseScreen = projectLocalToScreen(
                leftBaseLocal.x,
                leftBaseLocal.y,
                leftBaseLocal.z);

            const rightBaseScreen = projectLocalToScreen(
                rightBaseLocal.x,
                rightBaseLocal.y,
                rightBaseLocal.z);

            const topBaseScreen = projectLocalToScreen(
                topBaseLocal.x,
                topBaseLocal.y,
                topBaseLocal.z);

            const bottomBaseScreen = projectLocalToScreen(
                bottomBaseLocal.x,
                bottomBaseLocal.y,
                bottomBaseLocal.z);

            const centerDepthScreen = projectLocalToScreen(
                centerDepthLocal.x,
                centerDepthLocal.y,
                centerDepthLocal.z);

            const leftDepthScreen = projectLocalToScreen(
                leftDepthLocal.x,
                leftDepthLocal.y,
                leftDepthLocal.z);

            const rightDepthScreen = projectLocalToScreen(
                rightDepthLocal.x,
                rightDepthLocal.y,
                rightDepthLocal.z);

            const topDepthScreen = projectLocalToScreen(
                topDepthLocal.x,
                topDepthLocal.y,
                topDepthLocal.z);

            const bottomDepthScreen = projectLocalToScreen(
                bottomDepthLocal.x,
                bottomDepthLocal.y,
                bottomDepthLocal.z);

            if (!centerBaseScreen ||
                !leftBaseScreen ||
                !rightBaseScreen ||
                !topBaseScreen ||
                !bottomBaseScreen ||
                !centerDepthScreen ||
                !leftDepthScreen ||
                !rightDepthScreen ||
                !topDepthScreen ||
                !bottomDepthScreen)
                continue;

            const baseWidth = distance2(leftBaseScreen, rightBaseScreen);
            const baseHeight = distance2(topBaseScreen, bottomBaseScreen);

            const depthWidth = distance2(leftDepthScreen, rightDepthScreen);
            const depthHeight = distance2(topDepthScreen, bottomDepthScreen);

            if (baseWidth <= 0 || baseHeight <= 0 || depthWidth <= 0 || depthHeight <= 0)
                continue;

            const scaleX = depthWidth / baseWidth;
            const scaleY = depthHeight / baseHeight;

            const scale = (scaleX + scaleY) * 0.5;

            jobs.push({
                el: el,
                dx: centerDepthScreen.x - centerBaseScreen.x,
                dy: centerDepthScreen.y - centerBaseScreen.y,
                scale: scale
            });

            if (xrLogElementCount < XR_LOG_ELEMENTS) {

                xrLog("element", {
                    frame: state.frame,
                    eye: state.eye,
                    tag: el.tagName,
                    id: el.id || null,
                    className: typeof el.className === "string" ? el.className : null,
                    depth: depth,
                    rect: {
                        left: rect.left,
                        top: rect.top,
                        width: rect.width,
                        height: rect.height
                    },
                    center: { x: cx, y: cy },
                    centerBaseLocal,
                    centerDepthLocal,
                    centerBaseScreen,
                    centerDepthScreen,
                    dx: centerDepthScreen.x - centerBaseScreen.x,
                    dy: centerDepthScreen.y - centerBaseScreen.y,
                    baseWidth,
                    depthWidth,
                    baseHeight,
                    depthHeight,
                    scaleX,
                    scaleY,
                    scale
                });

                xrLogElementCount++;
            }

        }

        for (const job of jobs) {
            job.el.style.transformOrigin = "50% 50%";
            job.el.style.transform =
                `translate(${job.dx}px, ${job.dy}px) scale(${job.scale})`;

            job.el.style.setProperty("--xr-frame", String(state.frame));

            state.activeElements.push(job.el);
        }

        dirtyFullFrame();

        document.documentElement.style.setProperty("--xr-frame", String(state.frame));
        document.documentElement.style.setProperty("--xr-depth-sign", String(state.depthSign));

        //postFrameReady("refresh");



        return state.frame;
    }

    function reset() {
        restorePrevious();

        document.documentElement.style.removeProperty("--xr-frame");
        document.documentElement.style.removeProperty("--xr-depth-sign");
    }

    window.xrStereoUi = {
        refresh,
        reset,
        state,
        check
    };

    console.info("Stereo script injected");

})();