import {
    Activity,
    ArrowLeft,
    ArrowRight,
    BarChart3,
    Bell,
    Building,
    Check,
    Clock,
    Copy,
    CreditCard,
    createIcons,
    Grid,
    Home,
    Inbox,
    Key,
    Layers,
    Lock,
    LogOut,
    Menu,
    MessageSquare,
    Package,
    Plus,
    Settings,
    Shield,
    Sparkles,
    User,
    Users
} from "lucide";

const icons = {
    Activity,
    ArrowLeft,
    ArrowRight,
    BarChart3,
    Bell,
    Building,
    Check,
    Clock,
    Copy,
    CreditCard,
    Grid,
    Home,
    Inbox,
    Key,
    Layers,
    Lock,
    LogOut,
    Menu,
    MessageSquare,
    Package,
    Plus,
    Settings,
    Shield,
    Sparkles,
    User,
    Users
};

const iconNames = {
    home: "home",
    inbox: "inbox",
    users: "users",
    package: "package",
    message: "message-square",
    bell: "bell",
    key: "key",
    user: "user",
    logout: "log-out",
    building: "building",
    layers: "layers",
    grid: "grid",
    sparkles: "sparkles",
    shield: "shield",
    check: "check",
    clock: "clock",
    creditCard: "credit-card",
    chart: "bar-chart-3",
    settings: "settings",
    menu: "menu",
    plus: "plus",
    arrowLeft: "arrow-left",
    arrowRight: "arrow-right",
    copy: "copy",
    activity: "activity",
    lock: "lock"
};

function renderIcons() {
    document.querySelectorAll("[data-icon]").forEach((element) => {
        const iconName = iconNames[element.getAttribute("data-icon")];
        if (!iconName) return;
        element.setAttribute("data-lucide", iconName);
        element.classList.add("mtss-icon");
    });

    createIcons({
        icons,
        attrs: {
            "aria-hidden": "true",
            "stroke-width": 1.8
        }
    });
}

document.addEventListener("DOMContentLoaded", renderIcons, { once: true });
