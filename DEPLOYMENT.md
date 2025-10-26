# Deployment Guide - GitHub Actions CI/CD

This document explains how to set up automated deployment to Azure using GitHub Actions.

## Overview

The CI/CD pipeline is configured to:
- **On Pull Requests**: Build and test the application
- **On Merge to Main**: Build, test, AND deploy to Azure (https://eidosonto.com)

## Initial Setup

### 1. Create GitHub Repository

1. Go to https://github.com/new
2. Create a new repository (e.g., `ontology-builder` or `eidos`)
3. **DO NOT** initialize with README, .gitignore, or license (we have those locally)

### 2. Connect Local Repository to GitHub

```bash
cd /Users/benjaminhoffman/Documents/code/ontology-builder

# Add GitHub remote (replace with your repository URL)
git remote add origin https://github.com/YOUR_USERNAME/YOUR_REPO_NAME.git

# Add all files
git add .

# Create initial commit
git commit -m "Initial commit: Eidos Ontology Builder with CI/CD"

# Push to GitHub
git branch -M main
git push -u origin main
```

### 3. Configure GitHub Secrets

The deployment workflow requires one secret to be configured in your GitHub repository:

#### `AZURE_WEBAPP_PUBLISH_PROFILE`

This secret contains the Azure App Service publish profile for secure deployment.

**To add this secret:**

1. The publish profile has been saved to: `/tmp/azure-publish-profile.xml`

2. Copy the contents:
   ```bash
   cat /tmp/azure-publish-profile.xml | pbcopy
   ```
   (This copies the file contents to your clipboard)

3. Go to your GitHub repository
4. Navigate to: **Settings** → **Secrets and variables** → **Actions**
5. Click **"New repository secret"**
6. Name: `AZURE_WEBAPP_PUBLISH_PROFILE`
7. Value: Paste the contents from your clipboard
8. Click **"Add secret"**

### 4. Enable GitHub Actions

1. Go to your repository on GitHub
2. Click the **"Actions"** tab
3. You should see the "Build and Deploy to Azure" workflow
4. It will run automatically on the next push to main or PR creation

## Workflow Behavior

### Pull Requests
When you create a PR against `main`:
- ✅ Builds the application
- ✅ Runs tests (if available)
- ❌ Does NOT deploy to Azure

### Merges to Main
When you merge a PR into `main`:
- ✅ Builds the application
- ✅ Runs tests
- ✅ **Deploys to Azure** (https://eidosonto.com)

## Development Workflow

### Working on a Feature

```bash
# Create a new feature branch
git checkout -b feature/my-new-feature

# Make your changes
# ... edit files ...

# Commit changes
git add .
git commit -m "Add my new feature"

# Push branch to GitHub
git push -u origin feature/my-new-feature

# Create Pull Request on GitHub
# The CI will automatically build and test your changes
```

### Deploying to Production

```bash
# After PR is approved and passes CI checks:
# Merge the PR on GitHub (via the web interface)

# The deployment workflow will automatically:
# 1. Build the app
# 2. Run tests
# 3. Deploy to Azure
# 4. Update https://eidosonto.com
```

## Branch Protection Rules (Recommended)

To ensure code quality and prevent accidental deployments, set up branch protection:

1. Go to: **Settings** → **Branches**
2. Click **"Add branch protection rule"**
3. Branch name pattern: `main`
4. Enable:
   - ✅ Require a pull request before merging
   - ✅ Require approvals (1 minimum)
   - ✅ Require status checks to pass before merging
   - ✅ Require branches to be up to date before merging
   - ✅ Include administrators (optional but recommended)
5. Save changes

With these rules, you **cannot** push directly to main - you must create a PR first.

## Monitoring Deployments

### View Deployment Status
1. Go to **Actions** tab on GitHub
2. Click on the latest workflow run
3. Expand the "Deploy to Azure" job to see deployment details

### View Application Logs
```bash
# Stream live logs from Azure
az webapp log tail --name eidos --resource-group eidos_group

# Download logs
az webapp log download --name eidos --resource-group eidos_group --log-file logs.zip
```

### Rollback a Deployment

If you need to rollback to a previous version:

```bash
# List recent deployments
az webapp deployment list --name eidos --resource-group eidos_group --query "[].{id:id, author:author, status:status, message:message}" --output table

# Redeploy a specific commit
# (You can trigger this by re-running the workflow in GitHub Actions)
```

## Troubleshooting

### Workflow Fails on Deployment
- Check that `AZURE_WEBAPP_PUBLISH_PROFILE` secret is correctly set
- Verify the secret hasn't expired (regenerate if needed)
- Check Azure App Service is running

### Regenerate Publish Profile
If the publish profile expires or is compromised:

```bash
# Download new publish profile
az webapp deployment list-publishing-profiles \
  --name eidos \
  --resource-group eidos_group \
  --xml > new-publish-profile.xml

# Update the GitHub secret with the new profile contents
```

## Security Best Practices

1. ✅ Never commit secrets to the repository
2. ✅ Use GitHub Secrets for sensitive data
3. ✅ Enable branch protection on `main`
4. ✅ Require PR reviews before merging
5. ✅ Keep dependencies up to date
6. ✅ Monitor deployment logs for issues

## Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Azure App Service Deployment](https://docs.microsoft.com/en-us/azure/app-service/deploy-github-actions)
- [Blazor Deployment Guide](https://docs.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/)

## Next Steps

After setting up the repository and secrets:

1. Create a feature branch for your next change
2. Make changes and push to GitHub
3. Create a PR and watch the CI build automatically
4. After approval, merge to main
5. Watch your changes automatically deploy to production!

Happy coding! 🚀
