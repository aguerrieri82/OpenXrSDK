export function getResourceUrl(path: string) {
    const protocol =
        location.protocol == "ui:" && /Linux/i.test(navigator.userAgent)
            ? "https:"
            : location.protocol;

    return `${protocol}//${location.host}/${path.replace(/^\/+/, "")}`;
}


export async function getResourceAsync(path: string) {

    const url = getResourceUrl(path);

    return await fetch(url);
}