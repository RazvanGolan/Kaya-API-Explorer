let controllers = []
let selectedController = ""
let expandedEndpoints = []

let authConfig = {
  type: 'none',
  bearer: {
    token: ''
  },
  apikey: {
    name: 'X-API-Key',
    value: ''
  },
  oauth: {
    clientId: '',
    authUrl: '',
    tokenUrl: '',
    scopes: '',
    accessToken: ''
  }
}

const requestHeaders = [{ key: "Content-Type", value: "application/json" }]

let currentTheme = getInitialTheme()

function generateCurlCode(url, method, headers, body) {
  let curlCommand = `curl -X ${method.toUpperCase()} "${url}"`;
  
  Object.entries(headers).forEach(([key, value]) => {
    curlCommand += ` \\\n  -H "${key}: ${value}"`;
  });
  
  if (body && method.toUpperCase() !== 'GET') {
    curlCommand += ` \\\n  -d '${body}'`;
  }
  
  return curlCommand;
}

function generateJavaScriptCode(url, method, headers, body) {
  const headersObj = JSON.stringify(headers, null, 2);
  
  let code = `const response = await fetch('${url}', {\n  method: '${method.toUpperCase()}',\n  headers: ${headersObj}`;
  
  if (body && method.toUpperCase() !== 'GET') {
    code += `,\n  body: ${JSON.stringify(body)}`;
  }
  
  code += '\n});\n\nconst data = await response.json();\nconsole.log(data);';
  
  return code;
}

function generatePythonCode(url, method, headers, body) {
  let code = 'import requests\nimport json\n\n';
  code += `url = "${url}"\n`;
  code += `headers = ${JSON.stringify(headers, null, 2).replace(/"/g, "'")}\n`;
  
  if (body && method.toUpperCase() !== 'GET') {
    code += `data = ${JSON.stringify(body, null, 2).replace(/"/g, "'")}\n\n`;
    code += `response = requests.${method.toLowerCase()}(url, headers=headers, json=data)\n`;
  } else {
    code += `\nresponse = requests.${method.toLowerCase()}(url, headers=headers)\n`;
  }
  
  code += 'print(response.status_code)\nprint(response.json())';
  
  return code;
}

function generateRubyCode(url, method, headers, body) {
  let code = "require 'net/http'\nrequire 'json'\nrequire 'uri'\n\n";
  code += `uri = URI('${url}')\n`;
  code += `http = Net::HTTP.new(uri.host, uri.port)\n`;
  
  if (url.startsWith('https')) {
    code += 'http.use_ssl = true\n';
  }
  
  code += `\nrequest = Net::HTTP::${method.charAt(0).toUpperCase() + method.slice(1).toLowerCase()}.new(uri)\n`;
  
  Object.entries(headers).forEach(([key, value]) => {
    code += `request['${key}'] = '${value}'\n`;
  });
  
  if (body && method.toUpperCase() !== 'GET') {
    code += `request.body = ${JSON.stringify(body, null, 2).replace(/"/g, "'")}.to_json\n`;
  }
  
  code += '\nresponse = http.request(request)\nputs response.code\nputs response.body';
  
  return code;
}

function generateCSharpCode(url, method, headers, body) {
  let code = 'using System;\nusing System.Net.Http;\nusing System.Text;\nusing System.Threading.Tasks;\n\n';
  code += 'class Program\n{\n    static async Task Main(string[] args)\n    {\n';
  code += '        using var client = new HttpClient();\n';
  
  Object.entries(headers).forEach(([key, value]) => {
    if (key.toLowerCase() !== 'content-type') {
      code += `        client.DefaultRequestHeaders.Add("${key}", "${value}");\n`;
    }
  });
  
  code += `\n        var url = "${url}";\n`;
  
  if (body && method.toUpperCase() !== 'GET') {
    code += `        var json = ${JSON.stringify(body, null, 8)};\n`;
    code += '        var content = new StringContent(json, Encoding.UTF8, "application/json");\n\n';
    code += `        var response = await client.${method.charAt(0).toUpperCase() + method.slice(1).toLowerCase()}Async(url, content);\n`;
  } else {
    code += `        var response = await client.${method.charAt(0).toUpperCase() + method.slice(1).toLowerCase()}Async(url);\n`;
  }
  
  code += '\n        var responseContent = await response.Content.ReadAsStringAsync();\n';
  code += '        Console.WriteLine($"Status: {response.StatusCode}");\n';
  code += '        Console.WriteLine($"Content: {responseContent}");\n';
  code += '    }\n}';
  
  return code;
}

function buildRequestData(config) {
  const {
    endpoint,
    endpointIdentifier,
    baseUrl,
    customHeaders = {},
    includeAuth = true,
    validateBody = false,
    allowEmptyPathParams = false
  } = config;

  let finalUrl = endpoint ? endpoint.path : baseUrl;
  
  if (endpoint && endpoint.parameters) {
    const pathParams = endpoint.parameters.filter(p => p.source === "Route") || [];
    
    pathParams.forEach(param => {
      let paramValue = '';
      if (endpointIdentifier) {
        paramValue = document.getElementById(`param-${endpointIdentifier}-${param.name}`)?.value;
      }
      
      if (!paramValue && !allowEmptyPathParams) {
        paramValue = `{${param.name}}`;
      }
      
      if (paramValue) {
        finalUrl = finalUrl.replace(`{${param.name}}`, paramValue);
      }
    });
    
    const queryParams = endpoint.parameters.filter(p => p.source === "Query") || [];
    const queryString = new URLSearchParams();
    
    queryParams.forEach(param => {
      if (endpointIdentifier) {
        const paramValue = document.getElementById(`param-${endpointIdentifier}-${param.name}`)?.value;
        if (paramValue) {
          queryString.append(param.name, paramValue);
        }
      }
    });
    
    if (queryString.toString()) {
      finalUrl += (finalUrl.includes('?') ? '&' : '?') + queryString.toString();
    }
  }
  
  if (!finalUrl.startsWith('http')) {
    finalUrl = window.location.origin + (finalUrl.startsWith('/') ? '' : '/') + finalUrl;
  }
  
  const headers = { 'Content-Type': 'application/json', ...customHeaders };
  
  if (endpoint && endpoint.parameters && endpointIdentifier) {
    const headerParams = endpoint.parameters.filter(p => p.source === "Header") || [];
    headerParams.forEach(param => {
      const paramValue = document.getElementById(`param-${endpointIdentifier}-${param.name}`)?.value;
      if (paramValue) {
        headers[param.name] = paramValue;
      }
    });
  }
  
  if (includeAuth) {
    const authHeaders = getAuthHeaders();
    Object.keys(authHeaders).forEach(key => {
      if (!headers[key]) {
        headers[key] = authHeaders[key];
      }
    });
  }
  
  let requestBody = null;
  const method = endpoint ? endpoint.httpMethodType : 'GET';
  
  if (method !== 'GET' && endpoint && endpoint.requestBody && endpointIdentifier) {
    const bodyTextarea = document.getElementById(`request-body-${endpointIdentifier}`);
    const keyValueEditor = document.getElementById(`request-body-kv-${endpointIdentifier}`);
    
    let requestBodyContent = '';
    
    if (keyValueEditor && keyValueEditor.style.display !== 'none') {
      const keyValueData = getKeyValueData(`request-body-kv-${endpointIdentifier}`);
      if (Object.keys(keyValueData).length > 0) {
        requestBodyContent = JSON.stringify(keyValueData);
      }
    } else if (bodyTextarea && bodyTextarea.value.trim()) {
      requestBodyContent = bodyTextarea.value.trim();
    }
    
    if (requestBodyContent) {
      if (validateBody) {
        try {
          if (headers['Content-Type'].includes('json')) {
            JSON.parse(requestBodyContent);
          }
        } catch (e) {
          throw new Error('Invalid JSON in request body: ' + e.message);
        }
      }
      requestBody = requestBodyContent;
    }
  }
  
  return {
    url: finalUrl,
    method: method,
    headers: headers,
    body: requestBody,
    requestOptions: {
      method: method,
      headers: headers,
      ...(requestBody && { body: requestBody })
    }
  };
}

function buildRequestDataForExport(controllerName, endpointIndex) {
  const controller = controllers.find(c => c.name === controllerName);
  if (!controller) return null;
  
  const endpoint = controller.endpoints[endpointIndex];
  if (!endpoint) return null;
  
  const endpointIdentifier = `${controllerName}-${endpointIndex}`;
  
  try {
    const requestData = buildRequestData({
      endpoint: endpoint,
      endpointIdentifier: endpointIdentifier,
      allowEmptyPathParams: true
    });
    
    return {
      ...requestData,
      endpoint: endpoint
    };
  } catch (error) {
    console.error('Error building request data for export:', error);
    return null;
  }
}

function formatBytes(bytes) {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i]
}

function formatDuration(ms) {
  if (ms < 1000) return `${ms}ms`
  return `${(ms / 1000).toFixed(1)}s`
}

function getDurationColor(ms) {
  if (ms < 500) return '#22c55e' 
  if (ms < 1000) return '#f59e0b'
  return '#ef4444'
}

function getSizeColor(bytes) {
  if (bytes < 1024) return '#22c55e'
  if (bytes < 1024 * 100) return '#f59e0b'
  return '#ef4444'
}

function generatePerformanceHtml(duration, requestSize, responseSize, status) {
  return `
    <div class="performance-metrics" style="background: var(--bg-secondary); border-radius: 6px; padding: 12px; margin-bottom: 12px; border-left: 4px solid ${getDurationColor(duration)};">
      <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 8px;">
        <h6 style="margin: 0; color: var(--text-primary);">Performance</h6>
      </div>
      <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(120px, 1fr)); gap: 8px; font-size: 0.85em;">
        <div><strong>Duration:</strong> <span style="color: ${getDurationColor(duration)}; font-weight: 600;">${formatDuration(duration)}</span></div>
        <div><strong>Request:</strong> <span style="color: ${getSizeColor(requestSize)}; font-weight: 600;">${formatBytes(requestSize)}</span></div>
        <div><strong>Response:</strong> <span style="color: ${getSizeColor(responseSize)}; font-weight: 600;">${formatBytes(responseSize)}</span></div>
        <div><strong>Status:</strong> <span style="color: ${status >= 200 && status < 300 ? '#22c55e' : '#ef4444'}; font-weight: 600;">${status || 'Error'}</span></div>
      </div>
    </div>
  `
}

function escapeHtml(text) {
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

function createKeyValueField(containerId, key = '', value = '', config = {}) {
  const fieldsContainer = document.getElementById(containerId);
  
  const fieldRow = document.createElement('div');
  fieldRow.className = config.rowClassName || 'tryout-parameter-row';
  if (config.rowStyle) {
    fieldRow.style.cssText = config.rowStyle;
  } else {
    fieldRow.style.marginBottom = '8px';
  }
  
  let valueStr = value;
  if (typeof value === 'object' && value !== null) {
    valueStr = JSON.stringify(value);
  } else if (typeof value !== 'string') {
    valueStr = String(value);
  }
  
  const escapedKey = escapeHtml(key);
  const escapedValue = escapeHtml(valueStr);
  
  const removeButtonClass = config.removeButtonClass || 'remove-header';
  const removeButtonStyle = config.removeButtonStyle || 'background: #dc3545; color: white; border: none; padding: 6px 10px; border-radius: 4px; cursor: pointer;';
  const removeFunctionName = config.removeFunctionName || 'removeKeyValueField';
  const onChangeHandler = config.onChangeHandler ? `onchange="${config.onChangeHandler}"` : '';
  
  fieldRow.innerHTML = `
    <input type="text" placeholder="Field name" value="${escapedKey}" class="header-input" style="flex: 1; margin-right: 8px;" ${onChangeHandler}>
    <input type="text" placeholder="Field value" value="${escapedValue}" class="header-input" style="flex: 2; margin-right: 8px;" ${onChangeHandler}>
    <button type="button" class="${removeButtonClass}" onclick="${removeFunctionName}(this)" style="${removeButtonStyle}">&times;</button>
  `;
  
  fieldsContainer.appendChild(fieldRow);
}

function populateKeyValueContainer(containerId, data, config = {}) {
  const fieldsContainer = document.getElementById(containerId);
  fieldsContainer.innerHTML = '';
  
  Object.entries(data).forEach(([key, value]) => {
    createKeyValueField(containerId, key, value, config);
  });
  
  if (Object.keys(data).length === 0) {
    createKeyValueField(containerId, '', '', config);
  }
}

function parseKeyValueData(containerId) {
  const fieldsContainer = document.getElementById(containerId);
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
        } else if (value === '') {
          data[key] = '';
        } else if (!isNaN(value) && !isNaN(parseFloat(value)) && value !== '') {
          data[key] = parseFloat(value);
        } else {
          data[key] = value;
        }
      }
    }
  });
  
  return data;
}

function getInitialTheme() {
  console.log(window.KayaApiExplorerConfig)
  if (window.KayaApiExplorerConfig && window.KayaApiExplorerConfig.defaultTheme) {
    const serverTheme = window.KayaApiExplorerConfig.defaultTheme.toLowerCase()
    if (serverTheme === 'light' || serverTheme === 'dark') {
      const userTheme = localStorage.getItem('theme')
      return userTheme || serverTheme
    }
  }
  
  return localStorage.getItem('theme') || 'light'
}

function initializeTheme() {
  document.documentElement.setAttribute('data-theme', currentTheme)
  updateThemeButton()
}

function toggleTheme() {
  currentTheme = currentTheme === 'light' ? 'dark' : 'light'
  document.documentElement.setAttribute('data-theme', currentTheme)
  localStorage.setItem('theme', currentTheme)
  updateThemeButton()
}

function updateThemeButton() {
  const themeBtn = document.getElementById('themeToggleBtn')
  const sunIcon = themeBtn.querySelector('.sun-icon')
  const moonIcon = themeBtn.querySelector('.moon-icon')
  const themeText = themeBtn.querySelector('.theme-text')
  
  if (currentTheme === 'dark') {
    sunIcon.style.display = 'none'
    moonIcon.style.display = 'block'
    themeText.textContent = 'Light'
  } else {
    sunIcon.style.display = 'block'
    moonIcon.style.display = 'none'
    themeText.textContent = 'Dark'
  }
}

function switchAuthType() {
  const authType = document.getElementById('authType').value
  authConfig.type = authType
  
  document.querySelectorAll('.auth-section').forEach(section => {
    section.classList.add('hidden')
  })
  
  switch (authType) {
    case 'none':
      document.getElementById('authNone').classList.remove('hidden')
      break
    case 'bearer':
      document.getElementById('authBearer').classList.remove('hidden')
      break
    case 'apikey':
      document.getElementById('authApiKey').classList.remove('hidden')
      break
    case 'oauth':
      document.getElementById('authOAuth').classList.remove('hidden')
      break
  }
  
  updateAuthStatus()
}

function togglePasswordVisibility(inputId, button) {
  const input = document.getElementById(inputId)
  const eyeOpen = button.querySelector('.eye-open')
  const eyeClosed = button.querySelector('.eye-closed')
  
  if (input.type === 'password') {
    input.type = 'text'
    eyeOpen.style.display = 'none'
    eyeClosed.style.display = 'block'
  } else {
    input.type = 'password'
    eyeOpen.style.display = 'block'
    eyeClosed.style.display = 'none'
  }
}

function saveAuthConfiguration() {
  const authType = document.getElementById('authType').value
  authConfig.type = authType
  
  switch (authType) {
    case 'none':
      authConfig.bearer.token = ''
      authConfig.apikey.value = ''
      authConfig.oauth.accessToken = ''
      break
      
    case 'bearer':
      authConfig.bearer.token = document.getElementById('authToken').value.trim()
      break
      
    case 'apikey':
      authConfig.apikey.name = document.getElementById('apiKeyName').value.trim() || 'X-API-Key'
      authConfig.apikey.value = document.getElementById('apiKeyValue').value.trim()
      break
      
    case 'oauth':
      authConfig.oauth.clientId = document.getElementById('oauthClientId').value.trim()
      authConfig.oauth.authUrl = document.getElementById('oauthAuthUrl').value.trim()
      authConfig.oauth.tokenUrl = document.getElementById('oauthTokenUrl').value.trim()
      authConfig.oauth.scopes = document.getElementById('oauthScopes').value.trim()
      authConfig.oauth.accessToken = document.getElementById('oauthAccessToken').value.trim()
      break
  }
  
  localStorage.setItem('kayaAuthConfig', JSON.stringify(authConfig))
  
  updateAuthStatus()
  document.getElementById('authModal').classList.remove('show')
}

function clearAuthConfiguration() {
  authConfig = {
    type: 'none',
    bearer: { token: '' },
    apikey: { name: 'X-API-Key', value: '' },
    oauth: { clientId: '', authUrl: '', tokenUrl: '', scopes: '', accessToken: '' }
  }
  
  document.getElementById('authType').value = 'none'
  document.getElementById('authToken').value = ''
  document.getElementById('apiKeyName').value = 'X-API-Key'
  document.getElementById('apiKeyValue').value = ''
  document.getElementById('oauthClientId').value = ''
  document.getElementById('oauthAuthUrl').value = ''
  document.getElementById('oauthTokenUrl').value = ''
  document.getElementById('oauthScopes').value = ''
  document.getElementById('oauthAccessToken').value = ''
  
  localStorage.removeItem('kayaAuthConfig')
  
  switchAuthType()
  updateAuthStatus()
  document.getElementById('authModal').classList.remove('show')
}

function loadAuthConfiguration() {
  try {
    const saved = localStorage.getItem('kayaAuthConfig')
    if (saved) {
      const config = JSON.parse(saved)
      authConfig = { ...authConfig, ...config }
      
      document.getElementById('authType').value = authConfig.type
      document.getElementById('authToken').value = authConfig.bearer.token
      document.getElementById('apiKeyName').value = authConfig.apikey.name
      document.getElementById('apiKeyValue').value = authConfig.apikey.value
      document.getElementById('oauthClientId').value = authConfig.oauth.clientId
      document.getElementById('oauthAuthUrl').value = authConfig.oauth.authUrl
      document.getElementById('oauthTokenUrl').value = authConfig.oauth.tokenUrl
      document.getElementById('oauthScopes').value = authConfig.oauth.scopes
      document.getElementById('oauthAccessToken').value = authConfig.oauth.accessToken
      
      switchAuthType()
      updateAuthStatus()
    }
  } catch (error) {
    console.warn('Failed to load auth configuration:', error)
  }
}

function updateAuthStatus() {
  const statusDiv = document.getElementById('authStatus')
  const authType = authConfig.type
  
  statusDiv.classList.remove('hidden', 'success', 'info', 'warning')
  
  switch (authType) {
    case 'none':
      statusDiv.classList.add('hidden')
      break
      
    case 'bearer':
      if (authConfig.bearer.token) {
        statusDiv.classList.add('success')
        statusDiv.innerHTML = '<p>✓ JWT Bearer token is configured and will be automatically added to all requests</p>'
      } else {
        statusDiv.classList.add('hidden')
      }
      break
      
    case 'apikey':
      if (authConfig.apikey.value) {
        statusDiv.classList.add('success')
        statusDiv.innerHTML = `<p>✓ API Key (${authConfig.apikey.name}) is configured and will be automatically added to all requests</p>`
      } else {
        statusDiv.classList.add('hidden')
      }
      break
      
    case 'oauth':
      if (authConfig.oauth.accessToken) {
        statusDiv.classList.add('success')
        statusDiv.innerHTML = '<p>✓ OAuth access token is configured and will be automatically added to all requests</p>'
      } else if (authConfig.oauth.clientId && authConfig.oauth.authUrl) {
        statusDiv.classList.add('info')
        statusDiv.innerHTML = '<p>ℹ OAuth configuration saved. Use "Authorize with OAuth" to get an access token</p>'
      } else {
        statusDiv.classList.add('hidden')
      }
      break
  }
}

function getAuthHeaders() {
  const headers = {}
  
  switch (authConfig.type) {
    case 'bearer':
      if (authConfig.bearer.token) {
        headers['Authorization'] = `Bearer ${authConfig.bearer.token}`
      }
      break
      
    case 'apikey':
      if (authConfig.apikey.value) {
        headers[authConfig.apikey.name] = authConfig.apikey.value
      }
      break
      
    case 'oauth':
      if (authConfig.oauth.accessToken) {
        headers['Authorization'] = `Bearer ${authConfig.oauth.accessToken}`
      }
      break
  }
  
  return headers
}

function initiateOAuthFlow() {
  const clientId = document.getElementById('oauthClientId').value.trim()
  const authUrl = document.getElementById('oauthAuthUrl').value.trim()
  const scopes = document.getElementById('oauthScopes').value.trim()
  
  if (!clientId || !authUrl) {
    alert('Please enter Client ID and Authorization URL first')
    return
  }
  
  const params = new URLSearchParams({
    client_id: clientId,
    response_type: 'code',
    redirect_uri: window.location.origin + '/oauth-callback',
    scope: scopes || 'read write'
  })
  
  const oauthUrl = `${authUrl}?${params.toString()}`
  
  const popup = window.open(oauthUrl, 'oauth', 'width=500,height=600,scrollbars=yes,resizable=yes')
  
  const checkPopup = setInterval(() => {
    if (popup.closed) {
      clearInterval(checkPopup)
      alert('OAuth flow completed. Please manually enter the access token if you received one.')
    }
  }, 1000)
}

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

function doesEndpointMatchQuery(endpoint, query) {
  if (!query || !query.trim()) return true
  
  const lowerQuery = query.toLowerCase()
  const endpointPath = endpoint.path.toLowerCase()
  const endpointMethod = endpoint.httpMethodType.toLowerCase()
  const endpointName = endpoint.methodName.toLowerCase()
  const endpointDescription = endpoint.description.toLowerCase()
  
  return endpointPath.includes(lowerQuery) || 
         endpointMethod.includes(lowerQuery) || 
         endpointName.includes(lowerQuery) || 
         endpointDescription.includes(lowerQuery)
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

  const query = document.getElementById("searchInput").value.toLowerCase().trim()
  
  let endpointsToShow = controller.endpoints
  if (query) {
    endpointsToShow = controller.endpoints.filter(endpoint => 
      doesEndpointMatchQuery(endpoint, query)
    )
  }

  endpointsToShow.forEach((endpoint) => {
    const originalIndex = controller.endpoints.indexOf(endpoint)
    const endpointId = `${selectedController}-${originalIndex}`
    const isExpanded = expandedEndpoints.includes(endpointId)

    const card = document.createElement("div")
    card.className = "endpoint-card"

    const authBadge = endpoint.requiresAuthorization 
      ? `<span class="badge auth-badge" title="${endpoint.roles.length > 0 ? 'Requires role(s): ' + endpoint.roles.join(', ') : 'Requires authentication'}">
           <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="margin-right: 4px;">
             <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
             <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
           </svg>
           ${endpoint.roles.length > 0 ? endpoint.roles.join(', ') : 'Auth'}
         </span>`
      : '';

    card.innerHTML = `
            <div class="endpoint-header" onclick="toggleEndpoint('${endpointId}')">
                <div class="endpoint-title">
                    <div class="endpoint-method-path">
                        <span class="badge ${getMethodColor(endpoint.httpMethodType)}">${endpoint.httpMethodType}</span>
                        <code class="endpoint-path">${endpoint.path}</code>
                        ${authBadge}
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
                ${renderEndpointTabs(endpoint, endpointId, originalIndex)}
            </div>
        `

    container.appendChild(card)
  })
  
  if (query && endpointsToShow.length === 0) {
    container.innerHTML = '<p class="text-muted" style="text-align: center; padding: 2rem;">No endpoints match your search query.</p>'
  }
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

  const escapedType = escapeHtml(endpoint.requestBody.type);

  return `
        <div>
            <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 12px;">
                <h4>Request Body</h4>
                <span class="badge">${escapedType}</span>
            </div>
            <div class="code-block">
                <div style="position: absolute; top: 8px; right: 8px; z-index: 1; display: flex; gap: 4px;">
                    <button class="copy-btn" onclick="copyToClipboard(this)" title="Copy to clipboard">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
                            <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
                        </svg>
                    </button>
                    <button class="copy-btn save-btn" onclick="saveToFile(this, 'request-body')" title="Save to file">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"></path>
                            <polyline points="17,21 17,13 7,13 7,21"></polyline>
                            <polyline points="7,3 7,8 15,8"></polyline>
                        </svg>
                    </button>
                </div>
                <pre><code>${endpoint.requestBody.example}</code></pre>
            </div>
        </div>
    `
}

function renderResponses(endpoint) {
  if (!endpoint.response) {
    return '<p class="text-muted">No response body returned</p>'
  }

  const escapedType = escapeHtml(endpoint.response.type);

  return `
    <div>
      <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 12px;">
        <h4>Response Body</h4>
        <span class="badge">${escapedType}</span>
      </div>
      <div class="code-block">
        <div style="position: absolute; top: 8px; right: 8px; z-index: 1; display: flex; gap: 4px;">
          <button class="copy-btn" onclick="copyToClipboard(this)" title="Copy to clipboard">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
              <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
            </svg>
          </button>
          <button class="copy-btn save-btn" onclick="saveToFile(this, 'response-body')" title="Save to file">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"></path>
              <polyline points="17,21 17,13 7,13 7,21"></polyline>
              <polyline points="7,3 7,8 15,8"></polyline>
            </svg>
          </button>
        </div>
        <pre><code>${endpoint.response.example}</code></pre>
      </div>
    </div>
  `
}

function renderTryItOut(endpoint, index) {
  const endpointIdentifier = `${selectedController}-${index}`;
  const parametersSection = renderTryItOutParameters(endpoint, endpointIdentifier);
  const requestBodySection = renderTryItOutRequestBody(endpoint, endpointIdentifier);

  return `
        <div>
            ${parametersSection}
            ${requestBodySection}
            <div style="display: flex; gap: 8px; margin-bottom: 16px;">
                <button class="btn btn-primary" style="flex: 1;" onclick="executeEndpointById('${selectedController}', ${index})">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <polygon points="5,3 19,12 5,21 5,3"></polygon>
                    </svg>
                    Execute Request
                </button>
                <button class="btn btn-secondary" onclick="showExportModal('${selectedController}', ${index})" title="Export request as code">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <polyline points="16,16 12,12 8,16"></polyline>
                        <line x1="12" y1="12" x2="12" y2="21"></line>
                        <path d="M20.39 18.39A5 5 0 0 0 18 9h-1.26A8 8 0 1 0 3 16.3"></path>
                        <polyline points="16,16 12,12 8,16"></polyline>
                    </svg>
                    Export
                </button>
            </div>
            <div id="tryout-response-${endpointIdentifier}" class="response-container" style="margin-top: 16px; display: none;">
                <!-- Response will be displayed here -->
            </div>
        </div>
    `
}

function renderTryItOutParameters(endpoint, endpointIdentifier) {
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
            <input type="text" id="param-${endpointIdentifier}-${param.name}" placeholder="Enter ${param.name}" class="header-input" style="flex: 1;" ${param.defaultValue ? `value="${param.defaultValue}"` : ''}>
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
            <input type="text" id="param-${endpointIdentifier}-${param.name}" placeholder="Enter ${param.name}" class="header-input" style="flex: 1;" ${param.defaultValue ? `value="${param.defaultValue}"` : ''}>
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
            <input type="text" id="param-${endpointIdentifier}-${param.name}" placeholder="Enter ${param.name}" class="header-input" style="flex: 1;" ${param.defaultValue ? `value="${param.defaultValue}"` : ''}>
            <span class="tryout-parameter-type">${param.type}</span>
          </div>
        `).join('')}
      </div>
    `;
  }

  return parametersHtml;
}

function renderTryItOutRequestBody(endpoint, endpointIdentifier) {
  if (!endpoint.requestBody) {
    return '';
  }

  const bodyId = `request-body-${endpointIdentifier}`;
  const keyValueId = `request-body-kv-${endpointIdentifier}`;

  return `
    <div class="tryout-parameter-group">
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px;">
        <h4>Request Body <span class="badge">${endpoint.requestBody.type}</span></h4>
        <select id="bodyEditorMode-${endpointIdentifier}" class="method-select" onchange="switchTryItOutBodyEditorMode('${bodyId}', '${keyValueId}')">
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
  const codeBlock = button.parentElement.nextElementSibling;
  
  const text = codeBlock.textContent;
  navigator.clipboard.writeText(text).then(() => {
    button.innerHTML = "✓";
    setTimeout(() => {
      button.innerHTML = `
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
                    <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
                </svg>
            `;
    }, 2000);
  });
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
  
  const endpointIdentifier = `${controllerName}-${endpointIndex}`;
  await executeEndpoint(endpoint, endpointIdentifier);
}

async function executeEndpoint(endpoint, endpointIdentifier) {
  const responseContainerId = `tryout-response-${endpointIdentifier}`;
  const responseContainer = document.getElementById(responseContainerId);
  
  if (!responseContainer) {
    return;
  }
  
  responseContainer.style.display = 'block';
  responseContainer.innerHTML = '<p>Executing request...</p>';

  const startTime = performance.now();

  try {
    const { url: finalUrl, requestOptions } = buildRequestData({
      endpoint: endpoint,
      endpointIdentifier: endpointIdentifier,
      validateBody: true
    });

    const response = await fetch(finalUrl, requestOptions);
    const responseText = await response.text();
    
    const endTime = performance.now();
    const duration = Math.round(endTime - startTime);
    const requestSize = requestOptions.body ? new Blob([requestOptions.body]).size : 0;
    const responseSize = new Blob([responseText]).size;
    
    let responseData;
    try {
      responseData = JSON.parse(responseText);
    } catch {
      responseData = responseText;
    }

    const statusClass = response.ok ? 'status-2xx' : 'status-4xx';
    
    const responseHtml = `
      <div class="response-success" style="margin-top: 12px;">
        <div class="response-status" style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px;">
          <h5>Response</h5>
          <span class="status-badge ${statusClass}">${response.status} ${response.statusText}</span>
        </div>
        <div class="response-headers" style="margin-bottom: 12px;">
          <h6>Response Headers</h6>
          <pre class="response-headers-pre">${Array.from(response.headers.entries()).map(([key, value]) => `${key}: ${value}`).join('\\n')}</pre>
        </div>
        <div class="response-body">
          <h6>Response Body</h6>
          <div style="position: relative;">
            <div style="position: absolute; top: 8px; right: 8px; z-index: 1; display: flex; gap: 4px;">
              <button class="copy-btn" onclick="copyResponseToClipboard(this)" title="Copy to clipboard">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
                  <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
                </svg>
              </button>
              <button class="copy-btn save-btn" 
                      onclick="saveToFile(this, 'api-response')" 
                      data-endpoint='${JSON.stringify({
                        httpMethodType: endpoint.httpMethodType,
                        path: endpoint.path,
                        methodName: endpoint.methodName
                      })}' 
                      title="Save to file">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"></path>
                  <polyline points="17,21 17,13 7,13 7,21"></polyline>
                  <polyline points="7,3 7,8 15,8"></polyline>
                </svg>
              </button>
            </div>
            <pre class="response-body-pre">${typeof responseData === 'object' ? JSON.stringify(responseData, null, 2) : responseData}</pre>
          </div>
        </div>
      </div>
      ${generatePerformanceHtml(duration, requestSize, responseSize, response.status)}
    `;
    
    responseContainer.innerHTML = responseHtml;

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

function saveToFile(button, type, endpointInfo = null) {
  const buttonContainer = button.parentElement;
  let content;
  
  if (buttonContainer.nextElementSibling) {
    // For cases where buttons are in a container (API responses)
    content = buttonContainer.nextElementSibling.textContent;
  } else {
    // For cases where button is directly positioned (static examples) 
    content = button.nextElementSibling.textContent;
  }
  
  if (!endpointInfo && button.dataset.endpoint) {
    try {
      endpointInfo = JSON.parse(button.dataset.endpoint);
    } catch (e) {
      console.warn('Failed to parse endpoint data:', e);
    }
  }
  
  const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
  let filename;
  
  if (endpointInfo) {
    const method = endpointInfo.httpMethodType.toLowerCase();
    const pathName = endpointInfo.path.replace(/[{}/]/g, '').replace(/\//g, '-').replace(/^-+|-+$/g, '');
    const cleanMethodName = endpointInfo.methodName.replace(/[^a-zA-Z0-9]/g, '');
    filename = `${method}-${pathName || cleanMethodName}-${type}-${timestamp}.json`;
  } else {
    filename = `${type}-${timestamp}.json`;
  }
  
  const blob = new Blob([content], { type: 'application/json' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
  
  const originalIcon = button.innerHTML;
  button.innerHTML = "✓";
  setTimeout(() => {
    button.innerHTML = originalIcon;
  }, 2000);
}

function populateKeyValueEditor(keyValueEditorId, data) {
  const config = {
    onChangeHandler: `updateRequestBodyField('${keyValueEditorId}')`,
    removeFunctionName: 'removeRequestBodyField'
  };
  populateKeyValueContainer(`${keyValueEditorId}-fields`, data, config);
}

function addRequestBodyField(keyValueEditorId) {
  addRequestBodyFieldWithValue(keyValueEditorId, '', '');
}

function addRequestBodyFieldWithValue(keyValueEditorId, key = '', value = '') {
  const config = {
    onChangeHandler: `updateRequestBodyField('${keyValueEditorId}')`,
    removeFunctionName: 'removeRequestBodyField'
  };
  createKeyValueField(`${keyValueEditorId}-fields`, key, value, config);
}

function removeRequestBodyField(button, keyValueEditorId) {
  button.parentElement.remove();
}

function getKeyValueData(keyValueEditorId) {
  return parseKeyValueData(`${keyValueEditorId}-fields`);
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
  const endpointIdentifier = jsonEditorId.replace('request-body-', '');
  const selectElement = document.getElementById(`bodyEditorMode-${endpointIdentifier}`);
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
  const config = {
    rowClassName: 'kv-field-row',
    removeButtonClass: 'kv-remove-btn',
    removeFunctionName: 'removeRequestBuilderBodyField'
  };
  populateKeyValueContainer('requestBodyKvFields', data, config);
}

function addRequestBuilderBodyField() {
  addRequestBuilderBodyFieldWithValue('', '');
}

function addRequestBuilderBodyFieldWithValue(key = '', value = '') {
  const config = {
    rowClassName: 'kv-field-row',
    removeButtonClass: 'kv-remove-btn',
    removeFunctionName: 'removeRequestBuilderBodyField'
  };
  createKeyValueField('requestBodyKvFields', key, value, config);
}

function removeRequestBuilderBodyField(button) {
  button.parentElement.remove();
}

function getRequestBuilderKeyValueData() {
  return parseKeyValueData('requestBodyKvFields');
}

function getRequestBuilderBodyContent() {
  const bodyEditorMode = document.getElementById("bodyEditorMode").value;
  const body = document.getElementById("requestBody").value;
  
  if (bodyEditorMode === 'keyvalue') {
    const keyValueData = getRequestBuilderKeyValueData();
    if (Object.keys(keyValueData).length > 0) {
      return JSON.stringify(keyValueData);
    }
  } else if (body) {
    return body;
  }
  
  return '';
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
  const sendBtn = document.getElementById("sendRequestBtn")

  sendBtn.textContent = "Sending..."
  sendBtn.disabled = true

  const startTime = performance.now();

  try {
    const customHeaders = {}
    requestHeaders.forEach((header) => {
      if (header.key && header.value) {
        customHeaders[header.key] = header.value
      }
    })

    const { requestOptions } = buildRequestData({
      baseUrl: url,
      customHeaders: customHeaders,
      includeAuth: true
    });

    requestOptions.method = method;

    if (method !== "GET") {
      const requestBodyContent = getRequestBuilderBodyContent();
      if (requestBodyContent) {
        requestOptions.body = requestBodyContent;
      }
    }

    const response = await fetch(url, requestOptions)
    const responseText = await response.text()

    const endTime = performance.now();
    const duration = Math.round(endTime - startTime);
    const requestSize = requestOptions.body ? new Blob([requestOptions.body]).size : 0;
    const responseSize = new Blob([responseText]).size;

    const responseContainer = document.getElementById("responseContainer")

    if (response.ok) {
      let formattedResponse;
      try {
        const jsonData = JSON.parse(responseText);
        formattedResponse = JSON.stringify(jsonData, null, 2);
      } catch (e) {
        formattedResponse = responseText;
      }
      
      responseContainer.innerHTML = `
                <div class="response-success">
                    <div class="response-status">
                        <span class="status-badge ${getStatusClass(response.status.toString())}">${response.status} ${response.statusText}</span>
                    </div>
                    <div class="response-body">
                        <h5>Response Body</h5>
                        <div style="position: relative;">
                            <div style="position: absolute; top: 8px; right: 8px; z-index: 1; display: flex; gap: 4px;">
                                <button class="copy-btn" onclick="copyToClipboard(this)" title="Copy to clipboard">
                                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                        <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
                                        <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
                                    </svg>
                                </button>
                                <button class="copy-btn save-btn" onclick="saveToFile(this, 'request-builder-response')" title="Save to file">
                                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                        <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"></path>
                                        <polyline points="17,21 17,13 7,13 7,21"></polyline>
                                        <polyline points="7,3 7,8 15,8"></polyline>
                                    </svg>
                                </button>
                            </div>
                            <pre style="background-color: var(--bg-tertiary); color: var(--text-primary); padding: 12px; border-radius: 6px; margin: 0; transition: all 0.3s ease;">${formattedResponse}</pre>
                        </div>
                    </div>
                </div>
                ${generatePerformanceHtml(duration, requestSize, responseSize, response.status)}
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

function clearSearch() {
  const searchInput = document.getElementById("searchInput")
  const clearBtn = document.getElementById("clearSearchBtn")
  
  searchInput.value = ""
  clearBtn.style.display = "none"
  filterControllers() 
}

function filterControllers() {
  const query = document.getElementById("searchInput").value.toLowerCase()
  const clearBtn = document.getElementById("clearSearchBtn")
  const cards = document.querySelectorAll(".controller-card")
  let visibleControllers = []

  if (query.trim()) {
    clearBtn.style.display = "flex"
  } else {
    clearBtn.style.display = "none"
  }

  cards.forEach((card) => {
    const controllerName = card.querySelector("h3").textContent
    const controller = controllers.find(c => c.name === controllerName)
    
    if (!controller) {
      card.style.display = "none"
      return
    }

    if (!query.trim()) {
      card.style.display = "block"
      visibleControllers.push(controller)
      return
    }

    const hasMatchingEndpoint = controller.endpoints.some(endpoint => 
      doesEndpointMatchQuery(endpoint, query)
    )

    const title = card.querySelector("h3").textContent.toLowerCase()
    const description = card.querySelector("p").textContent.toLowerCase()
    const hasMatchingController = title.includes(query) || description.includes(query)

    if (hasMatchingEndpoint || hasMatchingController) {
      card.style.display = "block"
      visibleControllers.push(controller)
    } else {
      card.style.display = "none"
    }
  })
  
  if (query.trim()) {
    if (visibleControllers.length === 1) {
      if (selectedController !== visibleControllers[0].name) {
        selectedController = visibleControllers[0].name
        expandedEndpoints = []
        renderControllers() 
      }
    } else if (visibleControllers.length > 1) {
      const isCurrentSelectionVisible = visibleControllers.some(c => c.name === selectedController)
      if (!isCurrentSelectionVisible && visibleControllers.length > 0) {
        selectedController = visibleControllers[0].name
        expandedEndpoints = []
        renderControllers() 
      }
    }
  } else {
    if (!selectedController && controllers.length > 0) {
      selectedController = controllers[0].name
      expandedEndpoints = []
      renderControllers()
    }
  }
  
  renderEndpoints()
}

function showExportModal(controllerName, endpointIndex) {
  const requestData = buildRequestDataForExport(controllerName, endpointIndex);
  if (!requestData) {
    alert('Unable to generate export data');
    return;
  }
  
  showGenericExportModal(requestData, 'endpoint');
}

function showRequestBuilderExportModal() {
  const requestData = buildRequestBuilderExportData();
  showGenericExportModal(requestData, 'requestBuilder');
}

function showGenericExportModal(requestData, type = 'endpoint') {
  if (type === 'requestBuilder') {
    window.currentRequestBuilderExportData = requestData;
  } else {
    window.currentExportData = requestData;
  }
  
  updateExportCode('curl', type);
  
  const modalId = type === 'requestBuilder' ? 'requestBuilderExportModal' : 'exportModal';
  document.getElementById(modalId).classList.add('show');
}

function updateExportCode(format, type = 'endpoint') {
  const dataKey = type === 'requestBuilder' ? 'currentRequestBuilderExportData' : 'currentExportData';
  if (!window[dataKey]) return;
  
  const { url, method, headers, body } = window[dataKey];
  let code = '';
  
  switch (format) {
    case 'curl':
      code = generateCurlCode(url, method, headers, body);
      break;
    case 'javascript':
      code = generateJavaScriptCode(url, method, headers, body);
      break;
    case 'python':
      code = generatePythonCode(url, method, headers, body);
      break;
    case 'ruby':
      code = generateRubyCode(url, method, headers, body);
      break;
    case 'csharp':
      code = generateCSharpCode(url, method, headers, body);
      break;
    default:
      code = 'Format not supported';
  }
  
  const codeContentId = type === 'requestBuilder' ? 'requestBuilderExportCodeContent' : 'exportCodeContent';
  document.getElementById(codeContentId).textContent = code;
  
  const modalId = type === 'requestBuilder' ? 'requestBuilderExportModal' : 'exportModal';
  const modal = document.getElementById(modalId);
  modal.querySelectorAll('.export-tab').forEach(tab => {
    tab.classList.remove('active');
  });
  modal.querySelector(`[data-export-format="${format}"]`).classList.add('active');
}

function copyExportCode(type = 'endpoint') {
  const codeContentId = type === 'requestBuilder' ? 'requestBuilderExportCodeContent' : 'exportCodeContent';
  const buttonId = type === 'requestBuilder' ? 'copyRequestBuilderExportBtn' : 'copyExportBtn';
  
  const codeContent = document.getElementById(codeContentId).textContent;
  navigator.clipboard.writeText(codeContent).then(() => {
    const button = document.getElementById(buttonId);
    const originalText = button.innerHTML;
    button.innerHTML = '✓';
    setTimeout(() => {
      button.innerHTML = originalText;
    }, 2000);
  });
}

function downloadExportCode(type = 'endpoint') {
  const modalId = type === 'requestBuilder' ? 'requestBuilderExportModal' : 'exportModal';
  const codeContentId = type === 'requestBuilder' ? 'requestBuilderExportCodeContent' : 'exportCodeContent';
  const buttonId = type === 'requestBuilder' ? 'downloadRequestBuilderExportBtn' : 'downloadExportBtn';
  
  const modal = document.getElementById(modalId);
  const format = modal.querySelector('.export-tab.active').dataset.exportFormat;
  const codeContent = document.getElementById(codeContentId).textContent;
  
  const extensions = {
    curl: 'sh',
    javascript: 'js',
    python: 'py',
    ruby: 'rb',
    csharp: 'cs'
  };
  
  const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
  const prefix = type === 'requestBuilder' ? 'request-builder' : 'api-request';
  const filename = `${prefix}-${format}-${timestamp}.${extensions[format] || 'txt'}`;
  
  const blob = new Blob([codeContent], { type: 'text/plain' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
  
  const button = document.getElementById(buttonId);
  const originalText = button.innerHTML;
  button.innerHTML = '✓';
  setTimeout(() => {
    button.innerHTML = originalText;
  }, 2000);
}

function buildRequestBuilderExportData() {
  const method = document.getElementById("requestMethod").value;
  const url = document.getElementById("requestUrl").value;
  
  const customHeaders = { 'Content-Type': 'application/json' };
  requestHeaders.forEach((header) => {
    if (header.key && header.value) {
      customHeaders[header.key] = header.value;
    }
  });
  
  const authHeaders = getAuthHeaders();
  Object.keys(authHeaders).forEach(key => {
    if (!customHeaders[key]) {
      customHeaders[key] = authHeaders[key];
    }
  });
  
  let requestBody = null;
  if (method !== "GET") {
    const requestBodyContent = getRequestBuilderBodyContent();
    if (requestBodyContent) {
      requestBody = requestBodyContent;
    }
  }
  
  return {
    url: url,
    method: method,
    headers: customHeaders,
    body: requestBody
  };
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
  initializeTheme()
  
  loadAuthConfiguration()
  
  await loadApiData()
  
  renderHeaders()

  document.getElementById("searchInput").addEventListener("input", filterControllers)
  
  // Also add event listener to show/hide clear button on input
  document.getElementById("searchInput").addEventListener("input", function() {
    const clearBtn = document.getElementById("clearSearchBtn")
    if (this.value.trim()) {
      clearBtn.style.display = "flex"
    } else {
      clearBtn.style.display = "none"
    }
  })

  document.getElementById("themeToggleBtn").addEventListener("click", toggleTheme)
  document.getElementById("requestBuilderBtn").addEventListener("click", () => showModal("requestBuilderModal"))
  document.getElementById("authorizeBtn").addEventListener("click", () => showModal("authModal"))
  document.getElementById("exportBtn").addEventListener("click", exportOpenAPI)

  document.getElementById("closeRequestBuilder").addEventListener("click", () => hideModal("requestBuilderModal"))
  document.getElementById("closeAuth").addEventListener("click", () => hideModal("authModal"))
  document.getElementById("closeExport").addEventListener("click", () => hideModal("exportModal"))
  document.getElementById("closeRequestBuilderExport").addEventListener("click", () => hideModal("requestBuilderExportModal"))

  document.getElementById("addHeaderBtn").addEventListener("click", addHeader)
  document.getElementById("sendRequestBtn").addEventListener("click", sendRequest)

  document.getElementById("saveAuthBtn").addEventListener("click", saveAuthConfiguration)
  document.getElementById("clearAuthBtn").addEventListener("click", clearAuthConfiguration)

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
