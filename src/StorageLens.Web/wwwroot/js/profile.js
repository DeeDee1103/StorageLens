/**
 * StorageLens Profile Page
 * Manages user profile data stored in localStorage with Microsoft avatar integration.
 */

(function () {
    "use strict";

    const PROFILE_KEY = "storagelens-profile";
    const AVATAR_KEY  = "storagelens-avatar";

    // Microsoft Graph photo endpoint (used when tenant SSO is configured)
    const MS_GRAPH_PHOTO_URL = "https://graph.microsoft.com/v1.0/me/photo/$value";

    // Defaults
    const defaults = {
        displayName:  "DeeDee",
        email:        "admin@storagelens.local",
        jobTitle:     "Storage Administrator",
        department:   "IT Infrastructure",
        location:     "On-Premises",
        accountSince: new Date().toISOString(),
        lastLogin:    new Date().toISOString(),
        prefTheme:    "system",
        prefDefaultView: "dashboard",
        prefScanComplete: true,
        prefDuplicates:   true,
        prefThreshold:    false,
    };

    // ── Helpers ──────────────────────────────────────────────────────────────

    function getInitials(name) {
        if (!name) return "??";
        const parts = name.trim().split(/\s+/);
        if (parts.length === 1) return parts[0].substring(0, 2).toUpperCase();
        return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    }

    function loadProfile() {
        try {
            const raw = localStorage.getItem(PROFILE_KEY);
            if (raw) return { ...defaults, ...JSON.parse(raw) };
        } catch { /* ignore parse errors */ }
        return { ...defaults };
    }

    function saveProfile(profile) {
        localStorage.setItem(PROFILE_KEY, JSON.stringify(profile));
    }

    function loadAvatar() {
        return localStorage.getItem(AVATAR_KEY) || null;
    }

    function saveAvatar(dataUrl) {
        try {
            localStorage.setItem(AVATAR_KEY, dataUrl);
        } catch {
            // localStorage quota exceeded – silently ignore
        }
    }

    function formatDate(isoStr) {
        try {
            return new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" }).format(new Date(isoStr));
        } catch { return isoStr; }
    }

    function daysAgo(isoStr) {
        try {
            const diff = Date.now() - new Date(isoStr).getTime();
            return Math.max(1, Math.round(diff / 86_400_000));
        } catch { return 1; }
    }

    function showToast(message, type) {
        const toast   = document.getElementById("profileToast");
        const toastMsg = document.getElementById("profileToastMsg");
        if (!toast || !toastMsg) return;
        toastMsg.textContent = message;
        toast.className = `toast align-items-center border-0 text-bg-${type || "success"}`;
        const bsToast = bootstrap.Toast.getOrCreateInstance(toast, { delay: 2200 });
        bsToast.show();
    }

    // ── Avatar rendering ──────────────────────────────────────────────────────

    function renderAvatar(profile, avatarDataUrl) {
        const imgEl       = document.getElementById("profileAvatarImg");
        const initialsEl  = document.getElementById("profileAvatarInitials");

        const topbarImg   = document.getElementById("topbarAvatarImg");
        const topbarDropImg = document.getElementById("topbarAvatarImgDropdown");
        const sidebarEl   = document.getElementById("sidebarAvatarImg");

        const initials = getInitials(profile.displayName);

        function applyAvatarToTopbar(src) {
            if (topbarImg) {
                if (src) {
                    topbarImg.innerHTML = `<img src="${src}" alt="Profile" style="width:40px;height:40px;border-radius:50%;object-fit:cover;" />`;
                } else {
                    topbarImg.textContent = initials;
                }
            }
            if (topbarDropImg) {
                if (src) {
                    topbarDropImg.innerHTML = `<img src="${src}" alt="Profile" style="width:32px;height:32px;border-radius:50%;object-fit:cover;" />`;
                } else {
                    topbarDropImg.textContent = initials;
                }
            }
        }

        function applyAvatarToSidebar(src) {
            if (sidebarEl) {
                if (src) {
                    sidebarEl.innerHTML = `<img src="${src}" alt="Profile" style="width:36px;height:36px;border-radius:50%;object-fit:cover;" />`;
                } else {
                    sidebarEl.textContent = initials;
                    sidebarEl.classList.add("sidebar-profile-avatar-initials");
                }
            }
        }

        if (avatarDataUrl) {
            if (imgEl)      { imgEl.src = avatarDataUrl; imgEl.style.display = "block"; }
            if (initialsEl) { initialsEl.style.display = "none"; }
            applyAvatarToTopbar(avatarDataUrl);
            applyAvatarToSidebar(avatarDataUrl);
            return;
        }

        // Try Microsoft Graph photo as default
        const msImg = new Image();
        msImg.onload = function () {
            if (imgEl)      { imgEl.src = MS_GRAPH_PHOTO_URL; imgEl.style.display = "block"; }
            if (initialsEl) { initialsEl.style.display = "none"; }
            applyAvatarToTopbar(MS_GRAPH_PHOTO_URL);
            applyAvatarToSidebar(MS_GRAPH_PHOTO_URL);
        };
        msImg.onerror = function () {
            // MS Graph not available – fall back to initials
            if (imgEl)      { imgEl.style.display = "none"; }
            if (initialsEl) { initialsEl.style.display = "grid"; initialsEl.textContent = initials; }
            applyAvatarToTopbar(null);
            applyAvatarToSidebar(null);
        };
        msImg.src = MS_GRAPH_PHOTO_URL;
    }

    // ── Populate page ─────────────────────────────────────────────────────────

    function populatePage(profile) {
        const setText = (id, val) => {
            const el = document.getElementById(id);
            if (el) el.textContent = val || "";
        };

        setText("profileDisplayName", profile.displayName);
        setText("profileJobTitle",    profile.jobTitle);
        setText("profileEmail",       profile.email);
        setText("profileDepartment",  profile.department);
        setText("profileLocation",    profile.location);
        setText("secLastLogin",       formatDate(profile.lastLogin));
        setText("secAccountSince",    formatDate(profile.accountSince));
        setText("sidebarProfileName", profile.displayName);
        setText("topbarDropdownName",  profile.displayName);
        setText("topbarDropdownEmail", profile.email);

        // Stats
        setText("statScansRun",       localStorage.getItem("sl-stat-scans")  || "0");
        setText("statDuplicatesFound",localStorage.getItem("sl-stat-dups")   || "0");
        setText("statLocations",      localStorage.getItem("sl-stat-locs")   || "0");
        setText("statDaysActive",     String(daysAgo(profile.accountSince)));

        // Preferences
        const themeButtons = document.querySelectorAll(".profile-theme-btn");
        themeButtons.forEach(btn => {
            btn.classList.toggle("active", btn.dataset.theme === profile.prefTheme);
        });

        const defView = document.getElementById("prefDefaultView");
        if (defView) defView.value = profile.prefDefaultView || "dashboard";

        const check = (id, val) => { const el = document.getElementById(id); if (el) el.checked = !!val; };
        check("prefScanComplete", profile.prefScanComplete);
        check("prefDuplicates",   profile.prefDuplicates);
        check("prefThreshold",    profile.prefThreshold);
    }

    function populateEditModal(profile) {
        const set = (id, val) => { const el = document.getElementById(id); if (el) el.value = val || ""; };
        set("editDisplayName", profile.displayName);
        set("editEmail",       profile.email);
        set("editJobTitle",    profile.jobTitle);
        set("editDepartment",  profile.department);
        set("editLocation",    profile.location);
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    function bindEvents(profile) {
        // Avatar upload
        const uploadInput = document.getElementById("avatarUploadInput");
        if (uploadInput) {
            uploadInput.addEventListener("change", (e) => {
                const file = e.target.files && e.target.files[0];
                if (!file) return;
                if (!file.type.startsWith("image/")) {
                    showToast("Please select an image file.", "danger");
                    return;
                }
                if (file.size > 2 * 1024 * 1024) {
                    showToast("Image must be under 2 MB.", "warning");
                    return;
                }
                const reader = new FileReader();
                reader.onload = (ev) => {
                    const dataUrl = ev.target.result;
                    saveAvatar(dataUrl);
                    renderAvatar(profile, dataUrl);
                    showToast("Profile picture updated!");
                };
                reader.readAsDataURL(file);
            });
        }

        // Save profile from modal
        const btnSave = document.getElementById("btnSaveProfile");
        if (btnSave) {
            btnSave.addEventListener("click", () => {
                const get = (id) => {
                    const el = document.getElementById(id);
                    return el ? el.value.trim() : "";
                };
                const displayName = get("editDisplayName") || defaults.displayName;
                profile.displayName = displayName;
                profile.email       = get("editEmail")       || defaults.email;
                profile.jobTitle    = get("editJobTitle")    || defaults.jobTitle;
                profile.department  = get("editDepartment")  || defaults.department;
                profile.location    = get("editLocation")    || defaults.location;
                saveProfile(profile);
                populatePage(profile);
                renderAvatar(profile, loadAvatar());

                const modal = bootstrap.Modal.getInstance(document.getElementById("editProfileModal"));
                if (modal) modal.hide();
                showToast("Profile saved!");
            });
        }

        // Save preferences
        const btnSavePrefs = document.getElementById("btnSavePreferences");
        if (btnSavePrefs) {
            btnSavePrefs.addEventListener("click", () => {
                const activeThemeBtn = document.querySelector(".profile-theme-btn.active");
                profile.prefTheme = activeThemeBtn ? activeThemeBtn.dataset.theme : "system";

                const defView = document.getElementById("prefDefaultView");
                profile.prefDefaultView = defView ? defView.value : "dashboard";

                profile.prefScanComplete = !!document.getElementById("prefScanComplete")?.checked;
                profile.prefDuplicates   = !!document.getElementById("prefDuplicates")?.checked;
                profile.prefThreshold    = !!document.getElementById("prefThreshold")?.checked;

                saveProfile(profile);
                showToast("Preferences saved!");
            });
        }

        // Theme toggle buttons on profile page
        document.querySelectorAll(".profile-theme-btn").forEach(btn => {
            btn.addEventListener("click", () => {
                document.querySelectorAll(".profile-theme-btn").forEach(b => b.classList.remove("active"));
                btn.classList.add("active");
                const theme = btn.dataset.theme;
                if (theme === "dark") {
                    document.body.classList.add("dark-mode");
                    localStorage.setItem("storagelens-theme", "dark");
                } else if (theme === "light") {
                    document.body.classList.remove("dark-mode");
                    localStorage.setItem("storagelens-theme", "light");
                } else {
                    const prefersDark = window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches;
                    document.body.classList.toggle("dark-mode", prefersDark);
                    localStorage.removeItem("storagelens-theme");
                }
            });
        });

        // Populate modal when opened
        const editModal = document.getElementById("editProfileModal");
        if (editModal) {
            editModal.addEventListener("show.bs.modal", () => populateEditModal(profile));
        }

        // Sign out all sessions (UI only – shows confirmation)
        const btnSignOut = document.getElementById("btnSignOutAll");
        if (btnSignOut) {
            btnSignOut.addEventListener("click", () => {
                showToast("All sessions signed out. (Demo mode – no active sessions.)", "warning");
            });
        }

        // Export my data
        const btnExport = document.getElementById("btnDownloadData");
        if (btnExport) {
            btnExport.addEventListener("click", () => {
                const exportData = {
                    schemaVersion: "1.0",
                    profile,
                    exportedAt: new Date().toISOString(),
                    note: "StorageLens profile data export"
                };
                const blob = new Blob([JSON.stringify(exportData, null, 2)], { type: "application/json" });
                const url  = URL.createObjectURL(blob);
                const a    = document.createElement("a");
                a.href     = url;
                a.download = "storagelens-profile.json";
                document.body.appendChild(a);
                a.click();
                a.remove();
                URL.revokeObjectURL(url);
                showToast("Profile data exported!");
            });
        }
    }

    // ── Bootstrap ─────────────────────────────────────────────────────────────

    document.addEventListener("DOMContentLoaded", () => {
        const profile    = loadProfile();
        const avatarData = loadAvatar();

        populatePage(profile);
        renderAvatar(profile, avatarData);
        bindEvents(profile);
    });

    // Expose renderAvatar globally so site.js layout scripts can call it too
    window.slProfileInit = function () {
        const profile    = loadProfile();
        const avatarData = loadAvatar();
        renderAvatar(profile, avatarData);

        const topbarName  = document.getElementById("topbarDropdownName");
        const topbarEmail = document.getElementById("topbarDropdownEmail");
        const sidebarName = document.getElementById("sidebarProfileName");
        if (topbarName)  topbarName.textContent  = profile.displayName;
        if (topbarEmail) topbarEmail.textContent = profile.email;
        if (sidebarName) sidebarName.textContent = profile.displayName;
    };

}());
