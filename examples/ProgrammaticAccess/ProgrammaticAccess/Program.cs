// Example: Programmatic Access to 1Password Secrets
// Demonstrates direct usage of OnePasswordClient to retrieve secrets

using OnePassword.Sdk;
using OnePassword.Sdk.Client;
using OnePassword.Sdk.Exceptions;

Console.WriteLine("1Password .NET SDK - Programmatic Access Example");
Console.WriteLine("=================================================\n");

// Step 1: Configure client options
// In production, read these from environment variables!
var connectServer = Environment.GetEnvironmentVariable("ONEPASSWORD_CONNECT_SERVER")
    ?? "https://localhost:8080"; // Default for local testing

var token = Environment.GetEnvironmentVariable("ONEPASSWORD_TOKEN")
    ?? throw new InvalidOperationException(
        "ONEPASSWORD_TOKEN environment variable not set. " +
        "Set it to your 1Password Connect access token.");

var options = new OnePasswordClientOptions
{
    ConnectServer = connectServer,
    Token = token,
    Timeout = TimeSpan.FromSeconds(10),
    MaxRetries = 3
};

Console.WriteLine($"Connecting to: {connectServer}");
Console.WriteLine("Note: Token is read from ONEPASSWORD_TOKEN environment variable\n");

// Step 2: Create the client
using var client = new OnePasswordClient(options);

try
{
    // Example 1: List all accessible vaults
    Console.WriteLine("Example 1: Listing all vaults");
    Console.WriteLine("------------------------------");
    var vaults = await client.ListVaultsAsync();

    foreach (var vault in vaults)
    {
        Console.WriteLine($"  - {vault.Title} (ID: {vault.Id})");
    }
    Console.WriteLine();

    // Example 2: Get a specific vault
    var firstVault = vaults.FirstOrDefault();
    if (firstVault != null)
    {
        Console.WriteLine($"Example 2: Getting vault '{firstVault.Title}' by ID");
        Console.WriteLine("------------------------------");
        var vault = await client.GetVaultAsync(firstVault.Id);
        Console.WriteLine($"  Vault: {vault.Title}");
        Console.WriteLine($"  ID: {vault.Id}");
        Console.WriteLine($"  Description: {vault.Description ?? "(none)"}");
        Console.WriteLine();

        // Example 3: List items in the vault
        Console.WriteLine($"Example 3: Listing items in vault '{vault.Title}'");
        Console.WriteLine("------------------------------");
        var items = await client.ListItemsAsync(vault.Id);

        Console.WriteLine($"  Found {items.Count()} items:");
        foreach (var item in items.Take(5)) // Show first 5 items
        {
            Console.WriteLine($"    - {item.Title} (Category: {item.Category})");
        }

        if (items.Count() > 5)
        {
            Console.WriteLine($"    ... and {items.Count() - 5} more");
        }
        Console.WriteLine();

        // Example 4: Get a specific item
        var firstItem = items.FirstOrDefault();
        if (firstItem != null)
        {
            Console.WriteLine($"Example 4: Getting item '{firstItem.Title}'");
            Console.WriteLine("------------------------------");
            var item = await client.GetItemAsync(vault.Id, firstItem.Id);

            Console.WriteLine($"  Title: {item.Title}");
            Console.WriteLine($"  Category: {item.Category}");
            Console.WriteLine($"  Fields:");

            foreach (var field in item.Fields.Take(5)) // Show first 5 fields
            {
                // Note: Field.ToString() never includes the value (security feature)
                Console.WriteLine($"    - {field.Label ?? field.Id}: {field.Type}");

                // To get the actual value (for non-sensitive demo):
                if (field.Type != Models.FieldType.CONCEALED)
                {
                    Console.WriteLine($"      Value: {field.Value ?? "(empty)"}");
                }
                else
                {
                    Console.WriteLine($"      Value: *** (concealed field, not displayed)");
                }
            }
            Console.WriteLine();

            // Example 5: Get a specific secret value
            var passwordField = item.Fields.FirstOrDefault(f => f.Type == Models.FieldType.CONCEALED);
            if (passwordField != null)
            {
                Console.WriteLine($"Example 5: Retrieving a secret field");
                Console.WriteLine("------------------------------");
                Console.WriteLine($"  Field: {passwordField.Label ?? passwordField.Id}");
                Console.WriteLine($"  Type: {passwordField.Type}");
                Console.WriteLine($"  Value: *** (not displayed for security)");
                Console.WriteLine($"  Note: In your app, you would use this value to connect to services");
                Console.WriteLine();
            }
        }

        // Example 6: Batch retrieve multiple secrets using op:// URIs
        Console.WriteLine("Example 6: Batch retrieve secrets");
        Console.WriteLine("------------------------------");

        // Construct op:// URIs for the first few items
        var references = items.Take(3)
            .SelectMany(item => item.Fields.Take(1)
                .Select(field => $"op://{vault.Title}/{item.Title}/{field.Label ?? field.Id}"))
            .ToList();

        if (references.Any())
        {
            Console.WriteLine($"  Retrieving {references.Count} secrets in batch:");
            foreach (var reference in references)
            {
                Console.WriteLine($"    - {reference}");
            }

            var secrets = await client.GetSecretsAsync(references);

            Console.WriteLine($"\n  Retrieved {secrets.Count} secrets successfully");
            Console.WriteLine($"  Note: Secret values are not displayed for security");
            Console.WriteLine();
        }
    }

    Console.WriteLine("✅ All examples completed successfully!");
}
catch (AuthenticationException ex)
{
    Console.WriteLine($"❌ Authentication failed: {ex.Message}");
    Console.WriteLine("   Check that your token is valid and not expired.");
}
catch (VaultNotFoundException ex)
{
    Console.WriteLine($"❌ Vault not found: {ex.Message}");
    Console.WriteLine($"   Vault ID: {ex.VaultId}");
}
catch (ItemNotFoundException ex)
{
    Console.WriteLine($"❌ Item not found: {ex.Message}");
    Console.WriteLine($"   Vault: {ex.VaultId}, Item: {ex.ItemId}");
}
catch (FieldNotFoundException ex)
{
    Console.WriteLine($"❌ Field not found: {ex.Message}");
    Console.WriteLine($"   Field: {ex.FieldLabel}");
}
catch (NetworkException ex)
{
    Console.WriteLine($"❌ Network error: {ex.Message}");
    Console.WriteLine($"   Retry attempts: {ex.RetryAttempts}");
    Console.WriteLine("   Check that your Connect server is accessible.");
}
catch (OnePasswordException ex)
{
    Console.WriteLine($"❌ 1Password error: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Unexpected error: {ex.Message}");
}
finally
{
    Console.WriteLine("\nExample completed. Press any key to exit...");
    Console.ReadKey();
}
