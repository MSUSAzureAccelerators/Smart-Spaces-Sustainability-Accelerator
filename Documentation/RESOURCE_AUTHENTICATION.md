Architecture of how resources authenticate with each other.

1. System Assigned Managed Identity
    - The ADF is created as the first resource so that its system assigned managed identity can be granted to most other resources on creation, by assigning contributor access to the ADF SAI

2. User Assigned Managed Identity
    - The User Assigned Managed Identity is created for two resources that do not have support for system assigned managed idenities.
    - The first is the deploy scripts task.  It does not allow the ADF SAI to access azure CLI and deploy the scripts to blob storage so a user assigned managed idenity is created and assigned the role to deploy files to blob
    - The second is for accessing a specific batch pool within a batch account.
        - The ADF SAI grants access to the batch account, but a pool cannot give access to a SAI so a user assigned managed identity is needed.
