// Global state
let hubsData = [];
let currentHub = null;
let connections = new Map(); // hubName -> connection
let registeredHandlers = new Map(); // hubName -> Set of handler names
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
    const savedTheme = localStorage.getItem('theme') || config.defaultTheme;
    document.documentElement.setAttribute('data-theme', savedTheme);
    updateThemeIcons();
}

function toggleTheme() {
    const currentTheme = document.documentElement.getAttribute('data-theme');
    const newTheme = currentTheme === 'light' ? 'dark' : 'light';
    document.documentElement.setAttribute('data-theme', newTheme);
    localStorage.setItem('theme', newTheme);
    updateThemeIcons();
}

function updateThemeIcons() {
    const currentTheme = document.documentElement.getAttribute('data-theme');
    const sunIcon = document.querySelector('.sun-icon');
    const moonIcon = document.querySelector('.moon-icon');
    const themeText = document.querySelector('.theme-text');
    
    if (currentTheme === 'dark') {
        if (sunIcon) sunIcon.style.display = 'none';
        if (moonIcon) moonIcon.style.display = 'block';
        if (themeText) themeText.textContent = 'Light';
    } else {
        if (sunIcon) sunIcon.style.display = 'block';
        if (moonIcon) moonIcon.style.display = 'none';
        if (themeText) themeText.textContent = 'Dark';
    }
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

    // Authorization modal
    const authorizeBtn = document.getElementById('authorizeBtn');
    if (authorizeBtn) {
        authorizeBtn.addEventListener('click', () => showModal('authModal'));
    }

    const closeAuth = document.getElementById('closeAuth');
    if (closeAuth) {
        closeAuth.addEventListener('click', () => hideModal('authModal'));
    }

    const saveAuthBtn = document.getElementById('saveAuthBtn');
    if (saveAuthBtn) {
        saveAuthBtn.addEventListener('click', () => saveAuthConfiguration());
    }

    const clearAuthBtn = document.getElementById('clearAuthBtn');
    if (clearAuthBtn) {
        clearAuthBtn.addEventListener('click', () => clearAuthConfiguration());
    }

    // Modal backdrop clicks
    document.querySelectorAll('.modal').forEach(modal => {
        modal.addEventListener('click', (e) => {
            if (e.target === modal) {
                hideModal(modal.id);
            }
        });
    });

    // Load saved auth configuration
    loadAuthConfiguration();
}

// Modal management
function showModal(modalId) {
    document.getElementById(modalId).classList.add('show');
}

function hideModal(modalId) {
    document.getElementById(modalId).classList.remove('show');
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
                <span class="hub-name">
                    ${escapeHtml(hub.name)}
                    ${hub.requiresAuthorization ? '<span class="badge auth-badge" style="margin-left: 8px;"><svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="margin-right: 4px;"><rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect><path d="M7 11V7a5 5 0 0 1 10 0v4"></path></svg>Auth</span>' : ''}
                    ${hub.isObsolete ? '<span class="badge obsolete-badge" style="margin-left: 8px;"><svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="margin-right: 4px;"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="8" x2="12" y2="12"></line><line x1="12" y1="16" x2="12.01" y2="16"></line></svg>Obsolete</span>' : ''}
                </span>
                <span class="hub-status ${isHubConnected(hub.name) ? 'connected' : 'disconnected'}"></span>
            </div>
            <div class="hub-path">${escapeHtml(hub.path)}</div>
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
                        <button class="btn btn-purple btn-small" onclick="showEventHandlerModal()">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <circle cx="12" cy="12" r="10"></circle>
                                <line x1="12" y1="8" x2="12" y2="16"></line>
                                <line x1="8" y1="12" x2="16" y2="12"></line>
                            </svg>
                            Register Handler
                        </button>
                        <button class="btn btn-danger btn-small" onclick="disconnectHub()">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <line x1="18" y1="6" x2="6" y2="18"></line>
                                <line x1="6" y1="6" x2="18" y2="18"></line>
                            </svg>
                            Disconnect
                        </button>
                    ` : `
                        <button class="btn btn-connect btn-small" onclick="showConnectionModal()">
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
                <span class="method-name">
                    ${escapeHtml(method.name)}
                    ${method.requiresAuthorization ? '<span class="badge auth-badge" style="margin-left: 8px;"><svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="margin-right: 4px;"><rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect><path d="M7 11V7a5 5 0 0 1 10 0v4"></path></svg>Auth</span>' : ''}
                    ${method.isObsolete ? '<span class="badge obsolete-badge" style="margin-left: 8px;"><svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="margin-right: 4px;"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="8" x2="12" y2="12"></line><line x1="12" y1="16" x2="12.01" y2="16"></line></svg>Obsolete</span>' : ''}
                </span>
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
    return logs.map((log, index) => {
        const hasData = log.data !== null && log.data !== undefined;
        const dataStr = hasData ? JSON.stringify(log.data, null, log.expanded ? 2 : 0) : '';
        
        return `
            <div class="log-entry ${log.type}">
                <div class="log-header">
                    <span class="log-timestamp">[${log.timestamp}]</span>
                    <span class="log-message">${escapeHtml(log.message)}</span>
                    ${hasData ? `
                        <div class="log-actions">
                            <button class="log-action-btn" onclick="toggleLogFormat(${index})" title="${log.expanded ? 'Collapse' : 'Expand'} JSON">
                                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <polyline points="16 18 22 12 16 6"></polyline>
                                    <polyline points="8 6 2 12 8 18"></polyline>
                                </svg>
                            </button>
                            <button class="log-action-btn" onclick="copyLogData(${index}, event)" title="Copy JSON">
                                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
                                    <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
                                </svg>
                            </button>
                        </div>
                    ` : ''}
                </div>
                ${hasData ? `
                    <pre class="log-data">${escapeHtml(dataStr)}</pre>
                ` : ''}
            </div>
        `;
    }).join('');
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
}

// Connect to hub
async function connectToHub() {
    const hubUrl = document.getElementById('hubUrl').value;
    const accessToken = getAccessToken();
    
    console.log('Connecting to hub at:', hubUrl);
    try {
        addLog('info', `Connecting to ${currentHub.name}...`);
        
        const connectionOptions = {};
        
        // Add access token if available
        if (accessToken) {
            connectionOptions.accessTokenFactory = () => accessToken;
        }
        
        // Add custom headers (for API key auth)
        const customHeaders = {};
        if (authConfig.type === 'apikey' && authConfig.apikey.value) {
            customHeaders[authConfig.apikey.name] = authConfig.apikey.value;
        }
        
        if (Object.keys(customHeaders).length > 0) {
            connectionOptions.headers = customHeaders;
        }
        
        const connectionBuilder = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl, connectionOptions)
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information);
        
        const connection = connectionBuilder.build();
        
        // Setup event handlers
        connection.onreconnecting(_ => {
            addLog('warning', `Connection lost. Reconnecting...`);
        });
        
        connection.onreconnected(_ => {
            addLog('success', `Reconnected successfully`);
        });
        
        connection.onclose(error => {
            addLog('error', `Connection closed: ${error || 'Unknown reason'}`);
            connections.delete(currentHub.name);
            registeredHandlers.delete(currentHub.name);
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
        console.error('Connection failed:', error);
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
        registeredHandlers.delete(currentHub.name);
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
                    class="body-textarea" 
                    rows="3"
                    placeholder='${param.example ? escapeHtml(param.example) : 'Enter value as JSON'}'
                    data-param-name="${escapeHtml(param.name)}"
                    ${param.required ? 'required' : ''}
                >${param.example || ''}</textarea>
                <small class="help-text">Enter value as JSON</small>
            </div>
        `).join('');
    }
    
    modal.style.display = 'flex';
    modal.dataset.methodName = method.name;
}

function closeMethodModal() {
    document.getElementById('methodModal').style.display = 'none';
}

// Event Handler Modal
function showEventHandlerModal() {
    const modal = document.getElementById('eventHandlerModal');
    document.getElementById('eventHandlerName').value = '';
    modal.style.display = 'flex';
}

function closeEventHandlerModal() {
    document.getElementById('eventHandlerModal').style.display = 'none';
}

// Register event handler
function registerEventHandler() {
    const eventName = document.getElementById('eventHandlerName').value.trim();
    
    if (!eventName) {
        alert('Please enter an event name');
        return;
    }
    
    const connection = connections.get(currentHub.name);
    if (!connection) {
        alert('Not connected to hub');
        return;
    }
    
    // Check if handler is already registered
    const handlers = registeredHandlers.get(currentHub.name) || new Set();
    if (handlers.has(eventName)) {
        alert(`Handler for "${eventName}" is already registered`);
        return;
    }
    
    try {
        // Register the event handler
        connection.on(eventName, (...args) => {
            const data = args.length === 1 ? args[0] : args;
            addLog('incoming', `📨 ${eventName} received`, data);
        });
        
        // Track the registered handler
        handlers.add(eventName);
        registeredHandlers.set(currentHub.name, handlers);
        
        addLog('success', `✓ Registered event handler for "${eventName}"`);
        closeEventHandlerModal();
    } catch (error) {
        console.error('Failed to register handler:', error);
        addLog('error', `Failed to register handler: ${error.message}`);
        alert(`Failed to register handler: ${error.message}`);
    }
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
            if (value.length === 0) {
                // Skip optional parameters entirely
                if (!input.required) {
                    continue;
                }

                // Required parameter but empty → stop
                alert(`Parameter ${input.dataset.paramName} is required`);
                return;
            }

            // Parse JSON if possible
            try {
                args.push(JSON.parse(value));
            } catch {
                args.push(value);
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
        console.error('Method invocation failed:', error);
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
function addLog(type, message, data = null) {
    const timestamp = new Date().toLocaleTimeString();
    logs.push({ type, message, data, timestamp, expanded: false });
    
    // Keep only last 100 logs
    if (logs.length > 100) {
        logs.shift();
    }
    
    updateLogsDisplay();
}

function updateLogsDisplay() {
    const logsContainer = document.getElementById('logsContainer');
    if (logsContainer) {
        // Check if user was at the bottom before update
        const wasAtBottom = logsContainer.scrollHeight - logsContainer.scrollTop <= logsContainer.clientHeight + 50;
        
        logsContainer.innerHTML = logs.length === 0 
            ? '<div class="log-empty">No logs yet</div>' 
            : renderLogs();
        
        // Only auto-scroll if user was already at the bottom
        if (wasAtBottom) {
            logsContainer.scrollTop = logsContainer.scrollHeight;
        }
    }
}

function clearLogs() {
    logs = [];
    updateLogsDisplay();
}

// Log actions
function toggleLogFormat(index) {
    if (logs[index]) {
        logs[index].expanded = !logs[index].expanded;
        updateLogsDisplay();
    }
}

function copyLogData(index, evt) {
    if (logs[index] && logs[index].data) {
        const jsonStr = JSON.stringify(logs[index].data, null, 2);
        navigator.clipboard.writeText(jsonStr).then(() => {
            // Visual feedback
            const btn = evt.target.closest('.log-action-btn');
            if (btn) {
                const originalHTML = btn.innerHTML;
                btn.innerHTML = `
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <polyline points="20 6 9 17 4 12"></polyline>
                    </svg>
                `;
                setTimeout(() => {
                    btn.innerHTML = originalHTML;
                }, 1000);
            }
        }).catch(err => {
            console.error('Failed to copy:', err);
        });
    }
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
