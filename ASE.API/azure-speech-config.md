# Azure Speech Service Configuration Guide

## Overview
This document provides instructions for configuring the Azure Speech Service integration with the Data Anomalies Detector AI application.

## Prerequisites
- An Azure account with access to Azure Cognitive Services
- The Azure Speech Service resource created (already done: 'ase-query-builder')

## Configuration Steps

### 1. Get the Speech Service Key
1. Go to the Azure Portal: https://portal.azure.com
2. Navigate to your Speech Service resource ('ase-query-builder')
3. Go to "Keys and Endpoint" in the left menu
4. Copy either Key 1 or Key 2

### 2. Update Application Configuration
Add the key to your application by one of these methods:

#### Option A: Update appsettings.json
```json
"AzureSpeech": {
  "SubscriptionKey": "YOUR_SUBSCRIPTION_KEY_HERE",
  "Region": "westeurope",
  "Endpoint": "https://westeurope.stt.speech.microsoft.com"
}
```

#### Option B: Use User Secrets (Development)
```bash
cd ASE.API
dotnet user-secrets set "AzureSpeech:SubscriptionKey" "YOUR_SUBSCRIPTION_KEY_HERE"
```

#### Option C: Use Environment Variables (Production)
For production deployments, set these environment variables:
- `AzureSpeech__SubscriptionKey`
- `AzureSpeech__Region`
- `AzureSpeech__Endpoint`

### 3. Testing the Integration
After configuration, the AI Query Builder will use Azure Speech Services for speech-to-text functionality.

## Feature Details
The Speech-to-Text integration enables:
- Voice-driven querying of business data
- Verbal commands for anomaly detection
- Hands-free data analysis

## Troubleshooting
- Check logs for any Azure Speech Service error messages
- Verify the subscription key is correct
- Ensure the service region matches your deployment (westeurope)
- Confirm your Azure Speech Service F0 tier limits (free tier allows 5 hours of audio per month) 