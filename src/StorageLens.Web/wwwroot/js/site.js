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
});
