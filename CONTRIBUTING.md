# Contributing to HitMarker

Thank you for your interest in contributing to HitMarker! This document provides guidelines and instructions for contributing.

## Code of Conduct

- Be respectful and inclusive
- Provide constructive feedback
- Focus on what is best for the community
- Show empathy towards other community members

## How to Contribute

### Reporting Bugs

Before creating bug reports, please check existing issues to avoid duplicates. When creating a bug report, include:

- A clear and descriptive title
- Steps to reproduce the issue
- Expected behavior vs actual behavior
- Screenshots or videos if applicable
- Your Raft version and RaftModLoader version
- Any error messages from the console

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion, include:

- A clear and descriptive title
- Detailed description of the proposed feature
- Explanation of why this enhancement would be useful
- Possible implementation approach (optional)

### Pull Requests

1. Fork the repository
2. Create a new branch from `main`:
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. Make your changes:
   - Follow the existing code style
   - Add comments for complex logic
   - Update documentation if needed
   - Test your changes thoroughly

4. Commit your changes:
   ```bash
   git commit -m "Add feature: description of your changes"
   ```

5. Push to your fork:
   ```bash
   git push origin feature/your-feature-name
   ```

6. Open a Pull Request with:
   - Clear title and description
   - Reference to related issues
   - Screenshots/videos for UI changes
   - Test results

## Development Setup

1. Install prerequisites:
   - Visual Studio 2019 or later
   - .NET Framework 4.8
   - Raft game
   - RaftModLoader

2. Clone your fork:
   ```bash
   git clone https://github.com/YOUR_USERNAME/HitMarker.git
   cd HitMarker
   ```

3. Update reference paths in `HitMarker/HitMarker.csproj` to match your Raft installation

4. Build and test:
   ```bash
   msbuild HitMarker.sln /p:Configuration=Debug
   ```

## Code Style Guidelines

- Use meaningful variable and method names
- Add XML documentation comments for public methods
- Keep methods focused and concise
- Handle exceptions appropriately
- Use `try-catch` blocks for error-prone operations
- Log errors with descriptive messages using `Debug.LogError`

### Example:

```csharp
/// <summary>
/// Shows the hitmarker with fade animation
/// </summary>
public void ShowHitmarker()
{
    try
    {
        if (hitmarkerImage == null || imageComponent == null)
        {
            Debug.LogError("[HitMarker] Cannot show hitmarker: UI components not initialized");
            return;
        }
        
        // Implementation...
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"[HitMarker] Error in ShowHitmarker: {ex.Message}");
    }
}
```

## Testing

- Test your changes in-game before submitting
- Verify the mod works with different weapons
- Check for performance impact
- Test with and without other mods installed
- Ensure no errors appear in the console

## Commit Message Guidelines

- Use present tense ("Add feature" not "Added feature")
- Use imperative mood ("Move cursor to..." not "Moves cursor to...")
- Limit first line to 72 characters
- Reference issues and pull requests when relevant

Examples:
```
Add configurable hitmarker size option
Fix hitmarker not appearing for spear hits
Update README with new configuration options
```

## Questions?

Feel free to add me on discord: flazeiguess

## License

By contributing, you agree that your contributions will be licensed under the GNU AGPLv3 License.
