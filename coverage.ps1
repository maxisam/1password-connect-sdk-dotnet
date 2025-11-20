# Code Coverage Report Generator for dotnet-1password
# Runs all tests with coverage collection and generates an HTML report

Write-Host "Running tests with code coverage..." -ForegroundColor Cyan

# Clean previous test results
if (Test-Path "./TestResults") {
    Remove-Item -Recurse -Force "./TestResults"
}

# Run tests with coverage collection
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

if ($LASTEXITCODE -ne 0) {
    Write-Host "Tests failed. Coverage report not generated." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "`nGenerating HTML coverage report..." -ForegroundColor Cyan

# Generate HTML report
dotnet reportgenerator `
    -reports:"TestResults/**/coverage.cobertura.xml" `
    -targetdir:"TestResults/CoverageReport" `
    -reporttypes:Html

Write-Host "`nCoverage Summary:" -ForegroundColor Cyan
dotnet reportgenerator `
    -reports:"TestResults/**/coverage.cobertura.xml" `
    -targetdir:"TestResults/CoverageReport" `
    -reporttypes:TextSummary `
    | Select-String -Pattern "Line coverage|Branch coverage|Method coverage"

Write-Host "`nCoverage report generated at: TestResults/CoverageReport/index.html" -ForegroundColor Green
Write-Host "Open the file in a browser to view detailed coverage information.`n" -ForegroundColor Green
