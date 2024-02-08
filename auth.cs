from msal import ConfidentialClientApplication
import requests
import json

# Authentication details (replace with your own)
client_id = "your_client_id"
client_secret = "your_client_secret"
tenant_id = "your_tenant_id"
resource_url = "https://yourorganization.crm.dynamics.com"

# Create a Confidential Client Application
app = ConfidentialClientApplication(
    client_id,
    authority=f"https://login.microsoftonline.com/{tenant_id}",
    client_credential=client_secret
)

# Get OAuth token
token_response = app.acquire_token_for_client(scopes=[resource_url + "/.default"])
token = token_response['access_token']

# Example: Retrieve Account records
api_url = f"{resource_url}/api/data/v9.0/accounts"
headers = {
    'Authorization': f'Bearer {token}',
    'Content-Type': 'application/json',
    'OData-MaxVersion': '4.0',
    'OData-Version': '4.0'
}
response = requests.get(api_url, headers=headers)

# Display retrieved data
if response.status_code == 200:
    accounts = response.json().get('value')
    for account in accounts:
        print(f"Account Name: {account['name']}")
else:
    print(f"Error: {response.status_code} - {response.text}")
