(() => {
    const sidebar = document.getElementById("residentSidebar");
    const button = document.getElementById("residentMenuButton");
    const overlay = document.getElementById("residentOverlay");
    if (!sidebar || !button || !overlay) return;

    const closeMenu = () => {
        sidebar.classList.remove("is-open");
        overlay.classList.remove("is-visible");
        button.setAttribute("aria-expanded", "false");
    };

    button.addEventListener("click", () => {
        const open = sidebar.classList.toggle("is-open");
        overlay.classList.toggle("is-visible", open);
        button.setAttribute("aria-expanded", String(open));
    });
    overlay.addEventListener("click", closeMenu);
    window.addEventListener("resize", () => {
        if (window.innerWidth >= 768) closeMenu();
    });
})();
