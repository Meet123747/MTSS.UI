(function () {
    'use strict';

    function initDrawers() {
        document.querySelectorAll('[data-drawer-toggle]').forEach(function (btn) {
            const targetId = btn.getAttribute('data-drawer-toggle');
            const drawer = document.getElementById(targetId);
            const overlayId = btn.getAttribute('data-drawer-overlay');
            const overlay = overlayId ? document.getElementById(overlayId) : null;
            if (!drawer) return;

            function setOpen(open) {
                drawer.classList.toggle('open', open);
                if (overlay) overlay.classList.toggle('open', open);
                btn.setAttribute('aria-expanded', open ? 'true' : 'false');
            }

            btn.addEventListener('click', function () {
                setOpen(!drawer.classList.contains('open'));
            });
            if (overlay) {
                overlay.addEventListener('click', function () {
                    setOpen(false);
                });
            }
            document.addEventListener('keydown', function (e) {
                if (e.key === 'Escape' && drawer.classList.contains('open')) {
                    setOpen(false);
                }
            });
        });
    }

    function initFormLoading() {
        document.querySelectorAll('form').forEach(function (form) {
            form.addEventListener('submit', function () {
                if (form.dataset.noLoading === 'true') return;
                if (typeof form.checkValidity === 'function' && !form.checkValidity()) return;
                const btn = form.querySelector('button[type="submit"], input[type="submit"]');
                if (!btn || btn.classList.contains('mtss-btn-loading')) return;
                if (btn.tagName === 'BUTTON') {
                    btn.classList.add('mtss-btn-loading');
                    const spinner = document.createElement('span');
                    spinner.className = 'mtss-spinner';
                    spinner.setAttribute('aria-hidden', 'true');
                    btn.prepend(spinner);
                }
            });
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        initDrawers();
        initFormLoading();
    });
})();
