let controllers = []
let selectedController = ""
let expandedEndpoints = []
let authToken = ""
const requestHeaders = [{ key: "Content-Type", value: "application/json" }]

async function loadApiData() {
  try {
    const response = await fetch('api-explorer/api-docs');
    const data = await response.json();
    console.log('API data loaded:', data);
    controllers = data.controllers || [];
    if (controllers.length > 0) {
      selectedController = controllers[0].name;
    }
    renderControllers();
    renderEndpoints();
  } catch (error) {
    console.error('Failed to load API data:', error);
    controllers = [];
    selectedController = controllers[0]?.name || "";
    renderControllers();
    renderEndpoints();
  }
}

function getMethodColor(method) {
  switch (method) {
    case "GET":
      return "get"
    case "POST":
      return "post"
    case "PUT":
      return "put"
    case "DELETE":
      return "delete"
    case "PATCH":
      return "patch"
    default:
      return "default"
  }
}

function getStatusClass(code) {
  if (code.startsWith("2")) return "status-2xx"
  if (code.startsWith("4")) return "status-4xx"
  return "status-default"
}

function renderControllers() {
  const container = document.getElementById("controllersList")
  container.innerHTML = ""

  controllers.forEach((controller) => {
    const card = document.createElement("div")
    card.className = `controller-card ${selectedController === controller.name ? "active" : ""}`
    card.onclick = () => selectController(controller.name)

    const badges = controller.endpoints
      .map((endpoint) => `<span class="badge ${getMethodColor(endpoint.httpMethodType)}">${endpoint.httpMethodType}</span>`)
      .join("")

    card.innerHTML = `
            <h3>${controller.name}</h3>
            <p>${controller.description}</p>
            <div class="method-badges">${badges}</div>
        `

    container.appendChild(card)
  })
}

function renderEndpoints() {
  const controller = controllers.find((c) => c.name === selectedController)
  if (!controller) return

  document.getElementById("controllerTitle").textContent = controller.name
  document.getElementById("controllerDescription").textContent = controller.description

  const container = document.getElementById("endpointsList")
  container.innerHTML = ""

  controller.endpoints.forEach((endpoint, index) => {
    const endpointId = `${selectedController}-${index}`
    const isExpanded = expandedEndpoints.includes(endpointId)

    const card = document.createElement("div")
    card.className = "endpoint-card"

    card.innerHTML = `
            <div class="endpoint-header" onclick="toggleEndpoint('${endpointId}')">
                <div class="endpoint-title">
                    <div class="endpoint-method-path">
                        <span class="badge ${getMethodColor(endpoint.httpMethodType)}">${endpoint.httpMethodType}</span>
                        <code class="endpoint-path">${endpoint.path}</code>
                    </div>
                    <svg class="chevron ${isExpanded ? "expanded" : ""}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <polyline points="9,18 15,12 9,6"></polyline>
                    </svg>
                </div>
                <div class="endpoint-info">
                    <h4>${endpoint.methodName}</h4>
                    <p>${endpoint.description}</p>
                </div>
            </div>
            <div class="endpoint-content ${isExpanded ? "expanded" : ""}" id="content-${endpointId}">
                ${renderEndpointTabs(endpoint, endpointId)}
            </div>
        `

    container.appendChild(card)
  })
}

function renderEndpointTabs(endpoint, endpointId) {
  return `
        <div class="tabs">
            <div class="tab-list">
                <button class="tab-trigger active" onclick="switchTab(event, '${endpointId}', 'parameters')">Parameters</button>
                <button class="tab-trigger" onclick="switchTab(event, '${endpointId}', 'request')">Request</button>
                <button class="tab-trigger" onclick="switchTab(event, '${endpointId}', 'responses')">Responses</button>
                <button class="tab-trigger" onclick="switchTab(event, '${endpointId}', 'try')">Try it out</button>
            </div>
            
            <div class="tab-content active" id="${endpointId}-parameters">
                ${renderParameters(endpoint)}
            </div>
            
            <div class="tab-content" id="${endpointId}-request">
                ${renderRequest(endpoint)}
            </div>
            
            <div class="tab-content" id="${endpointId}-responses">
                ${renderResponses(endpoint)}
            </div>
            
            <div class="tab-content" id="${endpointId}-try">
                ${renderTryItOut(endpoint)}
            </div>
        </div>
    `
}

function renderParameters(endpoint) {
  if (!endpoint.parameters || endpoint.parameters.length === 0) {
    return '<p class="text-muted">No parameters required</p>'
  }

  return endpoint.parameters
    .map(
      (param) => `
        <div class="parameter-item">
            <div class="parameter-header">
                <code class="parameter-name">${param.name}</code>
                <span class="badge">${param.type}</span>
                ${param.required ? '<span class="badge delete">Required</span>' : ""}
            </div>
            <p class="parameter-description">${param.description}</p>
        </div>
    `,
    )
    .join("")
}

function renderRequest(endpoint) {
  if (!endpoint.requestBody) {
    return '<p class="text-muted">No request body required</p>'
  }

  return `
        <div>
            <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 12px;">
                <h4>Request Body</h4>
                <span class="badge">${endpoint.requestBody.type}</span>
            </div>
            <div class="code-block">
                <button class="copy-btn" onclick="copyToClipboard(this)">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
                        <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
                    </svg>
                </button>
                <pre><code>${endpoint.requestBody.example}</code></pre>
            </div>
        </div>
    `
}

function renderResponses(endpoint) {
  return Object.entries(endpoint.responses)
    .map(
      ([code, description]) => `
        <div class="response-item">
            <div class="response-header">
                <span class="status-badge ${getStatusClass(code)}">${code}</span>
            </div>
            <p class="parameter-description">${description}</p>
        </div>
    `,
    )
    .join("")
}

function renderTryItOut(endpoint) {
  const parametersSection =
    endpoint.parameters && endpoint.parameters.length > 0
      ? `
        <div style="margin-bottom: 16px;">
            <h4 style="margin-bottom: 8px;">Parameters</h4>
            ${endpoint.parameters
              .map(
                (param) => `
                <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 8px;">
                    <label style="width: 96px; font-weight: 500; font-size: 14px;">${param.name}:</label>
                    <input type="text" placeholder="Enter ${param.name}" class="header-input" style="flex: 1;">
                </div>
            `,
              )
              .join("")}
        </div>
    `
      : ""

  return `
        <div>
            <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 16px;">
                <span class="badge ${getMethodColor(endpoint.httpMethodType)}">${endpoint.httpMethodType}</span>
                <code class="endpoint-path" style="flex: 1;">${endpoint.path}</code>
            </div>
            ${parametersSection}
            <button class="btn btn-primary" style="width: 100%;">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <polygon points="5,3 19,12 5,21 5,3"></polygon>
                </svg>
                Execute Request
            </button>
        </div>
    `
}

function renderHeaders() {
  const container = document.getElementById("headersList")
  container.innerHTML = ""

  requestHeaders.forEach((header, index) => {
    const row = document.createElement("div")
    row.className = "header-row"
    row.innerHTML = `
            <input type="text" placeholder="Header name" value="${header.key}" class="header-input" onchange="updateHeader(${index}, 'key', this.value)">
            <input type="text" placeholder="Header value" value="${header.value}" class="header-input" onchange="updateHeader(${index}, 'value', this.value)">
            <button class="remove-header" onclick="removeHeader(${index})">&times;</button>
        `
    container.appendChild(row)
  })
}

// Event handlers
function selectController(controllerName) {
  selectedController = controllerName
  expandedEndpoints = []
  renderControllers()
  renderEndpoints()
}

function toggleEndpoint(endpointId) {
  const index = expandedEndpoints.indexOf(endpointId)
  if (index > -1) {
    expandedEndpoints.splice(index, 1)
  } else {
    expandedEndpoints.push(endpointId)
  }
  renderEndpoints()
}

function switchTab(event, endpointId, tabName) {
  // Remove active class from all triggers in this endpoint
  const tabList = event.target.parentElement
  tabList.querySelectorAll(".tab-trigger").forEach((trigger) => {
    trigger.classList.remove("active")
  })
  event.target.classList.add("active")

  // Hide all tab contents for this endpoint
  const tabsContainer = tabList.parentElement
  tabsContainer.querySelectorAll(".tab-content").forEach((content) => {
    content.classList.remove("active")
  })

  // Show selected tab content
  document.getElementById(`${endpointId}-${tabName}`).classList.add("active")
}

function copyToClipboard(button) {
  const codeBlock = button.nextElementSibling
  const text = codeBlock.textContent
  navigator.clipboard.writeText(text).then(() => {
    button.innerHTML = "✓"
    setTimeout(() => {
      button.innerHTML = `
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
                    <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
                </svg>
            `
    }, 2000)
  })
}

function addHeader() {
  requestHeaders.push({ key: "", value: "" })
  renderHeaders()
}

function updateHeader(index, field, value) {
  requestHeaders[index][field] = value
}

function removeHeader(index) {
  requestHeaders.splice(index, 1)
  renderHeaders()
}

async function sendRequest() {
  const method = document.getElementById("requestMethod").value
  const url = document.getElementById("requestUrl").value
  const body = document.getElementById("requestBody").value
  const sendBtn = document.getElementById("sendRequestBtn")

  sendBtn.textContent = "Sending..."
  sendBtn.disabled = true

  try {
    const headers = {}
    requestHeaders.forEach((header) => {
      if (header.key && header.value) {
        headers[header.key] = header.value
      }
    })

    if (authToken) {
      headers["Authorization"] = `Bearer ${authToken}`
    }

    const options = {
      method,
      headers,
    }

    if (method !== "GET" && body) {
      options.body = body
    }

    const response = await fetch(url, options)
    const responseText = await response.text()

    const responseContainer = document.getElementById("responseContainer")

    if (response.ok) {
      responseContainer.innerHTML = `
                <div class="response-success">
                    <div class="response-status">
                        <span class="status-badge ${getStatusClass(response.status.toString())}">${response.status} ${response.statusText}</span>
                    </div>
                    <div class="response-body">
                        <h5>Response Body</h5>
                        <pre>${responseText}</pre>
                    </div>
                </div>
            `
    } else {
      throw new Error(`${response.status} ${response.statusText}`)
    }

    // Switch to response tab
    document.querySelector('[data-tab="response"]').click()
  } catch (error) {
    const responseContainer = document.getElementById("responseContainer")
    responseContainer.innerHTML = `
            <div class="response-error">
                <h5>Error</h5>
                <p>${error.message}</p>
            </div>
        `
    document.querySelector('[data-tab="response"]').click()
  }

  sendBtn.textContent = "Send"
  sendBtn.disabled = false
}

function exportOpenAPI() {
  const openApiSpec = {
    openapi: "3.0.0",
    info: {
      title: "API Documentation",
      version: "1.0.0",
      description: "Generated by Kaya ApiExplorer",
    },
    paths: {},
  }

  const blob = new Blob([JSON.stringify(openApiSpec, null, 2)], { type: "application/json" })
  const url = URL.createObjectURL(blob)
  const a = document.createElement("a")
  a.href = url
  a.download = "api-spec.json"
  a.click()
  URL.revokeObjectURL(url)
}

function saveAuthToken() {
  const token = document.getElementById("authToken").value.trim()
  authToken = token

  const status = document.getElementById("tokenStatus")
  if (token) {
    status.classList.remove("hidden")
  } else {
    status.classList.add("hidden")
  }

  document.getElementById("authModal").classList.remove("show")
}

function clearAuthToken() {
  authToken = ""
  document.getElementById("authToken").value = ""
  document.getElementById("tokenStatus").classList.add("hidden")
  document.getElementById("authModal").classList.remove("show")
}

// Search functionality
function filterControllers() {
  const query = document.getElementById("searchInput").value.toLowerCase()
  const cards = document.querySelectorAll(".controller-card")

  cards.forEach((card) => {
    const title = card.querySelector("h3").textContent.toLowerCase()
    const description = card.querySelector("p").textContent.toLowerCase()

    if (title.includes(query) || description.includes(query)) {
      card.style.display = "block"
    } else {
      card.style.display = "none"
    }
  })
}

function showModal(modalId) {
  document.getElementById(modalId).classList.add("show")
}

function hideModal(modalId) {
  document.getElementById(modalId).classList.remove("show")
}

function switchRequestTab(tabName) {
  document.querySelectorAll(".tab-trigger").forEach((trigger) => {
    trigger.classList.remove("active")
  })

  document.querySelector(`[data-tab="${tabName}"]`).classList.add("active")

  document.querySelectorAll(".tab-content").forEach((content) => {
    content.classList.remove("active")
  })

  document.getElementById(`${tabName}-tab`).classList.add("active")
}

// Initialize the application
document.addEventListener("DOMContentLoaded", async () => {
  await loadApiData()
  
  renderHeaders()

  document.getElementById("searchInput").addEventListener("input", filterControllers)

  document.getElementById("requestBuilderBtn").addEventListener("click", () => showModal("requestBuilderModal"))
  document.getElementById("authorizeBtn").addEventListener("click", () => showModal("authModal"))
  document.getElementById("exportBtn").addEventListener("click", exportOpenAPI)

  document.getElementById("closeRequestBuilder").addEventListener("click", () => hideModal("requestBuilderModal"))
  document.getElementById("closeAuth").addEventListener("click", () => hideModal("authModal"))

  document.getElementById("addHeaderBtn").addEventListener("click", addHeader)
  document.getElementById("sendRequestBtn").addEventListener("click", sendRequest)

  document.getElementById("saveTokenBtn").addEventListener("click", saveAuthToken)
  document.getElementById("clearTokenBtn").addEventListener("click", clearAuthToken)

  document.querySelectorAll(".tab-trigger").forEach((trigger) => {
    trigger.addEventListener("click", (e) => {
      const tabName = e.target.getAttribute("data-tab")
      if (tabName) {
        switchRequestTab(tabName)
      }
    })
  })

  document.querySelectorAll(".modal").forEach((modal) => {
    modal.addEventListener("click", (e) => {
      if (e.target === modal) {
        modal.classList.remove("show")
      }
    })
  })
})
