// gRPC Explorer JavaScript
let services = []
let selectedService = ""
let expandedMethods = []
let currentServerAddress = ""

// Theme Management
function initializeTheme() {
    const config = window.KayaGrpcExplorerConfig || { defaultTheme: 'light' }
    const savedTheme = localStorage.getItem('kayaGrpcTheme') || config.defaultTheme
    document.documentElement.setAttribute('data-theme', savedTheme)
    updateThemeIcons()
}

function toggleTheme() {
    const currentTheme = document.documentElement.getAttribute('data-theme')
    const newTheme = currentTheme === 'light' ? 'dark' : 'light'
    document.documentElement.setAttribute('data-theme', newTheme)
    localStorage.setItem('kayaGrpcTheme', newTheme)
    updateThemeIcons()
}

function updateThemeIcons() {
    const currentTheme = document.documentElement.getAttribute('data-theme')
    const sunIcon = document.querySelector('.sun-icon')
    const moonIcon = document.querySelector('.moon-icon')
    const themeText = document.querySelector('.theme-text')
    
    if (currentTheme === 'dark') {
        if (sunIcon) sunIcon.style.display = 'block'
        if (moonIcon) moonIcon.style.display = 'none'
        if (themeText) themeText.textContent = 'Light'
    } else {
        if (sunIcon) sunIcon.style.display = 'none'
        if (moonIcon) moonIcon.style.display = 'block'
        if (themeText) themeText.textContent = 'Dark'
    }
}

// Modal Management
function showModal(modalId) {
    const modal = document.getElementById(modalId)
    if (modal) {
        modal.classList.add('show')
    }
}

function hideModal(modalId) {
    const modal = document.getElementById(modalId)
    if (modal) {
        modal.classList.remove('show')
    }
}

function initializeModals() {
    // Close modals when clicking outside
    document.querySelectorAll('.modal').forEach(modal => {
        modal.addEventListener('click', (e) => {
            if (e.target === modal) {
                hideModal(modal.id)
            }
        })
    })
}

// Initialize the application
document.addEventListener("DOMContentLoaded", async () => {
    initializeTheme()
    loadAuthConfiguration('kayaGrpcAuthConfig')
    
    // Get config
    const config = window.KayaGrpcExplorerConfig || {}
    currentServerAddress = sessionStorage.getItem('grpcServerAddress') || config.defaultServerAddress || 'localhost:5001'
    
    document.getElementById('serverAddress').value = currentServerAddress
    
    // Load services
    await loadServices()
    
    // Event listeners
    document.getElementById('searchInput').addEventListener('input', filterServices)
    document.getElementById('themeToggleBtn').addEventListener('click', toggleTheme)
    document.getElementById('serverConfigBtn').addEventListener('click', () => showModal('serverModal'))
    document.getElementById('authorizeBtn').addEventListener('click', () => showModal('authModal'))
    
    document.getElementById('closeServerModal').addEventListener('click', () => hideModal('serverModal'))
    document.getElementById('closeAuth').addEventListener('click', () => hideModal('authModal'))
    
    document.getElementById('saveServerBtn').addEventListener('click', saveServerConfig)
    document.getElementById('saveAuthBtn').addEventListener('click', () => saveAuthConfiguration('kayaGrpcAuthConfig', 'authModal'))
    document.getElementById('clearAuthBtn').addEventListener('click', () => clearAuthConfiguration('kayaGrpcAuthConfig', 'authModal'))
    
    initializeModals()
})

async function loadServices() {
    try {
        const config = window.KayaGrpcExplorerConfig || {}
        const routePrefix = config.routePrefix || '/grpc-explorer'
        
        const response = await fetch(`${routePrefix}/services?serverAddress=${encodeURIComponent(currentServerAddress)}`)
        
        if (!response.ok) {
            const error = await response.json()
            showError(`Failed to load services: ${error.error || response.statusText}`)
            services = []
            renderServices()
            return
        }
        
        services = await response.json()
        
        if (services.length > 0) {
            selectedService = services[0].serviceName
        }
        
        renderServices()
        renderMethods()
    } catch (error) {
        console.error('Failed to load services:', error)
        showError(`Failed to connect to ${currentServerAddress}. Ensure gRPC reflection is enabled.`)
        services = []
        renderServices()
    }
}

function saveServerConfig() {
    currentServerAddress = document.getElementById('serverAddress').value.trim()
    sessionStorage.setItem('grpcServerAddress', currentServerAddress)
    hideModal('serverModal')
    
    // Reload services with new address
    services = []
    selectedService = ""
    expandedMethods = []
    loadServices()
}

function showError(message) {
    const methodsList = document.getElementById('methodsList')
    methodsList.innerHTML = `
        <div class="server-status disconnected">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="12" cy="12" r="10"></circle>
                <line x1="15" y1="9" x2="9" y2="15"></line>
                <line x1="9" y1="9" x2="15" y2="15"></line>
            </svg>
            ${message}
        </div>
    `
}

function renderServices() {
    const container = document.getElementById('servicesList')
    container.innerHTML = ''
    
    if (services.length === 0) {
        container.innerHTML = '<p class="text-muted" style="padding: 16px; text-align: center;">No services found</p>'
        return
    }
    
    services.forEach(service => {
        const card = document.createElement('div')
        card.class = `service-card ${selectedService === service.serviceName ? 'active' : ''}`
        card.onclick = () => selectService(service.serviceName)
        
        const methodTypeCounts = {}
        service.methods.forEach(method => {
            const typeName = getMethodTypeBadgeClass(method.methodType)
            methodTypeCounts[typeName] = (methodTypeCounts[typeName] || 0) + 1
        })
        
        const badges = Object.entries(methodTypeCounts)
            .map(([type, count]) => `<span class="badge ${type}">${getMethodTypeDisplay(type)} (${count})</span>`)
            .join('')
        
        card.innerHTML = `
            <h3>${service.simpleName}</h3>
            <p class="service-package">${service.package || 'default'}</p>
            <div class="method-type-badges">${badges}</div>
        `
        
        container.appendChild(card)
    })
}

function renderMethods() {
    const service = services.find(s => s.serviceName === selectedService)
    if (!service) {
        document.getElementById('methodsList').innerHTML = '<p class="text-muted" style="text-align: center;">Select a service</p>'
        return
    }
    
    document.getElementById('serviceTitle').textContent = service.simpleName
    document.getElementById('serviceDescription').textContent = service.description || `Package: ${service.package || 'default'}`
    
    const container = document.getElementById('methodsList')
    container.innerHTML = ''
    
    service.methods.forEach((method, index) => {
        const methodId = `${selectedService}-${index}`
        const isExpanded = expandedMethods.includes(methodId)
        
        const card = document.createElement('div')
        card.className = 'method-card'
        
        const typeBadge = getMethodTypeBadgeClass(method.methodType)
        
        card.innerHTML = `
            <div class="method-header" onclick="toggleMethod('${methodId}')">
                <div class="method-title">
                    <div class="method-name-type">
                        <span class="badge ${typeBadge}">${getMethodTypeDisplay(typeBadge)}</span>
                        <code class="method-name">${method.methodName}</code>
                    </div>
                    <svg class="chevron ${isExpanded ? 'expanded' : ''}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <polyline points="9,18 15,12 9,6"></polyline>
                    </svg>
                </div>
                <div class="method-info">
                    <h4>${method.methodName}</h4>
                    <p>${method.description || 'No description available'}</p>
                </div>
            </div>
            <div class="method-content ${isExpanded ? 'expanded' : ''}" id="content-${methodId}">
                ${renderMethodTabs(method, methodId, index)}
            </div>
        `
        
        container.appendChild(card)
    })
}

function renderMethodTabs(method, methodId, index) {
    return `
        <div class="tabs">
            <div class="tab-list">
                <button class="tab-trigger active" onclick="switchTab(event, '${methodId}', 'request')">Request</button>
                <button class="tab-trigger" onclick="switchTab(event, '${methodId}', 'response')">Response</button>
                <button class="tab-trigger" onclick="switchTab(event, '${methodId}', 'try')">Try it out</button>
            </div>
            
            <div class="tab-content active" id="${methodId}-request">
                ${renderMessageSchema(method.requestType, 'Request')}
            </div>
            
            <div class="tab-content" id="${methodId}-response">
                ${renderMessageSchema(method.responseType, 'Response')}
            </div>
            
            <div class="tab-content" id="${methodId}-try">
                ${renderTryItOut(method, index)}
            </div>
        </div>
    `
}

function renderMessageSchema(schema, label) {
    return `
        <div class="message-schema">
            <h5>${schema.typeName}</h5>
            ${schema.description ? `<p style="font-size: 13px; color: var(--text-secondary); margin-bottom: 8px;">${schema.description}</p>` : ''}
            <div class="fields-list">
                ${schema.fields.map(field => `
                    <div class="field-item">
                        <div class="field-header">
                            <span class="field-name">${field.name}</span>
                            <span class="field-type">${field.type}${field.isRepeated ? '[]' : ''}</span>
                            ${field.isOptional ? '<span class="badge" style="font-size: 10px;">optional</span>' : ''}
                        </div>
                        ${field.description ? `<div class="field-description">${field.description}</div>` : ''}
                    </div>
                `).join('')}
            </div>
            <div style="margin-top: 12px;">
                <h6 style="font-size: 12px; font-weight: 600; margin-bottom: 4px;">Example JSON:</h6>
                <div class="code-block">
                    <pre><code>${schema.exampleJson}</code></pre>
                </div>
            </div>
        </div>
    `
}

function renderTryItOut(method, index) {
    const methodIdentifier = `${selectedService}-${index}`
    
    return `
        <div class="request-builder">
            <h4 style="margin-bottom: 12px;">Request Body</h4>
            <textarea id="request-${methodIdentifier}" 
                      class="body-textarea" 
                      style="width: 100%; height: 200px; font-family: monospace;"
                      placeholder="Enter request JSON">${method.requestType.exampleJson}</textarea>
            
            <div class="metadata-editor">
                <h4 style="margin-bottom: 8px;">Metadata (optional)</h4>
                <div id="metadata-${methodIdentifier}"></div>
                <button class="btn btn-outline btn-sm" onclick="addMetadata('${methodIdentifier}')">Add Metadata</button>
            </div>
            
            <button class="btn btn-primary" style="margin-top: 16px; width: 100%;" onclick="invokeMethod('${selectedService}', ${index})">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <polygon points="5,3 19,12 5,21 5,3"></polygon>
                </svg>
                Invoke Method
            </button>
            
            <div id="response-${methodIdentifier}" style="margin-top: 16px; display: none;"></div>
        </div>
    `
}

function selectService(serviceName) {
    selectedService = serviceName
    expandedMethods = []
    renderServices()
    renderMethods()
}

function toggleMethod(methodId) {
    const index = expandedMethods.indexOf(methodId)
    if (index > -1) {
        expandedMethods.splice(index, 1)
    } else {
        expandedMethods.push(methodId)
    }
    renderMethods()
}

function switchTab(event, methodId, tabName) {
    const tabList = event.target.parentElement
    tabList.querySelectorAll('.tab-trigger').forEach(trigger => {
        trigger.classList.remove('active')
    })
    event.target.classList.add('active')
    
    const tabsContainer = tabList.parentElement
    tabsContainer.querySelectorAll('.tab-content').forEach(content => {
        content.classList.remove('active')
    })
    
    document.getElementById(`${methodId}-${tabName}`).classList.add('active')
}

async function invokeMethod(serviceName, methodIndex) {
    const service = services.find(s => s.serviceName === serviceName)
    const method = service.methods[methodIndex]
    const methodIdentifier = `${serviceName}-${methodIndex}`
    
    const requestJson = document.getElementById(`request-${methodIdentifier}`).value
    const responseContainer = document.getElementById(`response-${methodIdentifier}`)
    
    responseContainer.style.display = 'block'
    responseContainer.innerHTML = '<p>Invoking method...</p>'
    
    try {
        const config = window.KayaGrpcExplorerConfig || {}
        const routePrefix = config.routePrefix || '/grpc-explorer'
        
        const authHeaders = getAuthHeaders()
        const metadata = {}
        
        // Convert auth headers to metadata
        Object.entries(authHeaders).forEach(([key, value]) => {
            metadata[key.toLowerCase()] = value
        })
        
        const requestBody = {
            serverAddress: currentServerAddress,
            serviceName: serviceName,
            methodName: method.methodName,
            requestJson: requestJson,
            metadata: metadata
        }
        
        const response = await fetch(`${routePrefix}/invoke`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestBody)
        })
        
        const result = await response.json()
        
        if (result.success) {
            responseContainer.innerHTML = `
                <div class="server-status connected">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <polyline points="20 6 9 17 4 12"></polyline>
                    </svg>
                    Success (${result.durationMs}ms)
                </div>
                <div class="code-block">
                    <pre>${result.responseJson || JSON.stringify(result.streamResponses, null, 2)}</pre>
                </div>
            `
        } else {
            responseContainer.innerHTML = `
                <div class="server-status disconnected">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <circle cx="12" cy="12" r="10"></circle>
                        <line x1="15" y1="9" x2="9" y2="15"></line>
                        <line x1="9" y1="9" x2="15" y2="15"></line>
                    </svg>
                    Error: ${result.errorMessage}
                </div>
            `
        }
    } catch (error) {
        responseContainer.innerHTML = `
            <div class="server-status disconnected">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <circle cx="12" cy="12" r="10"></circle>
                    <line x1="15" y1="9" x2="9" y2="15"></line>
                    <line x1="9" y1="9" x2="15" y2="15"></line>
                </svg>
                Request failed: ${error.message}
            </div>
        `
    }
}

function addMetadata(methodIdentifier) {
    const container = document.getElementById(`metadata-${methodIdentifier}`)
    const row = document.createElement('div')
    row.className = 'metadata-row'
    row.innerHTML = `
        <input type="text" placeholder="Key" class="metadata-input" style="flex: 1;">
        <input type="text" placeholder="Value" class="metadata-input" style="flex: 2;">
        <button class="btn btn-outline btn-sm" onclick="this.parentElement.remove()">&times;</button>
    `
    container.appendChild(row)
}

function getMethodTypeBadgeClass(methodType) {
    switch (methodType) {
        case 0: return 'unary'
        case 1: return 'server-stream'
        case 2: return 'client-stream'
        case 3: return 'bidi-stream'
        default: return 'unary'
    }
}

function getMethodTypeDisplay(badgeClass) {
    switch (badgeClass) {
        case 'unary': return 'Unary'
        case 'server-stream': return 'Server Stream'
        case 'client-stream': return 'Client Stream'
        case 'bidi-stream': return 'Bidi Stream'
        default: return 'Unary'
    }
}

function filterServices() {
    const query = document.getElementById('searchInput').value.toLowerCase()
    const clearBtn = document.getElementById('clearSearchBtn')
    
    if (query.trim()) {
        clearBtn.style.display = 'flex'
    } else {
        clearBtn.style.display = 'none'
    }
    
    const cards = document.querySelectorAll('.service-card')
    cards.forEach(card => {
        const text = card.textContent.toLowerCase()
        card.style.display = text.includes(query) ? 'block' : 'none'
    })
}

function clearSearch() {
    document.getElementById('searchInput').value = ''
    document.getElementById('clearSearchBtn').style.display = 'none'
    filterServices()
}
