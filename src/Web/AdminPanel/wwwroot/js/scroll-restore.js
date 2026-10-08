// Back and forward return to the place of the page where you were (Blazor scrolls every page to the top).
// The scroll position of every address is kept in the session storage; going back (popstate) reads it before Blazor
// renders and scrolls up, then waits until the page has loaded enough content to scroll there.
(() => {
    const keyOf = () => 'admin-scroll:' + location.pathname + location.search;
    let restoring = null;
    let pending = false;

    const save = () => {
        pending = false;
        if (restoring) {
            return;
        }

        try {
            sessionStorage.setItem(keyOf(), String(Math.round(window.scrollY)));
        } catch {
            // no storage (private mode): nothing to keep
        }
    };

    window.addEventListener('scroll', () => {
        if (!pending) {
            pending = true;
            requestAnimationFrame(save);
        }
    }, { passive: true });

    window.addEventListener('popstate', () => {
        let target = 0;
        try {
            target = Number(sessionStorage.getItem(keyOf())) || 0;
        } catch {
            target = 0;
        }

        clearInterval(restoring);
        restoring = null;
        if (target <= 0) {
            return;
        }

        // the page loads its data after the navigation; keep the position a little while longer, in case it is
        // scrolled to the top once more when the content arrives
        const started = Date.now();
        let reached = 0;
        restoring = setInterval(() => {
            const now = Date.now();
            if (document.documentElement.scrollHeight >= target + window.innerHeight || now - started > 5000) {
                reached ||= now;
                if (Math.abs(window.scrollY - target) > 2) {
                    window.scrollTo(0, target);
                }

                if (now - reached > 400) {
                    clearInterval(restoring);
                    restoring = null;
                }
            }
        }, 50);
    });
})();
