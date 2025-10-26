# Quick Start - GitHub CI/CD Setup

## ✅ What's Been Done

1. ✅ Git repository initialized
2. ✅ `.gitignore` configured for .NET projects
3. ✅ GitHub Actions workflow created (`.github/workflows/azure-deploy.yml`)
4. ✅ Azure publish profile retrieved
5. ✅ Documentation created (README.md, DEPLOYMENT.md)

## 📋 Next Steps (Your Action Items)

### Step 1: Create GitHub Repository

1. Go to https://github.com/new
2. Repository name: `eidos` or `ontology-builder` (your choice)
3. **Important**: DO NOT initialize with README, .gitignore, or license
4. Click "Create repository"

### Step 2: Push Code to GitHub

```bash
cd /Users/benjaminhoffman/Documents/code/ontology-builder

# Add GitHub remote (replace USERNAME and REPO_NAME with yours)
git remote add origin https://github.com/USERNAME/REPO_NAME.git

# Stage all files
git add .

# Create initial commit
git commit -m "Initial commit: Eidos Ontology Builder with CI/CD"

# Push to GitHub
git branch -M main
git push -u origin main
```

### Step 3: Configure GitHub Secret

**The Azure publish profile is already copied to your clipboard!**

1. Go to your repository on GitHub
2. Click **Settings** (top menu)
3. Click **Secrets and variables** → **Actions** (left sidebar)
4. Click **"New repository secret"**
5. Enter:
   - **Name**: `AZURE_WEBAPP_PUBLISH_PROFILE`
   - **Value**: Paste from clipboard (Cmd+V)
6. Click **"Add secret"**

### Step 4: Enable GitHub Actions

1. Go to the **Actions** tab in your repository
2. You should see "Build and Deploy to Azure" workflow
3. It will run automatically on the next push

## 🎯 How It Works

### On Pull Requests
```
PR Created → Build → Test → ✅ Success (NO deployment)
```

### On Merge to Main
```
Merge to Main → Build → Test → Deploy to Azure → ✅ Live at eidosonto.com
```

## 🚀 Your First Deployment

After setting up the GitHub repository and secrets:

```bash
# Create a feature branch
git checkout -b feature/test-cicd

# Make a small change (e.g., update README)
echo "# Testing CI/CD" >> README.md
git add README.md
git commit -m "Test: Verify CI/CD pipeline"

# Push to GitHub
git push -u origin feature/test-cicd

# Go to GitHub and create a Pull Request
# Watch the CI build automatically!

# After CI passes, merge the PR
# Watch it automatically deploy to production!
```

## 📊 Monitoring

### View Workflow Status
- GitHub → Your repo → **Actions** tab
- Click on any workflow run to see details

### View Azure Logs
```bash
# Live logs
az webapp log tail --name eidos --resource-group eidos_group

# Download logs
az webapp log download --name eidos --resource-group eidos_group --log-file logs.zip
```

## 🔒 Recommended: Branch Protection

Prevent accidental direct pushes to main:

1. GitHub → Settings → Branches
2. Add branch protection rule for `main`:
   - ✅ Require a pull request before merging
   - ✅ Require status checks to pass
   - ✅ Require branches to be up to date
3. Save changes

Now you **must** use PRs - no direct pushes to main!

## 📝 Important Files

- `.github/workflows/azure-deploy.yml` - CI/CD workflow
- `.gitignore` - Excludes build artifacts
- `DEPLOYMENT.md` - Detailed deployment guide
- `README.md` - Project documentation

## 🆘 Troubleshooting

### "Secret not found" error
- Verify `AZURE_WEBAPP_PUBLISH_PROFILE` is correctly set in GitHub Secrets
- Check for typos in the secret name
- Ensure secret value is the complete XML content

### Workflow doesn't trigger
- Check you've pushed to the `main` branch
- Verify GitHub Actions is enabled (Actions tab)
- Check workflow file is in `.github/workflows/` directory

### Deployment fails
- Check Azure App Service is running
- Verify publish profile is valid
- Check application logs in Azure

## 🎉 Success Criteria

After completing setup, you should see:

1. ✅ Code pushed to GitHub
2. ✅ Workflow visible in Actions tab
3. ✅ First workflow run completed successfully
4. ✅ Application deployed to https://eidosonto.com

---

**Need help?** Check [DEPLOYMENT.md](DEPLOYMENT.md) for detailed instructions.
