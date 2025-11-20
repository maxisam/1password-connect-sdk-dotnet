#!/bin/bash
# Code Coverage Report Generator for dotnet-1password
# Runs all tests with coverage collection and generates an HTML report

set -e

echo "Running tests with code coverage..."

# Clean previous test results
rm -rf ./TestResults

# Run tests with coverage collection
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

echo ""
echo "Generating HTML coverage report..."

# Generate HTML report
dotnet reportgenerator \
    -reports:"TestResults/**/coverage.cobertura.xml" \
    -targetdir:"TestResults/CoverageReport" \
    -reporttypes:Html

echo ""
echo "Coverage Summary:"
dotnet reportgenerator \
    -reports:"TestResults/**/coverage.cobertura.xml" \
    -targetdir:"TestResults/CoverageReport" \
    -reporttypes:TextSummary \
    | grep -E "Line coverage|Branch coverage|Method coverage"

echo ""
echo "Coverage report generated at: TestResults/CoverageReport/index.html"
echo "Open the file in a browser to view detailed coverage information."
