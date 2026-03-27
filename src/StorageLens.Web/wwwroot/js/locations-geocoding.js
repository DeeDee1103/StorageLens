/**
 * StorageLens Location Search & Geocoding
 * Supports multiple methods: text search, geolocation API, manual coordinates
 */

class LocationGeocoder {
    constructor() {
        this.nominatimBaseUrl = 'https://nominatim.openstreetmap.org';
        this.debounceTimer = null;
        this.debounceDelay = 300;
        this.searchCache = new Map();
        this.currentCoords = null;
        this.currentLocation = null;
    }

    /**
     * Initialize location search handlers
     */
    init() {
        this.setupClassicForm();
        this.setupInteractiveForm();
    }

    /**
     * Setup classic form handlers
     */
    setupClassicForm() {
        const searchInput = document.getElementById('classicLocationSearch');
        const dropdown = document.getElementById('classicLocationDropdown');
        const geoBtn = document.getElementById('classicGeoBtn');
        const advancedToggle = document.querySelector('[data-classic-coords-toggle]');
        const applyBtn = document.querySelector('[data-classic-coords-apply]');

        if (searchInput) {
            searchInput.addEventListener('input', (e) => this.handleSearchInput(e, 'classic', dropdown));
            document.addEventListener('click', (e) => this.handleClickOutside(e, dropdown));
        }

        if (geoBtn) {
            geoBtn.addEventListener('click', (e) => {
                e.preventDefault();
                this.useGeolocation('classic');
            });
        }

        if (advancedToggle) {
            advancedToggle.addEventListener('click', (e) => {
                e.preventDefault();
                this.toggleAdvancedCoords('classic');
            });
        }

        if (applyBtn) {
            applyBtn.addEventListener('click', (e) => {
                e.preventDefault();
                this.applyManualCoordinates('classic');
            });
        }
    }

    /**
     * Setup interactive form handlers
     */
    setupInteractiveForm() {
        const searchInput = document.getElementById('InteractiveLocationSearch');
        const dropdown = document.getElementById('InteractiveLocationDropdown');
        const geoBtn = document.getElementById('interactiveGeoBtn');
        const advancedToggle = document.querySelector('[data-interactive-coords-toggle]');
        const applyBtn = document.querySelector('[data-interactive-coords-apply]');

        if (searchInput) {
            searchInput.addEventListener('input', (e) => this.handleSearchInput(e, 'interactive', dropdown));
            document.addEventListener('click', (e) => this.handleClickOutside(e, dropdown));
        }

        if (geoBtn) {
            geoBtn.addEventListener('click', (e) => {
                e.preventDefault();
                this.useGeolocation('interactive');
            });
        }

        if (advancedToggle) {
            advancedToggle.addEventListener('click', (e) => {
                e.preventDefault();
                this.toggleAdvancedCoords('interactive');
            });
        }

        if (applyBtn) {
            applyBtn.addEventListener('click', (e) => {
                e.preventDefault();
                this.applyManualCoordinates('interactive');
            });
        }
    }

    /**
     * Handle location search input with debouncing
     */
    handleSearchInput(event, formType, dropdown) {
        const searchTerm = event.target.value.trim();
        const statusEl = document.getElementById(`${formType}LocationStatus`);

        clearTimeout(this.debounceTimer);

        if (!searchTerm) {
            dropdown.hidden = true;
            if (statusEl) statusEl.textContent = '';
            return;
        }

        if (statusEl) {
            statusEl.innerHTML = '<span class="location-status-loading">Searching locations...</span>';
        }

        this.debounceTimer = setTimeout(() => {
            this.searchLocations(searchTerm, formType, dropdown);
        }, this.debounceDelay);
    }

    /**
     * Search locations using OpenStreetMap Nominatim API
     */
    async searchLocations(query, formType, dropdown) {
        const statusEl = document.getElementById(`${formType}LocationStatus`);

        try {
            // Check cache first
            if (this.searchCache.has(query)) {
                const cachedResults = this.searchCache.get(query);
                this.displaySearchResults(cachedResults, formType, dropdown);
                if (statusEl) statusEl.innerHTML = '<span class="location-status-success">Found locations</span>';
                return;
            }

            const response = await fetch(
                `${this.nominatimBaseUrl}/search?format=json&q=${encodeURIComponent(query)}&limit=8&addressdetails=1`,
                {
                    headers: {
                        'Accept-Language': 'en'
                    }
                }
            );

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const results = await response.json();

            if (!results || results.length === 0) {
                dropdown.innerHTML = '<div class="location-dropdown-item" disabled style="color: var(--sl-muted);">No locations found for "' + query + '"</div>';
                dropdown.hidden = false;
                if (statusEl) statusEl.innerHTML = '<span class="location-status-error">No locations found. Try a different search term.</span>';
                return;
            }

            // Cache the results
            this.searchCache.set(query, results);
            this.displaySearchResults(results, formType, dropdown);
            if (statusEl) statusEl.innerHTML = '<span class="location-status-success">Found ' + results.length + ' location(s)</span>';

        } catch (error) {
            console.error('Location search error:', error);
            dropdown.innerHTML = '<div class="location-dropdown-item" disabled style="color: var(--sl-muted);">Unable to search locations. Please try again or enter coordinates manually.</div>';
            dropdown.hidden = false;
            if (statusEl) statusEl.innerHTML = '<span class="location-status-error">Search failed. Try entering coordinates manually.</span>';
        }
    }

    /**
     * Display search results in dropdown
     */
    displaySearchResults(results, formType, dropdown) {
        dropdown.innerHTML = '';

        results.forEach(result => {
            const item = document.createElement('button');
            item.type = 'button';
            item.className = 'location-dropdown-item';
            item.innerHTML = `
                <div class="location-result">
                    <span class="location-result-icon">📍</span>
                    <div class="location-result-details">
                        <div class="location-result-name">${this.escapeHtml(result.name)}</div>
                        <div class="location-result-country">${this.escapeHtml(result.address?.country || '')}</div>
                    </div>
                </div>
            `;

            item.addEventListener('click', (e) => {
                e.preventDefault();
                this.selectLocation(result, formType, dropdown);
            });

            dropdown.appendChild(item);
        });

        dropdown.hidden = false;
    }

    /**
     * Select a location from dropdown results
     */
    selectLocation(result, formType, dropdown) {
        const lat = parseFloat(result.lat);
        const lon = parseFloat(result.lon);
        
        const latInput = document.getElementById(`${formType === 'classic' ? 'classicLatitude' : 'InteractiveLatitude'}`);
        const lonInput = document.getElementById(`${formType === 'classic' ? 'classicLongitude' : 'InteractiveLongitude'}`);
        const siteLocationInput = document.getElementById(`${formType === 'classic' ? 'classicSiteLocation' : 'InteractiveSiteLocation'}`);
        const searchInput = document.getElementById(`${formType === 'classic' ? 'classicLocationSearch' : 'InteractiveLocationSearch'}`);
        const statusEl = document.getElementById(`${formType}LocationStatus`);

        // Hidden inputs for actual form submission
        const hiddenLat = document.querySelector(`input[name="Latitude"]`);
        const hiddenLon = document.querySelector(`input[name="Longitude"]`);
        const hiddenSiteLocation = document.querySelector(`input[name="SiteLocation"]`);

        if (hiddenLat && hiddenLon) {
            hiddenLat.value = lat;
            hiddenLon.value = lon;
        }

        if (siteLocationInput || hiddenSiteLocation) {
            const locationName = result.name + (result.address?.country ? ', ' + result.address.country : '');
            if (siteLocationInput) siteLocationInput.value = locationName;
            if (hiddenSiteLocation) hiddenSiteLocation.value = locationName;
        }

        // Update visible display
        if (searchInput) {
            searchInput.value = result.name;
        }

        // Store current coordinates
        this.currentCoords = { lat, lon };
        this.currentLocation = result.name;

        // Show success message
        if (statusEl) {
            statusEl.innerHTML = `<span class="location-status-success">📍 ${this.escapeHtml(result.name)} (${lat.toFixed(4)}, ${lon.toFixed(4)})</span>`;
        }

        // Hide dropdown
        dropdown.hidden = true;
    }

    /**
     * Use browser geolocation API
     */
    useGeolocation(formType) {
        const geoBtn = document.getElementById(`${formType}GeoBtn`);
        const statusEl = document.getElementById(`${formType}LocationStatus`);

        if (!navigator.geolocation) {
            if (statusEl) statusEl.innerHTML = '<span class="location-status-error">Browser geolocation is not supported. Please enter coordinates manually.</span>';
            return;
        }

        // Show loading state
        if (geoBtn) {
            geoBtn.classList.add('is-loading');
            geoBtn.disabled = true;
        }

        if (statusEl) {
            statusEl.innerHTML = '<span class="location-status-loading">Requesting your location...</span>';
        }

        navigator.geolocation.getCurrentPosition(
            (position) => this.handleGeolocationSuccess(position, formType, geoBtn, statusEl),
            (error) => this.handleGeolocationError(error, formType, geoBtn, statusEl),
            {
                timeout: 10000,
                enableHighAccuracy: false
            }
        );
    }

    /**
     * Handle successful geolocation
     */
    handleGeolocationSuccess(position, formType, geoBtn, statusEl) {
        const lat = position.coords.latitude;
        const lon = position.coords.longitude;
        const accuracy = Math.round(position.coords.accuracy);

        // Update hidden inputs
        const hiddenLat = document.querySelector(`input[name="Latitude"]`);
        const hiddenLon = document.querySelector(`input[name="Longitude"]`);

        if (hiddenLat && hiddenLon) {
            hiddenLat.value = lat;
            hiddenLon.value = lon;
        }

        this.currentCoords = { lat, lon };

        if (statusEl) {
            statusEl.innerHTML = `<span class="location-status-success">📍 Location found: ${lat.toFixed(4)}, ${lon.toFixed(4)} (±${accuracy}m)</span>`;
        }

        // Restore button
        if (geoBtn) {
            geoBtn.classList.remove('is-loading');
            geoBtn.disabled = false;
        }

        // Auto-hide status after 5 seconds
        setTimeout(() => {
            if (statusEl) statusEl.textContent = '';
        }, 5000);
    }

    /**
     * Handle geolocation errors
     */
    handleGeolocationError(error, formType, geoBtn, statusEl) {
        let errorMessage = '';

        switch (error.code) {
            case error.PERMISSION_DENIED:
                errorMessage = 'Location permission denied. Please enable location in your browser settings.';
                break;
            case error.POSITION_UNAVAILABLE:
                errorMessage = 'Your device location is not available. Please enter coordinates manually.';
                break;
            case error.TIMEOUT:
                errorMessage = 'Location request timed out. Please try again or enter coordinates manually.';
                break;
            default:
                errorMessage = 'Unable to get your location. Please enter coordinates manually.';
        }

        if (statusEl) {
            statusEl.innerHTML = `<span class="location-status-error">${errorMessage}</span>`;
        }

        // Restore button
        if (geoBtn) {
            geoBtn.classList.remove('is-loading');
            geoBtn.disabled = false;
        }
    }

    /**
     * Toggle advanced coordinates section
     */
    toggleAdvancedCoords(formType) {
        const panel = document.getElementById(`${formType === 'classic' ? 'classicAdvancedCoords' : 'InteractiveAdvancedCoords'}`);
        const toggle = document.querySelector(`[data-${formType}-coords-toggle]`);

        if (panel) {
            const isHidden = panel.hidden;
            panel.hidden = !isHidden;
            
            if (toggle) {
                toggle.setAttribute('aria-expanded', !isHidden);
            }
        }
    }

    /**
     * Apply manual coordinates
     */
    applyManualCoordinates(formType) {
        const latManual = document.getElementById(`${formType === 'classic' ? 'classicLatitudeManual' : 'InteractiveLatitudeManual'}`);
        const lonManual = document.getElementById(`${formType === 'classic' ? 'classicLongitudeManual' : 'InteractiveLongitudeManual'}`);
        const statusEl = document.getElementById(`${formType}LocationStatus`);

        if (!latManual || !lonManual) return;

        const lat = parseFloat(latManual.value);
        const lon = parseFloat(lonManual.value);

        // Validation
        if (isNaN(lat) || isNaN(lon)) {
            if (statusEl) statusEl.innerHTML = '<span class="location-status-error">Please enter valid latitude and longitude values.</span>';
            return;
        }

        if (lat < -90 || lat > 90) {
            if (statusEl) statusEl.innerHTML = '<span class="location-status-error">Latitude must be between -90 and 90.</span>';
            return;
        }

        if (lon < -180 || lon > 180) {
            if (statusEl) statusEl.innerHTML = '<span class="location-status-error">Longitude must be between -180 and 180.</span>';
            return;
        }

        // Apply coordinates
        const hiddenLat = document.querySelector(`input[name="Latitude"]`);
        const hiddenLon = document.querySelector(`input[name="Longitude"]`);

        if (hiddenLat && hiddenLon) {
            hiddenLat.value = lat;
            hiddenLon.value = lon;
        }

        this.currentCoords = { lat, lon };

        if (statusEl) {
            statusEl.innerHTML = `<span class="location-status-success">✓ Coordinates applied: ${lat.toFixed(4)}, ${lon.toFixed(4)}</span>`;
        }
    }

    /**
     * Handle clicks outside dropdown to close it
     */
    handleClickOutside(event, dropdown) {
        if (!dropdown || dropdown.hidden) return;

        const searchContainer = dropdown.parentElement;
        if (!searchContainer.contains(event.target)) {
            dropdown.hidden = true;
        }
    }

    /**
     * Escape HTML special characters to prevent XSS
     */
    escapeHtml(text) {
        if (!text) return '';
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return text.replace(/[&<>"']/g, m => map[m]);
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    const geocoder = new LocationGeocoder();
    geocoder.init();
});
