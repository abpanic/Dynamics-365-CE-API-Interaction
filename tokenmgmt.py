from msal import ConfidentialClientApplication
import requests
import json
import time

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

# Function to check if the token is expired
def is_token_expired(token_info):
    return 'exp' in token_info and token_info['exp'] <= time.time()

# Function to refresh the token
def refresh_token(app, token_info):
    result = app.acquire_token_for_client(scopes=[resource_url + "/.default"], force_refresh=True)
    return result['access_token']

# Get initial OAuth token
token_response = app.acquire_token_for_client(scopes=[resource_url + "/.default"])
token_info = token_response['id_token_claims']

# Example: Retrieve Account records
api_url = f"{resource_url}/api/data/v9.0/accounts"
headers = {
    'Authorization': f'Bearer {token_response["access_token"]}',
    'Content-Type': 'application/json',
    'OData-MaxVersion': '4.0',
    'OData-Version': '4.0'
}
response = requests.get(api_url, headers=headers)

# Check if token is expired and refresh if necessary
if is_token_expired(token_info):
    print("Token expired. Refreshing...")
    new_token = refresh_token(app, token_info)
    headers['Authorization'] = f'Bearer {new_token}'
    print("Token refreshed.")

# Display retrieved data
if response.status_code == 200:
    accounts = response.json().get('value')
    for account in accounts:
        print(f"Account Name: {account['name']}")
else:
    print(f"Error: {response.status_code} - {response.text}")
