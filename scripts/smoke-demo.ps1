param(
    [Parameter(Mandatory = $true)]
    [string] $BaseUrl
)

$ErrorActionPreference = "Stop"
$base = $BaseUrl.TrimEnd("/")
$paths = @(
    "/",
    "/disponibilites",
    "/reserve",
    "/login",
    "/health/live",
    "/health/ready"
)

foreach ($path in $paths) {
    $response = Invoke-WebRequest -UseBasicParsing "$base$path"
    if ($response.StatusCode -ne 200) {
        throw "Smoke check failed for ${path}: HTTP $($response.StatusCode)"
    }

    Write-Host "OK ${path}: HTTP $($response.StatusCode)"
}

$ready = Invoke-WebRequest -UseBasicParsing "$base/health/ready" -MaximumRedirection 0
if ($ready.StatusCode -ne 200) {
    throw "Health gate failed: /health/ready returned HTTP $($ready.StatusCode)"
}

Write-Host "Demo smoke checks passed for $base."
