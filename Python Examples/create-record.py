//Example 2: Creating a New Record
import requests
import json

# Authentication details (replace with your own)
client_id = "your_client_id"
client_secret = "your_client_secret"
tenant_id = "your_tenant_id"
resource_url = "https://yourorganization.crm.dynamics.com"

# Get OAuth token
token_url = f"https://login.microsoftonline.com/{tenant_id}/oauth2/token"
token_data = {
    'grant_type': 'client_credentials',
    'client_id': client_id,
    'client_secret': client_secret,
    'resource': resource_url
}
token_response = requests.post(token_url, data=token_data)
token = token_response.json().get('access_token')

# Example: Create a new Account record
api_url = f"{resource_url}/api/data/v9.0/accounts"
headers = {
    'Authorization': f'Bearer {token}',
    'Content-Type': 'application/json',
    'OData-MaxVersion': '4.0',
    'OData-Version': '4.0'
}

# New Account data
new_account = {
    "name": "New Account via API",
    "description": "Created using Python"
}

# Create the record
response = requests.post(api_url, headers=headers, data=json.dumps(new_account))

# Display result
if response.status_code == 204:
    print("New Account created successfully.")
else:
    print(f"Error: {response.status_code} - {response.text}")
