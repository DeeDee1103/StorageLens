document.addEventListener("DOMContentLoaded", () => {
	const themeStorageKey = "storagelens-theme";
	const darkThemeValue = "dark";
	const lightThemeValue = "light";
	const themeToggle = document.getElementById("darkModeToggle");
	const themeIcon = themeToggle?.querySelector("i");

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
	revealItems.forEach((item, index) => {
		item.animate(
			[
				{ opacity: 0, transform: "translateY(10px)" },
				{ opacity: 1, transform: "translateY(0)" }
			],
			{
				duration: 420,
				delay: index * 55,
				fill: "forwards",
				easing: "ease-out"
			});
	});

	// Form submit loading state: disable submit button and show spinner
	document.querySelectorAll("form[method='post']").forEach((form) => {
		form.addEventListener("submit", (e) => {
			const submitBtn = form.querySelector("button[type='submit']");
			if (!submitBtn || submitBtn.disabled) return;
			submitBtn.disabled = true;
			const originalHtml = submitBtn.innerHTML;
			submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>Working…';
			// Re-enable after 8 s as a safety net (e.g. validation failure keeps page alive)
			setTimeout(() => {
				submitBtn.disabled = false;
				submitBtn.innerHTML = originalHtml;
			}, 8000);
		});
	});
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
