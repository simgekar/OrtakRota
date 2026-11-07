// Fikir Havuzu: Leaflet haritası + OrtakRota Web API (api/trips/{id}/places, api/places/{id})
(function () {
    const app = document.getElementById('placesApp');
    if (!app || typeof L === 'undefined') {
        return;
    }

    const tripId = app.dataset.tripId;
    const canEdit = app.dataset.canEdit === 'true';

    const categoryIcons = {
        Sight: 'bi-camera',
        Food: 'bi-cup-hot',
        Museum: 'bi-bank',
        Accommodation: 'bi-house',
        Shopping: 'bi-bag',
        Other: 'bi-geo-alt'
    };

    // --- Harita ---
    const map = L.map('map').setView([39.0, 35.0], 6); // Türkiye
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> katkıcıları'
    }).addTo(map);

    const markerLayer = L.layerGroup().addTo(map);
    const markers = new Map(); // mekan id -> marker
    let draftMarker = null;    // eklenen / düzenlenen mekanın geçici işareti
    let places = [];
    let editingId = null;

    // --- Sayfa elemanları ---
    const listEl = document.getElementById('placeList');
    const countEl = document.getElementById('placeCount');
    const emptyEl = document.getElementById('emptyState');
    const pageErrorEl = document.getElementById('pageError');
    const searchInput = document.getElementById('searchInput');
    const searchButton = document.getElementById('searchButton');
    const searchResults = document.getElementById('searchResults');

    const form = document.getElementById('placeForm');
    const formTitle = document.getElementById('formTitle');
    const formErrors = document.getElementById('formErrors');
    const nameInput = document.getElementById('placeName');
    const categoryInput = document.getElementById('placeCategory');
    const latInput = document.getElementById('placeLat');
    const lngInput = document.getElementById('placeLng');
    const addressInput = document.getElementById('placeAddress');
    const noteInput = document.getElementById('placeNote');
    const saveButton = document.getElementById('saveButton');
    const cancelButton = document.getElementById('cancelButton');

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text ?? '';
        return div.innerHTML;
    }

    function showPageError(message) {
        pageErrorEl.textContent = message;
        pageErrorEl.classList.remove('d-none');
    }

    // --- Mekanları API'den yükle ve göster ---
    async function loadPlaces(fitToMarkers) {
        pageErrorEl.classList.add('d-none');
        try {
            const response = await fetch(`/api/trips/${tripId}/places`, { headers: { Accept: 'application/json' } });
            if (!response.ok) {
                showPageError('Mekanlar yüklenemedi (' + response.status + ').');
                return;
            }
            places = await response.json();
            render(fitToMarkers);
        } catch {
            showPageError('Sunucuya ulaşılamadı. Sayfayı yenileyip tekrar deneyin.');
        }
    }

    function render(fitToMarkers) {
        markerLayer.clearLayers();
        markers.clear();
        listEl.innerHTML = '';

        countEl.textContent = places.length ? `(${places.length})` : '';
        emptyEl.classList.toggle('d-none', places.length > 0);

        for (const place of places) {
            const marker = L.marker([place.latitude, place.longitude])
                .bindPopup(`<strong>${escapeHtml(place.name)}</strong><br><span class="text-muted">${escapeHtml(place.categoryName)}</span>`)
                .addTo(markerLayer);
            marker.on('click', () => highlight(place.id));
            markers.set(place.id, marker);

            listEl.appendChild(createCard(place));
        }

        if (fitToMarkers && places.length > 0) {
            const bounds = L.latLngBounds(places.map(p => [p.latitude, p.longitude]));
            map.fitBounds(bounds, { padding: [40, 40], maxZoom: 15 });
        }
    }

    function createCard(place) {
        const col = document.createElement('div');
        col.className = 'col';
        const icon = categoryIcons[place.category] || 'bi-geo-alt';

        col.innerHTML = `
            <div class="card h-100 place-card" data-id="${place.id}">
                <div class="card-body">
                    <div class="d-flex justify-content-between align-items-start gap-2">
                        <h3 class="h6 fw-bold mb-1"><i class="bi ${icon} me-1"></i>${escapeHtml(place.name)}</h3>
                        <span class="badge bg-light text-dark border">${escapeHtml(place.categoryName)}</span>
                    </div>
                    ${place.address ? `<p class="small text-muted mb-1">${escapeHtml(place.address)}</p>` : ''}
                    ${place.note ? `<p class="small mb-1">${escapeHtml(place.note)}</p>` : ''}
                    <p class="small text-muted mb-0">Ekleyen: ${escapeHtml(place.addedByName)}</p>
                </div>
                <div class="card-footer bg-transparent border-0 pt-0 d-flex gap-2">
                    <button type="button" class="btn btn-sm btn-outline-primary" data-action="show">
                        <i class="bi bi-geo-alt"></i> Haritada göster
                    </button>
                    ${canEdit ? `
                    <button type="button" class="btn btn-sm btn-outline-secondary" data-action="edit" title="Düzenle">
                        <i class="bi bi-pencil"></i>
                    </button>
                    <button type="button" class="btn btn-sm btn-outline-danger" data-action="delete" title="Sil">
                        <i class="bi bi-trash"></i>
                    </button>` : ''}
                </div>
            </div>`;

        col.querySelector('[data-action="show"]').addEventListener('click', () => focusPlace(place));
        if (canEdit) {
            col.querySelector('[data-action="edit"]').addEventListener('click', () => startEdit(place));
            col.querySelector('[data-action="delete"]').addEventListener('click', () => deletePlace(place));
        }
        return col;
    }

    function highlight(id) {
        document.querySelectorAll('.place-card').forEach(card => {
            card.classList.toggle('active', card.dataset.id === String(id));
        });
    }

    function focusPlace(place) {
        map.setView([place.latitude, place.longitude], 16);
        markers.get(place.id)?.openPopup();
        highlight(place.id);
        document.getElementById('map').scrollIntoView({ behavior: 'smooth', block: 'center' });
    }

    // --- Mekan arama (Nominatim / OpenStreetMap) ---
    async function search() {
        const query = searchInput.value.trim();
        searchResults.innerHTML = '';
        if (query.length < 3) {
            return;
        }

        searchButton.disabled = true;
        try {
            const url = 'https://nominatim.openstreetmap.org/search?format=jsonv2&limit=5&accept-language=tr&q='
                + encodeURIComponent(query);
            const response = await fetch(url, { headers: { Accept: 'application/json' } });
            const results = response.ok ? await response.json() : [];

            if (results.length === 0) {
                searchResults.innerHTML = '<div class="list-group-item small text-muted">Sonuç bulunamadı.</div>';
                return;
            }

            for (const result of results) {
                const item = document.createElement('button');
                item.type = 'button';
                item.className = 'list-group-item list-group-item-action small';
                item.textContent = result.display_name;
                item.addEventListener('click', () => {
                    const lat = parseFloat(result.lat);
                    const lng = parseFloat(result.lon);
                    map.setView([lat, lng], 16);
                    searchResults.innerHTML = '';
                    if (canEdit) {
                        setDraft(lat, lng);
                        if (!nameInput.value) {
                            nameInput.value = result.name || result.display_name.split(',')[0];
                        }
                        addressInput.value = result.display_name.substring(0, 250);
                    }
                });
                searchResults.appendChild(item);
            }
        } catch {
            searchResults.innerHTML = '<div class="list-group-item small text-danger">Arama yapılamadı.</div>';
        } finally {
            searchButton.disabled = false;
        }
    }

    searchButton.addEventListener('click', search);
    searchInput.addEventListener('keydown', e => {
        if (e.key === 'Enter') {
            e.preventDefault();
            search();
        }
    });

    // --- Ekleme / düzenleme formu (yalnızca Kurucu ve Düzenleyici) ---
    function setDraft(lat, lng) {
        latInput.value = lat.toFixed(6);
        lngInput.value = lng.toFixed(6);

        if (!draftMarker) {
            draftMarker = L.marker([lat, lng], { draggable: true, opacity: 0.75 }).addTo(map);
            draftMarker.on('dragend', () => {
                const position = draftMarker.getLatLng();
                latInput.value = position.lat.toFixed(6);
                lngInput.value = position.lng.toFixed(6);
            });
        } else {
            draftMarker.setLatLng([lat, lng]);
        }
    }

    function resetForm() {
        form.reset();
        editingId = null;
        formTitle.textContent = 'Yeni Mekan';
        saveButton.textContent = 'Ekle';
        cancelButton.classList.add('d-none');
        formErrors.classList.add('d-none');
        latInput.value = '';
        lngInput.value = '';
        if (draftMarker) {
            map.removeLayer(draftMarker);
            draftMarker = null;
        }
    }

    function startEdit(place) {
        resetForm();
        editingId = place.id;
        formTitle.textContent = 'Mekanı Düzenle';
        saveButton.textContent = 'Kaydet';
        cancelButton.classList.remove('d-none');

        nameInput.value = place.name;
        categoryInput.value = place.category;
        addressInput.value = place.address ?? '';
        noteInput.value = place.note ?? '';
        setDraft(place.latitude, place.longitude);

        map.setView([place.latitude, place.longitude], 16);
        highlight(place.id);
        form.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }

    function showFormErrors(messages) {
        formErrors.innerHTML = messages.map(m => `<div>${escapeHtml(m)}</div>`).join('');
        formErrors.classList.remove('d-none');
    }

    async function savePlace(e) {
        e.preventDefault();
        formErrors.classList.add('d-none');

        const payload = {
            name: nameInput.value.trim(),
            category: categoryInput.value,
            latitude: latInput.value ? parseFloat(latInput.value) : null,
            longitude: lngInput.value ? parseFloat(lngInput.value) : null,
            address: addressInput.value.trim() || null,
            note: noteInput.value.trim() || null
        };

        const isEdit = editingId !== null;
        const url = isEdit ? `/api/places/${editingId}` : `/api/trips/${tripId}/places`;

        saveButton.disabled = true;
        try {
            const response = await fetch(url, {
                method: isEdit ? 'PUT' : 'POST',
                headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
                body: JSON.stringify(payload)
            });

            if (response.status === 400) {
                // [ApiController] doğrulama hatalarını { errors: { Alan: ["mesaj"] } } biçiminde döndürür
                const problem = await response.json();
                const messages = Object.values(problem.errors || {}).flat();
                showFormErrors(messages.length ? messages : ['Bilgileri kontrol edin.']);
                return;
            }
            if (response.status === 403) {
                showFormErrors(['Bu işlem için yetkin yok.']);
                return;
            }
            if (!response.ok) {
                showFormErrors(['Kaydedilemedi (' + response.status + ').']);
                return;
            }

            resetForm();
            await loadPlaces(false);
        } catch {
            showFormErrors(['Sunucuya ulaşılamadı.']);
        } finally {
            saveButton.disabled = false;
        }
    }

    async function deletePlace(place) {
        if (!confirm(`"${place.name}" fikir havuzundan silinsin mi?`)) {
            return;
        }

        const response = await fetch(`/api/places/${place.id}`, { method: 'DELETE' });
        if (!response.ok) {
            showPageError('Mekan silinemedi (' + response.status + ').');
            return;
        }
        if (editingId === place.id) {
            resetForm();
        }
        await loadPlaces(false);
    }

    if (canEdit) {
        map.on('click', e => setDraft(e.latlng.lat, e.latlng.lng));
        form.addEventListener('submit', savePlace);
        cancelButton.addEventListener('click', resetForm);
    }

    loadPlaces(true);
})();
