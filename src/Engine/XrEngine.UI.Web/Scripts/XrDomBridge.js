/* XrDomBridge.ts */
class XrDomBridge {
    static ClassName = "xr-elevated";
    static IdCssProperty = "--xr-id";
    static DepthCssProperty = "--xr-depth";
    static BackgroundCssProperty = "--xr-background";
    static ExposedFunctionName = "xrGetElevatedElementsJson";
    static DefaultBackground = "#000000";
    getElevatedElements() {
        const elements = document.querySelectorAll(`.${XrDomBridge.ClassName}`);
        const result = [];
        console.log("found:", elements.length);
        for (const element of elements) {
            const style = getComputedStyle(element);
            const depth = this.getDepth(style);
            if (!depth) {
                console.log(element, "depth: ", depth);
                continue;
            }
            const rect = element.getBoundingClientRect();
            if (rect.width <= 0 || rect.height <= 0) {
                console.log(element, "rect: ", rect);
                continue;
            }
            result.push({
                id: this.getElementId(element, style),
                tag: element.tagName.toLowerCase(),
                textureRect: this.toTextureRect(rect),
                elevation: depth,
                background: this.getBackgroundHex(element, style),
                opacity: Number.parseFloat(style.opacity)
            });
        }
        return result;
    }
    getElevatedElementsJson() {
        return JSON.stringify(this.getElevatedElements());
    }
    getElementId(element, style) {
        const fromCss = style.getPropertyValue(XrDomBridge.IdCssProperty).trim();
        if (fromCss.length > 0)
            return this.unquoteCssString(fromCss);
        return element.id || null;
    }
    toTextureRect(rect) {
        const scale = window.devicePixelRatio || 1;
        console.log(scale, "scale");
        return {
            x: Math.round(rect.left * scale),
            y: Math.round(rect.top * scale),
            width: Math.round(rect.width * scale),
            height: Math.round(rect.height * scale)
        };
    }
    getDepth(style) {
        const value = style.getPropertyValue(XrDomBridge.DepthCssProperty).trim();
        if (value.length === 0)
            return 0;
        return this.parseCssLength(value);
    }
    getBackgroundHex(element, style) {
        const explicit = style.getPropertyValue(XrDomBridge.BackgroundCssProperty).trim();
        if (explicit.length > 0) {
            const parsed = this.cssColorToHex(explicit);
            if (parsed != null)
                return parsed;
        }
        const parent = element.parentElement;
        if (parent != null) {
            const parentBackground = getComputedStyle(parent).backgroundColor;
            const parsed = this.cssColorToHex(parentBackground);
            if (parsed != null)
                return parsed;
        }
        const bodyBackground = getComputedStyle(document.body).backgroundColor;
        return this.cssColorToHex(bodyBackground) ?? XrDomBridge.DefaultBackground;
    }
    parseCssLength(value) {
        const trimmed = value.trim().toLowerCase();
        if (trimmed.endsWith("cm")) {
            const n = Number.parseFloat(trimmed.slice(0, -2));
            return Number.isFinite(n) ? n / 100 : 0;
        }
        if (trimmed.endsWith("mm")) {
            const n = Number.parseFloat(trimmed.slice(0, -2));
            return Number.isFinite(n) ? n / 1000 : 0;
        }
        if (trimmed.endsWith("m")) {
            const n = Number.parseFloat(trimmed.slice(0, -1));
            return Number.isFinite(n) ? n : 0;
        }
        const n = Number.parseFloat(trimmed);
        return Number.isFinite(n) ? n : 0;
    }
    cssColorToHex(value) {
        const trimmed = value.trim();
        if (/^#[0-9a-fA-F]{6}$/.test(trimmed))
            return trimmed.toUpperCase();
        if (/^#[0-9a-fA-F]{3}$/.test(trimmed)) {
            const r = trimmed[1];
            const g = trimmed[2];
            const b = trimmed[3];
            return `#${r}${r}${g}${g}${b}${b}`.toUpperCase();
        }
        const match = trimmed.match(/\d+(\.\d+)?/g);
        if (match == null || match.length < 3)
            return null;
        const r = Number(match[0]);
        const g = Number(match[1]);
        const b = Number(match[2]);
        const a = match.length >= 4 ? Number(match[3]) : 1;
        if (a <= 0)
            return null;
        const byteToHex = (value) => Math.max(0, Math.min(255, Math.round(value)))
            .toString(16)
            .padStart(2, "0")
            .toUpperCase();
        return `#${byteToHex(r)}${byteToHex(g)}${byteToHex(b)}`;
    }
    unquoteCssString(value) {
        const trimmed = value.trim();
        if (trimmed.length >= 2 &&
            ((trimmed[0] === "\"" && trimmed[trimmed.length - 1] === "\"") ||
                (trimmed[0] === "'" && trimmed[trimmed.length - 1] === "'"))) {
            return trimmed.substring(1, trimmed.length - 1);
        }
        return trimmed;
    }
}
var domBridge = new XrDomBridge();
console.log("XrDomBridge attached");
