# GitHub Secrets Setup for Azure Deployment

Follow these steps to update your GitHub repository secrets for Azure deployment:

## 1. Set Up the Publish Profile Secret

1. Go to your GitHub repository
2. Click on **Settings** 
3. Click on **Secrets and variables** in the left sidebar
4. Select **Actions**
5. Click on **New repository secret**
6. For the name, enter: `AZUREAPPSERVICE_PUBLISHPROFILE_84FC86F92F1449178A8F9E2295F21BF1`
7. For the value, paste the entire contents of the Zip Deploy publish profile (but update it with a fresh one from Azure portal)
8. Click **Add secret**

## 2. Get a Fresh Publish Profile (Recommended)

Since you've shared your current publish profile publicly, it's best to generate a new one:

1. Go to the [Azure Portal](https://portal.azure.com)
2. Navigate to your App Service: **data-anomaly-api**
3. In the left sidebar, click on **Deployment Center**
4. Click on **Manage publish profile**
5. Click **Download** to get the new publish profile
6. Update your GitHub secret with this new profile

## 3. Testing the Deployment

After setting up the secret:

1. Go to your GitHub repository's **Actions** tab
2. Find the "Build and deploy ASP.Net Core app to Azure Web App" workflow
3. Click **Run workflow** and select the branch to deploy
4. Monitor the deployment logs for any errors

## Troubleshooting

If deployment still fails, check:

1. App Service logs in Azure Portal
2. Make sure Azure App Service is configured for .NET 9
   - Go to Azure Portal > App Service > Configuration > General settings
   - Ensure .NET 9 is selected as the runtime stack

Remember to NEVER share publish profiles with credentials publicly. 