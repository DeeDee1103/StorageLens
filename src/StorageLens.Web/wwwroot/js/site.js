document.addEventListener("DOMContentLoaded", () => {
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
