# PowerShell script to clear Redis cache
# Run this if you have Redis CLI installed

Write-Host "Clearing Redis cache..."

# Try to connect to Redis and flush all keys
try {
    redis-cli FLUSHALL
    Write-Host "Redis cache cleared successfully!"
} catch {
    Write-Host "Could not connect to Redis CLI. Please clear cache manually or restart Redis server."
    Write-Host "Alternative: You can restart your application and the cache will be fresh."
}

Write-Host "Press any key to continue..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")