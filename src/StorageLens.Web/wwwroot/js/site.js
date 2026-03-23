document.addEventListener("DOMContentLoaded", () => {
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

	const topLevelSections = Array.from(document.querySelectorAll("main.content-area > section"));
	if (topLevelSections.length > 0) {
		const contentArea = document.querySelector("main.content-area");
		const perPageCollapseKey = `sl-collapsed-sections:${window.location.pathname}`;
		let collapsedStateByPage = {};

		try {
			const raw = localStorage.getItem(perPageCollapseKey);
			collapsedStateByPage = raw ? JSON.parse(raw) : {};
		} catch {
			collapsedStateByPage = {};
		}

		const persistSectionState = (sectionKey, isCollapsed) => {
			collapsedStateByPage[sectionKey] = isCollapsed;
			localStorage.setItem(perPageCollapseKey, JSON.stringify(collapsedStateByPage));
		};

		const sectionControllers = [];

		topLevelSections.forEach((section, index) => {
			if (section.dataset.slCollapsibleInit === "true") {
				return;
			}

			const sectionStateKey = section.id ? `id:${section.id}` : `index:${index + 1}`;

			section.dataset.slCollapsibleInit = "true";
			section.classList.add("sl-collapsible-section");

			const controls = document.createElement("div");
			controls.className = "sl-section-toggle-wrap";

			const button = document.createElement("button");
			button.type = "button";
			button.className = "btn btn-sm btn-outline-secondary sl-section-toggle";
			button.setAttribute("aria-expanded", "true");

			const body = document.createElement("div");
			body.className = "sl-section-body";
			body.id = section.id ? `${section.id}__body` : `sl-section-body-${index + 1}`;
			button.setAttribute("aria-controls", body.id);

			while (section.firstChild) {
				body.appendChild(section.firstChild);
			}

			const setButtonState = (expanded) => {
				button.setAttribute("aria-expanded", String(expanded));
				button.innerHTML = expanded
					? '<i class="bi bi-chevron-up" aria-hidden="true"></i><span>Collapse Section</span>'
					: '<i class="bi bi-chevron-down" aria-hidden="true"></i><span>Expand Section</span>';
			};

			const applySectionState = (expanded) => {
				body.hidden = !expanded;
				section.classList.toggle("is-collapsed", !expanded);
				setButtonState(expanded);
			};

			button.addEventListener("click", () => {
				const currentlyExpanded = button.getAttribute("aria-expanded") === "true";
				const nextExpanded = !currentlyExpanded;
				applySectionState(nextExpanded);
				persistSectionState(sectionStateKey, !nextExpanded);
			});

			const shouldStartCollapsed = collapsedStateByPage[sectionStateKey] === true;
			applySectionState(!shouldStartCollapsed);
			sectionControllers.push({
				sectionKey: sectionStateKey,
				applyExpanded: () => {
					applySectionState(true);
					persistSectionState(sectionStateKey, false);
				},
				applyCollapsed: () => {
					applySectionState(false);
					persistSectionState(sectionStateKey, true);
				}
			});
			controls.appendChild(button);
			section.appendChild(controls);
			section.appendChild(body);
		});

		if (contentArea && sectionControllers.length > 1) {
			const pageControls = document.createElement("div");
			pageControls.className = "sl-page-controls";

			const expandAllButton = document.createElement("button");
			expandAllButton.type = "button";
			expandAllButton.className = "btn btn-sm btn-outline-primary sl-expand-all";
			expandAllButton.innerHTML = '<i class="bi bi-arrows-expand" aria-hidden="true"></i><span>Expand All Sections</span>';

			const collapseAllButton = document.createElement("button");
			collapseAllButton.type = "button";
			collapseAllButton.className = "btn btn-sm btn-outline-secondary sl-collapse-all";
			collapseAllButton.innerHTML = '<i class="bi bi-arrows-collapse" aria-hidden="true"></i><span>Collapse All Sections</span>';

			expandAllButton.addEventListener("click", () => {
				sectionControllers.forEach((controller) => controller.applyExpanded());
			});

			collapseAllButton.addEventListener("click", () => {
				sectionControllers.forEach((controller) => controller.applyCollapsed());
			});

			pageControls.appendChild(expandAllButton);
			pageControls.appendChild(collapseAllButton);
			contentArea.insertBefore(pageControls, topLevelSections[0]);
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
