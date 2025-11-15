// Global state
let hubsData = [];
let currentHub = null;
let connections = new Map(); // hubName -> connection
let logs = [];

// Initialize the application
document.addEventListener('DOMContentLoaded', () => {
    initializeTheme();
    setupEventListeners();
    loadHubs();
});

// Theme Management
function initializeTheme() {
    const config = window.KayaSignalRDebugConfig || { defaultTheme: 'light' };
    const savedTheme = localStorage.getItem('kayaSignalRTheme') || config.defaultTheme;
    document.documentElement.setAttribute('data-theme', savedTheme);
}

function toggleTheme() {
    const currentTheme = document.documentElement.getAttribute('data-theme');
    const newTheme = currentTheme === 'light' ? 'dark' : 'light';
    document.documentElement.setAttribute('data-theme', newTheme);
    localStorage.setItem('kayaSignalRTheme', newTheme);
}

// Event Listeners Setup
function setupEventListeners() {
    const themeToggle = document.getElementById('themeToggle');
    if (themeToggle) {
        themeToggle.addEventListener('click', toggleTheme);
    }

    const searchInput = document.getElementById('searchInput');
    const clearSearchBtn = document.getElementById('clearSearchBtn');
    
    if (searchInput) {
        searchInput.addEventListener('input', handleSearch);
    }
    
    if (clearSearchBtn) {
        clearSearchBtn.addEventListener('click', clearSearch);
    }
}

// Load hubs from API
async function loadHubs() {
    try {
        // Use relative path from current location
        const currentPath = window.location.pathname.replace(/\/$/, ''); // Remove trailing slash
        const hubsUrl = `${currentPath}/hubs`;
        
        console.log('Fetching hubs from:', hubsUrl);
        
        const response = await fetch(hubsUrl);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        const data = await response.json();
        
        // Validate the response structure
        if (!data || typeof data !== 'object') {
            throw new Error('Invalid response format');
        }
        
        hubsData = data.hubs || [];
        
        console.log('Loaded hubs:', hubsData.length);
        
        renderHubsList(hubsData);
    } catch (error) {
        console.error('Failed to load hubs:', error);
        showError(`Failed to load SignalR hubs: ${error.message}`);
    }
}

// Render hubs in sidebar
function renderHubsList(hubs) {
    const hubsList = document.getElementById('hubsList');
    if (!hubsList) return;

    if (hubs.length === 0) {
        hubsList.innerHTML = `
            <div style="text-align: center; padding: 24px; color: var(--text-secondary);">
                <p>No SignalR hubs found</p>
                <small>Make sure your application has SignalR hubs configured</small>
            </div>
        `;
        return;
    }

    hubsList.innerHTML = hubs.map(hub => `
        <div class="hub-item" onclick="selectHub('${escapeHtml(hub.name)}')">
            <div class="hub-item-header">
                <span class="hub-name">${escapeHtml(hub.name)}</span>
                <span class="hub-status ${isHubConnected(hub.name) ? 'connected' : 'disconnected'}"></span>
            </div>
            <div class="hub-path">${escapeHtml(hub.path)}</div>
            ${hub.requiresAuthorization || hub.isObsolete ? `
                <div class="hub-badges">
                    ${hub.requiresAuthorization ? '<span class="badge badge-auth">🔒 Auth</span>' : ''}
                    ${hub.isObsolete ? '<span class="badge badge-obsolete">⚠️ Obsolete</span>' : ''}
                </div>
            ` : ''}
        </div>
    `).join('');
}

// Select a hub
function selectHub(hubName) {
    currentHub = hubsData.find(h => h.name === hubName);
    if (!currentHub) return;

    // Update active state
    document.querySelectorAll('.hub-item').forEach(item => {
        item.classList.remove('active');
    });
    event.target.closest('.hub-item')?.classList.add('active');

    // Show hub details
    document.getElementById('welcomeScreen').style.display = 'none';
    document.getElementById('hubDetailView').style.display = 'block';
    
    renderHubDetails();
}

// Render hub details
function renderHubDetails() {
    if (!currentHub) return;

    const hubDetailView = document.getElementById('hubDetailView');
    const isConnected = isHubConnected(currentHub.name);

    hubDetailView.innerHTML = `
        <div class="hub-header">
            <div class="hub-title-row">
                <h3 class="hub-title">${escapeHtml(currentHub.name)}</h3>
                <div class="connection-controls">
                    <div class="connection-status ${isConnected ? 'connected' : 'disconnected'}">
                        <span class="status-dot"></span>
                        ${isConnected ? 'Connected' : 'Disconnected'}
                    </div>
                    ${isConnected ? `
                        <button class="btn btn-danger btn-small" onclick="disconnectHub()">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <line x1="18" y1="6" x2="6" y2="18"></line>
                                <line x1="6" y1="6" x2="18" y2="18"></line>
                            </svg>
                            Disconnect
                        </button>
                    ` : `
                        <button class="btn btn-primary btn-small" onclick="showConnectionModal()">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"></path>
                                <polyline points="15 3 21 3 21 9"></polyline>
                                <line x1="10" y1="14" x2="21" y2="3"></line>
                            </svg>
                            Connect
                        </button>
                    `}
                </div>
            </div>
            
            <div class="hub-info">
                <div class="info-item">
                    <span class="info-label">Path</span>
                    <span class="info-value">${escapeHtml(currentHub.path)}</span>
                </div>
                <div class="info-item">
                    <span class="info-label">Namespace</span>
                    <span class="info-value">${escapeHtml(currentHub.namespace)}</span>
                </div>
                ${currentHub.requiresAuthorization ? `
                    <div class="info-item">
                        <span class="info-label">Authorization</span>
                        <span class="info-value">
                            Required ${currentHub.roles.length > 0 ? `(${currentHub.roles.join(', ')})` : ''}
                        </span>
                    </div>
                ` : ''}
            </div>
        </div>

        <div class="methods-section">
            <div class="section-title">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <polyline points="16 18 22 12 16 6"></polyline>
                    <polyline points="8 6 2 12 8 18"></polyline>
                </svg>
                Hub Methods (${currentHub.methods.length})
            </div>
            <div class="methods-grid">
                ${currentHub.methods.map(method => renderMethodCard(method, isConnected)).join('')}
            </div>
        </div>

        <div class="logs-section">
            <div class="logs-header">
                <div class="section-title">
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
                        <polyline points="14 2 14 8 20 8"></polyline>
                        <line x1="16" y1="13" x2="8" y2="13"></line>
                        <line x1="16" y1="17" x2="8" y2="17"></line>
                        <polyline points="10 9 9 9 8 9"></polyline>
                    </svg>
                    Connection Logs
                </div>
                <button class="btn btn-secondary btn-small" onclick="clearLogs()">Clear Logs</button>
            </div>
            <div class="logs-container" id="logsContainer">
                ${logs.length === 0 ? '<div class="log-empty">No logs yet</div>' : renderLogs()}
            </div>
        </div>
    `;
}

// Render method card
function renderMethodCard(method, isConnected) {
    return `
        <div class="method-card">
            <div class="method-header">
                <span class="method-name">${escapeHtml(method.name)}</span>
                <div class="method-badges">
                    ${method.requiresAuthorization ? '<span class="badge badge-auth">🔒</span>' : ''}
                    ${method.isObsolete ? '<span class="badge badge-obsolete">⚠️</span>' : ''}
                </div>
            </div>
            <div class="method-description">${escapeHtml(method.description)}</div>
            
            ${method.parameters.length > 0 ? `
                <div class="method-params">
                    <div class="params-label">Parameters:</div>
                    ${method.parameters.map(param => `
                        <div class="param-item">
                            <span class="param-name">${escapeHtml(param.name)}</span>
                            <span class="param-type">${escapeHtml(param.type)}</span>
                            ${param.required ? '<span class="param-required">required</span>' : ''}
                        </div>
                    `).join('')}
                </div>
            ` : '<div class="method-params"><div class="params-label">No parameters</div></div>'}
            
            ${method.returnType !== 'void' ? `
                <div class="method-return">
                    <div class="return-label">Returns:</div>
                    <div class="return-type">${escapeHtml(method.returnType)}</div>
                </div>
            ` : ''}
            
            <button 
                class="btn btn-primary btn-small" 
                onclick='openMethodModal(${JSON.stringify(method).replace(/'/g, "&apos;")})'
                ${!isConnected ? 'disabled' : ''}
            >
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <polygon points="5 3 19 12 5 21 5 3"></polygon>
                </svg>
                Invoke
            </button>
        </div>
    `;
}

// Render logs
function renderLogs() {
    return logs.map(log => `
        <div class="log-entry ${log.type}">
            <span class="log-timestamp">[${log.timestamp}]</span>
            <span>${escapeHtml(log.message)}</span>
        </div>
    `).join('');
}

// Connection Modal
function showConnectionModal() {
    const modal = document.getElementById('connectionModal');
    const hubUrlInput = document.getElementById('hubUrl');
    const authWarning = document.getElementById('authWarning');
    
    const baseUrl = window.location.origin;
    hubUrlInput.value = `${baseUrl}${currentHub.path}`;
    
    if (currentHub.requiresAuthorization) {
        authWarning.style.display = 'flex';
    } else {
        authWarning.style.display = 'none';
    }
    
    modal.style.display = 'flex';
}

function closeConnectionModal() {
    document.getElementById('connectionModal').style.display = 'none';
    document.getElementById('accessToken').value = '';
}

// Connect to hub
async function connectToHub() {
    const hubUrl = document.getElementById('hubUrl').value;
    const accessToken = document.getElementById('accessToken').value.trim();
    
    try {
        addLog('info', `Connecting to ${currentHub.name}...`);
        
        const connectionBuilder = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl, accessToken ? {
                accessTokenFactory: () => accessToken
            } : {})
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information);
        
        const connection = connectionBuilder.build();
        
        // Setup event handlers
        connection.onreconnecting(error => {
            addLog('warning', `Connection lost. Reconnecting...`);
        });
        
        connection.onreconnected(connectionId => {
            addLog('success', `Reconnected successfully`);
        });
        
        connection.onclose(error => {
            addLog('error', `Connection closed: ${error || 'Unknown reason'}`);
            connections.delete(currentHub.name);
            updateHubsList();
            renderHubDetails();
        });
        
        await connection.start();
        connections.set(currentHub.name, connection);
        
        addLog('success', `Connected to ${currentHub.name} successfully`);
        closeConnectionModal();
        updateHubsList();
        renderHubDetails();
    } catch (error) {
        addLog('error', `Connection failed: ${error.message}`);
        alert(`Failed to connect: ${error.message}`);
    }
}

// Disconnect from hub
async function disconnectHub() {
    const connection = connections.get(currentHub.name);
    if (!connection) return;
    
    try {
        await connection.stop();
        connections.delete(currentHub.name);
        addLog('info', `Disconnected from ${currentHub.name}`);
        updateHubsList();
        renderHubDetails();
    } catch (error) {
        addLog('error', `Disconnect failed: ${error.message}`);
    }
}

// Method Invocation Modal
function openMethodModal(method) {
    const modal = document.getElementById('methodModal');
    const title = document.getElementById('methodModalTitle');
    const paramsContainer = document.getElementById('methodParameters');
    
    title.textContent = `Invoke ${method.name}`;
    
    if (method.parameters.length === 0) {
        paramsContainer.innerHTML = '<p style="color: var(--text-secondary);">This method has no parameters</p>';
    } else {
        paramsContainer.innerHTML = method.parameters.map((param, index) => `
            <div class="form-group">
                <label for="param_${index}">
                    ${escapeHtml(param.name)} 
                    <span style="color: var(--text-secondary); font-weight: normal;">(${escapeHtml(param.type)})</span>
                    ${param.required ? '<span style="color: var(--error-text);">*</span>' : ''}
                </label>
                <textarea 
                    id="param_${index}" 
                    class="form-control" 
                    rows="3"
                    placeholder='${param.example ? escapeHtml(param.example) : 'Enter value as JSON'}'
                    data-param-name="${escapeHtml(param.name)}"
                    ${param.required ? 'required' : ''}
                >${param.example || ''}</textarea>
                <small class="form-text">Enter value as JSON</small>
            </div>
        `).join('');
    }
    
    modal.style.display = 'flex';
    modal.dataset.methodName = method.name;
}

function closeMethodModal() {
    document.getElementById('methodModal').style.display = 'none';
}

// Invoke hub method
async function invokeMethod() {
    const modal = document.getElementById('methodModal');
    const methodName = modal.dataset.methodName;
    const connection = connections.get(currentHub.name);
    
    if (!connection) {
        alert('Not connected to hub');
        return;
    }
    
    try {
        // Collect parameters
        const paramInputs = document.querySelectorAll('#methodParameters textarea');
        const args = [];
        
        for (const input of paramInputs) {
            const value = input.value.trim();
            if (value) {
                try {
                    args.push(JSON.parse(value));
                } catch (e) {
                    // If not valid JSON, treat as string
                    args.push(value);
                }
            } else if (input.required) {
                alert(`Parameter ${input.dataset.paramName} is required`);
                return;
            } else {
                args.push(null);
            }
        }
        
        const argsDisplay = args.length > 0 ? JSON.stringify(args) : 'no arguments';
        addLog('info', `Invoking ${methodName}(${argsDisplay})`);
        
        const result = await connection.invoke(methodName, ...args);

        if (result !== undefined && result !== null) {
            addLog('success', `${methodName} returned: ${JSON.stringify(result, null, 2)}`);
        } else {
            addLog('success', `${methodName} completed successfully`);
        }
        
        closeMethodModal();
    } catch (error) {
        addLog('error', `${methodName} failed: ${error.message}`);
        alert(`Failed to invoke method: ${error.message}`);
    }
}

// Search functionality
function handleSearch(event) {
    const searchTerm = event.target.value.toLowerCase();
    const clearBtn = document.getElementById('clearSearchBtn');
    
    clearBtn.style.display = searchTerm ? 'block' : 'none';
    
    const filtered = hubsData.filter(hub => 
        hub.name.toLowerCase().includes(searchTerm) ||
        hub.path.toLowerCase().includes(searchTerm) ||
        hub.namespace.toLowerCase().includes(searchTerm)
    );
    
    renderHubsList(filtered);
}

function clearSearch() {
    const searchInput = document.getElementById('searchInput');
    searchInput.value = '';
    document.getElementById('clearSearchBtn').style.display = 'none';
    renderHubsList(hubsData);
}

// Logging
function addLog(type, message) {
    const timestamp = new Date().toLocaleTimeString();
    logs.push({ type, message, timestamp });
    
    // Keep only last 100 logs
    if (logs.length > 100) {
        logs.shift();
    }
    
    updateLogsDisplay();
}

function updateLogsDisplay() {
    const logsContainer = document.getElementById('logsContainer');
    if (logsContainer) {
        logsContainer.innerHTML = logs.length === 0 
            ? '<div class="log-empty">No logs yet</div>' 
            : renderLogs();
        logsContainer.scrollTop = logsContainer.scrollHeight;
    }
}

function clearLogs() {
    logs = [];
    updateLogsDisplay();
}

// Helper functions
function isHubConnected(hubName) {
    const connection = connections.get(hubName);
    return connection && connection.state === signalR.HubConnectionState.Connected;
}

function updateHubsList() {
    renderHubsList(hubsData);
}

function escapeHtml(text) {
    const map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return String(text).replace(/[&<>"']/g, m => map[m]);
}

function showError(message) {
    const hubsList = document.getElementById('hubsList');
    if (hubsList) {
        hubsList.innerHTML = `
            <div style="padding: 24px; text-align: center;">
                <div style="color: var(--error-text); background: var(--error-bg); border: 1px solid var(--error-border); border-radius: 8px; padding: 16px;">
                    <strong>Error</strong><br>
                    ${escapeHtml(message)}
                </div>
            </div>
        `;
    }
}
