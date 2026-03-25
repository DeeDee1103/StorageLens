// ── Quick Action Button definitions (shared with AppearanceSettings) ────────
window.SL_QUICK_BTN_DEFS = [
	{ id: 'dashboard',     label: 'Dashboard',          icon: 'bi-speedometer2',   href: '/Index' },
	{ id: 'globalview',    label: 'Global View',         icon: 'bi-globe2',         href: '/GlobalView' },
	{ id: 'locations',     label: 'Storage Locations',   icon: 'bi-hdd-stack',      href: '/StorageLocations' },
	{ id: 'scanjobs',      label: 'Scan Jobs',           icon: 'bi-activity',       href: '/ScanJobs' },
	{ id: 'flowmanager',   label: 'Flow Manager',        icon: 'bi-diagram-3',      href: '/FlowManager' },
	{ id: 'fileinventory', label: 'File Inventory',      icon: 'bi-folder2-open',   href: '/FileInventory' },
	{ id: 'duplicates',    label: 'Duplicates',          icon: 'bi-files',          href: '/Duplicates' },
	{ id: 'reports',       label: 'Reports',             icon: 'bi-graph-up-arrow', href: '/Reports' },
	{ id: 'insights',      label: 'Insights',            icon: 'bi-journal-text',   href: '/Insights' },
	{ id: 'alerts',        label: 'Alerts',              icon: 'bi-bell-fill',      href: '/Alerts' },
	{ id: 'logging',       label: 'Logging',             icon: 'bi-terminal',       href: '/Logging' },
	{ id: 'settings',      label: 'Settings',            icon: 'bi-gear',           href: '/Settings' },
	{ id: 'profile',       label: 'Profile',             icon: 'bi-person-circle',  href: '/Profile' },
];

window.SL_QUICK_BTNS_KEY = 'sl-quick-buttons';
window.SL_QUICK_BTNS_DEFAULT = ['locations', 'scanjobs', 'flowmanager'];

window.renderQuickActionBtns = function () {
	const container = document.getElementById('quickActionBtns');
	if (!container) return;
	let selected;
	try {
		const raw = localStorage.getItem(window.SL_QUICK_BTNS_KEY);
		selected = raw ? JSON.parse(raw) : window.SL_QUICK_BTNS_DEFAULT;
	} catch {
		selected = window.SL_QUICK_BTNS_DEFAULT;
	}
	if (!Array.isArray(selected) || selected.length === 0) selected = window.SL_QUICK_BTNS_DEFAULT;
	selected = selected.slice(0, 3);
	container.innerHTML = '';
	selected.forEach(id => {
		const def = window.SL_QUICK_BTN_DEFS.find(d => d.id === id);
		if (!def) return;
		const a = document.createElement('a');
		a.href = def.href;
		a.className = 'btn icon-btn';
		a.title = def.label;
		a.setAttribute('aria-label', def.label);
		a.innerHTML = `<i class="bi ${def.icon}" aria-hidden="true"></i>`;
		container.appendChild(a);
	});
};

document.addEventListener("DOMContentLoaded", () => {
	const applyButtonHoverText = (root = document) => {
		const selector = [
			"button",
			"input[type='button']",
			"input[type='submit']",
			"input[type='reset']",
			"a.btn",
			"[role='button']"
		].join(",");

		const targets = root.matches?.(selector)
			? [root, ...root.querySelectorAll(selector)]
			: root.querySelectorAll(selector);

		targets.forEach((el) => {
			if (el.hasAttribute("title")) {
				return;
			}

			const dataHoverText = (el.getAttribute("data-hover-text") || "").trim();
			const ariaLabel = (el.getAttribute("aria-label") || "").trim();
			const valueText = "value" in el ? String(el.value || "").trim() : "";
			const visibleText = (el.textContent || "").replace(/\s+/g, " ").trim();

			const hoverText = dataHoverText || ariaLabel || valueText || visibleText;
			if (hoverText) {
				el.setAttribute("title", hoverText);
			}
		});
	};

	const initGlobalButtonHoverText = () => {
		applyButtonHoverText(document);

		const initBootstrapTooltips = (root = document) => {
			if (!window.bootstrap || !window.bootstrap.Tooltip) {
				return;
			}

			const selector = "[title], [data-bs-title]";
			const targets = root.matches?.(selector)
				? [root, ...root.querySelectorAll(selector)]
				: root.querySelectorAll(selector);

			targets.forEach((el) => {
				if (el.hasAttribute("data-bs-no-tooltip")) {
					return;
				}

				const rawTitle = (el.getAttribute("title") || el.getAttribute("data-bs-title") || "").trim();
				if (!rawTitle) {
					return;
				}

				let placement = (el.getAttribute("data-tooltip-placement") || "").trim();
				if (!placement) {
					if (el.closest("#sidebarOffcanvas")) {
						placement = "top";
					} else if (el.closest(".sidebar-panel .menu-nav") || el.closest(".sidebar-panel .sidebar-profile-footer")) {
						placement = "right";
					} else {
						placement = "top";
					}
				}

				window.bootstrap.Tooltip.getOrCreateInstance(el, {
					trigger: "hover focus",
					placement,
					container: "body"
				});
			});
		};

		initBootstrapTooltips(document);

		if (!("MutationObserver" in window)) {
			return;
		}

		const observer = new MutationObserver((mutations) => {
			mutations.forEach((mutation) => {
				if (mutation.type === "childList") {
					mutation.addedNodes.forEach((node) => {
						if (node.nodeType === Node.ELEMENT_NODE) {
							applyButtonHoverText(node);
							initBootstrapTooltips(node);
						}
					});
				}

				if (
					mutation.type === "attributes" &&
					mutation.target.nodeType === Node.ELEMENT_NODE
				) {
					applyButtonHoverText(mutation.target);
					initBootstrapTooltips(mutation.target);
				}
			});
		});

		observer.observe(document.body, {
			childList: true,
			subtree: true,
			attributes: true,
			attributeFilter: ["class", "aria-label", "value", "data-hover-text", "title", "data-bs-title", "data-tooltip-placement"]
		});
	};

	initGlobalButtonHoverText();

	const themeStorageKey = "storagelens-theme";
	const uiColorStorageKey = "sl-ui-color";
	const uiColorCustomStorageKey = "sl-ui-color-custom";
	const darkThemeValue = "dark";
	const lightThemeValue = "light";
	const defaultCustomColor = "#6f64e7";
	const themeToggle = document.getElementById("darkModeToggle");
	const themeIcon = themeToggle?.querySelector("i");

	const uiPalettes = {
		azure: {
			"--sl-primary": "#1165d8",
			"--sl-teal": "#16b5aa",
			"--sl-cyan": "#0ea5e9",
			"--sl-green": "#14b85d",
			"--sl-accent": "#6f64e7",
			"--sl-neon-purple": "#7b61ff",
			"--sl-neon-blue": "#35b9ff",
			"--sl-neon-turquoise": "#3ef2d0"
		},
		lagoon: {
			"--sl-primary": "#0f766e",
			"--sl-teal": "#14b8a6",
			"--sl-cyan": "#06b6d4",
			"--sl-green": "#22c55e",
			"--sl-accent": "#0ea5e9",
			"--sl-neon-purple": "#22d3ee",
			"--sl-neon-blue": "#38bdf8",
			"--sl-neon-turquoise": "#5eead4"
		},
		forest: {
			"--sl-primary": "#166534",
			"--sl-teal": "#16a34a",
			"--sl-cyan": "#22c55e",
			"--sl-green": "#4ade80",
			"--sl-accent": "#15803d",
			"--sl-neon-purple": "#22c55e",
			"--sl-neon-blue": "#4ade80",
			"--sl-neon-turquoise": "#86efac"
		},
		sunset: {
			"--sl-primary": "#b45309",
			"--sl-teal": "#ea580c",
			"--sl-cyan": "#f59e0b",
			"--sl-green": "#f97316",
			"--sl-accent": "#fb923c",
			"--sl-neon-purple": "#f97316",
			"--sl-neon-blue": "#fb923c",
			"--sl-neon-turquoise": "#fdba74"
		}
	};

	const normalizeHex = (value) => {
		if (!value) return defaultCustomColor;
		let v = String(value).trim();
		if (!v.startsWith("#")) v = `#${v}`;
		if (/^#[0-9a-fA-F]{3}$/.test(v)) {
			v = `#${v[1]}${v[1]}${v[2]}${v[2]}${v[3]}${v[3]}`;
		}
		return /^#[0-9a-fA-F]{6}$/.test(v) ? v : defaultCustomColor;
	};

	const mixHex = (hexA, hexB, weight) => {
		const a = normalizeHex(hexA);
		const b = normalizeHex(hexB);
		const w = Math.max(0, Math.min(1, Number(weight) || 0));

		const ar = parseInt(a.slice(1, 3), 16);
		const ag = parseInt(a.slice(3, 5), 16);
		const ab = parseInt(a.slice(5, 7), 16);
		const br = parseInt(b.slice(1, 3), 16);
		const bg = parseInt(b.slice(3, 5), 16);
		const bb = parseInt(b.slice(5, 7), 16);

		const rr = Math.round(ar + (br - ar) * w);
		const rg = Math.round(ag + (bg - ag) * w);
		const rb = Math.round(ab + (bb - ab) * w);

		return `#${rr.toString(16).padStart(2, "0")}${rg.toString(16).padStart(2, "0")}${rb.toString(16).padStart(2, "0")}`;
	};

	const buildCustomPalette = (baseColor) => {
		const base = normalizeHex(baseColor);
		const darker = mixHex(base, "#0b1220", 0.34);
		const lighter = mixHex(base, "#ffffff", 0.22);
		const tealish = mixHex(base, "#22c1b4", 0.42);
		const mintish = mixHex(base, "#3ef2d0", 0.48);

		return {
			"--sl-primary": darker,
			"--sl-teal": tealish,
			"--sl-cyan": lighter,
			"--sl-green": mixHex(base, "#22c55e", 0.4),
			"--sl-accent": base,
			"--sl-neon-purple": base,
			"--sl-neon-blue": lighter,
			"--sl-neon-turquoise": mintish
		};
	};

	const applyUiPalette = (paletteName, customColor) => {
		const chosen = paletteName || localStorage.getItem(uiColorStorageKey) || "azure";
		const custom = customColor || localStorage.getItem(uiColorCustomStorageKey) || defaultCustomColor;
		const palette = chosen === "custom" ? buildCustomPalette(custom) : (uiPalettes[chosen] || uiPalettes.azure);

		Object.entries(palette).forEach(([key, value]) => {
			document.documentElement.style.setProperty(key, value);
		});
	};

	window.applyStorageLensUIColor = (paletteName, customColor) => {
		applyUiPalette(paletteName, customColor);
	};

	const applyTheme = (theme) => {
		const isDarkMode = theme === darkThemeValue;
		document.body.classList.toggle("dark-mode", isDarkMode);

		if (themeToggle) {
			themeToggle.setAttribute("aria-pressed", String(isDarkMode));
			themeToggle.setAttribute("title", isDarkMode ? "Switch to light mode" : "Switch to dark mode");
		}

		if (themeIcon) {
			themeIcon.className = isDarkMode ? "bi bi-sun-fill" : "bi bi-moon-stars-fill";
		}
	};

	const savedTheme = localStorage.getItem(themeStorageKey);
	applyUiPalette(localStorage.getItem(uiColorStorageKey), localStorage.getItem(uiColorCustomStorageKey));
	const prefersDarkMode = window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches;
	const initialTheme = savedTheme === darkThemeValue || savedTheme === lightThemeValue
		? savedTheme
		: (prefersDarkMode ? darkThemeValue : lightThemeValue);

	applyTheme(initialTheme);
	window.renderQuickActionBtns();

	themeToggle?.addEventListener("click", () => {
		const nextTheme = document.body.classList.contains("dark-mode") ? lightThemeValue : darkThemeValue;
		localStorage.setItem(themeStorageKey, nextTheme);
		applyTheme(nextTheme);
	});

	// Ctrl+K / Cmd+K focuses the global search box
	document.addEventListener("keydown", (e) => {
		if ((e.ctrlKey || e.metaKey) && e.key === "k") {
			const searchInput = document.getElementById("globalSearch");
			if (searchInput) {
				e.preventDefault();
				searchInput.focus();
				searchInput.select();
			}
		}
	});

	const revealItems = document.querySelectorAll(".reveal-item");
	const prefersReducedMotion = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

	if (revealItems.length > 0) {
		if (prefersReducedMotion || !("IntersectionObserver" in window)) {
			revealItems.forEach((item) => {
				item.style.opacity = "1";
				item.style.transform = "none";
			});
		} else {
			revealItems.forEach((item) => {
				item.style.opacity = "0";
				item.style.transform = "translateY(14px)";
			});

			const revealObserver = new IntersectionObserver((entries, observer) => {
				entries.forEach((entry) => {
					if (!entry.isIntersecting) {
						return;
					}

					const item = entry.target;
					item.animate(
						[
							{ opacity: 0, transform: "translateY(14px)" },
							{ opacity: 1, transform: "translateY(0)" }
						],
						{
							duration: 520,
							easing: "cubic-bezier(0.22, 1, 0.36, 1)",
							fill: "forwards"
						}
					);

					observer.unobserve(item);
				});
			}, { threshold: 0.12, rootMargin: "0px 0px -8% 0px" });

			revealItems.forEach((item) => revealObserver.observe(item));
		}
	}

});

const buildStoragePathCandidates = (rawPath) => {
	const trimmed = rawPath.trim();
	if (!trimmed) {
		return [];
	}

	if (trimmed.startsWith("\\\\")) {
		const uncForwardSlashes = trimmed.replace(/^\\+/, "").replaceAll("\\", "/");
		return [
			`file://///${uncForwardSlashes}`,
			`file:///${uncForwardSlashes}`,
			trimmed
		];
	}

	const localForwardSlashes = trimmed.replaceAll("\\", "/");
	return [
		`file:///${localForwardSlashes}`,
		trimmed
	];
};

window.openStoragePath = (event) => {
	const link = event.currentTarget;
	const rawPath = link?.dataset?.path;
	if (!rawPath) {
		return true;
	}

	event.preventDefault();
	const candidates = buildStoragePathCandidates(rawPath);

	for (const candidate of candidates) {
		const pathAnchor = document.createElement("a");
		pathAnchor.href = candidate;
		pathAnchor.target = "_self";
		pathAnchor.rel = "noopener noreferrer";
		document.body.appendChild(pathAnchor);
		pathAnchor.click();
		pathAnchor.remove();
	}

	return false;
};

const fallbackCopyText = (text) => {
	const textarea = document.createElement("textarea");
	textarea.value = text;
	textarea.setAttribute("readonly", "");
	textarea.style.position = "fixed";
	textarea.style.opacity = "0";
	document.body.appendChild(textarea);
	textarea.focus();
	textarea.select();

	let copied = false;
	try {
		copied = document.execCommand("copy");
	} catch {
		copied = false;
	}

	textarea.remove();
	return copied;
};

window.copyStoragePath = async (event) => {
	const button = event.currentTarget;
	const rawPath = button?.dataset?.path;
	if (!rawPath) {
		return false;
	}

	event.preventDefault();

	let copied = false;
	if (navigator.clipboard?.writeText) {
		try {
			await navigator.clipboard.writeText(rawPath);
			copied = true;
		} catch {
			copied = false;
		}
	}

	if (!copied) {
		copied = fallbackCopyText(rawPath);
	}

	const icon = button.querySelector("i");
	const originalTitle = button.title;

	if (copied) {
		button.classList.add("copied");
		button.title = "Copied";
		if (icon) {
			icon.className = "bi bi-check-lg copy-path-icon";
		}
	} else {
		button.title = "Copy failed";
	}

	window.setTimeout(() => {
		button.classList.remove("copied");
		button.title = originalTitle;
		if (icon) {
			icon.className = "bi bi-clipboard copy-path-icon";
		}
	}, 1200);

	return false;
};
