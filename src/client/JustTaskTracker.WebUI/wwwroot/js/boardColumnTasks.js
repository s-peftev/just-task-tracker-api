export function scrollToBottom(element) {
    if (!element)
        return;

    requestAnimationFrame(() => {
        element.scrollTop = element.scrollHeight;
    });
}

export function getBoundingClientRect(element) {
    if (!element)
        return null;

    const rect = element.getBoundingClientRect();
    return {
        top: rect.top,
        left: rect.left,
        right: rect.right,
        bottom: rect.bottom,
        width: rect.width,
        height: rect.height,
        viewportHeight: window.innerHeight,
    };
}
