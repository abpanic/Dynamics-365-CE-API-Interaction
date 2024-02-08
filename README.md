# Dynamics 365 CE API Interaction

This repository contains code examples in Python and C# for interacting with the Dynamics 365 Customer Engagement (CE) web API. The examples cover authentication, retrieving data, and managing token lifespan.

To learn/know about authentication/session management and authorization in general: Authentication.pptx

For Dynamics CE specific action: 

## Table of Contents

- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
  - [Python Examples](#python-examples)
  - [C# Examples](#c-examples)
- [Authentication](#authentication)
- [Token Lifespan Management](#token-lifespan-management)
- [Usage](#usage)
- [Contributing](#contributing)
- [License](#license)

## Prerequisites

- [Python](https://www.python.org/) (for Python examples)
- [.NET SDK](https://dotnet.microsoft.com/download) (for C# examples)
- [Visual Studio Code](https://code.visualstudio.com/) or any preferred IDE

## Getting Started

Clone the repository:

```bash
git clone https://github.com/abpanic/dynamics-365-api-interaction.git
```

### Python Examples
1. Navigate to the python-examples directory.
2. Open the files using a text editor or an IDE of your choice.
3. Replace placeholder values in the code with your Dynamics 365 CE authentication details.
4. Run the Python scripts.

### C# Examples
1. Open the solution file (Dynamics365ApiInteraction.sln) in the csharp-examples directory using Visual Studio or any preferred IDE.
2. Replace placeholder values in the code with your Dynamics 365 CE authentication details.
3. Build and run the solution.

## Authentication
Authentication is performed using OAuth 2.0 client credentials flow. Ensure you have the necessary client ID, client secret, tenant ID, and resource URL.

## Token Lifespan Management
The code includes functions to check if the token is expired and refresh it if necessary.

## Usage
These examples showcase basic interaction with the Dynamics 365 CE web API. Customize the code based on your specific requirements and use case.

## Contributing
Contributions are welcome! Feel free to open issues, submit pull requests, or provide feedback.

## License
This project is licensed under the MIT License - see the LICENSE file for details.