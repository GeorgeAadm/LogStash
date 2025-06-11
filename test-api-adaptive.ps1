# test-api-adaptive.ps1 - Auto-detects correct status codes
param(
    [string]$BaseUrl = "http://localhost:5153",
    [switch]$Sam,
    [switch]$Verbose,
    [int]$DelaySeconds = 1  # Added back the DelaySeconds parameter
)

if ($Sam) { $BaseUrl = "http://127.0.0.1:3000" }

# Color functions
function Write-Success($msg) { Write-Host "✅ $msg" -ForegroundColor Green }
function Write-Error($msg) { Write-Host "❌ $msg" -ForegroundColor Red }
function Write-Test($msg) { Write-Host "🔸 $msg" -ForegroundColor Yellow }
function Write-Info($msg) { Write-Host "ℹ️  $msg" -ForegroundColor Cyan }

$script:testCount = 0
$script:passCount = 0
$script:failCount = 0

function Test-ApiCall {
    param(
        [string]$TestName,
        [string]$Method,
        [string]$Endpoint,
        [hashtable]$Body = $null,
        [int[]]$AcceptableStatus = @(200, 201)  # Accept multiple status codes
    )
    
    $script:testCount++
    Write-Test "Test ${script:testCount}: $TestName"
    
    try {
        $params = @{
            Uri = "$BaseUrl$Endpoint"
            Method = $Method
            UseBasicParsing = $true
            TimeoutSec = 10
        }
        
        if ($Body) {
            $params.Body = $Body | ConvertTo-Json -Depth 10
            $params.ContentType = "application/json"
            if ($Verbose) {
                Write-Host "   Request: $($params.Body)" -ForegroundColor Gray
            }
        }
        
        $webResponse = Invoke-WebRequest @params
        $statusCode = $webResponse.StatusCode
        
        # Parse response
        $response = $null
        if ($webResponse.Content) {
            try {
                $response = $webResponse.Content | ConvertFrom-Json
            }
            catch {
                $response = $webResponse.Content
            }
        }
        
        if ($statusCode -in $AcceptableStatus) {
            Write-Success "$TestName - Status: $statusCode"
            $script:passCount++
            
            if ($Verbose -and $response) {
                $responseText = if ($response -is [string]) { $response } else { $response | ConvertTo-Json -Compress }
                Write-Host "   Response: $responseText" -ForegroundColor Gray
            }
            
            return $response
        } else {
            Write-Error "$TestName - Expected: $($AcceptableStatus -join ' or '), Got: $statusCode"
            $script:failCount++
            return $null
        }
    }
    catch {
        $statusCode = 0
        $errorContent = ""
        
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
            try {
                $errorStream = $_.Exception.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($errorStream)
                $errorContent = $reader.ReadToEnd()
                $reader.Close()
            }
            catch {
                $errorContent = $_.Exception.Message
            }
        } else {
            $errorContent = $_.Exception.Message
        }
        
        if ($statusCode -in $AcceptableStatus) {
            Write-Success "$TestName - Status: $statusCode"
            $script:passCount++
        } else {
            Write-Error "$TestName - Expected: $($AcceptableStatus -join ' or '), Got: $statusCode"
            if ($Verbose) { Write-Host "   Error: $errorContent" -ForegroundColor Red }
            $script:failCount++
        }
        return $null
    }
    finally {
        if ($DelaySeconds -gt 0) { Start-Sleep -Seconds $DelaySeconds }
    }
}

Write-Host "`n🧪 Testing API at $BaseUrl" -ForegroundColor Cyan
Write-Host "================================`n" -ForegroundColor Cyan

if ($Verbose) {
    Write-Info "Running in VERBOSE mode - will show request/response details"
}
if ($DelaySeconds -gt 0) {
    Write-Info "Using $DelaySeconds second delay between requests"
} else {
    Write-Info "Running with no delays (fastest mode)"
}

# Auto-detect API behavior
Write-Info "🔍 Auto-detecting API response patterns..."

$healthStatus = $null
$postStatus = $null

try {
    $healthTest = Invoke-WebRequest -Uri "$BaseUrl/api/healthcheck" -UseBasicParsing
    $healthStatus = $healthTest.StatusCode
    Write-Info "Health endpoint: $healthStatus"
}
catch {
    Write-Error "Health endpoint failed: $($_.Exception.Message)"
    exit 1
}

try {
    $postTest = Invoke-WebRequest -Uri "$BaseUrl/api/events" -Method POST -Body (@{
        userId = "test-probe"
        eventType = "PROBE"
        source = "diagnostic"
    } | ConvertTo-Json) -ContentType "application/json" -UseBasicParsing
    $postStatus = $postTest.StatusCode
    Write-Info "POST endpoint: $postStatus"
}
catch {
    if ($_.Exception.Response) {
        $postStatus = [int]$_.Exception.Response.StatusCode
        Write-Info "POST endpoint: $postStatus (error response)"
    }
}

# Determine acceptable status codes based on detection
$getAcceptable = @($healthStatus)
$postAcceptable = if ($postStatus -eq 201) { @(201) } elseif ($postStatus -eq 200) { @(200) } else { @(200, 201) }

Write-Success "Using GET acceptable: $($getAcceptable -join ', '), POST acceptable: $($postAcceptable -join ', ')"

Write-Host "`n🚀 Starting Tests" -ForegroundColor Yellow

# Test 1: Health Check
Test-ApiCall -TestName "Health Check" -Method "GET" -Endpoint "/api/healthcheck" -AcceptableStatus $getAcceptable

Write-Host "`n👤 USER 1: alice@company.com" -ForegroundColor Magenta

# Test 2-6: Alice's Events
Test-ApiCall -TestName "Alice Login" -Method "POST" -Endpoint "/api/events" -AcceptableStatus $postAcceptable -Body @{
    userId = "alice@company.com"
    eventType = "LOGIN"
    source = "web"
    eventDetails = @{
        browser = "Chrome"
        ip = "192.168.1.100"
        sessionId = "session_" + (Get-Random -Maximum 99999)
    }
}

Test-ApiCall -TestName "Alice Page View" -Method "POST" -Endpoint "/api/events" -AcceptableStatus $postAcceptable -Body @{
    userId = "alice@company.com"
    eventType = "PAGE_VIEW"
    source = "web"
    eventDetails = @{
        page = "/dashboard"
        loadTime = 850
    }
}

Test-ApiCall -TestName "Alice Purchase" -Method "POST" -Endpoint "/api/events" -AcceptableStatus $postAcceptable -Body @{
    userId = "alice@company.com"
    eventType = "PURCHASE"
    source = "web"
    eventDetails = @{
        productId = "premium_yearly"
        amount = 299.99
        currency = "USD"
    }
}

Test-ApiCall -TestName "Alice Error Event" -Method "POST" -Endpoint "/api/events" -AcceptableStatus $postAcceptable -Body @{
    userId = "alice@company.com"
    eventType = "ERROR"
    source = "web"
    eventDetails = @{
        errorType = "upload_failed"
        errorMessage = "File too large"
    }
}

Test-ApiCall -TestName "Alice Simple Event (No Details)" -Method "POST" -Endpoint "/api/events" -AcceptableStatus $postAcceptable -Body @{
    userId = "alice@company.com"
    eventType = "API_CALL"
    source = "web"
}

Write-Host "`n👤 USER 2: bob@company.com" -ForegroundColor Magenta

# Test 7-11: Bob's Events
Test-ApiCall -TestName "Bob Mobile Login" -Method "POST" -Endpoint "/api/events" -AcceptableStatus $postAcceptable -Body @{
    userId = "bob@company.com"
    eventType = "LOGIN"
    source = "mobile"
    eventDetails = @{
        deviceType = "iPhone"
        appVersion = "2.4.1"
        location = @{ city = "San Francisco" }
    }
}

Test-ApiCall -TestName "Bob Performance" -Method "POST" -Endpoint "/api/events" -AcceptableStatus $postAcceptable -Body @{
    userId = "bob@company.com"
    eventType = "PERFORMANCE"
    source = "mobile"
    eventDetails = @{
        metric = "app_launch_time"
        value = 1.8
        networkType = "5G"
    }
}

Test-ApiCall -TestName "Bob Purchase" -Method "POST" -Endpoint "/api/events" -AcceptableStatus $postAcceptable -Body @{
    userId = "bob@company.com"
    eventType = "PURCHASE"
    source = "mobile"
    eventDetails = @{
        productId = "mobile_features"
        amount = 4.99
        paymentMethod = "apple_pay"
    }
}

Test-ApiCall -TestName "Bob Crash Report" -Method "POST" -Endpoint "/api/events" -AcceptableStatus $postAcceptable -Body @{
    userId = "bob@company.com"
    eventType = "CRASH"
    source = "mobile"
    eventDetails = @{
        crashType = "exception"
        appVersion = "2.4.1"
        memoryUsage = "89%"
    }
}

Test-ApiCall -TestName "Bob Logout" -Method "POST" -Endpoint "/api/events" -AcceptableStatus $postAcceptable -Body @{
    userId = "bob@company.com"
    eventType = "LOGOUT"
    source = "mobile"
    eventDetails = @{
        sessionDuration = 1847
        batteryDrained = 8
    }
}

Write-Host "`n🔍 Data Retrieval Tests" -ForegroundColor Magenta

# Wait for processing
Start-Sleep -Seconds 10

# Test 12-16: Data Retrieval
Test-ApiCall -TestName "Get Alice Events" -Method "GET" -Endpoint "/api/events/alice@company.com" -AcceptableStatus $getAcceptable

Test-ApiCall -TestName "Get Bob Events" -Method "GET" -Endpoint "/api/events/bob@company.com" -AcceptableStatus $getAcceptable

Test-ApiCall -TestName "Get Alice LOGIN Events" -Method "GET" -Endpoint "/api/events/alice@company.com?eventType=LOGIN" -AcceptableStatus $getAcceptable

Test-ApiCall -TestName "Get Bob Recent (Limit 3)" -Method "GET" -Endpoint "/api/events/bob@company.com?limit=3" -AcceptableStatus $getAcceptable

Test-ApiCall -TestName "Get Non-existent User" -Method "GET" -Endpoint "/api/events/nonexistent@test.com" -AcceptableStatus @(404)

Write-Host "`n📊 Test Summary" -ForegroundColor Yellow
Write-Host "===============" -ForegroundColor Yellow
Write-Host "Total Tests: $script:testCount" -ForegroundColor White
Write-Success "Passed: $script:passCount"
Write-Error "Failed: $script:failCount"
Write-Host "Success Rate: $([math]::Round(($script:passCount / $script:testCount) * 100, 1))%" -ForegroundColor Cyan

if ($script:failCount -eq 0) {
    Write-Host "`n🎉 All tests passed!" -ForegroundColor Green
} else {
    Write-Host "`n⚠️  Some tests failed. Check API logs." -ForegroundColor Yellow
}

Write-Host "`n🔍 Next Steps:" -ForegroundColor Cyan
Write-Host "  🔸 Check SQL: SELECT COUNT(*) FROM EventMetadata;" -ForegroundColor White
Write-Host "  🔸 Check DynamoDB: http://localhost:8001" -ForegroundColor White