(() => {
    const root = document.documentElement;
    root.classList.add("public-js");
    const toggle = document.querySelector(".nav-toggle");
    const navigation = document.querySelector("#primary-navigation");
    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    if (toggle && navigation) {
        const closeNavigation = () => {
            navigation.classList.remove("is-open");
            toggle.setAttribute("aria-expanded", "false");
            toggle.setAttribute("aria-label", "Open navigation");
        };

        toggle.addEventListener("click", () => {
            const isOpen = navigation.classList.toggle("is-open");
            toggle.setAttribute("aria-expanded", String(isOpen));
            toggle.setAttribute("aria-label", isOpen ? "Close navigation" : "Open navigation");
        });

        navigation.querySelectorAll("a").forEach((link) => {
            link.addEventListener("click", closeNavigation);
        });

        document.addEventListener("keydown", (event) => {
            if (event.key === "Escape") closeNavigation();
        });

        window.addEventListener("resize", () => {
            if (window.innerWidth > 760) closeNavigation();
        });
    }

    const revealItems = document.querySelectorAll(".reveal");
    if (!revealItems.length || reducedMotion || !("IntersectionObserver" in window)) {
        revealItems.forEach((item) => item.classList.add("is-visible"));
        return;
    }

    root.classList.add("motion-ready");
    const observer = new IntersectionObserver((entries, currentObserver) => {
        entries.forEach((entry) => {
            if (!entry.isIntersecting) return;
            entry.target.classList.add("is-visible");
            currentObserver.unobserve(entry.target);
        });
    }, { threshold: 0.12, rootMargin: "0px 0px -24px 0px" });

    revealItems.forEach((item) => observer.observe(item));
})();
