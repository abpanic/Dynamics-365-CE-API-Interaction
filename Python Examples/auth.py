//Example 1: Authentication and Retrieving Data
import requests

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
