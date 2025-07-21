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
                ${renderEndpointTabs(endpoint, endpointId, index)}
            </div>
        `

    container.appendChild(card)
  })
}

function renderEndpointTabs(endpoint, endpointId, index) {
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
                ${renderTryItOut(endpoint, index)}
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

function renderTryItOut(endpoint, index) {
  const parametersSection = renderTryItOutParameters(endpoint);
  const requestBodySection = renderTryItOutRequestBody(endpoint);

  return `
        <div>
            ${parametersSection}
            ${requestBodySection}
            <button class="btn btn-primary" style="width: 100%;" onclick="executeEndpointById('${selectedController}', ${index})">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <polygon points="5,3 19,12 5,21 5,3"></polygon>
                </svg>
                Execute Request
            </button>
            <div id="tryout-response-${endpoint.path.replace(/[^a-zA-Z0-9]/g, '_')}" class="response-container" style="margin-top: 16px; display: none;">
                <!-- Response will be displayed here -->
            </div>
        </div>
    `
}

function renderTryItOutParameters(endpoint) {
  if (!endpoint.parameters || endpoint.parameters.length === 0) {
    return '';
  }

  const queryParams = endpoint.parameters.filter(p => p.source === "Query");
  const routeParams = endpoint.parameters.filter(p => p.source === "Route");
  const headerParams = endpoint.parameters.filter(p => p.source === "Header");

  let parametersHtml = '';

  if (queryParams.length > 0) {
    parametersHtml += `
      <div class="tryout-parameter-group">
        <h4>Query Parameters</h4>
        ${queryParams.map(param => `
          <div class="tryout-parameter-row">
            <label class="tryout-parameter-label ${param.required ? 'required' : ''}">${param.name}:</label>
            <input type="text" id="param-${param.name}" placeholder="Enter ${param.name}" class="header-input" style="flex: 1;" ${param.defaultValue ? `value="${param.defaultValue}"` : ''}>
            <span class="tryout-parameter-type">${param.type}</span>
          </div>
        `).join('')}
      </div>
    `;
  }

  if (routeParams.length > 0) {
    parametersHtml += `
      <div class="tryout-parameter-group">
        <h4>Path Parameters</h4>
        ${routeParams.map(param => `
          <div class="tryout-parameter-row">
            <label class="tryout-parameter-label ${param.required ? 'required' : ''}">${param.name}:</label>
            <input type="text" id="param-${param.name}" placeholder="Enter ${param.name}" class="header-input" style="flex: 1;" ${param.defaultValue ? `value="${param.defaultValue}"` : ''}>
            <span class="tryout-parameter-type">${param.type}</span>
          </div>
        `).join('')}
      </div>
    `;
  }

  if (headerParams.length > 0) {
    parametersHtml += `
      <div class="tryout-parameter-group">
        <h4>Header Parameters</h4>
        ${headerParams.map(param => `
          <div class="tryout-parameter-row">
            <label class="tryout-parameter-label ${param.required ? 'required' : ''}">${param.name}:</label>
            <input type="text" id="param-${param.name}" placeholder="Enter ${param.name}" class="header-input" style="flex: 1;" ${param.defaultValue ? `value="${param.defaultValue}"` : ''}>
            <span class="tryout-parameter-type">${param.type}</span>
          </div>
        `).join('')}
      </div>
    `;
  }

  return parametersHtml;
}

function renderTryItOutRequestBody(endpoint) {
  if (!endpoint.requestBody) {
    return '';
  }

  const bodyId = `request-body-${endpoint.path.replace(/[^a-zA-Z0-9]/g, '_')}`;
  const keyValueId = `request-body-kv-${endpoint.path.replace(/[^a-zA-Z0-9]/g, '_')}`;

  return `
    <div class="tryout-parameter-group">
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px;">
        <h4>Request Body <span class="badge">${endpoint.requestBody.type}</span></h4>
        <select id="bodyEditorMode-${endpoint.path.replace(/[^a-zA-Z0-9]/g, '_')}" class="method-select" onchange="switchTryItOutBodyEditorMode('${bodyId}', '${keyValueId}')">
          <option value="json">JSON Editor</option>
          <option value="keyvalue">Key-Value Editor</option>
        </select>
      </div>
      <textarea id="${bodyId}" 
                placeholder="Enter request body (JSON)" 
                class="body-textarea request-body-editor active" 
                style="width: 100%; height: 160px; font-family: Monaco, monospace;">${endpoint.requestBody.example}</textarea>
      <div id="${keyValueId}" class="request-body-kv-editor" style="display: none;">
        <div id="${keyValueId}-fields"></div>
        <button type="button" class="btn btn-secondary" style="margin-top: 8px;" onclick="addRequestBodyField('${keyValueId}')">Add Field</button>
      </div>
    </div>
  `;
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
  const tabList = event.target.parentElement
  tabList.querySelectorAll(".tab-trigger").forEach((trigger) => {
    trigger.classList.remove("active")
  })
  event.target.classList.add("active")

  const tabsContainer = tabList.parentElement
  tabsContainer.querySelectorAll(".tab-content").forEach((content) => {
    content.classList.remove("active")
  })

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

async function executeEndpointById(controllerName, endpointIndex) {
  const controller = controllers.find(c => c.name === controllerName);
  if (!controller) {
    console.error('Controller not found:', controllerName);
    return;
  }
  
  const endpoint = controller.endpoints[endpointIndex];
  if (!endpoint) {
    console.error('Endpoint not found at index:', endpointIndex);
    return;
  }
  
  await executeEndpoint(endpoint);
}

async function executeEndpoint(endpoint) {
  const responseContainerId = `tryout-response-${endpoint.path.replace(/[^a-zA-Z0-9]/g, '_')}`;
  const responseContainer = document.getElementById(responseContainerId);
  
  responseContainer.style.display = 'block';
  responseContainer.innerHTML = '<p>Executing request...</p>';

  try {
    let finalUrl = endpoint.path;
    const pathParams = endpoint.parameters?.filter(p => p.source === "Route") || [];

    pathParams.forEach(param => {
      const paramValue = document.getElementById(`param-${param.name}`)?.value || '';
      if (paramValue) {
        finalUrl = finalUrl.replace(`{${param.name}}`, paramValue);
      }
    });
    const queryParams = endpoint.parameters?.filter(p => p.source === "Query") || [];
    const queryString = new URLSearchParams();
    
    queryParams.forEach(param => {
      const paramValue = document.getElementById(`param-${param.name}`)?.value;
      if (paramValue) {
        queryString.append(param.name, paramValue);
      }
    });

    if (queryString.toString()) {
      finalUrl += (finalUrl.includes('?') ? '&' : '?') + queryString.toString();
    }

    const headers = { 'Content-Type': 'application/json' };
    
    const headerParams = endpoint.parameters?.filter(p => p.source === "Header") || [];
    headerParams.forEach(param => {
      const paramValue = document.getElementById(`param-${param.name}`)?.value;
      if (paramValue) {
        headers[param.name] = paramValue;
      }
    });

    if (authToken) {
      headers['Authorization'] = `Bearer ${authToken}`;
    }

    const requestOptions = {
      method: endpoint.httpMethodType,
      headers: headers
    };

    if (endpoint.httpMethodType !== 'GET' && endpoint.requestBody) {
      const bodyTextarea = document.getElementById(`request-body-${endpoint.path.replace(/[^a-zA-Z0-9]/g, '_')}`);
      const keyValueEditor = document.getElementById(`request-body-kv-${endpoint.path.replace(/[^a-zA-Z0-9]/g, '_')}`);
      
      let requestBodyContent = '';
      
      if (keyValueEditor && keyValueEditor.style.display !== 'none') {
        const keyValueData = getKeyValueData(`request-body-kv-${endpoint.path.replace(/[^a-zA-Z0-9]/g, '_')}`);
        if (Object.keys(keyValueData).length > 0) {
          requestBodyContent = JSON.stringify(keyValueData);
        }
      } else if (bodyTextarea && bodyTextarea.value.trim()) {
        requestBodyContent = bodyTextarea.value.trim();
      }
      
      if (requestBodyContent) {
        try {
          if (headers['Content-Type'].includes('json')) {
            JSON.parse(requestBodyContent);
          }
          requestOptions.body = requestBodyContent;
        } catch (e) {
          throw new Error('Invalid JSON in request body: ' + e.message);
        }
      }
    }

    const response = await fetch(finalUrl, requestOptions);
    const responseText = await response.text();
    
    let responseData;
    try {
      responseData = JSON.parse(responseText);
    } catch {
      responseData = responseText;
    }

    const statusClass = response.ok ? 'status-2xx' : 'status-4xx';
    responseContainer.innerHTML = `
      <div class="response-success" style="margin-top: 12px;">
        <div class="response-status" style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px;">
          <h5>Response</h5>
          <span class="status-badge ${statusClass}">${response.status} ${response.statusText}</span>
        </div>
        <div class="response-headers" style="margin-bottom: 12px;">
          <h6>Response Headers</h6>
          <pre style="background: #f8f9fa; padding: 8px; border-radius: 4px; font-size: 12px;">${Array.from(response.headers.entries()).map(([key, value]) => `${key}: ${value}`).join('\\n')}</pre>
        </div>
        <div class="response-body">
          <h6>Response Body</h6>
          <div style="position: relative;">
            <button class="copy-btn" onclick="copyResponseToClipboard(this)" style="position: absolute; top: 8px; right: 8px; z-index: 1;">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
                <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
              </svg>
            </button>
            <pre style="background: #f8f9fa; padding: 12px; border-radius: 4px; white-space: pre-wrap; margin: 0;">${typeof responseData === 'object' ? JSON.stringify(responseData, null, 2) : responseData}</pre>
          </div>
        </div>
      </div>
    `;

  } catch (error) {
    responseContainer.innerHTML = `
      <div class="response-error" style="margin-top: 12px;">
        <h5>Error</h5>
        <p style="color: #dc3545;">${error.message}</p>
      </div>
    `;
  }
}

function copyResponseToClipboard(button) {
  const responseBody = button.nextElementSibling.textContent;
  navigator.clipboard.writeText(responseBody).then(() => {
    button.innerHTML = "✓";
    setTimeout(() => {
      button.innerHTML = `
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
          <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2 2v1"></path>
        </svg>
      `;
    }, 2000);
  });
}

function toggleRequestBodyEditor(jsonEditorId, keyValueEditorId, mode) {
  const jsonEditor = document.getElementById(jsonEditorId);
  const keyValueEditor = document.getElementById(keyValueEditorId);
  
  if (mode === 'json') {
    if (keyValueEditor.style.display !== 'none') {
      const keyValueData = getKeyValueData(keyValueEditorId);
      if (Object.keys(keyValueData).length > 0) {
        jsonEditor.value = JSON.stringify(keyValueData, null, 2);
      }
    }
    
    jsonEditor.style.display = 'block';
    keyValueEditor.style.display = 'none';
  } else {
    try {
      const jsonData = JSON.parse(jsonEditor.value || '{}');
      populateKeyValueEditor(keyValueEditorId, jsonData);
    } catch (e) {
      populateKeyValueEditor(keyValueEditorId, {});
    }
    
    jsonEditor.style.display = 'none';
    keyValueEditor.style.display = 'block';
  }
}

function populateKeyValueEditor(keyValueEditorId, data) {
  const fieldsContainer = document.getElementById(`${keyValueEditorId}-fields`);
  fieldsContainer.innerHTML = '';
  
  Object.entries(data).forEach(([key, value]) => {
    addRequestBodyFieldWithValue(keyValueEditorId, key, value);
  });
  
  if (Object.keys(data).length === 0) {
    addRequestBodyField(keyValueEditorId);
  }
}

function addRequestBodyField(keyValueEditorId) {
  addRequestBodyFieldWithValue(keyValueEditorId, '', '');
}

function addRequestBodyFieldWithValue(keyValueEditorId, key = '', value = '') {
  const fieldsContainer = document.getElementById(`${keyValueEditorId}-fields`);
  const fieldIndex = fieldsContainer.children.length;
  
  const fieldRow = document.createElement('div');
  fieldRow.className = 'tryout-parameter-row';
  fieldRow.style.marginBottom = '8px';
  
  let valueStr = value;
  if (typeof value === 'object' && value !== null) {
    valueStr = JSON.stringify(value);
  } else if (typeof value !== 'string') {
    valueStr = String(value);
  }
  
  fieldRow.innerHTML = `
    <input type="text" placeholder="Field name" value="${key}" class="header-input" style="flex: 1; margin-right: 8px;" onchange="updateRequestBodyField('${keyValueEditorId}')">
    <input type="text" placeholder="Field value" value="${valueStr}" class="header-input" style="flex: 2; margin-right: 8px;" onchange="updateRequestBodyField('${keyValueEditorId}')">
    <button type="button" class="remove-header" onclick="removeRequestBodyField(this, '${keyValueEditorId}')" style="background: #dc3545; color: white; border: none; padding: 6px 10px; border-radius: 4px; cursor: pointer;">&times;</button>
  `;
  
  fieldsContainer.appendChild(fieldRow);
}

function removeRequestBodyField(button, keyValueEditorId) {
  button.parentElement.remove();
}

function getKeyValueData(keyValueEditorId) {
  const fieldsContainer = document.getElementById(`${keyValueEditorId}-fields`);
  const data = {};
  
  Array.from(fieldsContainer.children).forEach(row => {
    const inputs = row.querySelectorAll('input');
    const key = inputs[0].value.trim();
    const value = inputs[1].value.trim();
    
    if (key) {
      try {
        data[key] = JSON.parse(value);
      } catch (e) {
        if (value === 'true') {
          data[key] = true;
        } else if (value === 'false') {
          data[key] = false;
        } else if (value === 'null') {
          data[key] = null;
        } else if (!isNaN(value) && !isNaN(parseFloat(value))) {
          data[key] = parseFloat(value);
        } else {
          data[key] = value;
        }
      }
    }
  });
  
  return data;
}

function switchBodyEditorMode() {
  const mode = document.getElementById('bodyEditorMode').value;
  const jsonEditor = document.getElementById('requestBody');
  const keyValueEditor = document.getElementById('requestBodyKvEditor');
  
  if (mode === 'json') {
    if (keyValueEditor.style.display !== 'none') {
      const keyValueData = getRequestBuilderKeyValueData();
      if (Object.keys(keyValueData).length > 0) {
        jsonEditor.value = JSON.stringify(keyValueData, null, 2);
      }
    }
    
    jsonEditor.style.display = 'block';
    keyValueEditor.style.display = 'none';
  } else {
    try {
      const jsonData = JSON.parse(jsonEditor.value || '{}');
      populateRequestBuilderKeyValueEditor(jsonData);
    } catch (e) {
      populateRequestBuilderKeyValueEditor({});
    }
    
    jsonEditor.style.display = 'none';
    keyValueEditor.style.display = 'block';
  }
}

function switchTryItOutBodyEditorMode(jsonEditorId, keyValueEditorId) {
  const selectElement = document.getElementById(`bodyEditorMode-${jsonEditorId.replace('request-body-', '')}`);
  const mode = selectElement.value;
  const jsonEditor = document.getElementById(jsonEditorId);
  const keyValueEditor = document.getElementById(keyValueEditorId);
  
  if (mode === 'json') {
    if (keyValueEditor.style.display !== 'none') {
      const keyValueData = getKeyValueData(keyValueEditorId);
      if (Object.keys(keyValueData).length > 0) {
        jsonEditor.value = JSON.stringify(keyValueData, null, 2);
      }
    }
    
    jsonEditor.style.display = 'block';
    keyValueEditor.style.display = 'none';
  } else {
    try {
      const jsonData = JSON.parse(jsonEditor.value || '{}');
      populateKeyValueEditor(keyValueEditorId, jsonData);
    } catch (e) {
      populateKeyValueEditor(keyValueEditorId, {});
    }
    
    jsonEditor.style.display = 'none';
    keyValueEditor.style.display = 'block';
  }
}

function populateRequestBuilderKeyValueEditor(data) {
  const fieldsContainer = document.getElementById('requestBodyKvFields');
  fieldsContainer.innerHTML = '';
  
  Object.entries(data).forEach(([key, value]) => {
    addRequestBuilderBodyFieldWithValue(key, value);
  });
  
  if (Object.keys(data).length === 0) {
    addRequestBuilderBodyField();
  }
}

function addRequestBuilderBodyField() {
  addRequestBuilderBodyFieldWithValue('', '');
}

function addRequestBuilderBodyFieldWithValue(key = '', value = '') {
  const fieldsContainer = document.getElementById('requestBodyKvFields');
  
  const fieldRow = document.createElement('div');
  fieldRow.className = 'kv-field-row';
  
  let valueStr = value;
  if (typeof value === 'object' && value !== null) {
    valueStr = JSON.stringify(value);
  } else if (typeof value !== 'string') {
    valueStr = String(value);
  }
  
  fieldRow.innerHTML = `
    <input type="text" placeholder="Field name" value="${key}" class="header-input" style="flex: 1; margin-right: 8px;">
    <input type="text" placeholder="Field value" value="${valueStr}" class="header-input" style="flex: 2; margin-right: 8px;">
    <button type="button" class="kv-remove-btn" onclick="removeRequestBuilderBodyField(this)">&times;</button>
  `;
  
  fieldsContainer.appendChild(fieldRow);
}

function removeRequestBuilderBodyField(button) {
  button.parentElement.remove();
}

function getRequestBuilderKeyValueData() {
  const fieldsContainer = document.getElementById('requestBodyKvFields');
  const data = {};
  
  Array.from(fieldsContainer.children).forEach(row => {
    const inputs = row.querySelectorAll('input');
    const key = inputs[0].value.trim();
    const value = inputs[1].value.trim();
    
    if (key) {
      try {
        data[key] = JSON.parse(value);
      } catch (e) {
        if (value === 'true') {
          data[key] = true;
        } else if (value === 'false') {
          data[key] = false;
        } else if (value === 'null') {
          data[key] = null;
        } else if (!isNaN(value) && !isNaN(parseFloat(value))) {
          data[key] = parseFloat(value);
        } else {
          data[key] = value;
        }
      }
    }
  });
  
  return data;
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
  const bodyEditorMode = document.getElementById("bodyEditorMode").value

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

    if (method !== "GET") {
      let requestBodyContent = '';
      
      if (bodyEditorMode === 'keyvalue') {
        const keyValueData = getRequestBuilderKeyValueData();
        if (Object.keys(keyValueData).length > 0) {
          requestBodyContent = JSON.stringify(keyValueData);
        }
      } else if (body) {
        requestBodyContent = body;
      }
      
      if (requestBodyContent) {
        options.body = requestBodyContent;
      }
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
