# Data Anomalies Detector AI

An AI-powered application for detecting anomalies in data and generating database queries.

## Deployment to GitHub Pages

The UI of this application is configured to be deployed to GitHub Pages. The deployment process is automated using GitHub Actions.

### Prerequisites for GitHub Pages Deployment

1. Enable GitHub Pages in your repository:
   - Go to your repository settings
   - Scroll down to the "GitHub Pages" section
   - Select "GitHub Actions" as the source

2. Make sure your repository has the necessary GitHub Actions permissions:
   - Go to your repository settings
   - Navigate to "Actions" > "General"
   - Ensure "Read and write permissions" is selected under "Workflow permissions"

### Automatic Deployment

The UI is automatically deployed to GitHub Pages when:
- Code is pushed to the main branch
- The workflow is manually triggered

The deployment process:
1. Builds the Angular application
2. Configures it for GitHub Pages hosting
3. Uploads and deploys it to GitHub Pages

### Accessing the Deployed Application

Once deployed, the application will be available at:
`https://[your-github-username].github.io/DataAnomaliesDetectorAI/`

### Configuring the API URL

Before deploying to GitHub Pages, update the API URL in `ASE.UI/src/app/core/services/api-config.service.ts` to point to your deployed API instance. The current configuration checks if the application is running on GitHub Pages and uses a different API URL accordingly.

## Local Development Setup

### UI (Angular)

```bash
cd ASE.UI
npm install
npm start
```

### API (.NET Core)

```bash
cd ASE.API
dotnet restore
dotnet run
```

## Features

- AI-powered query generation
- Speech-to-text query input
- Data anomaly detection
- Dealer management
- Finance submissions tracking
- Master templates 