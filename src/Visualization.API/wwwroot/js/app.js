// ===== Application State =====
let network = null;
let map = null;
const API_BASE = '';

// ===== Navigation =====
document.querySelectorAll('.nav-btn').forEach(btn => {
    btn.addEventListener('click', () => {
        document.querySelectorAll('.nav-btn').forEach(b => b.classList.remove('active'));
        document.querySelectorAll('.view').forEach(v => v.classList.add('hidden'));
        btn.classList.add('active');
        const viewId = btn.dataset.view + '-view';
        document.getElementById(viewId).classList.remove('hidden');
        document.getElementById(viewId).classList.add('active');

        if (btn.dataset.view === 'geospatial' && !map) {
            initMap();
        }
        if (btn.dataset.view === 'dashboard') {
            loadDashboard();
        }
        if (btn.dataset.view === 'temporal') {
            loadTimeline();
        }
    });
});

// ===== Network Visualization (vis-network.js) =====
async function loadNetwork(filter = {}) {
    try {
        let url = `${API_BASE}/api/network`;
        const params = new URLSearchParams();
        if (filter.entityTypes) params.append('EntityTypes', filter.entityTypes.join(','));
        if (filter.startDate) params.append('StartDate', filter.startDate);
        if (filter.endDate) params.append('EndDate', filter.endDate);
        
        const queryString = params.toString();
        if (queryString) url += '?' + queryString;

        const response = await fetch(url);
        const data = await response.json();
        renderNetwork(data);
    } catch (error) {
        console.error('Error loading network:', error);
    }
}

function renderNetwork(data) {
    const container = document.getElementById('network-container');
    
    const groupColors = {
        'Provider': { background: '#ff6b6b', border: '#c0392b', font: { color: '#fff' } },
        'Claimant': { background: '#4ecdc4', border: '#16a085', font: { color: '#fff' } },
        'Policy': { background: '#f39c12', border: '#d35400', font: { color: '#fff' } },
        'Unknown': { background: '#95a5a6', border: '#7f8c8d', font: { color: '#fff' } }
    };

    const nodes = new vis.DataSet(data.nodes.map(node => ({
        id: node.id,
        label: node.label,
        group: node.group,
        title: `${node.label} (${node.group})`,
        size: node.size || 25,
        color: groupColors[node.group] || groupColors['Unknown'],
        font: { color: '#fff', size: 12 }
    })));

    const edges = new vis.DataSet(data.edges.map((edge, i) => ({
        id: i,
        from: edge.from,
        to: edge.to,
        label: edge.label,
        arrows: 'to',
        color: { color: '#555', highlight: '#00d4ff' },
        font: { color: '#aaa', size: 10 }
    })));

    const options = {
        nodes: {
            shape: 'dot',
            borderWidth: 2,
            shadow: true
        },
        edges: {
            width: 1.5,
            smooth: { type: 'continuous' }
        },
        physics: {
            forceAtlas2Based: {
                gravitationalConstant: -50,
                centralGravity: 0.005,
                springLength: 150,
                springConstant: 0.08
            },
            solver: 'forceAtlas2Based',
            stabilization: { iterations: 100 }
        },
        interaction: {
            hover: true,
            tooltipDelay: 200,
            navigationButtons: true,
            keyboard: true
        },
        groups: groupColors
    };

    network = new vis.Network(container, { nodes, edges }, options);

    network.on('click', function(params) {
        if (params.nodes.length > 0) {
            const nodeId = params.nodes[0];
            const node = data.nodes.find(n => n.id === nodeId);
            showNodeDetails(node);
        } else {
            document.getElementById('node-details').classList.add('hidden');
        }
    });

    network.on('doubleClick', function(params) {
        if (params.nodes.length > 0) {
            const nodeId = params.nodes[0];
            loadEntityNetwork(nodeId);
        }
    });
}

function showNodeDetails(node) {
    const panel = document.getElementById('node-details');
    const content = document.getElementById('node-details-content');
    
    let html = `<p><strong>ID:</strong> ${node.id}</p>`;
    html += `<p><strong>Name:</strong> ${node.label}</p>`;
    html += `<p><strong>Type:</strong> ${node.group}</p>`;
    
    if (node.properties) {
        html += '<h4>Properties:</h4>';
        Object.entries(node.properties).forEach(([key, value]) => {
            html += `<p><strong>${key}:</strong> ${value}</p>`;
        });
    }
    
    content.innerHTML = html;
    panel.classList.remove('hidden');
}

async function loadEntityNetwork(entityId) {
    const depth = document.getElementById('depth-filter').value || 2;
    try {
        const response = await fetch(`${API_BASE}/api/network/entity/${entityId}?depth=${depth}`);
        const data = await response.json();
        renderNetwork(data);
    } catch (error) {
        console.error('Error loading entity network:', error);
    }
}

async function findShortestPath() {
    const from = document.getElementById('path-from').value;
    const to = document.getElementById('path-to').value;
    
    if (!from || !to) {
        alert('Please enter both From and To entity IDs');
        return;
    }

    try {
        const response = await fetch(`${API_BASE}/api/network/path?from=${from}&to=${to}`);
        const data = await response.json();
        
        if (data.nodes && data.nodes.length > 0) {
            renderNetwork(data);
        } else {
            alert('No path found between the specified entities');
        }
    } catch (error) {
        console.error('Error finding path:', error);
    }
}

async function loadMetrics() {
    try {
        const response = await fetch(`${API_BASE}/api/network/metrics`);
        const metrics = await response.json();
        
        const panel = document.getElementById('metrics-panel');
        const content = document.getElementById('metrics-content');
        
        let html = '';
        html += `<div class="metric-item"><span class="label">Total Nodes</span><span class="value">${metrics.totalNodes}</span></div>`;
        html += `<div class="metric-item"><span class="label">Total Edges</span><span class="value">${metrics.totalEdges}</span></div>`;
        html += `<div class="metric-item"><span class="label">Density</span><span class="value">${metrics.density.toFixed(4)}</span></div>`;
        
        if (Object.keys(metrics.degreeCentrality).length > 0) {
            html += '<h4 style="margin-top:0.5rem;color:#ccc;">Degree Centrality (Top 10)</h4>';
            Object.entries(metrics.degreeCentrality).slice(0, 10).forEach(([id, value]) => {
                html += `<div class="metric-item"><span class="label">${id}</span><span class="value">${value.toFixed(4)}</span></div>`;
            });
        }
        
        content.innerHTML = html;
        panel.classList.toggle('hidden');
    } catch (error) {
        console.error('Error loading metrics:', error);
    }
}

// ===== Geospatial (Mapbox + Leaflet/OpenStreetMap fallback) =====
let mapProvider = null; // 'mapbox' or 'leaflet'
let leafletMap = null;
let leafletLayers = [];

async function initMap() {
    try {
        const configResponse = await fetch(`${API_BASE}/api/geospatial/config`);
        const config = await configResponse.json();
        
        const hasValidToken = config.accessToken && 
            config.accessToken !== 'YOUR_MAPBOX_ACCESS_TOKEN' && 
            config.accessToken.startsWith('pk.');

        if (hasValidToken) {
            await initMapbox(config);
        } else {
            initLeaflet(config);
        }
    } catch (error) {
        console.error('Error initializing map, falling back to Leaflet:', error);
        initLeaflet({ center: [-87.6298, 41.8781], zoom: 10 });
    }
}

async function initMapbox(config) {
    mapProvider = 'mapbox';
    mapboxgl.accessToken = config.accessToken;
    
    map = new mapboxgl.Map({
        container: 'map-container',
        style: config.style,
        center: config.center,
        zoom: config.zoom
    });

    map.addControl(new mapboxgl.NavigationControl());
    map.addControl(new mapboxgl.ScaleControl());

    map.on('load', () => {
        loadMapData('entities');
    });
}

function initLeaflet(config) {
    mapProvider = 'leaflet';
    const container = document.getElementById('map-container');
    container.innerHTML = '';
    
    const center = Array.isArray(config.center) ? [config.center[1], config.center[0]] : [41.8781, -87.6298];
    const zoom = config.zoom || 10;

    leafletMap = L.map('map-container', {
        zoomControl: true
    }).setView(center, zoom);

    // OpenStreetMap tiles (no token required)
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
        maxZoom: 19
    }).addTo(leafletMap);

    // Add dark tile layer option
    const darkTiles = L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/">CARTO</a>',
        maxZoom: 19
    });
    darkTiles.addTo(leafletMap);

    // Force a resize after init
    setTimeout(() => { leafletMap.invalidateSize(); }, 100);

    loadMapData('entities');
}

async function loadMapData(layer) {
    if (mapProvider === 'mapbox') {
        await loadMapDataMapbox(layer);
    } else if (mapProvider === 'leaflet') {
        await loadMapDataLeaflet(layer);
    }
}

// --- Leaflet map data loading ---
async function loadMapDataLeaflet(layer) {
    if (!leafletMap) return;

    // Clear existing layers
    leafletLayers.forEach(l => leafletMap.removeLayer(l));
    leafletLayers = [];

    const entityType = document.getElementById('map-entity-type').value;

    if (layer === 'entities') {
        const url = entityType 
            ? `${API_BASE}/api/geospatial/entities?type=${entityType}`
            : `${API_BASE}/api/geospatial/entities`;
        const response = await fetch(url);
        const geoJson = await response.json();

        const markers = L.markerClusterGroup();
        
        geoJson.features.forEach(feature => {
            const coords = feature.geometry.coordinates;
            const props = feature.properties;
            const color = props.entityType === 'Provider' ? '#ff6b6b' : 
                         props.entityType === 'Claimant' ? '#4ecdc4' : '#f39c12';
            const radius = Math.max(8, (props.claimCount || 0) * 4 + 8);

            const marker = L.circleMarker([coords[1], coords[0]], {
                radius: radius,
                fillColor: color,
                color: '#fff',
                weight: 2,
                opacity: 1,
                fillOpacity: 0.8
            });

            marker.bindPopup(`
                <div style="min-width:180px;">
                    <h4 style="margin:0 0 5px;color:#333;">${props.name}</h4>
                    <p style="margin:2px 0;"><strong>Type:</strong> ${props.entityType}</p>
                    <p style="margin:2px 0;"><strong>Address:</strong> ${props.address || 'N/A'}</p>
                    <p style="margin:2px 0;"><strong>Claims:</strong> ${props.claimCount} | <strong>Alerts:</strong> ${props.alertCount}</p>
                </div>
            `);

            markers.addLayer(marker);
        });

        leafletMap.addLayer(markers);
        leafletLayers.push(markers);

        // Fit bounds to show all markers
        if (geoJson.features.length > 0) {
            const bounds = L.latLngBounds(
                geoJson.features.map(f => [f.geometry.coordinates[1], f.geometry.coordinates[0]])
            );
            leafletMap.fitBounds(bounds, { padding: [30, 30] });
        }

    } else if (layer === 'claims') {
        const response = await fetch(`${API_BASE}/api/geospatial/claims`);
        const geoJson = await response.json();

        const markers = L.markerClusterGroup();

        geoJson.features.forEach(feature => {
            const coords = feature.geometry.coordinates;
            const props = feature.properties;
            const color = props.status === 'Investigating' ? '#ff8800' :
                         props.status === 'Open' ? '#ffcc00' :
                         props.status === 'Closed' ? '#44ff44' : '#95a5a6';
            const radius = Math.max(6, (props.amount || 0) / 5000 + 6);

            const marker = L.circleMarker([coords[1], coords[0]], {
                radius: Math.min(radius, 25),
                fillColor: color,
                color: '#fff',
                weight: 2,
                opacity: 1,
                fillOpacity: 0.8
            });

            marker.bindPopup(`
                <div style="min-width:180px;">
                    <h4 style="margin:0 0 5px;color:#333;">${props.claimNumber}</h4>
                    <p style="margin:2px 0;"><strong>Status:</strong> ${props.status}</p>
                    <p style="margin:2px 0;"><strong>Type:</strong> ${props.claimType}</p>
                    <p style="margin:2px 0;"><strong>Amount:</strong> $${Number(props.amount).toLocaleString()}</p>
                    <p style="margin:2px 0;"><strong>Date:</strong> ${new Date(props.incidentDate).toLocaleDateString()}</p>
                    <p style="margin:2px 0;"><strong>Location:</strong> ${props.location || 'N/A'}</p>
                </div>
            `);

            markers.addLayer(marker);
        });

        leafletMap.addLayer(markers);
        leafletLayers.push(markers);

        if (geoJson.features.length > 0) {
            const bounds = L.latLngBounds(
                geoJson.features.map(f => [f.geometry.coordinates[1], f.geometry.coordinates[0]])
            );
            leafletMap.fitBounds(bounds, { padding: [30, 30] });
        }

    } else if (layer === 'heatmap') {
        const response = await fetch(`${API_BASE}/api/geospatial/heatmap`);
        const heatData = await response.json();
        
        const heatPoints = heatData.map(d => [d.incidentLatitude, d.incidentLongitude, d.weight]);
        
        const heat = L.heatLayer(heatPoints, {
            radius: 35,
            blur: 20,
            maxZoom: 15,
            gradient: { 0.2: 'blue', 0.4: 'cyan', 0.6: 'lime', 0.8: 'yellow', 1.0: 'red' }
        });

        heat.addTo(leafletMap);
        leafletLayers.push(heat);

        if (heatData.length > 0) {
            const bounds = L.latLngBounds(
                heatData.map(d => [d.incidentLatitude, d.incidentLongitude])
            );
            leafletMap.fitBounds(bounds, { padding: [30, 30] });
        }
    }
}

// --- Mapbox map data loading ---
async function loadMapDataMapbox(layer) {
    if (!map) return;

    // Remove existing layers/sources
    ['entities-layer', 'claims-layer', 'heatmap-layer'].forEach(id => {
        if (map.getLayer(id)) map.removeLayer(id);
    });
    ['entities-source', 'claims-source', 'heatmap-source'].forEach(id => {
        if (map.getSource(id)) map.removeSource(id);
    });

    const entityType = document.getElementById('map-entity-type').value;

    if (layer === 'entities') {
        const url = entityType 
            ? `${API_BASE}/api/geospatial/entities?type=${entityType}`
            : `${API_BASE}/api/geospatial/entities`;
        const response = await fetch(url);
        const geoJson = await response.json();

        map.addSource('entities-source', { type: 'geojson', data: geoJson });
        map.addLayer({
            id: 'entities-layer',
            type: 'circle',
            source: 'entities-source',
            paint: {
                'circle-radius': ['interpolate', ['linear'], ['get', 'claimCount'], 0, 8, 5, 20],
                'circle-color': [
                    'match', ['get', 'entityType'],
                    'Provider', '#ff6b6b',
                    'Claimant', '#4ecdc4',
                    '#f39c12'
                ],
                'circle-stroke-width': 2,
                'circle-stroke-color': '#fff',
                'circle-opacity': 0.8
            }
        });

        map.on('click', 'entities-layer', (e) => {
            const props = e.features[0].properties;
            new mapboxgl.Popup()
                .setLngLat(e.lngLat)
                .setHTML(`
                    <div style="color:#333;padding:5px;">
                        <h4>${props.name}</h4>
                        <p>Type: ${props.entityType}</p>
                        <p>Address: ${props.address || 'N/A'}</p>
                        <p>Claims: ${props.claimCount} | Alerts: ${props.alertCount}</p>
                    </div>
                `)
                .addTo(map);
        });

    } else if (layer === 'claims') {
        const response = await fetch(`${API_BASE}/api/geospatial/claims`);
        const geoJson = await response.json();

        map.addSource('claims-source', { type: 'geojson', data: geoJson });
        map.addLayer({
            id: 'claims-layer',
            type: 'circle',
            source: 'claims-source',
            paint: {
                'circle-radius': ['interpolate', ['linear'], ['get', 'amount'], 0, 6, 50000, 25],
                'circle-color': [
                    'match', ['get', 'status'],
                    'Investigating', '#ff8800',
                    'Open', '#ffcc00',
                    'Closed', '#44ff44',
                    '#95a5a6'
                ],
                'circle-stroke-width': 2,
                'circle-stroke-color': '#fff',
                'circle-opacity': 0.8
            }
        });

        map.on('click', 'claims-layer', (e) => {
            const props = e.features[0].properties;
            new mapboxgl.Popup()
                .setLngLat(e.lngLat)
                .setHTML(`
                    <div style="color:#333;padding:5px;">
                        <h4>${props.claimNumber}</h4>
                        <p>Status: ${props.status}</p>
                        <p>Type: ${props.claimType}</p>
                        <p>Amount: $${Number(props.amount).toLocaleString()}</p>
                        <p>Date: ${new Date(props.incidentDate).toLocaleDateString()}</p>
                        <p>Location: ${props.location || 'N/A'}</p>
                    </div>
                `)
                .addTo(map);
        });

    } else if (layer === 'heatmap') {
        const response = await fetch(`${API_BASE}/api/geospatial/heatmap`);
        const heatData = await response.json();
        
        const geoJson = {
            type: 'FeatureCollection',
            features: heatData.map(d => ({
                type: 'Feature',
                geometry: { type: 'Point', coordinates: [d.incidentLongitude, d.incidentLatitude] },
                properties: { weight: d.weight }
            }))
        };

        map.addSource('heatmap-source', { type: 'geojson', data: geoJson });
        map.addLayer({
            id: 'heatmap-layer',
            type: 'heatmap',
            source: 'heatmap-source',
            paint: {
                'heatmap-weight': ['get', 'weight'],
                'heatmap-intensity': 1,
                'heatmap-radius': 30,
                'heatmap-opacity': 0.7,
                'heatmap-color': [
                    'interpolate', ['linear'], ['heatmap-density'],
                    0, 'rgba(0,0,255,0)',
                    0.2, 'royalblue',
                    0.4, 'cyan',
                    0.6, 'lime',
                    0.8, 'yellow',
                    1, 'red'
                ]
            }
        });
    }
}

// ===== Temporal Analysis =====
async function loadTimeline() {
    const startDate = document.getElementById('temporal-start').value;
    const endDate = document.getElementById('temporal-end').value;
    const period = document.getElementById('temporal-period').value;

    // Claims timeline
    try {
        const claimsResponse = await fetch(`${API_BASE}/api/temporal/claims?startDate=${startDate}&endDate=${endDate}`);
        const claims = await claimsResponse.json();
        renderClaimsTimeline(claims);
    } catch (e) { console.error('Error loading claims timeline:', e); }

    // Alerts timeline
    try {
        const alertsResponse = await fetch(`${API_BASE}/api/temporal/alerts?startDate=${startDate}&endDate=${endDate}`);
        const alerts = await alertsResponse.json();
        renderAlertsTimeline(alerts);
    } catch (e) { console.error('Error loading alerts timeline:', e); }

    // Activity summary
    try {
        const summaryResponse = await fetch(`${API_BASE}/api/temporal/summary?period=${period}`);
        const summary = await summaryResponse.json();
        renderActivitySummary(summary);
    } catch (e) { console.error('Error loading activity summary:', e); }
}

function renderClaimsTimeline(claims) {
    const container = document.getElementById('claims-timeline-content');
    if (!claims || claims.length === 0) {
        container.innerHTML = '<p style="color:#888;">No claims data available</p>';
        return;
    }

    container.innerHTML = claims.map(claim => `
        <div class="timeline-item">
            <div class="date">${new Date(claim.incidentDate).toLocaleDateString()}</div>
            <div class="content">
                <div class="title">${claim.claimNumber} - ${claim.type}</div>
                <div class="desc">${claim.description || ''} | Amount: $${claim.amount.toLocaleString()} | Status: ${claim.status}</div>
            </div>
        </div>
    `).join('');
}

function renderAlertsTimeline(alerts) {
    const container = document.getElementById('alerts-timeline-content');
    if (!alerts || alerts.length === 0) {
        container.innerHTML = '<p style="color:#888;">No alerts data available</p>';
        return;
    }

    container.innerHTML = alerts.map(alert => `
        <div class="timeline-item severity-${alert.severity.toLowerCase()}">
            <div class="date">${new Date(alert.createdAt).toLocaleDateString()}</div>
            <div class="content">
                <div class="title">[${alert.severity}] ${alert.alertType}</div>
                <div class="desc">${alert.description}${alert.entityName ? ' | Entity: ' + alert.entityName : ''}</div>
            </div>
        </div>
    `).join('');
}

function renderActivitySummary(summary) {
    const container = document.getElementById('activity-summary-content');
    if (!summary) {
        container.innerHTML = '<p style="color:#888;">No activity data available</p>';
        return;
    }

    let html = '<div style="display:grid;grid-template-columns:1fr 1fr;gap:1rem;">';
    
    if (summary.claims) {
        html += '<div><h4 style="color:#4ecdc4;margin-bottom:0.5rem;">Claims by Period</h4>';
        const claimsArr = Array.isArray(summary.claims) ? summary.claims : [];
        claimsArr.forEach(item => {
            const label = item.date ? new Date(item.date).toLocaleDateString() : `${item.year}/${item.month || item.week}`;
            html += `<div class="metric-item"><span class="label">${label}</span><span class="value">${item.count} ($${item.totalAmount?.toLocaleString() || 0})</span></div>`;
        });
        html += '</div>';
    }

    if (summary.alerts) {
        html += '<div><h4 style="color:#ff6b6b;margin-bottom:0.5rem;">Alerts by Period</h4>';
        const alertsArr = Array.isArray(summary.alerts) ? summary.alerts : [];
        alertsArr.forEach(item => {
            const label = item.date ? new Date(item.date).toLocaleDateString() : `${item.year}/${item.month || item.week}`;
            html += `<div class="metric-item"><span class="label">${label}</span><span class="value">${item.count}</span></div>`;
        });
        html += '</div>';
    }

    html += '</div>';
    container.innerHTML = html;
}

// ===== Power BI Integration =====
async function loadPowerBIReport(reportType) {
    try {
        const response = await fetch(`${API_BASE}/api/powerbi/embed/${reportType}`);
        const config = await response.json();
        
        if (config.message) {
            document.getElementById('powerbi-placeholder').innerHTML = `<p>${config.message}</p>`;
            return;
        }

        if (!config.embedToken || config.embedToken === 'demo-token-configure-azure-ad') {
            document.getElementById('powerbi-placeholder').style.display = 'flex';
            return;
        }

        document.getElementById('powerbi-placeholder').style.display = 'none';
        
        const embedConfig = {
            type: 'report',
            id: config.reportId,
            embedUrl: config.embedUrl,
            accessToken: config.embedToken,
            tokenType: window['powerbi-client'].models.TokenType.Embed,
            settings: {
                panes: { filters: { expanded: false, visible: true } },
                background: window['powerbi-client'].models.BackgroundType.Transparent
            }
        };

        const reportContainer = document.getElementById('powerbi-embed');
        const powerbi = new window['powerbi-client'].service.Service(
            window['powerbi-client'].factories.hpmFactory,
            window['powerbi-client'].factories.wpmpFactory,
            window['powerbi-client'].factories.routerFactory
        );
        powerbi.embed(reportContainer, embedConfig);
    } catch (error) {
        console.error('Error loading Power BI report:', error);
    }
}

// ===== Dashboard =====
async function loadDashboard() {
    try {
        const response = await fetch(`${API_BASE}/api/data/stats`);
        const stats = await response.json();
        
        const grid = document.getElementById('stats-grid');
        grid.innerHTML = `
            <div class="stat-card">
                <h3>Total Entities</h3>
                <div class="value">${stats.totalEntities}</div>
            </div>
            <div class="stat-card">
                <h3>Total Claims</h3>
                <div class="value">${stats.totalClaims}</div>
                <div class="sub-stats">Total Amount: $${stats.totalClaimAmount?.toLocaleString() || 0}</div>
            </div>
            <div class="stat-card">
                <h3>Total Alerts</h3>
                <div class="value">${stats.totalAlerts}</div>
                <div class="sub-stats">Unresolved: ${stats.unresolvedAlerts}</div>
            </div>
            <div class="stat-card">
                <h3>Claims by Status</h3>
                <div class="sub-stats">${(stats.claimsByStatus || []).map(s => `${s.status}: ${s.count}`).join('<br>')}</div>
            </div>
            <div class="stat-card">
                <h3>Alerts by Severity</h3>
                <div class="sub-stats">${(stats.alertsBySeverity || []).map(s => `${s.severity}: ${s.count}`).join('<br>')}</div>
            </div>
        `;
    } catch (error) {
        console.error('Error loading dashboard:', error);
        document.getElementById('stats-grid').innerHTML = '<div class="placeholder"><p>Start the API with MySQL to see dashboard data</p></div>';
    }
}

// ===== Event Listeners =====
document.getElementById('btn-refresh-network').addEventListener('click', () => loadNetwork());
document.getElementById('btn-metrics').addEventListener('click', loadMetrics);
document.getElementById('btn-find-path').addEventListener('click', findShortestPath);
document.getElementById('btn-time-filter').addEventListener('click', () => {
    const startDate = document.getElementById('network-start-date').value;
    const endDate = document.getElementById('network-end-date').value;
    loadNetwork({ startDate, endDate });
});
document.getElementById('btn-refresh-map').addEventListener('click', () => {
    const layer = document.getElementById('map-layer-select').value;
    loadMapData(layer);
});
document.getElementById('map-layer-select').addEventListener('change', (e) => {
    loadMapData(e.target.value);
});
document.getElementById('btn-refresh-temporal').addEventListener('click', loadTimeline);
document.getElementById('btn-pbi-geospatial').addEventListener('click', () => loadPowerBIReport('geospatial'));
document.getElementById('btn-pbi-temporal').addEventListener('click', () => loadPowerBIReport('temporal'));
document.getElementById('btn-seed-neo4j').addEventListener('click', async () => {
    await fetch(`${API_BASE}/api/network/seed`, { method: 'POST' });
    loadNetwork();
});
document.getElementById('btn-seed-mysql').addEventListener('click', async () => {
    await fetch(`${API_BASE}/api/data/seed`, { method: 'POST' });
    loadDashboard();
});

// ===== Initialize =====
loadNetwork();
